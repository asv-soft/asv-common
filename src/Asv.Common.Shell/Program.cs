using System.Reflection;
using System.Text;
using Asv.IO;
using ConsoleAppFramework;

namespace Asv.Common.Shell;

class Program
{
    static async Task Main(string[] args)
    {
        Assembly.GetExecutingAssembly().PrintWelcomeToConsole();
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
        Console.BackgroundColor = ConsoleColor.Black;
        var app = ConsoleApp.Create();
        app.Add<TcpTest>();
        await app.RunAsync(args);
    }
}