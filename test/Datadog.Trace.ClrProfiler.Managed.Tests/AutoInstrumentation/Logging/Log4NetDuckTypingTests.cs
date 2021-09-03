// <copyright file="Log4NetDuckTypingTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
#if NET5_0
using System;
using System.Collections.Generic;
using Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.ILogger;
using Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.Log4Net;
using Datadog.Trace.DuckTyping;
using FluentAssertions;
using log4net.Appender;
using log4net.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Xunit;
using IExternalScopeProvider = Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.ILogger.IExternalScopeProvider;
using ILoggerFactory = Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.ILogger.ILoggerFactory;

namespace Datadog.Trace.ClrProfiler.Managed.Tests.AutoInstrumentation.Logging
{
    public class Log4NetDuckTypingTests
    {
        [Fact]
        public void CanDuckTypeIAppender()
        {
            var appenderType = typeof(IAppender);

            var appender = new DirectSubmissionLog4NetAppender();
            var proxy = appender.DuckCast(appenderType);

            var appenderProxy = (IAppender)proxy;

            appenderProxy.Name = "Test";
            appenderProxy.Close();
            appenderProxy.DoAppend(new LoggingEvent(default));
        }
    }
}
#endif
