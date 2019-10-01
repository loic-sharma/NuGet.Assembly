using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Assembly
{
    public interface IAssemblyStore
    {
        Task<Stream> GetOrNullAsync(string key, CancellationToken cancellationToken);

        Task PutAsync(Stream content, CancellationToken cancellationToken);
    }
}
