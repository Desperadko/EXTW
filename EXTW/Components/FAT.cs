using EXTW.Structures;

namespace EXTW.Components
{
    public class FAT(SuperBlock sb, FileStream fs, BinaryReader br, BinaryWriter bw)
    {
        public int Length { get; } = sb.BLOCK_QUANTITY;

        private readonly FileStream fs = fs;
        private readonly BinaryReader br = br;
        private readonly BinaryWriter bw = bw;

        public int this[int index]
        {
            get
            {
                if(index < 0 || index > Length)
                    throw new IndexOutOfRangeException();

                fs.Seek(sizeof(short) * 6 + sizeof(int) * 4, SeekOrigin.Begin);
                short position = br.ReadInt16();

                fs.Seek(position + index * sizeof(int), SeekOrigin.Begin);

                return br.ReadInt32();
            }
            set
            {
                if (index < 0 || index > Length)
                    throw new IndexOutOfRangeException();

                fs.Seek(sizeof(short) * 6 + sizeof(int) * 4, SeekOrigin.Begin);
                short position = br.ReadInt16();

                fs.Seek(position + index * sizeof(int), SeekOrigin.Begin);

                bw.Write(value);
            }
        }

        public int[] ReadFAT(int firstIndex)
        {
            MyList<int> blocksMyList = new();

            while (this[firstIndex] != -1)
            {
                blocksMyList.Add(firstIndex);
                firstIndex = this[firstIndex];
            }
            blocksMyList.Add(firstIndex);

            return blocksMyList.ToArray();
        }
        public void WriteFAT(int index) => this[index] = -1;
        public void WriteFAT(int lastIndex, int blockToWrite)
        {
            this[lastIndex] = blockToWrite;
            this[blockToWrite] = -1;
        }
        public void WriteFAT(int firstIndex, int[] blocksToWrite)
        {
            int current = firstIndex;

            if (this[current] != 0)
                while (this[current] != -1)
                    current = this[current];

            for (int i = 0; i < blocksToWrite.Length; i++)
            {
                this[current] = blocksToWrite[i];
                current = blocksToWrite[i];
            }

            this[current] = -1;
        }
        public void ClearFAT(int firstIndex)
        {
            while (this[firstIndex] != -1)
            {
                int temp = this[firstIndex];
                this[firstIndex] = -1;
                firstIndex = temp;
            }
        }

        public short GetSize() => (short)(Length * sizeof(int));
    }
}
