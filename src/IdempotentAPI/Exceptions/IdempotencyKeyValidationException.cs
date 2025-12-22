using System;

namespace IdempotentAPI.Exceptions
{
    /// <summary>
    /// Exception thrown when the Idempotency-Key header validation fails.
    /// This is a client error (4xx) and should result in a 400 Bad Request response.
    /// </summary>
    public class IdempotencyKeyValidationException : Exception
    {
        public IdempotencyKeyValidationException(string message) : base(message)
        {
        }

        public IdempotencyKeyValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
