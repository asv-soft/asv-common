namespace Asv.Common;

public sealed class UnknownValidationException()
    : ValidationException(
        "Unknown validation exception",
        RS.ValidationException_Unknown_Message
    ) { }
