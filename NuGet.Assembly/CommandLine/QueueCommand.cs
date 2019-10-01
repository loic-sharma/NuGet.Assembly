using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;

namespace NuGet.Assembly
{
    public class QueueCommand
    {
        private readonly QueuePackageUrls _queue;
        private readonly ILogger<QueueCommand> _logger;

        public QueueCommand(
            QueuePackageUrls queue,
            ILogger<QueueCommand> logger)
        {
            _queue = queue;
            _logger = logger;
        }

        public async Task QueueAsync(
            string packageId,
            NuGetVersion packageVersion, CancellationToken cancellationToken = default)
        {
            // TODO: This is a hack. Use the service index instead to determine the package URL.
            var id = packageId.ToLowerInvariant();
            var version = packageVersion.ToNormalizedString().ToLowerInvariant();

            var url = $"https://api.nuget.org/v3-flatcontainer/{id}/{version}/{id}.{version}.nupkg";

            _logger.LogInformation("Enqueueing package url {PackageUrl}", url);

            await _queue.ProcessAsync(new[] { url }, cancellationToken);
        }
    }
}
