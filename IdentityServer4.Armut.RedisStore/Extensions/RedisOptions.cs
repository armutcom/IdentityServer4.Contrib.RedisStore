using System;
using StackExchange.Redis;

namespace IdentityServer4.Armut.RedisStore.Extensions
{
    /// <summary>
    /// Represents Redis general options.
    /// </summary>
    public abstract class RedisOptions
    {
        /// <summary>
        ///Configuration options objects for StackExchange.Redis Library.
        /// </summary>
        public ConfigurationOptions ConfigurationOptions { get; set; }

        /// <summary>
        /// Connection String for connecting to Redis Instance.
        /// </summary>
        public string RedisConnectionString { get; set; }

        /// <summary>
        ///The specific Db number to connect to, default is -1.
        /// </summary>
        public int Db { get; set; } = -1;

        private string _keyPrefix = string.Empty;

        /// <summary>
        /// The Prefix to add to each key stored on Redis Cache, default is Empty.
        /// </summary>
        public string KeyPrefix
        {
            get
            {
                return string.IsNullOrEmpty(_keyPrefix) ? _keyPrefix : $"{_keyPrefix}:";
            }
            set
            {
                _keyPrefix = value;
            }
        }

        internal RedisOptions()
        {
            multiplexer = GetConnectionMultiplexer();
        }

        private Lazy<IConnectionMultiplexer> GetConnectionMultiplexer()
        {
            return new Lazy<IConnectionMultiplexer>(
                () => string.IsNullOrEmpty(RedisConnectionString)
                    ? ConnectionMultiplexer.Connect(ConfigurationOptions)
                    : ConnectionMultiplexer.Connect(RedisConnectionString));
        }

        private Lazy<IConnectionMultiplexer> multiplexer = null;

        internal IConnectionMultiplexer Multiplexer => multiplexer.Value;
    }

    /// <summary>
    /// Represents Redis Operational store options.
    /// </summary>
    public class RedisOperationalStoreOptions : RedisOptions
    {
        
    }

    /// <summary>
    /// Represents Redis Operational store options.
    /// </summary>
    public class RedisConfigurationStoreOptions : RedisOptions
    {
        private string _keyPrefixIdentity = string.Empty;
        private string _keyPrefixApi = string.Empty;
        private string _keyPrefixClient = string.Empty;

        public string KeyPrefixIdentity
        {
            get => string.IsNullOrEmpty(_keyPrefixIdentity) ? _keyPrefixIdentity : $"{_keyPrefixIdentity}:";
            set => _keyPrefixIdentity = value;
        }

        public string KeyPrefixApi
        {
            get => string.IsNullOrEmpty(_keyPrefixApi) ? _keyPrefixApi : $"{_keyPrefixApi}:";
            set => _keyPrefixApi = value;
        }

        public string KeyPrefixClient
        {
            get => string.IsNullOrEmpty(_keyPrefixClient) ? _keyPrefixClient : $"{_keyPrefixClient}:";
            set => _keyPrefixClient = value;
        }
    }

    /// <summary>
    /// Represents Redis Cache options.
    /// </summary>
    public class RedisCacheOptions : RedisOptions
    {

    }
}
