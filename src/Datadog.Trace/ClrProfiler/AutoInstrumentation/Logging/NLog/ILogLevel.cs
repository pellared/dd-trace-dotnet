// <copyright file="ILogLevel.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.ComponentModel;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.NLog
{
    /// <summary>
    /// Duck type for ILogLevel
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ILogLevel
    {
        /// <summary>
        /// Gets the name of the log level
        /// </summary>
        public string Name { get; }
    }
}
