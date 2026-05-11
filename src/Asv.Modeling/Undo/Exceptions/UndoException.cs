namespace Asv.Modeling;

public class UndoException : Exception
{
    public UndoException() { }

    public UndoException(string message)
        : base(message) { }

    public UndoException(string message, Exception inner)
        : base(message, inner) { }
}
