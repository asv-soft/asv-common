#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using R3;
using ZLogger;

namespace Asv.IO;

/// <summary>
/// Provides methods to print a welcome message to the console.
/// </summary>
public static class ConsoleAppHelper
{

    public static ILoggerFactory CreateDefaultLog(LogLevel logLevel = LogLevel.Trace, string? folder = null)
    {
        return LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(logLevel);
            builder.AddZLoggerConsole(options =>
            {
                options.IncludeScopes = true;
                
                options.UsePlainTextFormatter(formatter =>
                {
                    formatter.SetPrefixFormatter($"{0:HH:mm:ss.fff} | ={1:short}= | {2,-40} ", (in MessageTemplate template, in LogInfo info) => template.Format(info.Timestamp, info.LogLevel,info.Category));
                    formatter.SetExceptionFormatter((writer, ex) => Utf8StringInterpolation.Utf8String.Format(writer, $"{ex.Message}"));
                });
            });
            if (folder != null)
            {
                builder.AddZLoggerRollingFile((dt, index) => $"{folder}/{dt:yyyy-MM-dd}_{index}.logs", 1024 * 1024);    
            }
        });
    }
    
    public static IDisposable WaitCancelPressOrProcessExit(ILogger? logger = null)
    {
        var waitForProcessShutdownStart = new ManualResetEventSlim();
        logger??=NullLogger.Instance;
        AppDomain.CurrentDomain.ProcessExit += (_, _) => {
            // We got a SIGTERM, signal that graceful shutdown has started
            logger.LogInformation("Receive ProcessExit event => shutdown app...");
            waitForProcessShutdownStart.Set();
        };
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            logger.LogInformation("Cancel key pressed => shutdown app...");
            waitForProcessShutdownStart.Set();
        };
        // Wait for shutdown to start
        waitForProcessShutdownStart.Wait();
        return waitForProcessShutdownStart;
    }
    public static void HandleExceptions(ILogger logger)
    {
        ObservableSystem.RegisterUnhandledExceptionHandler(ex =>
        {
            {
                logger.ZLogCritical(ex,
                    $"R3 unobserved exception: {ex.Message}");
                Debug.Fail($"R3 unobserved exception: {ex.Message}");
            };
        });
        TaskScheduler.UnobservedTaskException +=
            (sender, args) =>
            {
                logger.ZLogCritical(args.Exception,
                    $"Task scheduler unobserved task exception from '{sender}': {args.Exception.Message}");
                Debug.Fail($"R3 unobserved exception: {args.Exception.Message}");
            };

        AppDomain.CurrentDomain.UnhandledException +=
            (sender, eventArgs) =>
            {
                logger.ZLogCritical($"Unhandled AppDomain exception. Sender '{sender}'. Args: {eventArgs.ExceptionObject}");
                Debug.Fail($"R3 unobserved exception: {eventArgs.ExceptionObject}");
            };
    }
    /// <summary>
    /// Retrieves the version of the specified assembly.
    /// </summary>
    /// <param name="src">The assembly from which to retrieve the version.</param>
    /// <returns>The version of the assembly.</returns>
    public static Version GetVersion(this Assembly src)
    {
        return src.GetName().Version ?? new Version(0, 0, 0, 0);
    }

    /// <summary>
    /// Gets the informational version of the specified assembly.
    /// </summary>
    /// <param name="src">The assembly from which to retrieve the informational version.</param>
    /// <returns>The informational version of the assembly. Returns an empty string if no informational version attribute is found.</returns>
    public static string GetInformationalVersion(this Assembly src)
    {
        var attributes = src.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
        return attributes.Length == 0
            ? ""
            : ((AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion;
    }

    /// <summary>
    /// Retrieves the title of the given assembly.
    /// </summary>
    /// <param name="src">The assembly to retrieve the title from.</param>
    /// <returns>The title of the assembly. If the title is not specified, it returns the filename of the assembly without extension.</returns>
    public static string? GetTitle(this Assembly src)
    {
        var attributes = src.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
        if (attributes.Length > 0)
        {
            var titleAttribute = (AssemblyTitleAttribute)attributes[0];
            if (titleAttribute.Title.Length > 0) return titleAttribute.Title;
        }
        return src.GetName().Name;
    }

    /// <summary>
    /// Retrieves the product name of the given assembly.
    /// </summary>
    /// <param name="src">The assembly to retrieve the product name from.</param>
    /// <returns>The product name of the assembly. Returns an empty string if the assembly does not have a product name.</returns>
    public static string GetProductName(this Assembly src)
    {
        var attributes = src.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
        return attributes.Length == 0 ? string.Empty : ((AssemblyProductAttribute)attributes[0]).Product;
    }

    /// <summary>
    /// Get the description of the given assembly.
    /// </summary>
    /// <param name="src">The assembly to retrieve the description from.</param>
    /// <returns>The description of the assembly, or an empty string if no description is found.</returns>
    public static string GetDescription(this Assembly src)
    {
        var attributes = src.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
        return attributes.Length == 0 ? string.Empty : ((AssemblyDescriptionAttribute)attributes[0]).Description;
    }

    /// <summary>
    /// Returns the copyright holder of the specified assembly.
    /// </summary>
    /// <param name="src">The assembly to retrieve the copyright holder from.</param>
    /// <returns>The copyright holder of the specified assembly, or an empty string if not defined.</returns>
    public static string GetCopyrightHolder(this Assembly src)
    {
        var attributes = src.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
        return attributes.Length == 0 ? string.Empty : ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
    }

    /// <summary>
    /// Retrieve the company name of the given assembly.
    /// </summary>
    /// <param name="src">The assembly to retrieve the company name from.</param>
    /// <returns>The company name of the assembly. Returns an empty string if the assembly does not have a company name attribute.</returns>
    public static string GetCompanyName(this Assembly src)
    {
        var attributes = src.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
        return attributes.Length == 0 ? string.Empty : ((AssemblyCompanyAttribute)attributes[0]).Company;
    }

    /// <summary>
    /// Prints a welcome message to the console using the specified color and additional values.
    /// </summary>
    /// <param name="src">The assembly object.</param>
    /// <param name="color">The color of the message. The default value is ConsoleColor.Cyan.</param>
    /// <param name="additionalValues">Additional key-value pairs to include in the welcome message.</param>
    public static void PrintWelcomeToConsole(this Assembly src, ConsoleColor color = ConsoleColor.Cyan,
        params KeyValuePair<string, string>[] additionalValues)
    {
        var old = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(src.PrintWelcome(additionalValues));
        Console.ForegroundColor = old;
    }
    
   
    public static void PrintWelcomeToLog(this Assembly src, ILogger logger,
        params KeyValuePair<string, string>[] additionalValues)
    {
        using var rdr = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(src.PrintWelcome(additionalValues))));
        while (rdr.EndOfStream == false)
        {
            logger.LogInformation(rdr.ReadLine());    
        }
    }

    /// <summary>
    /// Prints the welcome message.
    /// </summary>
    /// <param name="src">The source assembly.</param>
    /// <param name="additionalValues">The additional key-value pairs to include in the welcome message.</param>
    /// <returns>The welcome message as a string.</returns>
    public static string PrintWelcome(this Assembly src,
        IEnumerable<KeyValuePair<string, string>>? additionalValues = null)
    {
        var header = new[]
        {
            src.GetTitle(),
            src.GetDescription(),
            src.GetCopyrightHolder(),
        };
        var values = new List<KeyValuePair<string, string>>
        {
            new("Version", src.GetInformationalVersion()),
#if DEBUG
            new("Build", "Debug"),
#else
                new KeyValuePair<string, string>("Build", "Release"),
#endif
            new("Process", Process.GetCurrentProcess().Id.ToString()),
            new("OS", Environment.OSVersion.ToString()),
            new("Machine", Environment.MachineName),
            new("Environment", Environment.Version.ToString()),
            new("Is64BitProcess", Environment.Is64BitProcess.ToString()),
        };

        if (additionalValues != null) values.AddRange(additionalValues);

        return PrintWelcome(header, values);
    }


    /// <summary>
    /// Prints a welcome message with a formatted header and values.
    /// </summary>
    /// <param name="header">The collection of strings for the header.</param>
    /// <param name="values">The collection of key-value pairs representing the values.</param>
    /// <param name="padding">The padding to apply between the keys and values. Default is 1.</param>
    /// <returns>A formatted welcome message.</returns>
    private static string PrintWelcome(IEnumerable<string> header, IEnumerable<KeyValuePair<string, string>> values,
        int padding = 1)
    {
        var keyValuePairs = values as KeyValuePair<string, string>[] ?? values.ToArray();
        var keysWidth = keyValuePairs.Select(p => p.Key.Length).Max();
        var valueWidth = keyValuePairs.Select(p => p.Value.Length).Max();
        return PrintWelcome(header, keyValuePairs, keysWidth, valueWidth, padding);
    }

    /// <summary>
    /// Prints a welcome message with formatted header and values.
    /// </summary>
    /// <param name="header">The collection of header strings.</param>
    /// <param name="values">The collection of key-value pairs.</param>
    /// <param name="keyWidth">The width of the key column.</param>
    /// <param name="valueWidth">The width of the value column.</param>
    /// <param name="padding">The padding width.</param>
    /// <returns>A string representing the formatted welcome message.</returns>
    public static string PrintWelcome(IEnumerable<string> header, IEnumerable<KeyValuePair<string, string>> values,
        int keyWidth, int valueWidth, int padding)
    {
        var sb = new StringBuilder();

        var headerWidth = keyWidth + valueWidth + padding * 4 + 1;

        sb.Append('╔').Append('═', headerWidth).Append('╗').Append(' ').AppendLine();
        foreach (var hdr in header)
        {
            sb.Append("║").Append(' ', padding).Append(hdr.PadLeft(headerWidth - padding * 2)).Append(' ', padding)
                .Append("║▒").AppendLine();
        }

        sb.Append('╠').Append('═', padding * 2).Append('═', keyWidth).Append('╦').Append('═', valueWidth)
            .Append('═', padding * 2).Append("╣▒").AppendLine();
        foreach (var pair in values)
        {
            sb.Append('║').Append(' ', padding).Append(pair.Key.PadLeft(keyWidth)).Append(' ', padding).Append('║')
                .Append(' ', padding).Append(pair.Value.PadRight(valueWidth)).Append(' ', padding).Append("║▒")
                .AppendLine();
        }

        sb.Append('╚').Append('═', padding * 2).Append('═', keyWidth).Append('╩').Append('═', valueWidth)
            .Append('═', padding * 2).Append("╝▒").AppendLine();
        sb.Append(' ').Append('▒', headerWidth + 2);
        return sb.ToString();
    }


}