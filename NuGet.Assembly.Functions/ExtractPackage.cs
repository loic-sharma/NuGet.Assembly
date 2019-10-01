using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace NuGet.Assembly.Functions
{
    public class ExtractPackage
    {
        private readonly HttpClient _httpClient;
        private readonly PackageExtractor _extractor;

        public ExtractPackage(HttpClient httpClient, PackageExtractor extractor)
        {
            _httpClient = httpClient;
            _extractor = extractor;
        }

        [FunctionName("ExtractPackage")]
        public async Task Run(
            [ServiceBusTrigger("packages", Connection = "ServiceBusConnectionString")]
            string packageUrl,
            ILogger log,
            CancellationToken cancellationToken)
        {
            log.LogInformation("Attempting to download {PackageUrl}", packageUrl);

            using (var packageStream = await GetPackageStreamOrNullAsync(packageUrl, cancellationToken))
            {
                if (packageStream == null)
                {
                    log.LogError("Package does not exist at url {PackageUrl}", packageUrl);
                    return;
                }

                await _extractor.ExtractAsync(packageStream, cancellationToken);
            }

            log.LogInformation($"C# ServiceBus queue trigger function processed message: {packageUrl}");
        }

        private async Task<Stream> GetPackageStreamOrNullAsync(string packageUrl, CancellationToken cancellationToken)
        {
            using (var response = await _httpClient.GetAsync(packageUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                using (var content = await response.Content.ReadAsStreamAsync())
                {
                    return await content.AsTemporaryFileStreamAsync(cancellationToken);
                }
            }
        }
    }
}
