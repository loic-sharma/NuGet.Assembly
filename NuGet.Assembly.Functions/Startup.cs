using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(NuGet.Assembly.Startup))]

namespace NuGet.Assembly
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.Configure<Configuration>(config =>
            {
                config.BlobStorageConnectionString = Config("BlobStorageConnectionString");
                config.BlobContainerName = Config("BlobContainerName");
            });

            builder.Services.AddNuGetAssembly();
        }

        private static string Config(string name)
        {
            var value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            return value;
        }
    }
}
