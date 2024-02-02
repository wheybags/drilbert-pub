using System;
using System.Runtime.InteropServices;

namespace Drilbert
{
    public unsafe class UnManagedArray<T> : IDisposable, ICloneable where T : unmanaged
    {
        public T* data { get; private set; }
        public int size { get; private set; }

        public UnManagedArray(int size)
        {
            this.size = size;
            data = (T*)Marshal.AllocHGlobal(size * sizeof(T));
            Util.ReleaseAssert(data != null);
            GC.AddMemoryPressure(size * sizeof(T));
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
                GC.SuppressFinalize(this);

            if (data != null)
            {
                Marshal.FreeHGlobal((IntPtr) data);
                GC.RemoveMemoryPressure(size * sizeof(T));
                data = null;
                size = 0;
            }
        }

        public void Dispose() { Dispose(true); }

        ~UnManagedArray() { Dispose(false); }

        public T* get(int i) { return &data[i]; }

        public void zeroMemory()
        {
            NativeFuncs.memset((IntPtr)data, 0, (UIntPtr)(size * sizeof(T)));
        }

        public virtual object Clone()
        {
            UnManagedArray<T> copy = new UnManagedArray<T>(size * sizeof(T));
            NativeFuncs.memcpy((IntPtr) copy.data, (IntPtr) data, size * sizeof(T));
            return copy;
        }
    }
}