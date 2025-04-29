using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

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
                // Add new record if key doesn't exist
                _ => new RequestRecord { Timestamp = now, Count = 1 },
                // Update existing record
                (_, existingRecord) =>
                {
                    existingRecord.Count++;
                    return existingRecord;
                });

            return record.Count;
        }

        /// <inheritdoc />
        public void Cleanup(long olderThanMs)
        {
            var expiryTime = DateTimeOffset.UtcNow.AddMilliseconds(-olderThanMs);
            
            // Find expired keys
            var keysToRemove = _store
                .Where(kvp => kvp.Value.Timestamp < expiryTime)
                .Select(kvp => kvp.Key)
                .ToList();

            // Remove them from the dictionary
            foreach (var key in keysToRemove)
            {
                _store.TryRemove(key, out _);
            }
        }
    }
}
