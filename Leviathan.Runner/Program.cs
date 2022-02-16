using System.Configuration;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.Text;
using Leviathan.Runner.Helpers;
using Serilog;
using Serilog.Events;

// WARNING!!!
// Please use this tool only for testing or when there is no possibility to create a service for linux/windows
// The runner can be unpredictable. This is the standard method of launching Leviathan without docker containers
// Needs to be published otherwise, dependency errors throws. It won't work through normal compilation(F5) and it's hard to fix
//
// How to publish: locate main folder and type "dotnet publish -c Release -o ./output"

namespace Leviathan.Runner
{
    public class Program
    {
        private static readonly string[] _assemblyNames = { "Leviathan.Worker", "Leviathan.Bot", "Leviathan.Web" };
        public static List<Process> Processes = new List<Process>();

        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                         .Enrich.FromLogContext()
                         .WriteTo.Console()
                         .CreateLogger();
            
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
            
            Log.Warning("Please use this tool only for testing or when there is no possibility to create a service for linux/windows" +
                        " The runner can be unpredictable. This is the standard method of launching Leviathan without docker containers");

            var dotnetPath = DotNetMuxer.MuxerPath;
            Log.Information($".NET Core dotnet utility path: {dotnetPath}");

            foreach (var assemblyName in _assemblyNames)
            {
                RunApplicationRedirectOutput(dotnetPath, assemblyName);
            }

            Console.ReadLine();
        }

        private static void CurrentDomainOnProcessExit(object? sender, EventArgs e)
        {
            Log.Information("Application wants exit, try to terminate created proceses");

            foreach (var process in Processes.Where(process => !process.HasExited))
            {
                process.Kill();
            }
        }

        public static void RunApplicationRedirectOutput(string dotnetPath, string assemblyName)
        {
            var callingArgs = $"\"{AppDomain.CurrentDomain.BaseDirectory}{assemblyName}.dll\"";
            Log.Information($"Trying to run {assemblyName} located in {callingArgs}");
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(dotnetPath, callingArgs)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.OutputDataReceived += (_, args) => Console.WriteLine(args.Data);
            process.ErrorDataReceived += (_, args) => Console.WriteLine(args.Data);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            Processes.Add(process);
        }
    }
}