namespace RateLimiter.Storage
{
    /// <summary>
    /// Interface for request storage providers used for rate limiting.
    /// </summary>
    public interface IRequestStore
    {
        /// <summary>
        /// Increments the request count for a given client and returns the current count.
        /// </summary>
        /// <param name="key">The unique key identifying the client and request (typically IP + endpoint + window).</param>
        /// <returns>The current request count after incrementing.</returns>
        long IncrementRequestCount(string key);

        /// <summary>
        /// Removes expired entries from the store.
        /// </summary>
        /// <param name="olderThanMs">Remove entries older than this many milliseconds.</param>
        void Cleanup(long olderThanMs);
    }
}
