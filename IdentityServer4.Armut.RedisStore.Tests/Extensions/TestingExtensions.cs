using IdentityServer4.Armut.RedisStore.Tests.Fakes;
using IdentityServer4.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class TestingExtensions
    {

        public static IIdentityServerBuilder AddFakeMemeoryCaching(this IIdentityServerBuilder builder)
        {
            builder.Services.AddSingleton<IMemoryCache>(new MemoryCache(new MemoryCacheOptions()));
            builder.Services.AddScoped(typeof(ICache<>), typeof(FakeCache<>));
            return builder;
        }

        public static IIdentityServerBuilder AddFakeLogger<T>(this IIdentityServerBuilder builder)
        {
            builder.Services.AddSingleton(new FakeLogger<T>());
            return builder;
        }
    }
}
