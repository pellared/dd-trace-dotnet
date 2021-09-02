// <copyright file="DirectSubmissionNLogSink.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Datadog.Trace.Logging;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.NLog
{
    internal class DirectSubmissionNLogSink
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<DirectSubmissionNLogSink>();

        // TODO: Probably Need to make this non-static etc
        public static void Write(ILogEventInfo logEventInfo)
        {
            Log.Warning("[NLOG] " + logEventInfo.FormattedMessage);
        }
    }
}
