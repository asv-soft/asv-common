namespace Asv.Modeling;

public class UndoExceptionException : Exception
{
    public UndoExceptionException() { }

    public UndoExceptionException(string message)
        : base(message) { }

    public UndoExceptionException(string message, Exception inner)
        : base(message, inner) { }
}
