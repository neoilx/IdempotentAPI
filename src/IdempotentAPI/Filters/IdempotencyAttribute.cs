using System;
using System.Collections.Generic;
using System.Text.Json;
using IdempotentAPI.AccessCache;
using IdempotentAPI.Core;
using IdempotentAPI.Telemetry;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IdempotentAPI.Filters
{
    /// <summary>
    /// Use Idempotent operations on POST, PUT, PATCH and DELETE HTTP methods
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class IdempotentAttribute : Attribute, IFilterFactory, IIdempotencyOptions
    {
        private TimeSpan _expiresIn = DefaultIdempotencyOptions.ExpiresIn;
        private bool _cacheOnlySuccessResponses = DefaultIdempotencyOptions.CacheOnlySuccessResponses;
        private bool _isIdempotencyOptional = DefaultIdempotencyOptions.IsIdempotencyOptional;

        public bool IsReusable => false;

        public bool Enabled { get; set; } = true;

        ///<inheritdoc/>
        public int ExpireHours
        {
            get => Convert.ToInt32(_expiresIn.TotalHours);
            set => _expiresIn = TimeSpan.FromHours(value);
        }

        ///<inheritdoc/>
        public double ExpiresInMilliseconds
        {
            get => _expiresIn.TotalMilliseconds;
            set => _expiresIn = TimeSpan.FromMilliseconds(value);
        }

        ///<inheritdoc/>
        public string DistributedCacheKeysPrefix { get; set; } = DefaultIdempotencyOptions.DistributedCacheKeysPrefix;

        ///<inheritdoc/>
        public string HeaderKeyName { get; set; } = DefaultIdempotencyOptions.HeaderKeyName;

        ///<inheritdoc/>
        public bool CacheOnlySuccessResponses
        {
            get => _cacheOnlySuccessResponses;
            set
            {
                _cacheOnlySuccessResponses = value;
                CacheOnlySuccessResponsesSpecified = true;
            }
        }

        ///<inheritdoc/>
        public bool CacheOnlySuccessResponsesSpecified { get; private set; }

        ///<inheritdoc/>
        public double DistributedLockTimeoutMilli { get; set; } = DefaultIdempotencyOptions.DistributedLockTimeoutMilli;

        ///<inheritdoc/>
        public bool IsIdempotencyOptional
        {
            get => _isIdempotencyOptional;
            set
            {
                _isIdempotencyOptional = value;
                IsIdempotencyOptionalSpecified = true;
            }
        }

        ///<inheritdoc/>
        public bool IsIdempotencyOptionalSpecified { get; private set; }

        /// <summary>
        /// By default, idempotency settings are taken from the attribute properties.
        /// When this flag is set to true, the settings will be taken from the registered <see cref="IIdempotencyOptions"/> in the ServiceCollection
        /// </summary>
        public bool UseIdempotencyOption { get; set; } = false;

        public JsonSerializerOptions? SerializerOptions { get => null; set => throw new NotImplementedException(); }
        public List<Type>? ExcludeRequestSpecialTypes { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        ///<inheritdoc/>
        public bool UseProblemDetailsForErrors { get; set; } = DefaultIdempotencyOptions.UseProblemDetailsForErrors;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var distributedCache = (IIdempotencyAccessCache)serviceProvider.GetService(typeof(IIdempotencyAccessCache));
            var loggerFactory = (ILoggerFactory)serviceProvider.GetService(typeof(ILoggerFactory));
            var metrics = serviceProvider.GetService<IIdempotencyMetrics>();

            var generalIdempotencyOptions = serviceProvider.GetRequiredService<IIdempotencyOptions>();
            var idempotencyOptions = UseIdempotencyOption ? generalIdempotencyOptions : this;

            TimeSpan? distributedLockTimeout = idempotencyOptions.DistributedLockTimeoutMilli >= 0
                ? TimeSpan.FromMilliseconds(idempotencyOptions.DistributedLockTimeoutMilli)
                : null;

            // When UseIdempotencyOption is true, use global options as the base,
            // but allow attribute-level overrides for explicitly set properties.
            var cacheOnlySuccessResponses = UseIdempotencyOption && !CacheOnlySuccessResponsesSpecified
                ? generalIdempotencyOptions.CacheOnlySuccessResponses
                : CacheOnlySuccessResponses;

            var isIdempotencyOptional = UseIdempotencyOption && !IsIdempotencyOptionalSpecified
                ? generalIdempotencyOptions.IsIdempotencyOptional
                : IsIdempotencyOptional;

            // When UseIdempotencyOption is true, use global options; otherwise use attribute-level settings
            var useProblemDetailsForErrors = UseIdempotencyOption
                ? generalIdempotencyOptions.UseProblemDetailsForErrors
                : UseProblemDetailsForErrors;

            return new IdempotencyAttributeFilter(
                distributedCache,
                loggerFactory,
                Enabled,
                idempotencyOptions.ExpiresInMilliseconds,
                idempotencyOptions.HeaderKeyName,
                idempotencyOptions.DistributedCacheKeysPrefix,
                distributedLockTimeout,
                cacheOnlySuccessResponses,
                isIdempotencyOptional,
                generalIdempotencyOptions.SerializerOptions,
                useProblemDetailsForErrors,
                metrics);
        }
    }
}
