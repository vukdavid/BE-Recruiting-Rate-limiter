using RateLimiter.Matching;

namespace RateLimiter.Tests.Matching
{
    public class SimpleEndpointMatcherTests
    {
        private readonly SimpleEndpointMatcher _matcher;

        public SimpleEndpointMatcherTests()
        {
            _matcher = new SimpleEndpointMatcher();
        }

        [Fact]
        public void IsMatch_SameStrings_ReturnsTrue()
        {
            // Arrange
            string requestPath = "/api/products";
            string configuredPath = "/api/products";

            // Act
            bool result = _matcher.IsMatch(requestPath, configuredPath);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsMatch_DifferentStrings_ReturnsFalse()
        {
            // Arrange
            string requestPath = "/api/products";
            string configuredPath = "/api/orders";

            // Act
            bool result = _matcher.IsMatch(requestPath, configuredPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsMatch_DifferentCasing_ReturnsTrue()
        {
            // Arrange
            string requestPath = "/api/Products";
            string configuredPath = "/api/products";

            // Act
            bool result = _matcher.IsMatch(requestPath, configuredPath);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(null, "/api/products")]
        [InlineData("/api/products", null)]
        [InlineData("", "/api/products")]
        [InlineData("/api/products", "")]
        [InlineData(null, null)]
        [InlineData("", "")]
        public void IsMatch_NullOrEmptyInput_ReturnsFalse(string requestPath, string configuredPath)
        {
            // Act
            bool result = _matcher.IsMatch(requestPath, configuredPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsMatch_RequestPathContainsConfiguredPath_ReturnsFalse()
        {
            // Arrange
            string requestPath = "/api/products/123";
            string configuredPath = "/api/products";

            // Act
            bool result = _matcher.IsMatch(requestPath, configuredPath);

            // Assert
            Assert.False(result, "Matcher should only match exact paths, not when one contains the other");
        }

        [Fact]
        public void IsMatch_ConfiguredPathContainsRequestPath_ReturnsFalse()
        {
            // Arrange
            string requestPath = "/api/products";
            string configuredPath = "/api/products/123";

            // Act
            bool result = _matcher.IsMatch(requestPath, configuredPath);

            // Assert
            Assert.False(result, "Matcher should only match exact paths, not when one contains the other");
        }

        [Fact]
        public void IsMatch_PathsWithTrailingSlash_ReturnsFalse()
        {
            // Arrange
            string requestPath = "/api/products/";
            string configuredPath = "/api/products";

            // Act
            bool result = _matcher.IsMatch(requestPath, configuredPath);

            // Assert
            Assert.False(result, "Paths with and without trailing slashes should not match");
        }
    }
}
