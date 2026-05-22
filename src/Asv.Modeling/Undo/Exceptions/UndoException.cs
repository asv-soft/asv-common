namespace Asv.Modeling;

/// <summary>
/// Represents an error that occurs while registering or executing undo operations.
/// </summary>
public class UndoException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UndoException"/> class.
    /// </summary>
    public UndoException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UndoException"/> class with an error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public UndoException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UndoException"/> class with an error message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="inner">The exception that caused this exception.</param>
    public UndoException(string message, Exception inner)
        : base(message, inner) { }
}
