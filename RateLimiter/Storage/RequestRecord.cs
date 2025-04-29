using System;

namespace RateLimiter.Storage
{
    internal class RequestRecord
    {
        public DateTimeOffset Timestamp { get; set; }

        public long Count { get; set; }
    }
}
