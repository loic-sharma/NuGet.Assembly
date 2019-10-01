# NuGet Assembly Store

Extracts assemblies from NuGet packages into a content-addressable store.

Extract Newtonsoft.Json to disk:

```ps1
dotnet NuGet.Assembly extract Newtonsoft.Json 12.0.1
```

Enqueue all packages for extraction:

```ps1
dotnet NuGet.Assembly enqueue
```

This requires a valid Service Bus connection string in `appsettings.json`.