using System.Diagnostics;

namespace Drilbert;

public static class Time
{
    private static Stopwatch stopwatch;
    public static void init()
    {
        stopwatch = Stopwatch.StartNew();
    }

    public static long getMs()
    {
        return stopwatch.ElapsedMilliseconds;
    }
}