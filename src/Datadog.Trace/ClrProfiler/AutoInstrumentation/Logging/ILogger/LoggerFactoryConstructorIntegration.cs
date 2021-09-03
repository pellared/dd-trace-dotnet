// <copyright file="LoggerFactoryConstructorIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.ComponentModel;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.Logging;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.ILogger
{
    /// <summary>
    /// LoggerFactory() calltarget instrumentation
    /// </summary>
    [InstrumentMethod(
        AssemblyName = "Microsoft.Extensions.Logging",
        TypeName = "Microsoft.Extensions.Logging.LoggerFactory",
        MethodName = ".ctor",
        ReturnTypeName = ClrNames.Void,
        ParameterTypeNames = new[] { "System.Collections.Generic.IEnumerable`1[Microsoft.Extensions.Logging.ILoggerProvider]", "Microsoft.Extensions.Options.IOptionsMonitor`1[Microsoft.Extensions.Logging.LoggerFilterOptions]", "Microsoft.Extensions.Options.IOptions`1[Microsoft.Extensions.Logging.LoggerFactoryOptions]" },
        MinimumVersion = "2.0.0",
        MaximumVersion = "5.*.*",
        IntegrationName = LoggerIntegrationCommon.IntegrationName)]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class LoggerFactoryConstructorIntegration
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<LoggerFactoryConstructorIntegration>();

        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TProviders">Type of the IEnumerable of log providers</typeparam>
        /// <typeparam name="TFilterOptions">Type of the filter options</typeparam>
        /// <typeparam name="TOptions">Type of the options</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="providers">The providers to use in producing <see cref="ILogger"/> instances.</param>
        /// <param name="filterOption">The filter option to use.</param>
        /// <param name="options">The options</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget, TProviders, TFilterOptions, TOptions>(
            TTarget instance, TProviders providers, TFilterOptions filterOption, TOptions options)
        where TTarget : ILoggerFactory
        {
            // TODO: skip if not configured
            // TODO: Check configuration is valid etc
            // The ILoggerProvider type is in a different assembly to the LoggerFactory, so go via the ILogger type
            // returned by CreateLogger
            var providerType = instance.Type.GetMethod("CreateLogger")
                                      ?.ReturnType.Assembly.GetType("Microsoft.Extensions.Logging.ILoggerProvider");

            var provider = new DirectSubmissionLoggerProvider();
            var proxy = provider.DuckCast(providerType);

            return new CallTargetState(scope: null, state: proxy);
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
            where TTarget : ILoggerFactory
        {
            // TODO: skip if not configured
            // TODO: Skip if there's an exception? Log the exception too?
            var proxy = state.State;
            if (proxy is not null)
            {
                Log.Information("Direct log submission via ILogger enabled");
                instance.AddProvider(proxy);
            }

            return CallTargetReturn.GetDefault();
        }
    }
}
