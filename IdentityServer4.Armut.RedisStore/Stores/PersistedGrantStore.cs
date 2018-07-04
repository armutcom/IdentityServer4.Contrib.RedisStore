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
    /// <summary>
    /// Provides the implementation of IPersistedGrantStore for Redis Cache.
    /// </summary>
    public class PersistedGrantStore : IPersistedGrantStore
    {
        private readonly RedisOperationalStoreOptions options;
        private readonly IDatabase database;
        private readonly ILogger<PersistedGrantStore> logger;
        private ISystemClock clock;

        public PersistedGrantStore(RedisMultiplexer<RedisOperationalStoreOptions> multiplexer, ILogger<PersistedGrantStore> logger, ISystemClock clock)
        {
            if (multiplexer is null)
                throw new ArgumentNullException(nameof(multiplexer));
            options = multiplexer.RedisOptions;
            database = multiplexer.Database;
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.clock = clock;
        }

        public async Task StoreAsync(PersistedGrant grant)
        {
            if (grant == null)
                throw new ArgumentNullException(nameof(grant));
            try
            {
                var data = grant.Serialize();
                var grantKey = GetKey(grant.Key);
                var expiresIn = grant.Expiration - clock.UtcNow;
                if (!string.IsNullOrEmpty(grant.SubjectId))
                {
                    var setKey = GetSetKey(grant.SubjectId, grant.ClientId, grant.Type);
                    var setKeyforSubject = GetSetKey(grant.SubjectId);
                    var setKeyforClient = GetSetKey(grant.SubjectId, grant.ClientId);

                    //get keys to clean, if any
                    var (_, keysToDelete) = await GetGrants(setKeyforSubject).ConfigureAwait(false);

                    var transaction = database.CreateTransaction();
                    transaction.StringSetAsync(grantKey, data, expiresIn);
                    transaction.SetAddAsync(setKeyforSubject, grantKey);
                    transaction.SetAddAsync(setKeyforClient, grantKey);
                    transaction.SetAddAsync(setKey, grantKey);
                    transaction.KeyExpireAsync(setKey, expiresIn);

                    if (keysToDelete.Any())//cleanup sets while persisting new grant
                    {
                        transaction.SetRemoveAsync(setKey, keysToDelete.ToArray());
                        transaction.SetRemoveAsync(setKeyforSubject, keysToDelete.ToArray());
                        transaction.SetRemoveAsync(setKeyforClient, keysToDelete.ToArray());
                    }
                    await transaction.ExecuteAsync().ConfigureAwait(false);
                }
                else
                {
                    await database.StringSetAsync(grantKey, data, expiresIn).ConfigureAwait(false);
                }
                logger.LogDebug($"grant for subject {grant.SubjectId}, clientId {grant.ClientId}, grantType {grant.Type} persisted successfully");
            }
            catch (Exception ex)
            {
                logger.LogWarning($"exception storing persisted grant to Redis database for subject {grant.SubjectId}, clientId {grant.ClientId}, grantType {grant.Type} : {ex.Message}");
            }

        }

        public async Task<PersistedGrant> GetAsync(string key)
        {
            var data = await database.StringGetAsync(GetKey(key)).ConfigureAwait(false);
            logger.LogDebug($"{key} found in database: {data.HasValue}");
            return data.HasValue ? data.Deserialize<PersistedGrant>() : null;
        }

        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            var setKey = GetSetKey(subjectId);
            var (grants, keysToDelete) = await GetGrants(setKey).ConfigureAwait(false);
            if (keysToDelete.Any())
                await database.SetRemoveAsync(setKey, keysToDelete.ToArray()).ConfigureAwait(false);
            logger.LogDebug($"{grants.Count()} persisted grants found for {subjectId}");
            return grants.Where(_ => _.HasValue).Select(_ => _.Deserialize<PersistedGrant>());
        }

        private async Task<(IEnumerable<RedisValue> grants, IEnumerable<RedisValue> keysToDelete)> GetGrants(string setKey)
        {
            var grantsKeys = await database.SetMembersAsync(setKey).ConfigureAwait(false);
            if (!grantsKeys.Any())
                return (Enumerable.Empty<RedisValue>(), Enumerable.Empty<RedisValue>());
            var grants = await database.StringGetAsync(grantsKeys.Select(_ => (RedisKey)_.ToString()).ToArray()).ConfigureAwait(false);
            var keysToDelete = grantsKeys.Zip(grants, (key, value) => new KeyValuePair<RedisValue, RedisValue>(key, value))
                                         .Where(_ => !_.Value.HasValue).Select(_ => _.Key);
            return (grants, keysToDelete);
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                var grant = await GetAsync(key).ConfigureAwait(false);
                if (grant == null)
                {
                    logger.LogDebug($"no {key} persisted grant found in database");
                    return;
                }
                var grantKey = GetKey(key);
                logger.LogDebug($"removing {key} persisted grant from database");
                var transaction = database.CreateTransaction();
                transaction.KeyDeleteAsync(grantKey);
                transaction.SetRemoveAsync(GetSetKey(grant.SubjectId), grantKey);
                transaction.SetRemoveAsync(GetSetKey(grant.SubjectId, grant.ClientId), grantKey);
                transaction.SetRemoveAsync(GetSetKey(grant.SubjectId, grant.ClientId, grant.Type), grantKey);
                await transaction.ExecuteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogInformation($"exception removing {key} persisted grant from database: {ex.Message}");
            }

        }

        public async Task RemoveAllAsync(string subjectId, string clientId)
        {
            try
            {
                var setKey = GetSetKey(subjectId, clientId);
                var grantsKeys = await database.SetMembersAsync(setKey).ConfigureAwait(false);
                logger.LogDebug($"removing {grantsKeys.Count()} persisted grants from database for subject {subjectId}, clientId {clientId}");
                if (!grantsKeys.Any()) return;
                var transaction = database.CreateTransaction();
                transaction.KeyDeleteAsync(grantsKeys.Select(_ => (RedisKey)_.ToString()).Concat(new RedisKey[] { setKey }).ToArray());
                transaction.SetRemoveAsync(GetSetKey(subjectId), grantsKeys);
                await transaction.ExecuteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogInformation($"removing persisted grants from database for subject {subjectId}, clientId {clientId}: {ex.Message}");
            }
        }

        public async Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            try
            {
                var setKey = GetSetKey(subjectId, clientId, type);
                var grantsKeys = await database.SetMembersAsync(setKey).ConfigureAwait(false);
                logger.LogDebug($"removing {grantsKeys.Count()} persisted grants from database for subject {subjectId}, clientId {clientId}, grantType {type}");
                if (!grantsKeys.Any()) return;
                var transaction = database.CreateTransaction();
                transaction.KeyDeleteAsync(grantsKeys.Select(_ => (RedisKey)_.ToString()).Concat(new RedisKey[] { setKey }).ToArray());
                transaction.SetRemoveAsync(GetSetKey(subjectId, clientId), grantsKeys);
                transaction.SetRemoveAsync(GetSetKey(subjectId), grantsKeys);
                await transaction.ExecuteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogInformation($"exception removing persisted grants from database for subject {subjectId}, clientId {clientId}, grantType {type}: {ex.Message}");
            }
        }

        

        private string GetKey(string key) => $"{options.KeyPrefix}{key}";

        private string GetSetKey(string subjectId) => $"{options.KeyPrefix}{subjectId}";

        private string GetSetKey(string subjectId, string clientId) => $"{options.KeyPrefix}{subjectId}:{clientId}";

        private string GetSetKey(string subjectId, string clientId, string type) => $"{options.KeyPrefix}{subjectId}:{clientId}:{type}";


    }
}