using System.Configuration;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.Text;
using Serilog;
using Serilog.Events;

// WARNING!!!
// Please use this tool only for testing or when there is no possibility to create a service for linux/windows
// The runner can be unpredictable. This is the standard method of launching Leviathan without docker containers

namespace Leviathan.Runner
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                         .WriteTo.Console()
                         .CreateLogger();

            Log.Warning("Please use this tool only for testing or when there is no possibility to create a service for linux/windows" +
                        " The runner can be unpredictable. This is the standard method of launching Leviathan without docker containers");

            await Task.WhenAny(Bot.Program.Main(args), Web.Program.Main(args), Worker.Program.Main(args));
            
            throw new ApplicationException("One of projects stopped");
        }
    }
}