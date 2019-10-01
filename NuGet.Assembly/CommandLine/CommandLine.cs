using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NuGet.Versioning;

namespace NuGet.Assembly
{
    public static class CommandLine
    {
        public static CommandLineBuilder Create()
        {
            return new CommandLineBuilder(BuildRootCommand());
        }

        private static RootCommand BuildRootCommand()
        {
            var rootCommand = new RootCommand();

            rootCommand.Handler = CommandHandler.Create((IHelpBuilder help) =>
            {
                help.Write(rootCommand);
            });

            rootCommand.Add(BuildExtractCommand());
            rootCommand.Add(BuildQueueCommand());

            return rootCommand;
        }

        private static Command BuildExtractCommand()
        {
            var command = new Command("extract", "Extract assemblies from a package on NuGet.org")
            {
                new Argument<string>("package-id"),
                new Argument<NuGetVersion>("package-version"),

                new Option("--output")
                {
                    Argument = new Argument<DirectoryInfo>(
                        defaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()))
                }
            };

            command.Handler = CommandHandler.Create<IHost, string, NuGetVersion, DirectoryInfo, CancellationToken>(TestAsync);

            return command;
        }

        private static Command BuildQueueCommand()
        {
            return new Command("queue", "Add NuGet package URLs to the queue")
            {
                Handler = CommandHandler.Create<IHost, bool, CancellationToken>(QueueAsync)
            };
        }

        private static async Task TestAsync(
            IHost host,
            string packageId,
            NuGetVersion packageVersion,
            DirectoryInfo output,
            CancellationToken cancellationToken)
        {
            await host
                .Services
                .GetRequiredService<ExtractCommand>()
                .ExtractAsync(packageId, packageVersion, output, cancellationToken);
        }

        private static async Task QueueAsync(IHost host, bool enqueue, CancellationToken cancellationToken)
        {
            await host
                .Services
                .GetRequiredService<QueueCommand>()
                .RunAsync(cancellationToken);
        }
    }
}
