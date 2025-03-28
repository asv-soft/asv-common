using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Asv.Common;

public static class AsyncTaskExtensions
{
    static Action<Exception>? _exceptionHandler;
    static bool _alwaysRethrowExceptions;

    public static void ExecuteAsync(this ValueTask task, in Action<Exception>? exceptionHandler = null, in bool continueOnCapturedContext = false) 
        => ProcessAsyncExecution(task, continueOnCapturedContext, exceptionHandler);
    public static void ExecuteAsync<T>(this ValueTask<T> task, in Action<Exception>? exceptionHandler = null, in bool continueOnCapturedContext = false) 
        => ProcessAsyncExecution(task, continueOnCapturedContext, exceptionHandler);
    public static void ExecuteAsync<TException>(this ValueTask task, in Action<TException>? exceptionHandler = null, in bool continueOnCapturedContext = false) 
        where TException : Exception => ProcessAsyncExecution(task, continueOnCapturedContext, exceptionHandler);
    public static void ExecuteAsync<T, TException>(this ValueTask<T> task, in Action<TException>? exceptionHandler = null, in bool continueOnCapturedContext = false) 
        where TException : Exception => ProcessAsyncExecution(task, continueOnCapturedContext, exceptionHandler);

    public static void ExecuteAsync(this Task task, in ConfigureAwaitOptions configureAwaitOptions, in Action<Exception>? exceptionHandler = null) 
        => ProcessAsyncExecution(task, configureAwaitOptions, exceptionHandler);
    public static void ExecuteAsync<TException>(this Task task, in ConfigureAwaitOptions configureAwaitOptions, in Action<TException>? exceptionHandler = null) 
        where TException : Exception => ProcessAsyncExecution(task, configureAwaitOptions, exceptionHandler);
    public static void ExecuteAsync(this Task task, in Action<Exception>? exceptionHandler = null, in bool continueOnCapturedContext = false) 
        => ProcessAsyncExecution(task, continueOnCapturedContext, exceptionHandler);
    public static void ExecuteAsync<TException>(this Task task, in Action<TException>? exceptionHandler = null, in bool continueOnCapturedContext = false) 
        where TException : Exception => ProcessAsyncExecution(task, continueOnCapturedContext, exceptionHandler);
    public static void Configure(in bool alwaysRethrowExceptions = false) => _alwaysRethrowExceptions = alwaysRethrowExceptions;
    public static void ClearDefaultExceptionHandler() => _exceptionHandler = null;
    public static void SetDefaultExceptionHandler(in Action<Exception> exceptionHandler) 
        => _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
    static async void ProcessAsyncExecution<TException>(ValueTask valueTask, bool continueOnCapturedContext, Action<TException>? exceptionHandler) 
        where TException : Exception
    {
        try
        {
            await valueTask.ConfigureAwait(continueOnCapturedContext);
        }
        catch (TException ex) when (_exceptionHandler is not null || exceptionHandler is not null)
        {
            ProcessException(ex, exceptionHandler);
            if (_alwaysRethrowExceptions)
            {
                ExceptionDispatchInfo.Throw(ex);
            }
        }
    }

    static async void ProcessAsyncExecution<T, TException>(ValueTask<T> valueTask, bool continueOnCapturedContext, Action<TException>? exceptionHandler) 
        where TException : Exception
    {
        try
        {
            await valueTask.ConfigureAwait(continueOnCapturedContext);
        }
        catch (TException ex) when (_exceptionHandler is not null || exceptionHandler is not null)
        {
            ProcessException(ex, exceptionHandler);
            if (_alwaysRethrowExceptions)
            {
                ExceptionDispatchInfo.Throw(ex);
            }
        }
    }

    static async void ProcessAsyncExecution<TException>(Task task, bool continueOnCapturedContext, Action<TException>? exceptionHandler) 
        where TException : Exception
    {
        try
        {
            await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (TException ex) when (_exceptionHandler is not null || exceptionHandler is not null)
        {
            ProcessException(ex, exceptionHandler);
            if (_alwaysRethrowExceptions)
            {
                ExceptionDispatchInfo.Throw(ex);
                throw;
            }
        }
    }

    static async void ProcessAsyncExecution<TException>(Task task, ConfigureAwaitOptions configureAwaitOptions, Action<TException>? exceptionHandler) 
        where TException : Exception
    {
        try
        {
            await task.ConfigureAwait(configureAwaitOptions);
        }
        catch (TException ex) when (_exceptionHandler is not null || exceptionHandler is not null)
        {
            ProcessException(ex, exceptionHandler);
            if (_alwaysRethrowExceptions)
                ExceptionDispatchInfo.Throw(ex);
        }
    }

    static void ProcessException<TException>(in TException exception, in Action<TException>? exceptionHandler) 
        where TException : Exception
    {
        _exceptionHandler?.Invoke(exception);
        exceptionHandler?.Invoke(exception);
    }
}