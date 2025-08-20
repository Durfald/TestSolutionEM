using ApiServerWarframe.Services.Storage.MemoryStorage;
using Moq;
using TestSolution.Models;
using TestSolution.Services.Ads;
using TestSolution.Services.Parser;
using Xunit;

namespace TestSolution.Tests
{
    public class AdvertisingServiceTests
    {
        [Fact]
        public void LoadFromFile_ShouldStorePlatformsInCache()
        {
            // Arrange
            var mockParser = new Mock<IFileParser>();
            var mockCache = new MemoryCacheService(new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));
            var mockLogger = new Mock<ILogger<AdvertisingService>>();

            mockParser.Setup(p => p.ParseFromFile(It.IsAny<string>()))
                .Returns(new List<AdvertisingData>
                {
                    new AdvertisingData { AdvertisingPlatform = "Яндекс.Директ", Locations = new [] { "/ru" } },
                    new AdvertisingData { AdvertisingPlatform = "Крутая реклама", Locations = new [] { "/ru/svrd" } }
                });

            var service = new AdvertisingService(mockParser.Object, mockCache, mockLogger.Object);

            // Act
            service.LoadFromFile("fakepath.txt");

            // Assert
            var ruPlatforms = mockCache.Get<List<string>>("adplatforms:/ru");
            var svrdPlatforms = mockCache.Get<List<string>>("adplatforms:/ru/svrd");

            Assert.NotNull(ruPlatforms);
            Assert.Single(ruPlatforms);
            Assert.Contains("Яндекс.Директ", ruPlatforms);

            Assert.NotNull(svrdPlatforms);
            Assert.Single(svrdPlatforms);
            Assert.Contains("Крутая реклама", svrdPlatforms);
        }

        [Fact]
        public void Search_ShouldReturnPlatformsForLocationAndParents()
        {
            // Arrange
            var mockParser = new Mock<IFileParser>();
            var mockCache = new MemoryCacheService(new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));
            var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<AdvertisingService>>();

            // Наполняем кэш вручную
            mockCache.Set("adplatforms:/ru", new List<string> { "Яндекс.Директ" });
            mockCache.Set("adplatforms:/ru/svrd", new List<string> { "Крутая реклама" });
            mockCache.Set("adplatforms:/ru/svrd/revda", new List<string> { "Ревдинский рабочий" });

            var service = new AdvertisingService(mockParser.Object, mockCache, mockLogger.Object);

            // Act
            var result = service.Search("/ru/svrd/revda");

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains("Яндекс.Директ", result);
            Assert.Contains("Крутая реклама", result);
            Assert.Contains("Ревдинский рабочий", result);
        }
    }
}
