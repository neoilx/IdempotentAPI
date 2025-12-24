#nullable enable
namespace IdempotentAPI.Telemetry
{
    /// <summary>
    /// No-op implementation of <see cref="IIdempotencyMetrics"/> for backward compatibility.
    /// Used when metrics are not explicitly enabled.
    /// </summary>
    public sealed class NullIdempotencyMetrics : IIdempotencyMetrics
    {
        /// <summary>
        /// Singleton instance of the null metrics implementation.
        /// </summary>
        public static readonly NullIdempotencyMetrics Instance = new NullIdempotencyMetrics();

        private NullIdempotencyMetrics() { }

        /// <inheritdoc />
        public void RecordCacheHit(string httpMethod, string path) { }

        /// <inheritdoc />
        public void RecordCacheMiss(string httpMethod, string path) { }

        /// <inheritdoc />
        public void RecordCacheStore(string httpMethod, string path) { }

        /// <inheritdoc />
        public void RecordLockFailure(string operation) { }

        /// <inheritdoc />
        public void RecordHashMismatch() { }

        /// <inheritdoc />
        public void RecordNonSuccessSkip(string httpMethod, string path, int statusCode) { }

        /// <inheritdoc />
        public void RecordIdempotencyCancelled() { }
    }
}
