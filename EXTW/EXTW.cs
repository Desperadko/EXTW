using EXTW.Components;
using System.Text;

namespace EXTW
{
    public class EXTW
    {
        public SuperBlock SuperBlock { get; }
        public Bitmap INodeBitmap { get; }
        public Bitmap DataBlockBitmap { get; }
        public INodeTable INodeTable { get; }
        public FAT FAT { get; }

        public FileStream Fs { get; }
        public BinaryReader Br { get; }
        public BinaryWriter Bw { get; }

        public string CurrentFilePath { get; private set; }

        public EXTW(string fsysPath, short blockSize, int blockQuantity, short inodeQuantity)
        {
            if (!File.Exists(fsysPath))
            {
                Fs = new(fsysPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                Br = new(Fs, Encoding.ASCII);
                Bw = new(Fs, Encoding.ASCII);

                SuperBlock = new(Fs, Br, Bw, blockSize, blockQuantity, inodeQuantity);
                INodeBitmap = new(SuperBlock, Fs, Br, Bw, BitmapDesignation.INodeTable);
                DataBlockBitmap = new(SuperBlock, Fs, Br, Bw, BitmapDesignation.DataBlocks);
                INodeTable = new(SuperBlock, Fs, Br, Bw);
                FAT = new(SuperBlock, Fs, Br, Bw);

                SuperBlock.AdjustPositions(this);

                SuperBlock.Write();

                Fs.Seek(sizeof(short) * 7 + sizeof(int) * 4, SeekOrigin.Begin);
                short rootPos = Br.ReadInt16();

                Fs.SetLength(rootPos + blockQuantity * blockSize);

                int blockIndex = DataBlockBitmap.GetFreeBit();
                int inodeIndex = INodeBitmap.GetFreeBit();

                INodeTable[inodeIndex] = new INode(FileType.DirFile, blockSize, blockIndex);

                DirFile Root = new(Fs, Br, Bw, blockIndex);

                Root.SetupDirFile((short)inodeIndex, -1);

                FAT.WriteFAT(blockIndex);

                SuperBlock.Update(1, 1, UpdateOperation.Subtract);

            }
            else
            {
                Fs = new(fsysPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                Br = new(Fs, Encoding.ASCII);
                Bw = new(Fs, Encoding.ASCII);

                SuperBlock = new(Fs, Br, Bw);
                INodeBitmap = new(SuperBlock, Fs, Br, Bw, BitmapDesignation.INodeTable);
                DataBlockBitmap = new(SuperBlock, Fs, Br, Bw, BitmapDesignation.DataBlocks);
                INodeTable = new(SuperBlock, Fs, Br, Bw);
                FAT = new(SuperBlock, Fs, Br, Bw);

                SuperBlock.AdjustPositions(this);

                Fs.Seek(sizeof(short) * 7 + sizeof(int) * 4, SeekOrigin.Begin);
                short rootPos = Br.ReadInt16();

                Fs.SetLength(rootPos + blockQuantity * blockSize);
            }

            CurrentFilePath = "Root";
        }

        public void MakeDirectory(string input)
        {
            string filePath = SetUpFilePath(input);

            if (!IsFilePathValid(ExcludeLastFile(filePath)))
            {
                Console.WriteLine("Invalid filepath.");
                return;
            }

            DirFile parentDir = GetParentDir(filePath);

            string[] filePathArr = DissectFilePath(filePath);
            string dirName = filePathArr[^1];

            if (parentDir.ContainsFile(dirName))
            {
                Console.WriteLine("Directory already exists.");
                return;
            }

            if (!parentDir.HasSpace())
                parentDir = LenghtenDirectory(ExcludeLastFile(filePath));

            int blockIndex = DataBlockBitmap.GetFreeBit();
            int inodeIndex = INodeBitmap.GetFreeBit();

            parentDir[parentDir.GetFreeIndex()] = new DirEntry(dirName, (short)inodeIndex);

            Fs.Position = 0;
            short blockSize = Br.ReadInt16();
            INodeTable[inodeIndex] = new INode(FileType.DirFile, blockSize, blockIndex);

            FAT.WriteFAT(blockIndex);

            DirFile newDir = ReadDirBlock(blockIndex);
            newDir.SetupDirFile((short)inodeIndex, parentDir[0].INodeIndex);
            SuperBlock.Update(1, 1, UpdateOperation.Subtract);
        }
        public void RemoveDirectory(string input)
        {
            string filePath = SetUpFilePath(input);

            if (!IsFilePathValid(filePath))
            {
                Console.WriteLine("Invalid filepath.");
                return;
            }

            DirFile dir = GetDir(filePath);

            if (IsDirEmpty(dir))
            {
                DirFile parentDir = GetParentDir(filePath);

                string[] filePathArr = DissectFilePath(filePath);
                string dirName = filePathArr[^1];
                short inodeIndex = 0;
                int dirEntryIndex = 0;

                for (int i = 2; i < parentDir.Length; i++)
                    if (parentDir[i].FileName == dirName)
                    {
                        dirEntryIndex = i;
                        inodeIndex = parentDir[i].INodeIndex;
                        break;
                    }

                INodeBitmap.DeallocateBlock(inodeIndex);
                INodeTable[inodeIndex] = new INode(default, default, default);

                for (int i = 0; i < dir.Blocks.Length; i++)
                    DataBlockBitmap.DeallocateBlock(dir.Blocks[i]);

                FAT.ClearFAT(dir.Blocks[0]);

                parentDir[dirEntryIndex] = new DirEntry("", default);

                SuperBlock.Update(1, 1, UpdateOperation.Add);
            }
            else
            {
                Console.WriteLine("Directory needs to be emptied, before deleted.");
                return;
            }
        }
        public void ChangeDirectory(string input)
        {
            string filePath = SetUpFilePath(input);

            if (!IsFilePathValid(filePath))
            {
                Console.WriteLine("Invalid filepath.");
                return;
            }

            int block = GetFileBlockIndex(filePath);
            string[] filePathArr = DissectFilePath(filePath);

            switch (filePathArr[^1])
            {
                case ".":
                    return;
                case "..":
                    CurrentFilePath = ExcludeLastFile(CurrentFilePath);
                    break;
                default:
                    if (GetFileType(block) == FileType.DirFile)
                        CurrentFilePath = filePath;
                    else
                        return;
                    break;
            }
        }
        public void MyListDirectoryFiles()
        {
            DirFile currDir = GetDir(CurrentFilePath);

            Console.WriteLine("Files in current directory:");
            for (int i = 0; i < currDir.Length; i++)
                if (currDir[i].FileName != "")
                    Console.WriteLine($"{i + 1}. {currDir[i].FileName}");
        }
        public void MyListDirectoryFiles(string input)
        {
            string filePath = SetUpFilePath(input);

            if (!IsFilePathValid(filePath))
            {
                Console.WriteLine("Invalid filepath.");
                return;
            }

            DirFile currDir = GetDir(filePath);

            Console.WriteLine("Files in current directory:");
            for (int i = 0; i < currDir.Length; i++)
                if (currDir[i].FileName != "")
                    Console.WriteLine($"{i + 1}. {currDir[i].FileName}");
        }
        public void MakeFile(string input, string content)
        {
            string filePath = SetUpFilePath(input);

            if (!IsFilePathValid(ExcludeLastFile(filePath)))
            {
                Console.WriteLine("Invalid filepath.");
                return;
            }

            DirFile parentDir = GetParentDir(filePath);

            string[] filePathAsArr = DissectFilePath(filePath);
            string fileName = filePathAsArr[^1];

            if (!parentDir.ContainsFile(fileName))
            {
                if (!parentDir.HasSpace())
                    parentDir = LenghtenDirectory(ExcludeLastFile(filePath));

                byte[] data = Encoding.ASCII.GetBytes(content);

                Fs.Position = 0;
                short blockSize = Br.ReadInt16();

                int blocksRequired = (data.Length + (blockSize - 1)) / blockSize;

                int[] blocks = DataBlockBitmap.GetFreeBits(blocksRequired);
                int inodeIndex = INodeBitmap.GetFreeBit();

                parentDir[parentDir.GetFreeIndex()] = new DirEntry(fileName, (short)inodeIndex);

                INodeTable[inodeIndex] = new INode(FileType.DataFile, data.Length, blocks[0]);

                FAT.WriteFAT(blocks[0], blocks);

                int byteIndex = 0;
                for (int i = 0; i < blocks.Length; i++)
                {
                    byte[] buffer = new byte[blockSize];

                    for (int j = 0; j < buffer.Length; j++)
                    {
                        buffer[j] = data[byteIndex++];
                        if (byteIndex >= data.Length)
                            break;
                    }

                    WriteDataBlock(blocks[i], buffer);
                }

                SuperBlock.Update(blocks.Length, 1, UpdateOperation.Subtract);
            }
            else
            {
                int inodeIndex = -1;
                for (int i = 0; i < parentDir.Length; i++)
                    if (parentDir[i].FileName == fileName)
                    {
                        inodeIndex = parentDir[i].INodeIndex;
                        break;
                    }

                byte[] data = Encoding.ASCII.GetBytes(content);

                int blockIndex = INodeTable[inodeIndex].FileBlockIndex;

                INodeTable[inodeIndex] = new INode(FileType.DataFile, data.Length, blockIndex);

                int[] blocksToDealocate = FAT.ReadFAT(blockIndex);
                for (int i = 0; i < blocksToDealocate.Length; i++)
                    DataBlockBitmap.DeallocateBlock(blocksToDealocate[i]);

                SuperBlock.Update(blocksToDealocate.Length, 0, UpdateOperation.Add);

                Fs.Position = 0;
                short blockSize = Br.ReadInt16();

                int blocksRequired = (data.Length + (blockSize - 1)) / blockSize;
                int[] blocks = DataBlockBitmap.GetFreeBits(blocksRequired);

                FAT.ClearFAT(blockIndex);
                FAT.WriteFAT(blockIndex, blocks);

                int dataByteIndex = 0;
                WriteDataBlocks(data, dataByteIndex, blockSize, blocks);

                SuperBlock.Update(blocks.Length, 0, UpdateOperation.Subtract);
            }
        }

        private void WriteDataBlocks(byte[] data, int dataByteIndex, short blockSize, int[] blocks)
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                byte[] buffer = new byte[blockSize];

                for (int j = 0; j < buffer.Length; j++)
                {
                    buffer[j] = data[dataByteIndex++];
                    if (dataByteIndex >= data.Length)
                        break;
                }

                WriteDataBlock(blocks[i], buffer);
            }
        }

