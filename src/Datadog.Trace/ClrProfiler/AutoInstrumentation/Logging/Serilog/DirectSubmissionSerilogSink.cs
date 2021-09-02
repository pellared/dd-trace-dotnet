// <copyright file="DirectSubmissionSerilogSink.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.ComponentModel;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.Logging;
using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.Serilog
{
    /// <summary>
    /// Serilog Sink
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class DirectSubmissionSerilogSink
    {
        private static readonly JsonSerializerSettings Settings = new() { NullValueHandling = NullValueHandling.Ignore, };
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<DirectSubmissionSerilogSink>();

        /// <summary>
        /// Emit the provided log event to the sink
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        [DuckReverseMethod("Serilog.Events.LogEvent")]
        public void Emit(ILogEvent logEvent)
        {
            var log = JsonConvert.SerializeObject(logEvent.Instance, Settings);
            Log.Warning("[SERILOG]: " + log);
        }
    }
}
