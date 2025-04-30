using System;
using System.Threading;
using RateLimiter.Storage;

namespace RateLimiter.Tests.Storage
{
    public class InMemoryRequestStoreTests
    {
        private readonly InMemoryRequestStore _store;

        public InMemoryRequestStoreTests()
        {
            _store = new InMemoryRequestStore();
        }

        [Fact]
        public void IncrementRequestCount_NewKey_ReturnsOne()
        {
            // Arrange
            string key = "test-key-" + Guid.NewGuid().ToString();

            // Act
            long result = _store.IncrementRequestCount(key);

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public void IncrementRequestCount_ExistingKey_IncrementsCount()
        {
            // Arrange
            string key = "test-key-" + Guid.NewGuid().ToString();
            
            // Call once to create initial entry
            _store.IncrementRequestCount(key);

            // Act
            long result = _store.IncrementRequestCount(key);

            // Assert
            Assert.Equal(2, result);
        }

        [Fact]
        public void IncrementRequestCount_MultipleIncrements_CountsCorrectly()
        {
            // Arrange
            string key = "test-key-" + Guid.NewGuid().ToString();
            int incrementCount = 5;

            // Act
            long lastResult = 0;
            for (int i = 0; i < incrementCount; i++)
            {
                lastResult = _store.IncrementRequestCount(key);
            }

            // Assert
            Assert.Equal(incrementCount, lastResult);
        }

        [Fact]
        public void Cleanup_RemovesOldEntries()
        {
            // Arrange
            string oldKey = "old-key-" + Guid.NewGuid().ToString();
            string newKey = "new-key-" + Guid.NewGuid().ToString();
            
            // Create a key and increment it (this is current)
            _store.IncrementRequestCount(newKey);
            
            // Since we can't directly modify timestamps, create a key
            // that we'll check after cleanup to verify behavior indirectly
            _store.IncrementRequestCount(oldKey);
            
            // Sleep to ensure enough time passes before cleanup
            Thread.Sleep(100);
            
            // Act - cleanup with a very small window (1ms)
            // This should remove entries older than 1ms
            _store.Cleanup(1);
            
            // Verify the old key is gone by seeing if incrementing returns 1 again
            long oldKeyCount = _store.IncrementRequestCount(oldKey);
            
            // Assert
            Assert.Equal(1, oldKeyCount);
            // The old entry should have been removed during cleanup
        }

        [Fact]
        public void Cleanup_KeepsNewEntries()
        {
            // Arrange
            string key = "new-key-" + Guid.NewGuid().ToString();
            
            // Add a new entry
            _store.IncrementRequestCount(key);
            
            // Act - use a large window so all entries should be kept
            _store.Cleanup(1000000); // Very large window (1000 seconds)
            
            // If the entry was kept, increment should return 2
            long count = _store.IncrementRequestCount(key);
            
            // Assert
            Assert.Equal(2, count);
            // New entry should be kept after cleanup
        }

        [Fact]
        public void Cleanup_NoEntries_DoesNotThrowException()
        {
            // Act & Assert - should not throw
            _store.Cleanup(60000);
        }

        [Fact]
        public void IncrementRequestCount_ConcurrentAccess_CountsCorrectly()
        {
            // Arrange
            string key = "concurrent-key-" + Guid.NewGuid().ToString();
            int threadCount = 10;
            int incrementsPerThread = 100; // Reduced to make test run faster
            int expectedTotal = threadCount * incrementsPerThread;
            
            Thread[] threads = new Thread[threadCount];
            
            // Act
            for (int i = 0; i < threadCount; i++)
            {
                threads[i] = new Thread(() =>
                {
                    for (int j = 0; j < incrementsPerThread; j++)
                    {
                        _store.IncrementRequestCount(key);
                    }
                });
                threads[i].Start();
            }
            
            // Wait for all threads to complete
            foreach (var thread in threads)
            {
                thread.Join();
            }
            
            // Check the final count
            long finalCount = _store.IncrementRequestCount(key);
            
            // Assert
            Assert.Equal(expectedTotal + 1, finalCount); // +1 for the final increment
        }
    }
}
