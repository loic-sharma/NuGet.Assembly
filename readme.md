# NuGet Assembly Store

Extracts assemblies from NuGet packages into a content-addressable store.

Extract Newtonsoft.Json to disk:

```ps1
dotnet NuGet.Assembly.dll extract Newtonsoft.Json 12.0.1
```

The files' names are the hex-encoded SHA-512 hash of the files' contents.

## Running on Azure

First, setup the Azure resources:

1. Create a Azure Blob Storage account
1. Create an Azure Blob Storage container
1. Create a Service Bus queue
1. Update `NuGet.Assembly/appsettings.json`
    1. Set `ServiceBusConnectionString`
1. Publish `NuGet.Assembly.Functions` to Azure Functions
    1. Use the consumption plan if possible
    1. Add a connection string `ServiceBusConnectionString`
    1. Add app setting `BlobStorageConnectionString` (use Key Vault)
    1. Add app setting `BlobContainerName`

Now use the `NuGet.Assembly` tool to enqueue packages. You can queue a single package:

```ps1
dotnet NuGet.Assembly.dll queue Newtonsoft.Json 12.0.1
```

Or, you can queue all packages:

```ps1
dotnet NuGet.Assembly.dll queue-all
```

Enqueueing all NuGet.org packages should take ~20 minutes. The Azure Function should be able to process all packages in a few hours on the consumption plan.