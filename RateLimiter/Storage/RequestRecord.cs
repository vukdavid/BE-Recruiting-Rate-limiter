using System;

namespace RateLimiter.Storage
{
    internal class RequestRecord
    {
        public DateTimeOffset Timestamp;
        public long Count; 
    }
}
