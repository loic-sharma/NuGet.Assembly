using System.Net;
using System.Net.Http;
using BaGet.Protocol;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace NuGet.Assembly
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddNuGetAssembly(this IServiceCollection services)
        {            
            services.AddSingleton<IAssemblyStore, AssemblyBlobStore>();
            services.AddSingleton<PackageExtractor>();

            services.AddSingleton(provider =>
            {
                return new HttpClient(new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                });
            });

            services.AddSingleton(provider =>
            {
                return new NuGetClientFactory(
                    provider.GetRequiredService<HttpClient>(),
                    "https://api.nuget.org/v3/index.json");
            });

            services.AddSingleton<IQueueClient>(provider =>
            {
                var config = provider.GetRequiredService<IOptions<Configuration>>();
                var builder = new ServiceBusConnectionStringBuilder(
                    config.Value.ServiceBusConnectionString);

                return new QueueClient(builder, ReceiveMode.PeekLock);
            });

            services.AddSingleton(provider =>
            {
                var config = provider.GetRequiredService<IOptions<Configuration>>();
                var blobClient = CloudStorageAccount
                    .Parse(config.Value.BlobStorageConnectionString)
                    .CreateCloudBlobClient();

                return blobClient.GetContainerReference(config.Value.BlobContainerName);
            });

            return services;
        }
    }
}
