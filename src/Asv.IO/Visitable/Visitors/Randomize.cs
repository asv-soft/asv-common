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
    
    public static T Randomize<T>(this T src, 
        Random random, 
        string? allowedChars = null, 
        int? maxStringSize = null,
        int? minStringSize = null, 
        uint minListSize = RandomizeVisitor.MinListSize,
        uint maxListSize = RandomizeVisitor.MaxListSize)
        where T : IVisitable =>
        src.Randomize(new RandomizeVisitor(random, 
            allowedChars ?? RandomizeVisitor.AllowedChars,
            minStringSize ?? RandomizeVisitor.MinStringSize,
            maxStringSize ?? RandomizeVisitor.MaxStringSize,
            minListSize, maxListSize));

    public static T Randomize<T>(this T src, 
        int seed, 
        string? allowedChars = null, 
        int? minStringSize = null,
        int? maxStringSize = null,
        uint minListSize = RandomizeVisitor.MinListSize,
        uint maxListSize = RandomizeVisitor.MaxListSize)
        where T : IVisitable =>
        src.Randomize(new RandomizeVisitor(new Random(seed), 
            allowedChars ?? RandomizeVisitor.AllowedChars , 
            minStringSize ?? RandomizeVisitor.MinStringSize,
            maxStringSize ?? RandomizeVisitor.MaxStringSize,
            minListSize,
            maxListSize));
    
    public static T Randomize<T>(this T src)
        where T : IVisitable =>
        src.Randomize(RandomizeVisitor.Shared);
}