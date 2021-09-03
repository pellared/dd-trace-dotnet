// <copyright file="AppenderCollectionIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections;
using System.ComponentModel;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.Logging;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.Log4Net
{
    /// <summary>
    /// () calltarget instrumentation
    /// </summary>
    [InstrumentMethod(
        AssemblyName = "log4net",
        TypeName = "log4net.Appender.AppenderCollection",
        MethodName = "ToArray",
        ReturnTypeName = "log4net.Appender.IAppender[]",
        ParameterTypeNames = new string[0],
        MinimumVersion = "1.0.0",
        MaximumVersion = "2.*.*",
        IntegrationName = Log4NetConstants.IntegrationName)]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class AppenderCollectionIntegration
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<AppenderCollectionIntegration>();
        private static Type _iappenderType;

        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget>(TTarget instance)
        {
            // TODO: skip if not configured
            // TODO: Check configuration is valid etc
            return CallTargetState.GetDefault();
        }

        /// <summary>
        /// OnMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TResponse">The type of the response</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="response">The returned ILoggerWrapper </param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A default CallTargetReturn to satisfy the CallTarget contract</returns>
        public static CallTargetReturn<TResponse> OnMethodEnd<TTarget, TResponse>(TTarget instance, TResponse response, Exception exception, CallTargetState state)
        {
            var responseArray = (Array)(object)response;
            if (_iappenderType is null)
            {
                Log.Information("Direct log submission via Log4Net enabled");
                _iappenderType = response.GetType().GetElementType();
            }

            Log.Information("Adding Log4Net appender");

            var finalArray = Array.CreateInstance(_iappenderType, responseArray.Length + 1);
            Array.Copy(responseArray, finalArray, responseArray.Length);

            var appender = new DirectSubmissionLog4NetAppender();
            var proxy = appender.DuckCast(_iappenderType);
            finalArray.SetValue(proxy, responseArray.Length);

            return new CallTargetReturn<TResponse>((TResponse)(object)finalArray);
        }
    }
}
