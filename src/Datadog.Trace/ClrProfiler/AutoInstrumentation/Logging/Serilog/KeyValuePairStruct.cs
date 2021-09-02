// <copyright file="KeyValuePairStruct.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.ComponentModel;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.Serilog
{
    /// <summary>
    /// Duck type for KeyValuePair&lt;object, LogEventPropertyValue&gt;
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DuckCopy]
    public struct KeyValuePairStruct
    {
        /// <summary>
        /// Gets the key
        /// </summary>
        public string Key;

        /// <summary>
        /// Gets the value
        /// </summary>
        public object Value;
    }
}
