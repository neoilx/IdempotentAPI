#nullable enable
using System.Diagnostics.Metrics;
using System.Reflection;

namespace IdempotentAPI.Telemetry
{
    /// <summary>
    /// Provides the static Meter instance for IdempotentAPI metrics.
    /// Use this meter name ("IdempotentAPI") when configuring OpenTelemetry or other metrics collectors.
    /// </summary>
    internal static class IdempotencyMeterProvider
    {
        /// <summary>
        /// The meter name used for all IdempotentAPI metrics.
        /// Use this value when configuring OpenTelemetry: <c>.AddMeter("IdempotentAPI")</c>
        /// </summary>
        public const string MeterName = "IdempotentAPI";

        private static readonly AssemblyName AssemblyName = typeof(IdempotencyMeterProvider).Assembly.GetName();

        /// <summary>
        /// The shared Meter instance for recording IdempotentAPI metrics.
        /// </summary>
        public static readonly Meter Meter = new Meter(
            MeterName,
            AssemblyName.Version?.ToString() ?? "0.0.0");
    }
}
