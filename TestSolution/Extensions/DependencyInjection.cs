using ApiServerWarframe.Services.Storage.MemoryStorage;
using TestSolution.Services.Ads;
using TestSolution.Services.Parser;

namespace TestSolution.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDI(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddSingleton<MemoryCacheService>();
            services.AddSingleton<IFileParser, FileParser>();
            services.AddSingleton<IAdvertisingService, AdvertisingService>();

            return services;
        }
    }
}
