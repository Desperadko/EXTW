namespace EXTW.Components
{
    public class DirFile
    {
        public int Length { get; }
        public int[] Blocks { get; private set; }

        private int blocksNum;

        private readonly short FILENAME_SIZE = 30;
        private readonly short DIRENTRY_SIZE = DirEntry.GetSize();
        private readonly int BLOCK_MAX_ENTRIES;

        private readonly FileStream fs;
        private readonly BinaryReader br;
        private readonly BinaryWriter bw;

        public DirFile(FileStream fs, BinaryReader br, BinaryWriter bw, int block)
        {
            this.fs = fs;
            this.br = br;
            this.bw = bw;

            fs.Position = 0;
            short blockSize = br.ReadInt16();

            Length = blockSize / DIRENTRY_SIZE;
            Blocks = [block];
            BLOCK_MAX_ENTRIES = blockSize / DIRENTRY_SIZE;
        }
        public DirFile(FileStream fs, BinaryReader br, BinaryWriter bw, int[] blocks)
        {
            this.fs = fs;
            this.br = br;
            this.bw = bw;

            fs.Position = 0;
            short blockSize = br.ReadInt16();

            Length = blocks.Length * blockSize / DIRENTRY_SIZE;
            Blocks = blocks;
            BLOCK_MAX_ENTRIES = blockSize / DIRENTRY_SIZE;
        }

        public DirEntry this[int index]
        {
            get
            {
                if (index < 0 || index > Length)
                    throw new IndexOutOfRangeException();

                blocksNum = 0;
                int actualIndex = GetBlockIndex(index);

                fs.Position = 0;
                short blockSize = br.ReadInt16();

                fs.Seek(sizeof(short) * 7 + sizeof(int) * 4, SeekOrigin.Begin);
                short rootPos = br.ReadInt16();

                fs.Seek(rootPos + Blocks[blocksNum] * blockSize + actualIndex * DIRENTRY_SIZE, SeekOrigin.Begin);

                return new DirEntry
                    (
                    CharsToString(br.ReadChars(FILENAME_SIZE)),
                    br.ReadInt16()
                    );
            }
            set
            {
                if (index < 0 || index > Length)
                    throw new IndexOutOfRangeException();

                blocksNum = 0;
                int actualIndex = GetBlockIndex(index);

                fs.Position = 0;
                short blockSize = br.ReadInt16();

                fs.Seek(sizeof(short) * 7 + sizeof(int) * 4, SeekOrigin.Begin);
                short rootPos = br.ReadInt16();

                fs.Seek(rootPos + Blocks[blocksNum] * blockSize + actualIndex * DIRENTRY_SIZE, SeekOrigin.Begin);

                bw.Write(StringToChars(value.FileName, FILENAME_SIZE));
                bw.Write(value.INodeIndex);
            }
        }

        public void SetupDirFile(short inodeIndex, short parentINodeIndex)
        {
            this[0] = new DirEntry(".", inodeIndex);
            this[1] = new DirEntry("..", parentINodeIndex);
        }
        public int GetFreeIndex()
        {
            for (int i = 0; i < Length; i++)
                if (this[i].FileName == "")
                    return i;

            throw new Exception("Critical error, wtf did you do to make it (get free index method-a)");
        }
        public bool ContainsFile(string fileName)
        {
            for (int i = 2; i < Length; i++)
                if (this[i].FileName == fileName)
                    return true;

            return false;
        }
        public bool HasSpace()
        {
            for (int i = 2; i < Length; i++)
                if (this[i].FileName == "")
                    return true;

            return false;
        }
        private int GetBlockIndex(int index)
        {
            if (index >= BLOCK_MAX_ENTRIES)
            {
                blocksNum++;
                GetBlockIndex(index -= BLOCK_MAX_ENTRIES);
            }
            return index;
        }

        private string CharsToString(char[] chars)
        {
            int c = 0;

            for (; c < chars.Length && chars[c] != 0; c++) ;

            return new string(chars, 0, c);
        }
        private char[] StringToChars(string str, int length)
        {
            var chars = new char[length];
            var info = str.ToCharArray();

            for (int c = 0; c < Math.Min(length, info.Length); c++)
                chars[c] = info[c];

            return chars;
        }
    }

    public class DirEntry(string fileName, short inodeIndex)
    {
        public string FileName { get; private set; } = fileName;
        public short INodeIndex { get; private set; } = inodeIndex;

        public static short GetSize() => 30 + sizeof(short);
    }
}
