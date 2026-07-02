#nullable enable

namespace Asv.Store;

public interface IDictionaryPart
{
    TDto? Read<TDto>(params string[] path);

    TDto? Read<TDto>(IEnumerable<string> path);
    void Write<TDto>(TDto? value, params string[] path);
    void Write<TDto>(TDto? value, IEnumerable<string> path);
}
