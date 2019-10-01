using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace NuGet.Assembly
{
    public class AssemblyBlobStore : IAssemblyStore
    {
        private readonly CloudBlobContainer _container;
        private readonly ILogger<AssemblyBlobStore> _logger;

        public AssemblyBlobStore(CloudBlobContainer container, ILogger<AssemblyBlobStore> logger)
        {
            _container = container;
            _logger = logger;
        }

        public async Task<Stream> GetOrNullAsync(string key, CancellationToken cancellationToken)
        {
            try
            {
                var blob = _container.GetBlockBlobReference(Path.Combine("SHA512", key));

                return await blob.OpenReadAsync(cancellationToken);
            }
            catch (StorageException e) when (e.RequestInformation?.HttpStatusCode == 404)
            {
                return null;
            }
        }

        public async Task PutAsync(Stream content, CancellationToken cancellationToken)
        {
            string path;
            using (var hasher = SHA512.Create())
            {
                var hashHex = hasher.ComputeHash(content).Select(b => b.ToString("X2"));
                var fileName = string.Concat(hashHex).ToLowerInvariant();

                path = Path.Combine("SHA512", fileName);
            }

            var blob = _container.GetBlockBlobReference(path);
            var condition = AccessCondition.GenerateIfNotExistsCondition();

            try
            {
                _logger.LogInformation("Saving content at path {Path}...", path);

                await blob.UploadFromStreamAsync(
                    content,
                    condition,
                    options: null,
                    operationContext: null,
                    cancellationToken: cancellationToken);
            }
            catch (StorageException e) when (e?.RequestInformation?.HttpStatusCode == 409)
            {
                _logger.LogInformation(
                    "Content already exists at path {Path}",
                    path);
            }
        }
    }
}
