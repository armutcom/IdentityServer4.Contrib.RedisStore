using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityServer4.Armut.RedisStore.Cache;
using IdentityServer4.Armut.RedisStore.Extensions;
using IdentityServer4.Armut.RedisStore.Tests.Fakes;
using IdentityServer4.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IdentityServer4.Armut.RedisStore.Tests.UnitTesting.Cache
{
    public class CachingProfileServiceTests
    {
        private readonly FakeProfileService inner;
        private readonly FakeCache<IsActiveContextCacheEntry> cache;
        private readonly FakeLogger<FakeCache<IsActiveContextCacheEntry>> logger;
        private readonly CachingProfileService<FakeProfileService> profileServiceCache;
        private readonly IMemoryCache memoryCache;

        public CachingProfileServiceTests()
        {
            inner = new FakeProfileService();
            memoryCache = new MemoryCache(new MemoryCacheOptions());
            logger = new FakeLogger<FakeCache<IsActiveContextCacheEntry>>();
            cache = new FakeCache<IsActiveContextCacheEntry>(memoryCache, logger);
            profileServiceCache = new CachingProfileService<FakeProfileService>(inner, cache, new ProfileServiceCachingOptions<FakeProfileService>(), Mock.Of<ILogger<CachingProfileService<FakeProfileService>>>());
        }

        [Fact]
        public async Task AssertHitingDataStoreAtLeastOnce()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim("sub", "1") }));
            var context = new IsActiveContext(principal, new Client(), "test");
            await profileServiceCache.IsActiveAsync(context);
            await profileServiceCache.IsActiveAsync(context);
            await profileServiceCache.IsActiveAsync(context);
            context.IsActive.Should().BeTrue();
            logger.AccessCount["Cache hit for 1"].Should().Equals(2);
        }

        [Fact]
        public async Task AssertIsInactive()
        {
            inner.IsActive = cxt => cxt.IsActive = false;
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim("sub", "1") }));
            var context = new IsActiveContext(principal, new Client(), "test");
            await profileServiceCache.IsActiveAsync(context);
            context.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task AssertExpiryOfCacheEntry()
        {
            var profileServiceCache = new CachingProfileService<FakeProfileService>(inner, cache, new ProfileServiceCachingOptions<FakeProfileService>() { Expiration = TimeSpan.FromSeconds(1) }, Mock.Of<ILogger<CachingProfileService<FakeProfileService>>>());
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim("sub", "1") }));
            var context = new IsActiveContext(principal, new Client(), "test");
            await profileServiceCache.IsActiveAsync(context);
            await profileServiceCache.IsActiveAsync(context);
            Thread.Sleep(1000);
            await profileServiceCache.IsActiveAsync(context);
            await profileServiceCache.IsActiveAsync(context);
            context.IsActive.Should().BeTrue();
            logger.AccessCount["Cache hit for 1"].Should().Equals(2);
        }
    }
}
