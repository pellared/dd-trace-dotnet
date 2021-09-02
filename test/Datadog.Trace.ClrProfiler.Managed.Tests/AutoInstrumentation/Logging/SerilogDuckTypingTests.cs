// <copyright file="SerilogDuckTypingTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.Serilog;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.Vendors.Newtonsoft.Json;
using Datadog.Trace.Vendors.Serilog;
using Datadog.Trace.Vendors.Serilog.Core;
using Datadog.Trace.Vendors.Serilog.Events;
using Datadog.Trace.Vendors.Serilog.Parsing;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Datadog.Trace.ClrProfiler.Managed.Tests.AutoInstrumentation.Logging
{
    public class SerilogDuckTypingTests
    {
        [Fact]
        public void CanDuckTypeMessageTemplate()
        {
            var instance = new MessageTemplate("Some text", Enumerable.Empty<MessageTemplateToken>());
            instance.TryDuckCast(out IMessageTemplate duck).Should().BeTrue();
            duck.Should().NotBeNull();
            duck.Text.Should().Be(instance.Text);
        }

        [Fact]
        public void CanDuckTypeLogEvent()
        {
            var instance = new LogEvent(
                DateTimeOffset.UtcNow,
                LogEventLevel.Error,
                new Exception(),
                new MessageTemplate("Some text", Enumerable.Empty<MessageTemplateToken>()),
                new[] { new LogEventProperty("SomeProp", new ScalarValue(123)) });

            instance.TryDuckCast(out ILogEvent duck).Should().BeTrue();
            var intLevel = (int)instance.Level;
            var intLevel2 = (LogEventLevelDuck)duck.Level;
            duck.Should().NotBeNull();
            duck.Exception.Should().Be(instance.Exception);
            intLevel2.Should().Be(intLevel);
            duck.Timestamp.Should().Be(instance.Timestamp);
            duck.MessageTemplate.Text.Should().Be(instance.MessageTemplate.Text);
            var properties = new List<KeyValuePairStruct>();

            foreach (var duckProperty in duck.Properties)
            {
                properties.Add(duckProperty.DuckCast<KeyValuePairStruct>());
            }

            foreach (var property in instance.Properties)
            {
                properties.Should()
                    .ContainSingle(
                         x => x.Key == property.Key
                           && x.Value.ToString() == property.Value.ToString());
            }
        }

        [Fact]
        public void CanDuckTypeLoggerConfiguration()
        {
            var config = new LoggerConfiguration();

            config.TryDuckCast(out ILoggerConfiguration duckConfig).Should().BeTrue();
            duckConfig.Should().NotBeNull();
            duckConfig.LogEventSinks.Should().BeEmpty();

            Type sinkType = typeof(ILogEventSink);
            var sink = new TestSerilogSink();

            var duckSink = sink.DuckCast(sinkType);
            duckConfig.LogEventSinks.Add(duckSink);

            var logger = config.CreateLogger();
            var message = "This is a test";
            logger.Information(message);

            sink.Logs.Should().ContainSingle(x => x == message);
        }

        public class TestSerilogSink
        {
            public List<string> Logs { get; } = new();

            [DuckReverseMethod("Datadog.Trace.Vendors.Serilog.Events.LogEvent")]
            public void Emit(ILogEvent logEvent)
            {
                Logs.Add(logEvent.RenderMessage());
            }
        }
    }
}
