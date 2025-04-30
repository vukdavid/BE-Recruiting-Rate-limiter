using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RateLimiter.Algorithms;
using RateLimiter.Configuration;
using RateLimiter.Matching;

namespace RateLimiter.Tests
{
    public class RateLimiterMiddlewareTests
    {
        private readonly Mock<RequestDelegate> _mockNext;
        private readonly Mock<ILogger<RateLimiterMiddleware>> _mockLogger;
        private readonly Mock<IRateLimitAlgorithm> _mockAlgorithm;
        private readonly Mock<IEndpointMatcher> _mockEndpointMatcher;
        private readonly Mock<IOptions<RateLimiterOptions>> _mockOptions;
        private readonly RateLimiterOptions _options;
        private readonly DefaultHttpContext _httpContext;
        private readonly MemoryStream _responseBody;

        public RateLimiterMiddlewareTests()
        {
            _mockNext = new Mock<RequestDelegate>();
            _mockLogger = new Mock<ILogger<RateLimiterMiddleware>>();
            _mockAlgorithm = new Mock<IRateLimitAlgorithm>();
            _mockEndpointMatcher = new Mock<IEndpointMatcher>();
            
            _options = new RateLimiterOptions
            {
                RequestLimiterEnabled = true,
                DefaultRequestLimitMs = 1000,
                DefaultRequestLimitCount = 10,
                EndpointLimits = new List<EndpointLimitOptions>
                {
                    new() {
                        Endpoint = "/api/limited",
                        RequestLimitMs = 500,
                        RequestLimitCount = 5
                    }
                }
            };
            
            _mockOptions = new Mock<IOptions<RateLimiterOptions>>();
            _mockOptions.Setup(o => o.Value).Returns(_options);
            
            _httpContext = new DefaultHttpContext();
            _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
            _httpContext.Request.Method = "GET";
            _httpContext.Request.Path = "/api/test";
            
            // Set up a memory stream to capture response output
            _responseBody = new MemoryStream();
            _httpContext.Response.Body = _responseBody;
        }

        [Theory]
        [InlineData("next", 1)]
        [InlineData("logger", 2)]
        [InlineData("algorithm", 3)]
        [InlineData("endpointMatcher", 4)]
        [InlineData("options", 5)]
        public void Constructor_NullParameters_ThrowsArgumentNullException(string expectedParamName, int paramIndex)
        {
            // Arrange
            RequestDelegate next = paramIndex == 1 ? null : _mockNext.Object;
            ILogger<RateLimiterMiddleware> logger = paramIndex == 2 ? null : _mockLogger.Object;
            IRateLimitAlgorithm algorithm = paramIndex == 3 ? null : _mockAlgorithm.Object;
            IEndpointMatcher endpointMatcher = paramIndex == 4 ? null : _mockEndpointMatcher.Object;
            IOptions<RateLimiterOptions> options = paramIndex == 5 ? null : _mockOptions.Object;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new RateLimiterMiddleware(
                next,
                logger,
                algorithm,
                endpointMatcher,
                options));
                
            Assert.Equal(expectedParamName, exception.ParamName);
        }

        [Fact]
        public async Task InvokeAsync_RateLimiterDisabled_CallsNextMiddleware()
        {
            // Arrange
            _options.RequestLimiterEnabled = false;
            
            var middleware = new RateLimiterMiddleware(
                _mockNext.Object,
                _mockLogger.Object,
                _mockAlgorithm.Object,
                _mockEndpointMatcher.Object,
                _mockOptions.Object);
                
            // Act
            await middleware.InvokeAsync(_httpContext);
            
            // Assert
            _mockNext.Verify(next => next(_httpContext), Times.Once);
            _mockAlgorithm.Verify(a => a.ShouldLimitRequest(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_UnderLimit_CallsNextMiddleware()
        {
            // Arrange
            _mockAlgorithm.Setup(a => a.ShouldLimitRequest(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .Returns(false); // Not rate-limited
                
            var middleware = new RateLimiterMiddleware(
                _mockNext.Object,
                _mockLogger.Object,
                _mockAlgorithm.Object,
                _mockEndpointMatcher.Object,
                _mockOptions.Object);
                
            // Act
            await middleware.InvokeAsync(_httpContext);
            
            // Assert
            _mockNext.Verify(next => next(_httpContext), Times.Once);
            Assert.NotEqual(StatusCodes.Status429TooManyRequests, _httpContext.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_OverLimit_Returns429()
        {
            // Arrange
            _mockAlgorithm.Setup(a => a.ShouldLimitRequest(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .Returns(true); // Rate-limited
                
            var middleware = new RateLimiterMiddleware(
                _mockNext.Object,
                _mockLogger.Object,
                _mockAlgorithm.Object,
                _mockEndpointMatcher.Object,
                _mockOptions.Object);
                
            // Act
            await middleware.InvokeAsync(_httpContext);
            
            // Assert
            _mockNext.Verify(next => next(_httpContext), Times.Never);
            Assert.Equal(StatusCodes.Status429TooManyRequests, _httpContext.Response.StatusCode);
            
            // Check response body
            _responseBody.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(_responseBody);
            var responseText = await reader.ReadToEndAsync();
            Assert.Contains("Too many requests", responseText);
        }

        [Fact]
        public async Task InvokeAsync_DefaultEndpoint_UsesDefaultLimits()
        {
            // Arrange
            _httpContext.Request.Path = "/api/default";
            
            // No endpoint match will be found
            _mockEndpointMatcher.Setup(m => m.IsMatch(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(false);
                
            var middleware = new RateLimiterMiddleware(
                _mockNext.Object,
                _mockLogger.Object,
                _mockAlgorithm.Object,
                _mockEndpointMatcher.Object,
                _mockOptions.Object);
                
            // Act
            await middleware.InvokeAsync(_httpContext);
            
            // Assert
            _mockAlgorithm.Verify(a => a.ShouldLimitRequest(
                It.IsAny<string>(),
                "__default__", // Default path marker
                _options.DefaultRequestLimitMs, // Default time window
                _options.DefaultRequestLimitCount), // Default count limit
                Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_SpecificEndpoint_UsesEndpointSpecificLimits()
        {
            // Arrange
            string specificEndpoint = "/api/limited";
            _httpContext.Request.Path = specificEndpoint;
            
            // Set up endpoint matcher to find a match
            _mockEndpointMatcher.Setup(m => m.IsMatch(specificEndpoint, specificEndpoint))
                .Returns(true);
                
            var endpointLimit = _options.EndpointLimits[0]; // The /api/limited endpoint
                
            var middleware = new RateLimiterMiddleware(
                _mockNext.Object,
                _mockLogger.Object,
                _mockAlgorithm.Object,
                _mockEndpointMatcher.Object,
                _mockOptions.Object);
                
            // Act
            await middleware.InvokeAsync(_httpContext);
            
            // Assert
            _mockAlgorithm.Verify(a => a.ShouldLimitRequest(
                It.IsAny<string>(),
                endpointLimit.Endpoint, // Specific endpoint path 
                endpointLimit.RequestLimitMs, // Endpoint-specific time window
                endpointLimit.RequestLimitCount), // Endpoint-specific count limit
                Times.Once);
        }
    }
}