        public void AppendDataFile(string input, string content)
        {
            string filePath = SetUpFilePath(input);

            if (!IsFilePathValid(filePath))
            {
                Console.WriteLine("Filepath is not valid.");
                return;
            }

            DirFile parentDir = GetParentDir(filePath);

            string[] filePathAsArr = DissectFilePath(filePath);
            string fileName = filePathAsArr[^1];

            int inodeIndex = -1;

            for (int i = 0; i < parentDir.Length; i++)
                if (parentDir[i].FileName == fileName)
                {
                    inodeIndex = parentDir[i].INodeIndex;
                    break;
                }

            if (INodeTable[inodeIndex].FileType != (byte)FileType.DataFile)
            {
                Console.WriteLine("The given file is a directory, it's not eligible of appending such data.");
                return;
            }

            byte[] data = Encoding.ASCII.GetBytes(content);

            int oldSize = INodeTable[inodeIndex].FileSize;
            int blockIndex = INodeTable[inodeIndex].FileBlockIndex;

            INodeTable[inodeIndex] = new INode(FileType.DataFile, oldSize + data.Length, blockIndex);

            int[] blocks = FAT.ReadFAT(blockIndex);
            byte[] lastBlock = ReadDataBlock(blocks[^1]);
            int dataByteIndex = 0;

            for (int i = 0; i < lastBlock.Length; i++)
                if (lastBlock[i] == 0)
                {
                    lastBlock[i] = data[dataByteIndex++];
                    if (dataByteIndex >= data.Length)
                        break;
                }

            WriteDataBlock(blocks[^1], lastBlock);

            if (dataByteIndex < data.Length)
            {
                int dataNewSize = data.Length - dataByteIndex;

                Fs.Position = 0;
                short blockSize = Br.ReadInt16();

                int blocksRequired = (dataNewSize + (blockSize - 1)) / blockSize;
                int[] newBlocks = DataBlockBitmap.GetFreeBits(blocksRequired);

                FAT.WriteFAT(blocks[^1], newBlocks);

                WriteDataBlocks(data, dataByteIndex, blockSize, newBlocks);

                //for (int i = 0; i < newBlocks.Length; i++)
                //{
                //    byte[] buffer = new byte[blockSize];

                //    for (int j = 0; j < buffer.Length; j++)
                //    {
                //        buffer[j] = data[byteIndex++];
                //        if (byteIndex >= data.Length)
                //            break;
                //    }

                //    WriteDataBlock(newBlocks[i], buffer);
                //}

                SuperBlock.Update(newBlocks.Length, 0, UpdateOperation.Subtract);
            }
        }
        public void ShowFileContent(string input)
        {
            string filePath = SetUpFilePath(input);

            if (!IsFilePathValid(filePath))
            {
                Console.WriteLine("Invalid filepath.");
                return;
            }

            if (GetFileType(filePath) == FileType.DirFile)
            {
                MyListDirectoryFiles(filePath);
                return;
            }

            int block = GetFileBlockIndex(filePath);
            int[] blocks = FAT.ReadFAT(block);

            for (int i = 0; i < blocks.Length; i++)
            {
                byte[] data = ReadDataBlock(blocks[i]);

                int length = 0;
                for (int j = 0; j < data.Length; j++)
                    if (data[j] == (byte)'\0')
                    {
                        length = j;
                        break;
                    }

                string content = Encoding.ASCII.GetString(data, 0, length);

                Console.WriteLine(content);
            }
        }
        public int CopyFileTo(string input, string destination)
        {
            string filePath = SetUpFilePath(input);
            string destinationFilePath = SetUpFilePath(destination);

            if (!IsFilePathValid(filePath))
            {
                Console.WriteLine("Invalid filepath of original file.");
                return -1;
            }
            else
            if (!IsFilePathValid(destinationFilePath))
            {
                Console.WriteLine("Invalid destination filepath.");
                return -1;
            }

            FileType fileType = GetFileType(filePath);

            if (filePath.Length <= destinationFilePath.Length)
            {
                if (ContainsFilePath(destinationFilePath, filePath) && fileType == FileType.DirFile)
                {
                    Console.WriteLine("The destination directory is a subdirectory of the selected directory to copy.");
                    return -1;
                }
            }

            DirFile destinationDir = GetDir(destinationFilePath);
            string[] filePathAsArr = DissectFilePath(filePath);

            if (destinationDir.ContainsFile(filePathAsArr[^1]))
                filePathAsArr[^1] += " - Copy";

            string filePathForCopiedFile = AddToFilePath(destinationFilePath, filePathAsArr[^1]);

            if (fileType == FileType.DataFile)
            {
                int block = GetFileBlockIndex(filePath);
                int[] blocks = FAT.ReadFAT(block);

                string content = "";
                StringBuilder sb = new(content);

                for (int i = 0; i < blocks.Length; i++)
                {
                    byte[] data = ReadDataBlock(blocks[i]);

                    int length = 0;
                    for (int j = 0; j < data.Length; j++)
                        if (data[j] == (byte)'\0')
                        {
                            length = j;
                            break;
                        }

                    string temp = Encoding.ASCII.GetString(data, 0, length);
                    sb.Append(temp);
                }

                content = sb.ToString();

                MakeFile(filePathForCopiedFile, content);

                return GetFileINodeIndex(filePathForCopiedFile);
            }
            else
            {
                MakeDirectory(filePathForCopiedFile);

                DirFile originalDir = GetDir(filePath);
                DirFile copiedDir = GetDir(filePathForCopiedFile);

                for (int i = 2; i < originalDir.Length; i++)
                {
                    if (originalDir[i].FileName != "")
                    {
                        string filePathOfDirEntry = AddToFilePath(filePath, originalDir[i].FileName);

                        int inodeIndex = CopyFileTo(filePathOfDirEntry, filePathForCopiedFile);

                        copiedDir[i] = new DirEntry(originalDir[i].FileName, (short)inodeIndex);
                    }
                }

                return GetFileINodeIndex(filePathForCopiedFile);
            }
        }
        public void RemoveFile(string input)
        {
            string filePath = SetUpFilePath(input);

            if (!IsFilePathValid(filePath))
            {
                Console.WriteLine("Invalid filepath.");
                return;
            }

            if (GetFileType(filePath) == FileType.DirFile)
            {
                RemoveDirectory(filePath);
                return;
            }

            DirFile parentDir = GetParentDir(filePath);

            string[] filePathArr = DissectFilePath(filePath);
            string fileName = filePathArr[^1];
            short inodeIndex = 0;
            int dirEntryIndex = 0;
            int block = GetFileBlockIndex(filePath);
            int[] blocks = FAT.ReadFAT(block);

            for (int i = 2; i < parentDir.Length; i++)
                if (parentDir[i].FileName == fileName)
                {
                    dirEntryIndex = i;
                    inodeIndex = parentDir[i].INodeIndex;
                    break;
                }

            INodeBitmap.DeallocateBlock(inodeIndex);
            INodeTable[inodeIndex] = new INode(default, default, default);

            for (int i = 0; i < blocks.Length; i++)
                DataBlockBitmap.DeallocateBlock(blocks[i]);

            FAT.ClearFAT(blocks[0]);

            parentDir[dirEntryIndex] = new DirEntry("", default);

            SuperBlock.Update(1, 1, UpdateOperation.Add);
        }
        public void ImportFile(string source, string destination)
        {
            string destinationFilePath = SetUpFilePath(destination);

            if (!File.Exists(source))
            {
                Console.WriteLine("Invalid source path.");
                return;
            }

            if (!IsFilePathValid(destinationFilePath))
            {
                Console.WriteLine("Invalid destination path.");
                return;
            }

            byte[] data = File.ReadAllBytes(source);
            string content = Encoding.ASCII.GetString(data);

            string[] sourceDissected = DissectFilePath(source);
            RemoveImportedFileExtension(ref sourceDissected[^1]);
            string destinationFilePathWithFileName = AddToFilePath(destinationFilePath, sourceDissected[^1]);

            MakeFile(destinationFilePathWithFileName, content);
        }
        public void AppendImportedFile(string source, string destination)
        {
            string destinationFilePath = SetUpFilePath(destination);

            if (!IsFilePathValid(destinationFilePath))
            {
                Console.WriteLine("Invalid destination path.");
                return;
            }

            if (!File.Exists(source))
            {
                Console.WriteLine("Invalid source path.");
                return;
            }

            DirFile parentDir = GetParentDir(destinationFilePath);

            string[] filePathAsArr = DissectFilePath(destinationFilePath);
            string fileName = filePathAsArr[^1];

            int inodeIndex = -1;

            for (int i = 0; i < parentDir.Length; i++)
                if (parentDir[i].FileName == fileName)
                {
                    inodeIndex = parentDir[i].INodeIndex;
                    break;
                }

            if (INodeTable[inodeIndex].FileType != (byte)FileType.DataFile)
            {
                Console.WriteLine("Destination file is a directory, it's not eligible of appending such data.");
                return;
            }

            byte[] data = File.ReadAllBytes(source);

            int oldSize = INodeTable[inodeIndex].FileSize;
            int blockIndex = INodeTable[inodeIndex].FileBlockIndex;

            INodeTable[inodeIndex] = new INode(FileType.DataFile, oldSize + data.Length, blockIndex);

            int[] blocks = FAT.ReadFAT(blockIndex);
            byte[] lastBlock = ReadDataBlock(blocks[^1]);
            int dataIndex = 0;

            for (int i = 0; i < lastBlock.Length; i++)
                if (lastBlock[i] == 0)
                {
                    lastBlock[i] = data[dataIndex++];
                    if (dataIndex >= data.Length)
                        break;
                }

            WriteDataBlock(blocks[^1], lastBlock);

            if (dataIndex < data.Length)
            {
                int dataNewSize = data.Length - dataIndex;

                Fs.Position = 0;
                short blockSize = Br.ReadInt16();

                int blocksRequired = (dataNewSize + (blockSize - 1)) / blockSize;
                int[] newBlocks = DataBlockBitmap.GetFreeBits(blocksRequired);

                FAT.WriteFAT(blocks[^1], newBlocks);

                for (int i = 0; i < newBlocks.Length; i++)
                {
                    byte[] buffer = new byte[blockSize];

                    for (int j = 0; j < buffer.Length; j++)
                    {
                        buffer[j] = data[dataIndex++];
                        if (dataIndex >= data.Length)
                            break;
                    }

                    WriteDataBlock(newBlocks[i], buffer);
                }

                SuperBlock.Update(newBlocks.Length, 0, UpdateOperation.Subtract);
            }
        }
        public void ExportFile(string source, string destination)
        {
            string sourceFilePath = SetUpFilePath(source);

            if (!IsFilePathValid(sourceFilePath))
            {
                Console.WriteLine("Invalid source filepath.");
                return;
            }

            if (!Directory.Exists(destination))
            {
                Console.WriteLine("Invalid destination filepath.");
                return;
            }

            string[] sourceFilePathArr = DissectFilePath(sourceFilePath);
            string fileName = sourceFilePathArr[^1];
            destination += $@"\{fileName}";

            if (GetFileType(sourceFilePath) == FileType.DataFile)
            {
                int block = GetFileBlockIndex(sourceFilePath);
                int[] blocks = FAT.ReadFAT(block);

                Fs.Position = 0;
                short blockSize = Br.ReadInt16();

                byte[] data = new byte[blocks.Length * blockSize];
                int dataIndex = 0;

                for (int i = 0; i < blocks.Length; i++)
                {
                    byte[] buffer = ReadDataBlock(blocks[i]);

                    for (int j = 0; j < buffer.Length; j++)
                    {
                        if (buffer[j] == (byte)'\0')
                            break;

                        data[dataIndex++] = buffer[j];
                    }
                }

                using FileStream fs = new(destination, FileMode.Create, FileAccess.Write, FileShare.Write);
                using BinaryWriter bw = new(fs, Encoding.ASCII);
                bw.Write(data);
            }
            else
            {
                DirFile currDir = GetDir(sourceFilePath);

                Directory.CreateDirectory(destination);

                for (int i = 2; i < currDir.Length; i++)
                {
                    if (currDir[i].FileName != "")
                    {
                        string filePathOfDirEntry = AddToFilePath(sourceFilePath, currDir[i].FileName);

                        ExportFile(filePathOfDirEntry, destination);
                    }
                }
            }
        }

