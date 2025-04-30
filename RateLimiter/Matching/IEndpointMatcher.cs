namespace RateLimiter.Matching
{
    /// <summary>
    /// Interface for matching HTTP requests against configured endpoints.
    /// </summary>
    public interface IEndpointMatcher
    {
        /// <summary>
        /// Determines if the provided HTTP request path matches a configured endpoint.
        /// </summary>
        /// <param name="requestPath">The current request path.</param>
        /// <param name="configuredPath">The configured endpoint path.</param>
        /// <returns>True if the paths match, false otherwise.</returns>
        bool IsMatch(string requestPath, string configuredPath);
    }
}
