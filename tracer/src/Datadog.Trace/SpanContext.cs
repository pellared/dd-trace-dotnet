// <copyright file="SpanContext.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using Datadog.Trace.Util;

namespace Datadog.Trace
{
    /// <summary>
    /// The SpanContext contains all the information needed to express relationships between spans inside or outside the process boundaries.
    /// </summary>
    public class SpanContext : ISpanContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpanContext"/> class FOR TESTING PURPOSES ONLY.
        /// </summary>
        /// <param name="traceId">The propagated trace id.</param>
        /// <param name="spanId">The propagated span id.</param>
        /// <remarks>FOR TESTING PURPOSES ONLY.</remarks>
        internal SpanContext(ulong? traceId, ulong spanId)
            : this(traceId, serviceName: null)
        {
            SpanId = spanId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpanContext"/> class FOR TESTING PURPOSES ONLY.
        /// </summary>
        /// <param name="traceId">The propagated trace id.</param>
        /// <param name="spanId">The propagated span id.</param>
        /// <param name="serviceName">The service name to propagate to child spans.</param>
        /// <remarks>FOR TESTING PURPOSES ONLY.</remarks>
        internal SpanContext(ulong? traceId, ulong spanId, string serviceName)
            : this(traceId, serviceName)
        {
            SpanId = spanId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpanContext"/> class FOR TESTING PURPOSES ONLY.
        /// </summary>
        /// <param name="traceId">The propagated trace id.</param>
        /// <param name="spanId">The propagated span id.</param>
        /// <param name="samplingPriority">The propagated sampling priority.</param>
        /// <remarks>FOR TESTING PURPOSES ONLY.</remarks>
        internal SpanContext(ulong? traceId, ulong spanId, SamplingPriority? samplingPriority)
            : this(traceId, serviceName: null)
        {
            SpanId = spanId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpanContext"/> class
        /// from a propagated context.
        /// </summary>
        /// <param name="traceId">The propagated trace id.</param>
        /// <param name="spanId">The propagated span id.</param>
        /// <param name="samplingPriority">Not used.</param>
        /// <param name="serviceName">The service name to propagate to child spans.</param>
        [Obsolete("To create a span context for propagation, use Datadog.Trace.PropagatedSpanContext.")]
        public SpanContext(ulong? traceId, ulong spanId, SamplingPriority? samplingPriority = null, string serviceName = null)
            : this(traceId, serviceName)
        {
            SpanId = spanId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpanContext"/> class FOR TESTING PURPOSES ONLY.
        /// </summary>
        /// <param name="traceId">The propagated trace id.</param>
        /// <param name="spanId">The propagated span id.</param>
        /// <param name="samplingPriority">The propagated sampling priority.</param>
        /// <param name="serviceName">The service name to propagate to child spans.</param>
        /// <param name="origin">The propagated origin of the trace.</param>
        /// <remarks>FOR TESTING PURPOSES ONLY.</remarks>
        internal SpanContext(ulong? traceId, ulong spanId, SamplingPriority? samplingPriority, string serviceName, string origin)
            : this(traceId, serviceName)
        {
            SpanId = spanId;
            Origin = origin;
            TraceContext = new TraceContext(null) { SamplingPriority = samplingPriority };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpanContext"/> class
        /// that is the child of the specified parent context.
        /// </summary>
        /// <param name="parent">The parent context.</param>
        /// <param name="traceContext">The trace context.</param>
        /// <param name="serviceName">The service name to propagate to child spans.</param>
        /// <param name="spanId">The propagated span id.</param>
        internal SpanContext(ISpanContext parent, ITraceContext traceContext, string serviceName, ulong? spanId = null)
            : this(parent?.TraceId, serviceName)
        {
            SpanId = spanId ?? SpanIdGenerator.ThreadInstance.CreateNew();
            Parent = parent;
            TraceContext = traceContext;
            if (parent is SpanContext spanContext)
            {
                Origin = spanContext.Origin;
            }
        }

        private SpanContext(ulong? traceId, string serviceName)
        {
            TraceId = traceId > 0
                          ? traceId.Value
                          : SpanIdGenerator.ThreadInstance.CreateNew();

            ServiceName = serviceName;
        }

        /// <summary>
        /// Gets the parent context.
        /// </summary>
        public ISpanContext Parent { get; }

        /// <summary>
        /// Gets the trace id
        /// </summary>
        public ulong TraceId { get; }

        /// <summary>
        /// Gets the span id of the parent span
        /// </summary>
        public ulong? ParentId => Parent?.SpanId;

        /// <summary>
        /// Gets the span id
        /// </summary>
        public ulong SpanId { get; }

        /// <summary>
        /// Gets or sets the service name to propagate to child spans.
        /// </summary>
        [Obsolete]
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets the origin of the trace.
        /// </summary>
        [Obsolete]
        internal string Origin => TraceContext?.Origin;

        /// <summary>
        /// Gets the trace context.
        /// </summary>
        internal ITraceContext TraceContext { get; }

        /// <summary>
        /// Gets the sampling priority for contexts created from incoming propagated context.
        /// </summary>
        [Obsolete]
        internal SamplingPriority? SamplingPriority => TraceContext?.SamplingPriority;
    }
}
