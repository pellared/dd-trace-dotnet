// <copyright file="ILogEventInfo.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.NLog
{
    /// <summary>
    /// Duck type for LogEventInfo
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ILogEventInfo
    {
        /// <summary>
        /// Gets the timestamp
        /// </summary>
        public DateTime TimeStamp { get; }

        /// <summary>
        /// Gets the log level
        /// </summary>
        public ILogLevel Level { get; }

        /// <summary>
        /// Gets the stack trace
        /// </summary>
        public StackTrace StackTrace { get; }

        /// <summary>
        /// Gets the exception
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the logger name.
        /// </summary>
        public string LoggerName { get; }

        /// <summary>
        /// Gets the log message including any parameter placeholders.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the formatted message.
        /// </summary>
        public string FormattedMessage { get; }

        /// <summary>
        /// Gets the dictionary of per-event context properties
        /// </summary>
        public IDictionary<object, object> Properties { get; }
    }
}
