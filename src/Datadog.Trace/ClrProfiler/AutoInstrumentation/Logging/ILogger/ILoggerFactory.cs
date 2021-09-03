// <copyright file="ILoggerFactory.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.ComponentModel;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.ILogger
{
    /// <summary>
    /// Duck type for ILogLevel
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ILoggerFactory : IDuckType
    {
        /// <summary>
        /// Gets the name of the log level
        /// </summary>
        [Duck]
        void AddProvider(object provider);
    }
}
