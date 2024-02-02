using System.Collections;
using System.Collections.Generic;

namespace Drilbert
{
    public struct MySlice<T> : IEnumerable<T>
    {
        private List<T> original;
        public int startIndex;
        public int length;

        public MySlice(List<T> original, int startIndex = 0, int length = -1)
        {
            this.original = original;
            this.startIndex = startIndex;
            if (length < 0)
                length = original.Count - startIndex;
            this.length = length;
            Util.DebugAssert((length + startIndex) <= original.Count);
        }

        public MySlice(MySlice<T> otherSlice, int startIndex = 0, int length = -1)
        {
            this.original = otherSlice.original;
            this.startIndex = otherSlice.startIndex + startIndex;
            if (length == -1)
                length = otherSlice.length - startIndex;
            this.length = length;
            Util.DebugAssert((length + startIndex) <= otherSlice.length);
        }

        public T this[int index]
        {
            get => original[this.startIndex + index];
            set => original[index] = value;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < length; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < length; i++)
                yield return this[i];
        }
    }
}