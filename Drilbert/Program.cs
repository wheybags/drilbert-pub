using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Steamworks;

namespace Drilbert
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            Time.init();
#if DEBUG
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                NativeFuncs.AllocConsole();
#endif

            if (!Debugger.IsAttached)
                AppDomain.CurrentDomain.UnhandledException += (_, args) => crashHandler((Exception)args.ExceptionObject);

            DrilbertSteam.init();

            using (var game = new Game1())
                game.Run();
        }

        static void crashHandler(Exception e)
        {
            Logger.log("Drilbert has crashed");
            Logger.log(e.ToString());
            NativeFuncs.SDL_ShowSimpleMessageBox(NativeFuncs.SDL_MESSAGEBOX_ERROR, "Drilbert has crashed", e.ToString(), IntPtr.Zero);
            Process.GetCurrentProcess().Kill();
        }
    }
}
