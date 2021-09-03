// <copyright file="DirectSubmissionLogger.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.Logging;
using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.ILogger
{
    /// <summary>
    /// An implementation of ILogger for use with direct log submission
    /// </summary>
    public class DirectSubmissionLogger
    {
        private static readonly IDatadogLogger Logger = DatadogLogging.GetLoggerFor<DirectSubmissionLogger>();
        private readonly string _name;
        private readonly IExternalScopeProvider _scopeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectSubmissionLogger"/> class.
        /// </summary>
        /// <param name="name">The category name of the logger</param>
        /// <param name="scopeProvider">A provider for tracking scopes</param>
        public DirectSubmissionLogger(string name, IExternalScopeProvider scopeProvider)
        {
            _name = name;
            _scopeProvider = scopeProvider;
        }

        /// <summary>
        /// Writes a log entry.
        /// </summary>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">Id of the event.</param>
        /// <param name="state">The entry to be written. Can be also an object.</param>
        /// <param name="exception">The exception related to this entry.</param>
        /// <param name="formatter">Function to create a <see cref="string"/> message of the <paramref name="state"/> and <paramref name="exception"/>.</param>
        /// <typeparam name="TState">The type of the object to be written.</typeparam>
        [DuckReverseMethod("Microsoft.Extensions.Logging.LogLevel", "Microsoft.Extensions.Logging.EventId", "TState", "System.Exception", "Func`3")]
        public void Log<TState>(int logLevel, object eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            // TODO: this is super simplistic, redo completely in practice
            string serializedState = null;
            if (_scopeProvider is not null)
            {
                var scopeValues = new List<string>();
                _scopeProvider.ForEachScope(
                    (scope, state) =>
                    {
                        var list = (List<string>)state;
                        if (scope is IEnumerable<KeyValuePair<string, object>> scopeItems)
                        {
                            foreach (KeyValuePair<string, object> item in scopeItems)
                            {
                                list.Add($"{item.Key}: {item.Value}");
                            }
                        }
                        else
                        {
                            list.Add(scope.ToString());
                        }
                    },
                    scopeValues);

                serializedState = string.Join(", ", scopeValues);
            }

            Logger.Warning($"[ILOGGER {_name}] {formatter(state, exception)} {serializedState}");
        }

        /// <summary>
        /// Checks if the given <paramref name="logLevel"/> is enabled.
        /// </summary>
        /// <param name="logLevel">Level to be checked.</param>
        /// <returns><c>true</c> if enabled.</returns>
        [DuckReverseMethod("Microsoft.Extensions.Logging.LogLevel")]
        public bool IsEnabled(int logLevel) => true;

        /// <summary>
        /// Begins a logical operation scope.
        /// </summary>
        /// <param name="state">The identifier for the scope.</param>
        /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
        /// <returns>An <see cref="IDisposable"/> that ends the logical operation scope on dispose.</returns>
        [DuckReverseMethod("Microsoft.Extensions.Logging.LogLevel")]
        public IDisposable BeginScope<TState>(TState state) => _scopeProvider?.Push(state) ?? NullDisposable.Instance;

        private class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
