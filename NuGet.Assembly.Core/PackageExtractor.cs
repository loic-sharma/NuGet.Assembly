using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;

namespace NuGet.Assembly
{
    public class PackageExtractor
    {
        private readonly IAssemblyStore _store;
        private readonly ILogger<PackageExtractor> _logger;

        public PackageExtractor(IAssemblyStore store, ILogger<PackageExtractor> logger)
        {
            _store = store;
            _logger = logger;
        }

        public async Task ExtractAsync(Stream packageStream, CancellationToken cancellationToken)
        {
            using (var packageReader = new PackageArchiveReader(packageStream, leaveStreamOpen: true))
            {
                var packageAssemblies = packageReader
                    .GetFiles()
                    .Where(p => Path.GetExtension(p) == ".dll");

                foreach (var packageAssembly in packageAssemblies)
                {
                    _logger.LogInformation("Extracting {PackageAssembly}...", packageAssembly);

                    using (var assemblyStream = await GetAssemblyStreamAsync(packageReader, packageAssembly, cancellationToken))
                    {
                        await _store.PutAsync(assemblyStream, cancellationToken);
                    }
                }
            }
        }

        private async Task<Stream> GetAssemblyStreamAsync(
            PackageArchiveReader packageReader,
            string path,
            CancellationToken cancellationToken)
        {
            using (var assemblyStream = packageReader.GetStream(path))
            {
                return await assemblyStream.AsTemporaryFileStreamAsync(cancellationToken);
            }
        }
    }
}
