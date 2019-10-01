using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core;
using BaGet.Protocol;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;

namespace NuGet.Assembly
{
    public class ExtractCommand
    {
        private readonly NuGetClientFactory _clientFactory;
        private readonly  Func<DirectoryInfo, PackageExtractor> _extractorFactory;
        private readonly  ILogger<ExtractCommand> _logger;

        public ExtractCommand(
            NuGetClientFactory clientFactory,
            Func<DirectoryInfo, PackageExtractor> extractorFactory,
            ILogger<ExtractCommand> logger)
        {
            _clientFactory = clientFactory;
            _extractorFactory = extractorFactory;
            _logger = logger;
        }

        public async Task ExtractAsync(
            string packageId,
            NuGetVersion packageVersion,
            DirectoryInfo outputPath,
            CancellationToken cancellationToken)
        {
            using (var packageStream = await GetPackageStreamOrNullAsync(packageId, packageVersion, cancellationToken))
            {
                if (packageStream == null)
                {
                    _logger.LogError(
                        "Could not find package {PackageId} {PackageVersion}",
                        packageId,
                        packageVersion);

                    return;
                }

                await _extractorFactory(outputPath).ExtractAsync(packageStream, cancellationToken);
            }
        }

        private async Task<Stream> GetPackageStreamOrNullAsync(
            string packageId,
            NuGetVersion packageVersion,
            CancellationToken cancellationToken)
        {
            var client = await _clientFactory.CreatePackageContentClientAsync();

            using (var stream = await client.GetPackageContentStreamOrNullAsync(packageId, packageVersion, cancellationToken))
            {
                return await stream.AsTemporaryFileStreamAsync(cancellationToken);
            }
        }
    }
}
