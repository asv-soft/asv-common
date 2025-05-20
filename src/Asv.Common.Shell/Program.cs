using System.Text;
using ConsoleAppFramework;

namespace Asv.Common.Shell;

class Program
{
    static async Task Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
        var app = ConsoleApp.Create();
        app.Add<TcpTest>();
        await app.RunAsync(args);
    }
}