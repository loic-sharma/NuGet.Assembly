using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NuGet.Assembly
{
    public class AssemblyFileStore : IAssemblyStore
    {
        private readonly string _path;
        private readonly ILogger<AssemblyFileStore> _logger;

        public AssemblyFileStore(DirectoryInfo path, ILogger<AssemblyFileStore> logger)
        {
            _path = Path.Combine(path.FullName, "SHA512");
            _logger = logger;
        }

        public Task<Stream> GetOrNullAsync(string key, CancellationToken cancellationToken)
        {
            var path = Path.Combine(_path, key.ToLowerInvariant());

            if (!File.Exists(path))
            {
                return Task.FromResult<Stream>(null);
            }

            return Task.FromResult(
                (Stream)new FileStream(path, FileMode.Open));
        }

        public async Task PutAsync(Stream content, CancellationToken cancellationToken)
        {
            string path;
            using (var hasher = SHA512.Create())
            {
                var hashHex = hasher.ComputeHash(content).Select(b => b.ToString("X2"));
                var fileName = string.Concat(hashHex).ToLowerInvariant();

                path = Path.Combine(_path, fileName);
            }

            if (File.Exists(path))
            {
                _logger.LogInformation(
                    "Content already exists at path {Path}",
                    path);

                return;
            }

            Directory.CreateDirectory(_path);

            _logger.LogInformation("Saving content at path {Path}...", path);

            content.Position = 0;
            using (var outputStream = new FileStream(path, FileMode.CreateNew))
            {
                await content.CopyToAsync(outputStream);
            }
        }
    }
}
