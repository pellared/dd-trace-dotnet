// <copyright file="ILoggingEvent.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.Log4Net
{
    /// <summary>
    /// Duck type for LoggingEvent
    /// </summary>
    public interface ILoggingEvent
    {
        /// <summary>
        /// Gets the UTC time the event was logged
        /// </summary>
        public DateTime TimeStampUtc { get; }

        /// <summary>
        /// Gets the name of the logger that logged the event.
        /// </summary>
        public string LoggerName { get; }

        /// <summary>
        /// Gets the Level of the logging event
        /// </summary>
        public abstract ILevel Level { get; }

        /// <summary>
        /// Gets the message, rendered through the RendererMap".
        /// </summary>
        public abstract string RenderedMessage { get; }

        /// <summary>
        /// Gets the exception object used to initialize this event
        /// </summary>
        public abstract Exception ExceptionObject { get; }

        /// <summary>
        /// Get all the composite properties in this event
        /// </summary>
        /// <returns>Dictionary containing all the properties</returns>
        public abstract IDictionary GetProperties();
    }
}
