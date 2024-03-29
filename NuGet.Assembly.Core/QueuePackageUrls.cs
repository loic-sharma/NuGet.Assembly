﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace NuGet.Assembly
{
    public class QueuePackageUrls
    {
        private const long MaxBatchSizeBytes = 262000;

        private readonly IQueueClient _queue;
        private readonly ILogger<QueuePackageUrls> _logger;

        private int _headerSizeEstimateBytes = 100;

        public QueuePackageUrls(IQueueClient queue, ILogger<QueuePackageUrls> logger)
        {
            _queue = queue;
            _logger = logger;
        }

        public async Task ProcessAsync(
            IEnumerable<string> packageUrls,
            CancellationToken cancellationToken = default)
        {
            var messages = packageUrls.Select(ToMessage).ToList();

            var batchStart = 0;

            while (batchStart < messages.Count)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batchEnd = FindBatchEnd(messages, batchStart);

                try
                {
                    // Create the batch of messages to send. It should include the element at index
                    // "batchStart" but not the element at index "batchEnd".
                    var batch = messages
                        .Skip(batchStart)
                        .Take(batchEnd - batchStart)
                        .ToList();

                    _logger.LogInformation(
                        "Enqueueing batch of {Messages} messages...",
                        batch.Count);

                    await _queue.SendAsync(batch);

                    _logger.LogInformation(
                        "Enqueued batch of {Messages} messages",
                        batch.Count);

                    batchStart = batchEnd;
                }
                catch (MessageSizeExceededException e)
                {
                    // The batch was too big. Increase our estimated header size and try again.
                    _headerSizeEstimateBytes = (int)(_headerSizeEstimateBytes * 1.5);

                    _logger.LogWarning(
                        e,
                        "Enqueued batch exceeded max message size. " +
                        "Increased header size estimate to {EstimatedHeaderBytes} bytes",
                        _headerSizeEstimateBytes);
                }
            }

            _logger.LogInformation("Completed enqueueing messages");
            await _queue.CloseAsync();
        }

        private int FindBatchEnd(IReadOnlyList<Message> messages, int batchStart)
        {
            // Start the batch by including one element. Keep increasing the batch
            // until we exceed the max size, or, until we run out of messages. Note
            // that the batch does not include the element at index "batchEnd".
            long estimatedBytes = 0;
            var batchEnd = batchStart + 1;

            while (batchEnd < messages.Count)
            {
                estimatedBytes += messages[batchEnd - 1].Size + _headerSizeEstimateBytes;

                if (estimatedBytes > MaxBatchSizeBytes)
                {
                    return batchEnd;
                }

                batchEnd++;
            }

            return batchEnd;
        }

        private Message ToMessage(string packageUrl)
        {
            return new Message
            {
                Body = Encoding.UTF8.GetBytes(packageUrl),
                ContentType = "application/json;charset=unicode"
            };
        }
    }
}
