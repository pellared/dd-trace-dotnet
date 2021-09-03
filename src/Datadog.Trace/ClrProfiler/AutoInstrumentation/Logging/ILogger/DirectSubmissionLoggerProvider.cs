// <copyright file="DirectSubmissionLoggerProvider.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Concurrent;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.ILogger
{
    /// <summary>
    /// Duck type for ILoggerProvider
    /// </summary>
    public class DirectSubmissionLoggerProvider
    {
        private readonly ConcurrentDictionary<string, DirectSubmissionLogger> _loggers = new();
        private IExternalScopeProvider _scopeProvider;

        /// <summary>
        /// Creates a new <see cref="ILogger"/> instance.
        /// </summary>
        /// <param name="categoryName">The category name for messages produced by the logger.</param>
        /// <returns>The instance of <see cref="ILogger"/> that was created.</returns>
        [DuckReverseMethod(ClrNames.String)]
        public DirectSubmissionLogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, CreateLoggerImplementation);
        }

        private DirectSubmissionLogger CreateLoggerImplementation(string name)
        {
            return new DirectSubmissionLogger(name, _scopeProvider);
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        [DuckReverseMethod]
        public void Dispose()
        {
        }

        /// <summary>
        /// Method for ISupportExternalScope
        /// </summary>
        /// <param name="scopeProvider">The provider of scope data</param>
        [DuckReverseMethod("Microsoft.Extensions.Logging.IExternalScopeProvider")]
        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }
    }
}
