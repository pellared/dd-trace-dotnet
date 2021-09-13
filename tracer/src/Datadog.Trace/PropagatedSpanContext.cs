// <copyright file="PropagatedSpanContext.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace
{
    /// <summary>
    /// A span context that was propagated from a remote distributed trace.
    /// </summary>
    internal class PropagatedSpanContext : ISpanContext
    {
        public PropagatedSpanContext(ulong traceId, ulong spanId, SamplingPriority? samplingPriority, string origin)
        {
            TraceId = traceId;
            SpanId = spanId;
            SamplingPriority = samplingPriority;
            Origin = origin;
        }

        /// <summary>
        /// Gets the trace identifier.
        /// </summary>
        public ulong TraceId { get; }

        /// <summary>
        /// Gets the span identifier.
        /// </summary>
        public ulong SpanId { get; }

        /// <summary>
        /// Gets the service name.
        /// </summary>
        public string ServiceName => null;

        /// <summary>
        /// Gets the origin of the trace.
        /// </summary>
        internal string Origin { get; }

        /// <summary>
        /// Gets the propagated sampling priority.
        /// </summary>
        internal SamplingPriority? SamplingPriority { get; }
    }
}
