using System;

namespace Asv.IO;

public static class RandomizeVisitorMixin
{
    public static T Randomize<T>(this T src, RandomizeVisitor visitor)
        where T : IVisitable
    {
        src.Accept(visitor);
        return src;
    }

    public static T Randomize<T>(this T src, Random random, string? allowedChars = null)
        where T : IVisitable =>
        src.Randomize(new RandomizeVisitor(random, allowedChars ?? RandomizeVisitor.AllowedChars));

    public static T Randomize<T>(this T src, int seed, string? allowedChars = null)
        where T : IVisitable =>
        src.Randomize(
            new RandomizeVisitor(new Random(seed), allowedChars ?? RandomizeVisitor.AllowedChars)
        );

    public static T Randomize<T>(this T src)
        where T : IVisitable => src.Randomize(RandomizeVisitor.Shared);
}
