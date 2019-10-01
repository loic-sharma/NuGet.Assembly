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
            rootCommand.Add(BuildQueueAllCommand());

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

            command.Handler = CommandHandler.Create<IHost, string, NuGetVersion, DirectoryInfo, CancellationToken>(ExtractAsync);

            return command;
        }

        private static Command BuildQueueCommand()
        {
            var command = new Command("queue", "Add a single NuGet package to the queue")
            {
                new Argument<string>("package-id"),
                new Argument<NuGetVersion>("package-version"),
            };

            command.Handler = CommandHandler.Create<IHost, string, NuGetVersion, CancellationToken>(QueueAsync);

            return command;
        }

        private static Command BuildQueueAllCommand()
        {
            return new Command("queue-all", "Add all NuGet package URLs to the queue")
            {
                Handler = CommandHandler.Create<IHost, bool, CancellationToken>(QueueAllAsync)
            };
        }

        private static async Task ExtractAsync(
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

        private static async Task QueueAsync(
            IHost host,
            string packageId,
            NuGetVersion packageVersion,
            CancellationToken cancellationToken)
        {
            await host
                .Services
                .GetRequiredService<QueueCommand>()
                .QueueAsync(packageId, packageVersion, cancellationToken);
        }

        private static async Task QueueAllAsync(IHost host, bool enqueue, CancellationToken cancellationToken)
        {
            await host
                .Services
                .GetRequiredService<QueueAllCommand>()
                .RunAsync(cancellationToken);
        }
    }
}
