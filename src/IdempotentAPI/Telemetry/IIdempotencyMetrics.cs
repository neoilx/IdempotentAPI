#nullable enable
namespace IdempotentAPI.Telemetry
{
    /// <summary>
    /// Interface for recording idempotency-related metrics.
    /// Implement this interface to integrate with your metrics/telemetry system.
    /// </summary>
    public interface IIdempotencyMetrics
    {
        /// <summary>
        /// Records a cache hit - when a response is served from the idempotency cache.
        /// </summary>
        /// <param name="httpMethod">The HTTP method (POST, PATCH)</param>
        /// <param name="path">The request path</param>
        void RecordCacheHit(string httpMethod, string path);

        /// <summary>
        /// Records a cache miss - when a new request processing starts (no cached response).
        /// </summary>
        /// <param name="httpMethod">The HTTP method (POST, PATCH)</param>
        /// <param name="path">The request path</param>
        void RecordCacheMiss(string httpMethod, string path);

        /// <summary>
        /// Records a successful cache store operation - when a response is cached.
        /// </summary>
        /// <param name="httpMethod">The HTTP method (POST, PATCH)</param>
        /// <param name="path">The request path</param>
        void RecordCacheStore(string httpMethod, string path);

        /// <summary>
        /// Records a distributed lock acquisition failure.
        /// </summary>
        /// <param name="operation">The operation that failed (get_or_set, set, remove)</param>
        void RecordLockFailure(string operation);

        /// <summary>
        /// Records a hash mismatch - same idempotency key used with different request data.
        /// </summary>
        void RecordHashMismatch();

        /// <summary>
        /// Records when caching is skipped due to a non-success response status code.
        /// </summary>
        /// <param name="httpMethod">The HTTP method (POST, PATCH)</param>
        /// <param name="path">The request path</param>
        /// <param name="statusCode">The HTTP response status code</param>
        void RecordNonSuccessSkip(string httpMethod, string path, int statusCode);

        /// <summary>
        /// Records when idempotency is cancelled (e.g., due to an exception during processing).
        /// </summary>
        void RecordIdempotencyCancelled();
    }
}
