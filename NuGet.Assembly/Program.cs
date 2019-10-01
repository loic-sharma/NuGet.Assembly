using System;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NuGet.Assembly
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var parser = CommandLine.Create()
                .UseDefaults()
                .UseHost((IHostBuilder host) =>
                {
                    host.ConfigureAppConfiguration((ctx, config) =>
                    {
                        config.SetBasePath(Directory.GetCurrentDirectory());
                        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    });

                    host.ConfigureLogging((ctx, logging) =>
                    {
                        logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
                        logging.AddConsole();
                    });

                    host.ConfigureServices((ctx, services) =>
                    {
                        services.AddNuGetAssembly();

                        services.AddSingleton<ExtractCommand>();
                        services.AddSingleton<QueueCommand>();

                        services.Configure<Configuration>(ctx.Configuration);

                        services.AddSingleton<Func<DirectoryInfo, PackageExtractor>>(provider =>
                        {
                            return (DirectoryInfo path) =>
                            {
                                var storeFactory = provider.GetRequiredService<Func<DirectoryInfo, IAssemblyStore>>();
                                var logger = provider.GetRequiredService<ILogger<PackageExtractor>>();

                                return new PackageExtractor(
                                    storeFactory(path),
                                    logger);
                            };
                        });

                        services.AddSingleton<Func<DirectoryInfo, IAssemblyStore>>(provider =>
                        {
                            return (DirectoryInfo path) => new AssemblyFileStore(
                                path,
                                provider.GetRequiredService<ILogger<AssemblyFileStore>>());
                        });

                    });
                })
                .Build();

            await parser.InvokeAsync(args);
        }
    }
}
