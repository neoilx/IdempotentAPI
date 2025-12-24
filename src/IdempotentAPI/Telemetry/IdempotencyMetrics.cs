#nullable enable
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace IdempotentAPI.Telemetry
{
    /// <summary>
    /// Implementation of <see cref="IIdempotencyMetrics"/> using System.Diagnostics.Metrics.
    /// These metrics are automatically compatible with OpenTelemetry when you add the "IdempotentAPI" meter.
    /// </summary>
    public sealed class IdempotencyMetrics : IIdempotencyMetrics
    {
        private readonly Counter<long> _cacheHits;
        private readonly Counter<long> _cacheMisses;
        private readonly Counter<long> _cacheStores;
        private readonly Counter<long> _lockFailures;
        private readonly Counter<long> _hashMismatches;
        private readonly Counter<long> _nonSuccessSkips;
        private readonly Counter<long> _idempotencyCancelled;

        /// <summary>
        /// Creates a new instance using the default IdempotentAPI meter.
        /// </summary>
        public IdempotencyMetrics() : this(IdempotencyMeterProvider.Meter)
        {
        }

        /// <summary>
        /// Creates a new instance with a custom Meter (useful for testing).
        /// </summary>
        /// <param name="meter">The meter to use for creating instruments.</param>
        public IdempotencyMetrics(Meter meter)
        {
            _cacheHits = meter.CreateCounter<long>(
                "idempotentapi.cache.hits",
                unit: "{hit}",
                description: "Number of idempotent cache hits (responses served from cache)");

            _cacheMisses = meter.CreateCounter<long>(
                "idempotentapi.cache.misses",
                unit: "{miss}",
                description: "Number of idempotent cache misses (new request processing)");

            _cacheStores = meter.CreateCounter<long>(
                "idempotentapi.cache.stores",
                unit: "{store}",
                description: "Number of responses stored in idempotency cache");

            _lockFailures = meter.CreateCounter<long>(
                "idempotentapi.lock.failures",
                unit: "{failure}",
                description: "Number of distributed lock acquisition failures");

            _hashMismatches = meter.CreateCounter<long>(
                "idempotentapi.hash.mismatches",
                unit: "{mismatch}",
                description: "Number of hash mismatches (same idempotency key, different request data)");

            _nonSuccessSkips = meter.CreateCounter<long>(
                "idempotentapi.cache.skips",
                unit: "{skip}",
                description: "Number of responses not cached due to non-success status code");

            _idempotencyCancelled = meter.CreateCounter<long>(
                "idempotentapi.cancelled",
                unit: "{cancellation}",
                description: "Number of idempotency operations cancelled due to exceptions");
        }

        /// <inheritdoc />
        public void RecordCacheHit(string httpMethod, string path)
        {
            _cacheHits.Add(1,
                new KeyValuePair<string, object?>("http.request.method", httpMethod),
                new KeyValuePair<string, object?>("url.path", path));
        }

        /// <inheritdoc />
        public void RecordCacheMiss(string httpMethod, string path)
        {
            _cacheMisses.Add(1,
                new KeyValuePair<string, object?>("http.request.method", httpMethod),
                new KeyValuePair<string, object?>("url.path", path));
        }

        /// <inheritdoc />
        public void RecordCacheStore(string httpMethod, string path)
        {
            _cacheStores.Add(1,
                new KeyValuePair<string, object?>("http.request.method", httpMethod),
                new KeyValuePair<string, object?>("url.path", path));
        }

        /// <inheritdoc />
        public void RecordLockFailure(string operation)
        {
            _lockFailures.Add(1,
                new KeyValuePair<string, object?>("idempotentapi.operation", operation));
        }

        /// <inheritdoc />
        public void RecordHashMismatch()
        {
            _hashMismatches.Add(1);
        }

        /// <inheritdoc />
        public void RecordNonSuccessSkip(string httpMethod, string path, int statusCode)
        {
            _nonSuccessSkips.Add(1,
                new KeyValuePair<string, object?>("http.request.method", httpMethod),
                new KeyValuePair<string, object?>("url.path", path),
                new KeyValuePair<string, object?>("http.response.status_code", statusCode));
        }

        /// <inheritdoc />
        public void RecordIdempotencyCancelled()
        {
            _idempotencyCancelled.Add(1);
        }
    }
}
