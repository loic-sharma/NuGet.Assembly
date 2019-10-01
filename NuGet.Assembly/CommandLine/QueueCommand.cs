using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Protocol;
using BaGet.Protocol.Catalog;
using BaGet.Protocol.Models;
using Microsoft.Extensions.Logging;
using NuGet.Packaging.Core;

namespace NuGet.Assembly
{
    public class QueueCommand
    {
        private readonly NuGetClientFactory _clientFactory;
        private readonly QueuePackageUrls _queue;
        private readonly ICursor _cursor;
        private readonly ILogger<QueueCommand> _logger;

        public QueueCommand(
            NuGetClientFactory clientFactory,
            QueuePackageUrls queue,
            ICursor cursor,
            ILogger<QueueCommand> logger)
        {
            _clientFactory = clientFactory;
            _queue = queue;
            _cursor = cursor;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            // Prepare the processing.
            ThreadPool.SetMinThreads(ParallelHelper.MaxDegreeOfParallelism, completionPortThreads: 4);
            ServicePointManager.DefaultConnectionLimit = ParallelHelper.MaxDegreeOfParallelism;
            ServicePointManager.MaxServicePointIdleTime = 10000;

            var cursor = await _cursor.GetAsync(cancellationToken);
            if (cursor == null)
            {
                cursor = DateTimeOffset.MinValue;
            }

            _logger.LogInformation("Finding catalog leafs comitted after time {Cursor}...", cursor);

            var catalogClient = await _clientFactory.CreateCatalogClientAsync(cancellationToken);
            var catalogIndex = await catalogClient.GetIndexAsync(cancellationToken);
            var catalogLeafItems = await GetCatalogLeafItems(catalogClient, catalogIndex, cursor.Value, cancellationToken);

            catalogLeafItems = DeduplicateCatalogLeafItems(catalogLeafItems);

            _logger.LogInformation("Processing {CatalogLeafs} catalog leafs...", catalogLeafItems.Count());

            await ProcessCatalogLeafsAsync(catalogLeafItems, cancellationToken);
            await _cursor.SetAsync(catalogIndex.CommitTimestamp, cancellationToken);

            _logger.LogInformation("Finished processing catalog leafs");
        }

        private async Task<IEnumerable<CatalogLeafItem>> GetCatalogLeafItems(
            ICatalogClient catalogClient,
            CatalogIndex catalogIndex,
            DateTimeOffset cursor,
            CancellationToken cancellationToken)
        {
            var catalogLeafItems = new ConcurrentBag<CatalogLeafItem>();
            var catalogPageUrls = new ConcurrentBag<string>(
                catalogIndex
                    .Items
                    .Where(i => i.CommitTimestamp > cursor)
                    .Select(i => i.CatalogPageUrl));

            await ParallelHelper.ProcessInParallel(
                catalogPageUrls,
                ProcessCatalogPageUrlAsync,
                cancellationToken);

            return catalogLeafItems;

            async Task ProcessCatalogPageUrlAsync(string catalogPageUrl, CancellationToken token)
            {
                _logger.LogInformation("Processing catalog page {CatalogPageUrl}...", catalogPageUrl);

                var page = await catalogClient.GetPageAsync(catalogPageUrl, token);

                foreach (var catalogLeafItem in page.Items.Where(i => i.CommitTimestamp > cursor))
                {
                    catalogLeafItems.Add(catalogLeafItem);
                }

                _logger.LogInformation("Processed catalog page {CatalogPageUrl}", catalogPageUrl);
            }
        }

        private IEnumerable<CatalogLeafItem> DeduplicateCatalogLeafItems(IEnumerable<CatalogLeafItem> catalogLeafItems)
        {
            // Grab only the latest catalog leaf for each package id and version.
            // Skip packages that were deleted.
            _logger.LogInformation("Removing duplicate leafs...");

            return catalogLeafItems
                .GroupBy(l => new PackageIdentity(l.PackageId, l.ParsePackageVersion()))
                .Select(g => g.OrderByDescending(l => l.CommitTimestamp).First())
                .Where(l => l.IsPackageDetails())
                .ToList();
        }

        private async Task ProcessCatalogLeafsAsync(IEnumerable<CatalogLeafItem> catalogLeafItems, CancellationToken cancellationToken)
        {
            var packageUrls = catalogLeafItems.Select(ToPackageUrl);

            await _queue.ProcessAsync(packageUrls, cancellationToken);
 
            string ToPackageUrl(CatalogLeafItem catalogLeafItem)
            {
                // TODO: This is a hack. Use the service index instead to determine the package URL.
                var id = catalogLeafItem.PackageId.ToLowerInvariant();
                var version = catalogLeafItem.ParsePackageVersion().ToNormalizedString().ToLowerInvariant();

                return $"https://api.nuget.org/v3-flatcontainer/{id}/{version}/{id}.{version}.nupkg";
            }
        }
    }
}