        private byte[] ReadDataBlock(int index)
        {
            Fs.Position = 0;
            short blockSize = Br.ReadInt16();

            byte[] blockData = new byte[blockSize];

            Fs.Seek(SuperBlock.ROOT_DIR_POS + index * blockSize, SeekOrigin.Begin);
            Br.Read(blockData, 0, blockSize);

            return blockData;
        }
        private void WriteDataBlock(int index, byte[] data)
        {
            Fs.Position = 0;
            short blockSize = Br.ReadInt16();

            Fs.Seek(sizeof(short) * 7 + sizeof(int) * 4, SeekOrigin.Begin);
            short rootPos = Br.ReadInt16();

            Fs.Seek(rootPos + index * blockSize, SeekOrigin.Begin);
            Bw.Write(data);
        }
        private FileType GetFileType(string filePath)
        {
            int index = GetFileBlockIndex(filePath);
            return GetFileType(index);
        }
        private FileType GetFileType(int index)
        {
            byte[] data = ReadDataBlock(index);

            int length = 0;
            for (int i = 0; i < data.Length; i++)
                if (data[i] == (byte)'\0')
                {
                    length = i;
                    break;
                }

            string file = Encoding.ASCII.GetString(data, 0, length);

            if (file[0] == '.' && length == 1)
                return FileType.DirFile;
            else
                return FileType.DataFile;
        }
        private bool IsDirEmpty(DirFile dir)
        {
            for (int i = 2; i < dir.Length; i++)
                if (dir[i].FileName != "")
                    return false;

            return true;
        }
        private DirFile ReadDirBlock(int block) => new(Fs, Br, Bw, block);
        private DirFile ReadDirBlocks(int[] blocks) => new(Fs, Br, Bw, blocks);
        private DirFile GetDir(string filePath)
        {
            int index = GetFileBlockIndex(filePath);
            int[] blocks = FAT.ReadFAT(index);
            return ReadDirBlocks(blocks);
        }
        private DirFile GetDir(int index)
        {
            int[] blocks = FAT.ReadFAT(index);
            return ReadDirBlocks(blocks);
        }
        private DirFile GetParentDir(string filePath)
        {
            string parentFilePath = ExcludeLastFile(filePath);
            return GetDir(parentFilePath);
        }
        private DirFile LenghtenDirectory(string filePath)
        {
            DirFile dir = GetDir(filePath);

            int inodeIndex = GetFileINodeIndex(filePath);
            int block = INodeTable[inodeIndex].FileBlockIndex;

            FAT.WriteFAT(dir.Blocks[^1], DataBlockBitmap.GetFreeBit());

            int[] blocks = FAT.ReadFAT(block);

            Fs.Position = 0;
            short blockSize = Br.ReadInt16();

            INodeTable[inodeIndex] = new(FileType.DirFile, blockSize * blocks.Length, block);

            return ReadDirBlocks(blocks);
        }
        private int GetFileBlockIndex(string filePath)
        {
            string[] dirArr = DissectFilePath(filePath);
            int dirArrIndex = 1;

            DirFile currDir = GetDir(0);

            while (dirArrIndex < dirArr.Length)
                for (int i = 0; i < currDir.Length; i++)
                    if (currDir[i].FileName == dirArr[dirArrIndex])
                    {
                        int inodeIndex = currDir[i].INodeIndex;
                        int dirBlockIndex = INodeTable[inodeIndex].FileBlockIndex;
                        int[] blocks = FAT.ReadFAT(dirBlockIndex);
                        currDir = ReadDirBlocks(blocks);
                        dirArrIndex++;
                        break;
                    }

            return currDir.Blocks[0];
        }
        private int GetFileINodeIndex(string filePath)
        {
            DirFile parentDir = GetParentDir(filePath);

            string[] filePathAsArr = DissectFilePath(filePath);
            string fileName = filePathAsArr[^1];

            for (int i = 0; i < parentDir.Length; i++)
                if (parentDir[i].FileName == fileName)
                    return parentDir[i].INodeIndex;

            return -1;
        }
        public static string[] DissectCommand(string command)
        {
            string result = "";

            int wordsCounter = 0;

            for (int i = 0; i < command.Length; i++)
                if (command[i] == '"')
                {
                    wordsCounter++;

                    for (int j = i; j < command.Length; j++)
                        if (command[j] == '"' && j != i || j + 1 >= command.Length)
                        {
                            i = j;
                            break;
                        }

                    continue;
                }
                else if (command[i] != ' ')
                {
                    wordsCounter++;

                    for (int j = i; j < command.Length; j++)
                        if (command[j] == ' ' || j + 1 >= command.Length)
                        {
                            i = j;
                            break;
                        }
                }

            string[] commandDissected = new string[wordsCounter];
            int word = 0;

            while (word < commandDissected.Length)
            {
                for (int i = 0; i < command.Length; i++)
                {
                    if (command[i] == ' ')
                    {
                        if (result.Length > 0)
                        {
                            commandDissected[word++] = result;
                            result = "";
                        }
                    }
                    else if (command[i] == '"')
                    {
                        for (int j = i; j < command.Length; j++)
                        {
                            if (command[j] == '"' && i != j && result.Length > 0)
                            {
                                commandDissected[word++] = result;
                                result = "";
                                i = j;
                                break;
                            }

                            if (command[j] != '"')
                                result += command[j];

                            if (j + 1 >= command.Length && command[i] != '"' || j + 1 >= command.Length && command[i] == '"')
                            {
                                Console.WriteLine("Invalid use of <\" \"> for literal argument. ");
                                return null!;
                            }
                        }
                    }
                    else
                        result += command[i];
                }
                if (result.Length > 0)
                {
                    commandDissected[word++] = result;
                    result = "";
                }
            }

            return commandDissected;
        }
        private string[] DissectFilePath(string filePath)
        {
            string result = "";

            int filesCounter = 1;

            for (int i = 0; i < filePath.Length; i++)
                if (filePath[i] == '\\')
                    filesCounter++;

            string[] filePathDissected = new string[filesCounter];
            int file = 0;

            for (int i = 0; i < filePath.Length; i++)
            {
                if (filePath[i] == '\\')
                {
                    filePathDissected[file++] = result;
                    result = "";
                }
                else
                    result += filePath[i];
            }

            filePathDissected[file] = result;
            return filePathDissected;
        }
        private void RemoveImportedFileExtension(ref string fileName)
        {
            int dotIndex = 0;

            for (int i = fileName.Length - 1; i >= 0; i--)
                if (fileName[i] == '.')
                {
                    dotIndex = i;
                    break;
                }

            int charsToRemove = fileName.Length - dotIndex;

            RemoveFromString(ref fileName, dotIndex, charsToRemove);
        }
        private string AddToFilePath(string filePath, string fileName)
        {
            filePath += @$"\{fileName}";
            return filePath;
        }
        private string ExcludeLastFile(string filePath)
        {
            if (filePath == "Root")
                return filePath;

            int slashIndex = filePath.Length - 1;

            for (; slashIndex >= 0; slashIndex--)
                if (filePath[slashIndex] == '\\')
                    break;

            int charsToRemove = filePath.Length - slashIndex;

            RemoveFromString(ref filePath, slashIndex, charsToRemove);

            return filePath;
        }
        private bool ContainsFileSeparator(string input)
        {
            for (int i = 0; i < input.Length; i++)
                if (input[i] == '\\')
                    return true;

            return false;
        }
        private bool IsFilePathValid(string filePath)
        {
            bool IsValid = true;

            string[] dirArr = DissectFilePath(filePath);

            if (dirArr[0] == "Root")
            {
                int dirArrIndex = 1;

                int[] rootFileBlocks = FAT.ReadFAT(0);
                DirFile currDir = ReadDirBlocks(rootFileBlocks);

                while (dirArrIndex < dirArr.Length)
                {
                    IsValid = false;
                    for (int i = 0; i < currDir.Length; i++)
                        if (currDir[i].FileName == dirArr[dirArrIndex])
                        {
                            int inodeIndex = currDir[i].INodeIndex;
                            int dirBlockIndex = INodeTable[inodeIndex].FileBlockIndex;
                            int[] blocks = FAT.ReadFAT(dirBlockIndex);
                            currDir = ReadDirBlocks(blocks);
                            IsValid = true;
                            break;
                        }
                    dirArrIndex++;
                }
            }
            else
                return false;

            return IsValid;
        }
        private string SetUpFilePath(string input)
        {
            if (ContainsFileSeparator(input))
                return input;
            else
            {
                if (input == "Root")
                    return input;

                string temp = CurrentFilePath;
                temp += $@"\{input}";
                return temp;
            }
        }
        private bool ContainsFilePath(string filePath, string filePathToCheckFor)
        {
            if (filePathToCheckFor.Length > filePath.Length)
                return false;

            for (int i = 0; i < filePathToCheckFor.Length; i++)
                if (filePath[i] != filePathToCheckFor[i])
                    return false;

            return true;
        }
        private static void RemoveFromString(ref string str, int index, int length)
        {
            string temp = "";

            for (int i = 0; i < str.Length; i++)
            {
                if (i == index)
                    i += length;

                if (i < str.Length)
                    temp += str[i];
            }

            str = temp;
        }
    }
}
