using System;
using Microsoft.AspNetCore.Http;
using Moq;
using RateLimiter.Algorithms;
using RateLimiter.Storage;

namespace RateLimiter.Tests.Algorithms
{
    public class FixedWindowAlgorithmTests
    {
        private readonly Mock<IRequestStore> _mockRequestStore;
        private readonly FixedWindowAlgorithm _algorithm;
        private readonly Mock<HttpContext> _mockHttpContext;

        public FixedWindowAlgorithmTests()
        {
            _mockRequestStore = new Mock<IRequestStore>();
            _algorithm = new FixedWindowAlgorithm(_mockRequestStore.Object);
            _mockHttpContext = new Mock<HttpContext>();
        }

        [Fact]
        public void Constructor_NullRequestStore_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new FixedWindowAlgorithm(null));
            Assert.Equal("requestStore", exception.ParamName);
        }

        [Fact]
        public void ShouldLimitRequest_UnderLimit_ReturnsFalse()
        {
            // Arrange
            string ipAddress = "192.168.1.1";
            string path = "/api/test";
            int requestLimitMs = 1000;
            int requestLimitCount = 5;

            // Setup mock to return a count that's under the limit
            _mockRequestStore.Setup(s => s.IncrementRequestCount(It.IsAny<string>()))
                .Returns(3); // Under the limit of 5

            // Act
            bool result = _algorithm.ShouldLimitRequest(
                _mockHttpContext.Object,
                ipAddress,
                path,
                requestLimitMs,
                requestLimitCount);

            // Assert
            Assert.False(result);
            _mockRequestStore.Verify(s => s.IncrementRequestCount(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void ShouldLimitRequest_AtLimit_ReturnsFalse()
        {
            // Arrange
            string ipAddress = "192.168.1.1";
            string path = "/api/test";
            int requestLimitMs = 1000;
            int requestLimitCount = 5;

            // Setup mock to return a count that's at the limit
            _mockRequestStore.Setup(s => s.IncrementRequestCount(It.IsAny<string>()))
                .Returns(5); // At the limit of 5

            // Act
            bool result = _algorithm.ShouldLimitRequest(
                _mockHttpContext.Object,
                ipAddress,
                path,
                requestLimitMs,
                requestLimitCount);

            // Assert
            Assert.False(result);
            _mockRequestStore.Verify(s => s.IncrementRequestCount(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void ShouldLimitRequest_OverLimit_ReturnsTrue()
        {
            // Arrange
            string ipAddress = "192.168.1.1";
            string path = "/api/test";
            int requestLimitMs = 1000;
            int requestLimitCount = 5;

            // Setup mock to return a count that's over the limit
            _mockRequestStore.Setup(s => s.IncrementRequestCount(It.IsAny<string>()))
                .Returns(6); // Over the limit of 5

            // Act
            bool result = _algorithm.ShouldLimitRequest(
                _mockHttpContext.Object,
                ipAddress,
                path,
                requestLimitMs,
                requestLimitCount);

            // Assert
            Assert.True(result);
            _mockRequestStore.Verify(s => s.IncrementRequestCount(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void ShouldLimitRequest_FirstRequest_TriggersCleanup()
        {
            // Arrange
            string ipAddress = "192.168.1.1";
            string path = "/api/test";
            int requestLimitMs = 1000;
            int requestLimitCount = 5;

            // Setup mock to return 1 (first request in the window)
            _mockRequestStore.Setup(s => s.IncrementRequestCount(It.IsAny<string>()))
                .Returns(1);

            // Act
            _algorithm.ShouldLimitRequest(
                _mockHttpContext.Object,
                ipAddress,
                path,
                requestLimitMs,
                requestLimitCount);

            // We need to wait for the background task to execute
            System.Threading.Thread.Sleep(100);

            // Assert - verify Cleanup was called with correct parameter
            _mockRequestStore.Verify(s => s.Cleanup(requestLimitMs * 2), Times.Once);
        }

        [Fact]
        public void ShouldLimitRequest_NotFirstRequest_DoesNotTriggerCleanup()
        {
            // Arrange
            string ipAddress = "192.168.1.1";
            string path = "/api/test";
            int requestLimitMs = 1000;
            int requestLimitCount = 5;

            // Setup mock to return non-1 value (not the first request)
            _mockRequestStore.Setup(s => s.IncrementRequestCount(It.IsAny<string>()))
                .Returns(2);

            // Act
            _algorithm.ShouldLimitRequest(
                _mockHttpContext.Object,
                ipAddress,
                path,
                requestLimitMs,
                requestLimitCount);

            // We need to wait for the background task to execute
            System.Threading.Thread.Sleep(100);

            // Assert - verify Cleanup was not called
            _mockRequestStore.Verify(s => s.Cleanup(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public void ShouldLimitRequest_UsesFixedWindowKey()
        {
            // Arrange
            string ipAddress = "192.168.1.1";
            string path = "/api/test";
            int requestLimitMs = 1000;
            int requestLimitCount = 5;
            string capturedKey = null;

            // Setup mock to capture the key used for request counting
            _mockRequestStore.Setup(s => s.IncrementRequestCount(It.IsAny<string>()))
                .Callback<string>(key => capturedKey = key)
                .Returns(1);

            // Act
            _algorithm.ShouldLimitRequest(
                _mockHttpContext.Object,
                ipAddress,
                path,
                requestLimitMs,
                requestLimitCount);

            // Assert
            Assert.NotNull(capturedKey);
            Assert.Contains(ipAddress, capturedKey);  // Key should contain IP address
            Assert.Contains(path, capturedKey);       // Key should contain path
            
            // Key should contain window timestamp - can't test exact value but we can verify format
            var parts = capturedKey.Split(':');
            Assert.Equal(3, parts.Length);            // Format should be ip:path:windowId
            Assert.True(long.TryParse(parts[2], out _)); // Last part should be a number
        }
    }
}
