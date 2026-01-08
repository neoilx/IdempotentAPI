using System;
using System.Collections.Generic;
using System.Text.Json;

namespace IdempotentAPI.Core
{
    public class IdempotencyOptions : IIdempotencyOptions
    {
        private TimeSpan _expiresIn = DefaultIdempotencyOptions.ExpiresIn;
        private bool _cacheOnlySuccessResponses = DefaultIdempotencyOptions.CacheOnlySuccessResponses;
        private bool _isIdempotencyOptional = DefaultIdempotencyOptions.IsIdempotencyOptional;

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

        public JsonSerializerOptions? SerializerOptions { get; set; }

        ///<inheritdoc/>
        public List<Type>? ExcludeRequestSpecialTypes { get; set; }

        ///<inheritdoc/>
        public bool UseProblemDetailsForErrors { get; set; } = DefaultIdempotencyOptions.UseProblemDetailsForErrors;
    }
}
