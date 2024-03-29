﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Assembly
{
    public static class ParallelHelper
    {
        public const int MaxDegreeOfParallelism = 32;
        private const int MaxRetries = 3;

        public static async Task ProcessInParallel<T>(
            ConcurrentBag<T> allWork,
            Func<T, CancellationToken, Task> worker,
            CancellationToken cancellationToken)
        {
             await Task.WhenAll(
                Enumerable
                    .Repeat(allWork, MaxDegreeOfParallelism)
                    .Select(async work =>
                    {
                        while (work.TryTake(out var item))
                        {
                            var attempt = 0;

                            while (true)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                try
                                {
                                    await worker(item, cancellationToken);
                                    break;
                                }
                                catch (Exception) when (attempt < MaxRetries)
                                {
                                    attempt++;
                                }
                            }

                        }
                    }));
        }
    }
}
