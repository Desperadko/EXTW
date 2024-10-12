namespace EXTW.Components
{
    public class INodeTable(SuperBlock sb, FileStream fs, BinaryReader br, BinaryWriter bw)
    {
        public int Length { get; } = sb.INODE_QUANTITY;

        private readonly short INODE_SIZE = INode.GetSize();
        private readonly FileStream fs = fs;
        private readonly BinaryReader br = br;
        private readonly BinaryWriter bw = bw;

        public INode this[int index]
        {
            get
            {
                if(index < -1 || index > Length)
                    throw new IndexOutOfRangeException();

                fs.Seek(sizeof(short) * 5 + sizeof(int) * 4, SeekOrigin.Begin);
                short position = br.ReadInt16();

                fs.Seek(position + index * INODE_SIZE, SeekOrigin.Begin);

                return new INode
                    (
                    (FileType)br.ReadByte(),
                    br.ReadInt32(),
                    br.ReadInt32()
                    );
            }
            set
            {
                if (index < 0 || index > Length)
                    throw new IndexOutOfRangeException();

                fs.Seek(sizeof(short) * 5 + sizeof(int) * 4, SeekOrigin.Begin);
                short position = br.ReadInt16();

                fs.Seek(position + index * INODE_SIZE, SeekOrigin.Begin);

                bw.Write(value.FileType);
                bw.Write(value.FileSize);
                bw.Write(value.FileBlockIndex);
            }
        }

        public short GetSize() => (short)(INODE_SIZE * Length);
    }

    public class INode(FileType fileType, int fileSize, int fileBlockIndex)
    {
        public byte FileType { get; } = (byte)fileType;
        public int FileSize { get; } = fileSize;
        public int FileBlockIndex { get; } = fileBlockIndex;

        public static short GetSize() => sizeof(byte) + sizeof(int) + sizeof(int);
    }
}
