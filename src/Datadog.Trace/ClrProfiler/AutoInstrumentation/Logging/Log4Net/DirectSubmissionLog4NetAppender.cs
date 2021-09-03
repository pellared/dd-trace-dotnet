// <copyright file="DirectSubmissionLog4NetAppender.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Datadog.Trace.DuckTyping;
using Datadog.Trace.Logging;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.Log4Net
{
    /// <summary>
    /// Duck type for IAppender
    /// </summary>
    public class DirectSubmissionLog4NetAppender
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<DirectSubmissionLog4NetAppender>();

        /// <summary>
        /// Gets or sets the name of this appender
        /// </summary>
        public string Name { get; set; } = "Datadog";

        /// <summary>
        /// Closes the appender and releases resources
        /// </summary>
        [DuckReverseMethod]
        public void Close()
        {
        }

        /// <summary>
        /// Log the logging event in Appender specific way.
        /// </summary>
        /// <param name="event">The logging event</param>
        [DuckReverseMethod("log4net.Core.LoggingEvent")]
        public void DoAppend(ILoggingEvent @event)
        {
            Log.Warning("[LOG4NET] " + @event.RenderedMessage);
        }
    }
}
