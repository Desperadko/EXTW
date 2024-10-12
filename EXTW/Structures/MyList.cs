namespace EXTW.Structures
{
    public class MyList<T>
    {
        T[] values;

        public int Capacity
        {
            get => values.Length;
        }
        public int Count { get; private set; }

        public MyList()
        {
            values = new T[10];
            Count = 0;
        }

        public void Add(T value)
        {
            if (Count == Capacity)
                DoubleArray();

            values[Count] = value;
            Count++;
        }
        public void Remove(T value)
        {
            int index = IndexOf(value);

            if (index >= 0)
            {
                for (int i = index; i < Count - 1; i++)
                    values[i] = values[i + 1];

                values[Count] = default!;
                Count--;
            }
        }
        public T[] ToArray()
        {
            T[] result = new T[Count];

            for (int i = 0; i < result.Length; i++)
                result[i] = values[i];

            return result;
        }

        private int IndexOf(T value)
        {
            for (int i = 0; i < values.Length; i++)
                if (values[i]!.Equals(value))
                    return i;

            return -1;
        }
        private void DoubleArray()
        {
            T[] result = new T[Count * 2];

            for (int i = 0; i < values.Length; i++)
                result[i] = values[i];

            values = result;
        }
    }
}
