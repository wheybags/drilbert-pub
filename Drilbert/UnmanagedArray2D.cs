using System;

namespace Drilbert;

public unsafe class UnManagedArray2D<T> : UnManagedArray<T> where T : unmanaged
{
    public int width { get; private set; }
    public int height { get; private set; }

    public UnManagedArray2D(int width, int height) : base(width * height)
    {
        this.width = width;
        this.height = height;
    }

    public T* get(int x, int y)
    {
        Util.DebugAssert(isPointValid(x, y));
        return &data[(y * width) + x];
    }
    public T* get(Vec2i p) { return get(p.x, p.y); }

    public bool isPointValid(int x, int y) { return x >= 0 && x < width && y >= 0 && y < height; }
    public bool isPointValid(Vec2i p) { return isPointValid(p.x, p.y); }

    public override object Clone()
    {
        UnManagedArray2D<T> copy = new UnManagedArray2D<T>(width, height);
        NativeFuncs.memcpy((IntPtr) copy.data, (IntPtr) data, size * sizeof(T));
        return copy;
    }
}
