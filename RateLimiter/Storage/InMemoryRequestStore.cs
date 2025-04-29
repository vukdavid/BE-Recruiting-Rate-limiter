using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace RateLimiter.Storage
{
    /// <summary>
    /// In-memory implementation of the request store for rate limiting.
    /// </summary>
    public class InMemoryRequestStore : IRequestStore
    {
        private readonly ConcurrentDictionary<string, RequestRecord> _store = new();

        /// <inheritdoc />
        public long IncrementRequestCount(string key)
        {
            var now = DateTimeOffset.UtcNow;
            var record = _store.AddOrUpdate(
                key,
                _ => new RequestRecord { Timestamp = now, Count = 1 },
                (_, existingRecord) => 
                {
                    Interlocked.Increment(ref existingRecord.Count);
                    return existingRecord;
                }
            );

            return record.Count;
        }

        /// <inheritdoc />
        public void Cleanup(long olderThanMs)
        {
            var expiryTime = DateTimeOffset.UtcNow.AddMilliseconds(-olderThanMs);

            var keysToRemove = _store
                .Where(kvp => kvp.Value.Timestamp < expiryTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _store.TryRemove(key, out _);
            }
        }
    }
}
