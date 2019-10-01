using System;

namespace NuGet.Assembly
{
    public class Configuration
    {
        public string ServiceBusConnectionString { get; set; }

        public string BlobStorageConnectionString { get; set; }
        public string BlobContainerName { get; set; }
    }
}
