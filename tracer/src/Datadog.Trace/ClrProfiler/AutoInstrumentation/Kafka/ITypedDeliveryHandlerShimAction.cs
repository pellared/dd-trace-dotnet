// <copyright file="ITypedDeliveryHandlerShimAction.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.ComponentModel;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Kafka
{
    /// <summary>
    /// TypedDeliveryHandlerShim_Action for duck-typing
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITypedDeliveryHandlerShimAction
    {
        /// <summary>
        /// Sets the delivery report handler
        /// </summary>
        [DuckField]
        public object Handler { set; }
    }
}
