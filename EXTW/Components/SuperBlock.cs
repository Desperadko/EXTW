namespace EXTW.Components
{
    public class SuperBlock
    {
        public short BLOCK_SIZE { get; }
        public int BLOCK_QUANTITY { get; }
        public short INODE_QUANTITY { get; }
        public int FREE_BLOCKS { get; private set; }
        public short FREE_INODES { get; private set; }
        public int FSYS_TOTAL_STORAGE { get; }
        public int FSYS_AVAILABLE_STORAGE { get; private set; }
        public short INODE_BITMAP_POSITION { get; private set; }
        public short DATABLOCK_BITMAP_POSITION { get; private set; }
        public short INODE_TABLE_POSITION { get; private set; }
        public short FAT_POSITION { get; private set; }
        public short ROOT_DIR_POS { get; private set; }

        private readonly FileStream fs;
        private readonly BinaryReader br;
        private readonly BinaryWriter bw;

        public SuperBlock(FileStream fs, BinaryReader br, BinaryWriter bw, short blockSize, int blockQuantity, short inodeQuantity)
        {
            BLOCK_SIZE = blockSize;
            BLOCK_QUANTITY = blockQuantity;
            INODE_QUANTITY = inodeQuantity;

            FREE_BLOCKS = BLOCK_QUANTITY;
            FREE_INODES = INODE_QUANTITY;

            FSYS_TOTAL_STORAGE = BLOCK_QUANTITY * BLOCK_SIZE;
            FSYS_AVAILABLE_STORAGE = FSYS_TOTAL_STORAGE;

            this.fs = fs;
            this.br = br;
            this.bw = bw;
        }
        public SuperBlock(FileStream fs, BinaryReader br, BinaryWriter bw)
        {
            BLOCK_SIZE = br.ReadInt16();
            BLOCK_QUANTITY = br.ReadInt32();
            INODE_QUANTITY = br.ReadInt16();

            FREE_BLOCKS = br.ReadInt32();
            FREE_INODES = br.ReadInt16();

            FSYS_TOTAL_STORAGE = BLOCK_QUANTITY * BLOCK_SIZE;
            FSYS_AVAILABLE_STORAGE = FREE_BLOCKS * BLOCK_SIZE;

            this.fs = fs;
            this.br = br;
            this.bw = bw;
        }

        public void AdjustPositions(EXTW fsys)
        {
            INODE_BITMAP_POSITION = GetSize();
            DATABLOCK_BITMAP_POSITION = (short)(INODE_BITMAP_POSITION + fsys.INodeBitmap.GetSize());
            INODE_TABLE_POSITION = (short)(DATABLOCK_BITMAP_POSITION + fsys.DataBlockBitmap.GetSize());
            FAT_POSITION = (short)(INODE_TABLE_POSITION + fsys.INodeTable.GetSize());
            ROOT_DIR_POS = (short)(FAT_POSITION + fsys.FAT.GetSize());
        }
        public void Write()
        {
            fs.Seek(0, SeekOrigin.Begin);

            bw.Write(BLOCK_SIZE);
            bw.Write(BLOCK_QUANTITY);
            bw.Write(INODE_QUANTITY);
            bw.Write(FREE_BLOCKS);
            bw.Write(FREE_INODES);
            bw.Write(FSYS_TOTAL_STORAGE);
            bw.Write(FSYS_AVAILABLE_STORAGE);
            bw.Write(INODE_BITMAP_POSITION);
            bw.Write(DATABLOCK_BITMAP_POSITION);
            bw.Write(INODE_TABLE_POSITION);
            bw.Write(FAT_POSITION);
            bw.Write(ROOT_DIR_POS);
        }
        public void Update(int blocks, short inodes, UpdateOperation updateOperation)
        {
            switch(updateOperation)
            {
                case UpdateOperation.Add:
                    FREE_BLOCKS += blocks;
                    FREE_INODES += inodes;
                    break;
                case UpdateOperation.Subtract:
                    FREE_BLOCKS -= blocks;
                    FREE_INODES -= inodes;
                    break;
            }

            FSYS_AVAILABLE_STORAGE = FREE_BLOCKS * BLOCK_SIZE;

            fs.Seek(sizeof(ushort) * 2 + sizeof(uint), SeekOrigin.Begin);
            bw.Write(FREE_BLOCKS);

            bw.Write(FREE_INODES);

            fs.Seek(sizeof(uint), SeekOrigin.Current);
            bw.Write(FSYS_AVAILABLE_STORAGE);
        }
        public void DisplayMetaData()
        {
            fs.Position = 0;
            short bs = br.ReadInt16();
            Console.WriteLine($"Block size: {bs}");
            int bq = br.ReadInt32();
            Console.WriteLine($"Block quantity: {bq}");
            short iq = br.ReadInt16();
            Console.WriteLine($"INode quantity: {iq}");
            int fb = br.ReadInt32();
            Console.WriteLine($"Free blocks: {fb}");
            short fi = br.ReadInt16();
            Console.WriteLine($"Free INodes: {fi}");
            int ts = br.ReadInt32();
            Console.WriteLine($"Total storage: {ts}");
            int aS = br.ReadInt32();
            Console.WriteLine($"Available storage: {aS}");
        }
        public static short GetSize() => sizeof(short) * 8 + sizeof(int) * 4;
    }
}
