// <copyright file="LoggerImplInstrumentation.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.ComponentModel;
using Datadog.Trace.ClrProfiler.CallTarget;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.NLog
{
    /// <summary>
    /// LoggerImplInstrumentation calltarget instrumentation
    /// </summary>
    [InstrumentMethod(
        AssemblyName = "NLog",
        TypeName = "NLog.LoggerImpl",
        MethodName = "Write",
        ReturnTypeName = ClrNames.Void,
        ParameterTypeNames = new[] { ClrNames.Type, "NLog.Internal.TargetWithFilterChain", "NLog.LogEventInfo", "NLog.LogFactory" },
        MinimumVersion = "2.0.0",
        MaximumVersion = "4.*.*",
        IntegrationName = NLogConstants.IntegrationName)]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class LoggerImplInstrumentation
    {
        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="loggerType">The type of the logger instance writing the log</param>
        /// <param name="targets">The other targets the log is being written to</param>
        /// <param name="logEvent">The log event details</param>
        /// <param name="logFactory">The logFactory used to create the logger</param>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TTargetsWithFilterChain">The existing registered targets for the logger</typeparam>
        /// <typeparam name="TLogEventInfo">The logEventInfo</typeparam>
        /// <typeparam name="TLogFactory">The factory used to create the logger</typeparam>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget, TTargetsWithFilterChain, TLogEventInfo, TLogFactory>(
            TTarget instance, Type loggerType, TTargetsWithFilterChain targets, TLogEventInfo logEvent, TLogFactory logFactory)
        where TLogEventInfo : ILogEventInfo
        {
            // TODO: skip if not configured
            // TODO: Check configuration is valid etc
            // Send to the sink in the End method, as Write may enhance the log event
            return new CallTargetState(scope: null, state: logEvent);
        }

        /// <summary>
        /// OnMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A default CallTargetReturn to satisfy the CallTarget contract</returns>
        public static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception exception, CallTargetState state)
        {
            // TODO: skip if not configured
            // TODO: Skip if there's an exception? Log the exception too?
            if (state.State is ILogEventInfo info)
            {
                DirectSubmissionNLogSink.Write(info);
            }

            return CallTargetReturn.GetDefault();
        }
    }
}
