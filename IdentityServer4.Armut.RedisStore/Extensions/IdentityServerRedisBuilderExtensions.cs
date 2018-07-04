using System;
using IdentityServer4.Armut.RedisStore.Cache;
using IdentityServer4.Armut.RedisStore.Extensions;
using IdentityServer4.Armut.RedisStore.Stores;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IdentityServerRedisBuilderExtensions
    {
        /// <summary>
        /// Add Redis Operational Store.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="optionsBuilder">Redis Operational Store Options builder</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddOperationalStore(this IIdentityServerBuilder builder, Action<RedisOperationalStoreOptions> optionsBuilder)
        {
            var options = new RedisOperationalStoreOptions();
            optionsBuilder?.Invoke(options);
            builder.Services.AddSingleton(options);

            builder.Services.AddScoped<RedisMultiplexer<RedisOperationalStoreOptions>>();
            builder.Services.AddTransient<IPersistedGrantStore, PersistedGrantStore>();
            return builder;
        }

        /// <summary>
        /// Add Redis Configuration Store.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="optionsBuilder">Redis Configuration Store Options builder</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddConfigurationStore(this IIdentityServerBuilder builder, Action<RedisConfigurationStoreOptions> optionsBuilder)
        {
            var options = new RedisConfigurationStoreOptions();
            optionsBuilder?.Invoke(options);
            builder.Services.AddSingleton(options);

            builder.Services.AddScoped<RedisMultiplexer<RedisConfigurationStoreOptions>>();
            builder.Services.AddTransient<IClientStore, ClientStore>();
            builder.Services.AddTransient<IResourceStore, ResourceStore>();
            return builder;
        }

        /// <summary>
        /// Add Redis caching that implements ICache<typeparamref name="T"/>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="optionsBuilder">Redis Cache Options builder</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddRedisCaching(this IIdentityServerBuilder builder, Action<RedisCacheOptions> optionsBuilder)
        {
            var options = new RedisCacheOptions();
            optionsBuilder?.Invoke(options);
            builder.Services.AddSingleton(options);

            builder.Services.AddScoped<RedisMultiplexer<RedisCacheOptions>>();
            builder.Services.AddTransient(typeof(ICache<>), typeof(RedisCache<>));
            return builder;
        }

        ///<summary>
        /// Add Redis caching for IProfileService Implementation
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="optionsBuilder">Profile Service Redis Cache Options builder</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddProfileServiceCache<TProfileService>(this IIdentityServerBuilder builder, Action<ProfileServiceCachingOptions<TProfileService>> optionsBuilder = null)
        where TProfileService : class, IProfileService
        {
            var options = new ProfileServiceCachingOptions<TProfileService>();
            optionsBuilder?.Invoke(options);
            builder.Services.AddSingleton(options);

            builder.Services.TryAddTransient(typeof(TProfileService));
            builder.Services.AddTransient<IProfileService, CachingProfileService<TProfileService>>();
            return builder;
        }
    }
}
