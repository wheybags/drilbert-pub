using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Drilbert;

public static class NativeFuncs
{
    [DllImport("libc", EntryPoint = "memcpy")]
    public static extern IntPtr memcpy(IntPtr dest, IntPtr src, Int64 count);

    [DllImport("libc", EntryPoint = "memset")]
    public static extern IntPtr memset(IntPtr dest, int c, UIntPtr byteCount);

    public const UInt32 SDL_MESSAGEBOX_ERROR = 0x00000010;
    public const UInt32 SDL_MESSAGEBOX_INFORMATION = 0x00000040;

    [DllImport("SDL2.dll")]
    public static extern int SDL_ShowSimpleMessageBox(UInt32 flags, string title, string message, IntPtr window);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AllocConsole();

    static NativeFuncs()
    {
        NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver);
    }

    private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName == "libc")
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return NativeLibrary.Load("msvcrt.dll", assembly, searchPath);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return NativeLibrary.Load("libc.so.6", assembly, searchPath);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return NativeLibrary.Load("libc.dylib", assembly, searchPath);
        }
        else if (libraryName == "SDL2.dll")
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return NativeLibrary.Load("libSDL2-2.0.so.0", assembly, searchPath);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return NativeLibrary.Load("libSDL2.dylib", assembly, searchPath);
        }

        // Otherwise, fallback to default import resolver.
        return IntPtr.Zero;
    }
}