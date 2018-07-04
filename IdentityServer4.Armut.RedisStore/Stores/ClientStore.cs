using System;
using System.Threading.Tasks;
using IdentityServer4.Armut.RedisStore.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace IdentityServer4.Armut.RedisStore.Stores
{
    public class ClientStore : IClientStore, IAidRedisPersistence
    {
        private readonly RedisConfigurationStoreOptions options;
        private readonly IDatabase database;
        private readonly ILogger<PersistedGrantStore> logger;
        private ISystemClock clock;

        public ClientStore(RedisMultiplexer<RedisConfigurationStoreOptions> multiplexer, ILogger<PersistedGrantStore> logger, ISystemClock clock)
        {
            if (multiplexer is null)
                throw new ArgumentNullException(nameof(multiplexer));
            options = multiplexer.RedisOptions;
            database = multiplexer.Database;
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.clock = clock;
        }

        public async Task<Client> FindClientByIdAsync(string clientId)
        {
            try
            {
                var raw = await database.StringGetAsync(KeyForClient(clientId)).ConfigureAwait(false);
                var client = raw.Deserialize<Client>();
                return client;
            }
            catch (Exception e)
            {
                logger.LogWarning($"clientid not found : {clientId} - ex:{e.Message}");
                return null;
            }
        }

        public async void SaveAsync(string key, object data)
        {
            await database.StringSetAsync(key, data.Serialize()).ConfigureAwait(false);
        }

        private string KeyForClient(string key) => $"{options.KeyPrefixClient}{key}";
    }
}
