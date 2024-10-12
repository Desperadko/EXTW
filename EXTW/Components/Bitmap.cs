namespace EXTW.Components
{
    public class Bitmap
    {
        public int Length { get; }

        private readonly int byteArrayLength;
        private readonly BitmapDesignation designation;

        private readonly FileStream fs;
        private readonly BinaryReader br;
        private readonly BinaryWriter bw;

        public Bitmap(SuperBlock sb, FileStream fs, BinaryReader br, BinaryWriter bw, BitmapDesignation designation)
        {
            Length = designation switch
            {
                BitmapDesignation.DataBlocks => sb.BLOCK_QUANTITY,
                BitmapDesignation.INodeTable => sb.INODE_QUANTITY,
                _ => 0
            };

            byteArrayLength = (Length + 7) / 8;

            this.fs = fs;
            this.br = br;
            this.bw = bw;

            this.designation = designation;
        }

        public void AllocateBlock(int index)
        {
            byte[] bitmap = ReadBitmap();

            int byteIndex = index / 8;
            int bitOffset = index % 8;

            bitmap[byteIndex] |= (byte)(1 << bitOffset);

            WriteBitmap(bitmap);
        }
        public void DeallocateBlock(int index)
        {
            byte[] bitmap = ReadBitmap();

            int byteIndex = index / 8;
            int bitOffset = index % 8;

            bitmap[byteIndex] &= (byte)~(1 << bitOffset);

            WriteBitmap(bitmap);
        }
        public bool IsBlockAllocated(int index)
        {
            byte[] bitmap = ReadBitmap();

            int byteIndex = index / 8;
            int bitOffset = index % 8;

            return (bitmap[byteIndex] & (1 << bitOffset)) != 0;
        }
        public int GetFreeBit()
        {
            for(int i = 0; i < Length; i++)
            {
                if(!IsBlockAllocated(i))
                {
                    AllocateBlock(i);
                    return i;
                }
            }

            throw new Exception("Not enough space, initiate defragmentation?");
        }
        public int[] GetFreeBits(int length)
        {
            int[] bits = new int[length];
            int bitsIndex = 0;

            for(int i = 0; i < Length; i++)
            {
                if(!IsBlockAllocated(i))
                {
                    AllocateBlock(i);
                    bits[bitsIndex++] = i;
                    if (bitsIndex >= bits.Length)
                        break;
                }
            }

            for (int i = 0; i < bits.Length; i++)
                if (bits[i] == 0)
                    throw new Exception("Not enough space, initiate defragmentation?");

            return bits;
        }

        private byte[] ReadBitmap()
        {
            byte[] bitmap = new byte[byteArrayLength];

            short position = 0;
            switch (designation)
            {
                case BitmapDesignation.INodeTable:
                    fs.Seek(sizeof(short) * 3 + sizeof(int) * 4, SeekOrigin.Begin);
                    position = br.ReadInt16();
                    break;
                case BitmapDesignation.DataBlocks:
                    fs.Seek(sizeof(short) * 4 + sizeof(int) * 4, SeekOrigin.Begin);
                    position = br.ReadInt16();
                    break;
            }

            fs.Position = position;
            br.Read(bitmap, 0, byteArrayLength);

            return bitmap;
        }
        private void WriteBitmap(byte[] bitmap)
        {
            short position = 0;
            switch (designation)
            {
                case BitmapDesignation.INodeTable:
                    fs.Seek(sizeof(short) * 3 + sizeof(int) * 4, SeekOrigin.Begin);
                    position = br.ReadInt16();
                    break;
                case BitmapDesignation.DataBlocks:
                    fs.Seek(sizeof(short) * 4 + sizeof(int) * 4, SeekOrigin.Begin);
                    position = br.ReadInt16();
                    break;
            }

            fs.Position = position;
            bw.Write(bitmap);
        }

        public short GetSize() => (short)byteArrayLength;
    }
}
