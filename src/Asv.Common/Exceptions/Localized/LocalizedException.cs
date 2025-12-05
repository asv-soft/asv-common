using System;

namespace Asv.Common;

#pragma warning disable RCS1194
public class LocalizedException : Exception
#pragma warning restore RCS1194
{
    public LocalizedException()
        : base() { }

    public LocalizedException(string? message, string? localizedMessage = null)
        : base(message)
    {
        LocalizedMessage = localizedMessage;
    }

    public LocalizedException(
        string? message,
        Exception? innerException,
        string? localizedMessage = null
    )
        : base(message, innerException)
    {
        LocalizedMessage = localizedMessage;
    }

    public string? LocalizedMessage { get; init; }

    public Exception GetExceptionWithLocalizationOrSelf()
    {
        return GetExceptionWithLocalization() ?? this;
    }

    public Exception? GetExceptionWithLocalization() =>
        LocalizedMessage is null ? null : new Exception(LocalizedMessage);
}
