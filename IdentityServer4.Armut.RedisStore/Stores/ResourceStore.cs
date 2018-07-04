using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Armut.RedisStore.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace IdentityServer4.Armut.RedisStore.Stores
{
    public class ResourceStore : IResourceStore, IAidRedisPersistence
    {
        private readonly RedisConfigurationStoreOptions options;
        private readonly IDatabase database;
        private readonly ILogger<PersistedGrantStore> logger;
        private readonly IServer server;
        private ISystemClock clock;

        public ResourceStore(RedisMultiplexer<RedisConfigurationStoreOptions> multiplexer, ILogger<PersistedGrantStore> logger, ISystemClock clock)
        {
            if (multiplexer is null)
                throw new ArgumentNullException(nameof(multiplexer));
            options = multiplexer.RedisOptions;
            database = multiplexer.Database;
            server = database.Multiplexer.GetServer(multiplexer.Database.IdentifyEndpoint());
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.clock = clock;
        }

        public async Task<ApiResource> FindApiResourceAsync(string name)
        {
            var apiList = await GetAllApiResources();
            var found = apiList.FirstOrDefault(a => a.Name == name);
            return found;
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var apiList = await GetAllApiResources();
            var found = apiList.Where(a => a.Scopes.Any(s => scopeNames.Contains(s.Name)));
            return found;
        }

        public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var identityList = await GetAllIdentityResources();
            var found = identityList.Where(id => scopeNames.Contains(id.Name));
            return found;
        }

        public async Task<Resources> GetAllResourcesAsync()
        {
            var apiList = await GetAllApiResources();
            var identityList = await GetAllIdentityResources();

            var resources = new Resources(identityList, apiList);
            return resources;
        }

        public async void SaveAsync(string key, object data)
        {
            await database.StringSetAsync(key, data.Serialize()).ConfigureAwait(false);
        }

        private async Task<IEnumerable<IdentityResource>> GetAllIdentityResources()
        {
            var keyIdentityResources = server.Keys(database.Database, KeyForIdentityResource("*")).Select(r => r.ToString());

            var list = new List<RedisValue>();

            foreach (var key in keyIdentityResources)
            {
                var raw = await database.StringGetAsync(key).ConfigureAwait(false);
                list.Add(raw);
            }

            return list.Deserialize<IdentityResource>();
        }

        private async Task<IEnumerable<ApiResource>> GetAllApiResources()
        {
            var keyApiResources = server.Keys(database.Database, KeyForApiResource("*")).Select(r => r.ToString());

            var list = new List<RedisValue>();

            foreach (var key in keyApiResources)
            {
                var raw = await database.StringGetAsync(key).ConfigureAwait(false);
                list.Add(raw);
            }

            return list.Deserialize<ApiResource>();
        }


        private string KeyForApiResource(string key) => $"{options.KeyPrefixApi}{key}";

        private string KeyForIdentityResource(string key) => $"{options.KeyPrefixIdentity}{key}";

    }
}
