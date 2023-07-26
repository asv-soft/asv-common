namespace Asv.Templater;

internal static class LongHelper
{
    public static long Inches(this double size)
    {
        return (long)(size * 1000000);
    }
}