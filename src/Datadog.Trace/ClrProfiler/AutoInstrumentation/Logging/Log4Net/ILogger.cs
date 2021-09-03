// <copyright file="ILogger.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.Log4Net
{
    /// <summary>
    /// Duck type for Logger
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Gets the appenders contained in this logger as an AppenderCollection
        /// </summary>
        public IEnumerable Appenders { get; }

        /// <summary>
        /// Gets the Repository where this <c>Logger</c> instance is attached to.
        /// </summary>
        public IRepository Repository { get; }

        /// <summary>
        /// Add <paramref name="newAppender"/> to the list of appenders of this
        /// Logger instance.
        /// </summary>
        /// <param name="newAppender">An appender to add to this logger</param>
        public void AddAppender(object newAppender);
    }
}
