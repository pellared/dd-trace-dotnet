// <copyright file="IntegrationTelemetryCollectorTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Configuration;
using Datadog.Trace.PlatformHelpers;
using Datadog.Trace.Telemetry;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.Tests.Telemetry
{
    public class IntegrationTelemetryCollectorTests
    {
        private const string ServiceName = "serializer-test-app";
        private static readonly AzureAppServices EmptyAasSettings = new(new Dictionary<string, string>());

        public static TheoryData<SerializableIntegrationInfo> Integrations()
            => new()
            {
                new SerializableIntegrationInfo(IntegrationIds.Kafka),
                new SerializableIntegrationInfo("Custom integration"),
            };

        [Theory]
        [MemberData(nameof(Integrations))]
        public void HasChangesWhenNewIntegrationRunning(SerializableIntegrationInfo info)
        {
            var collector = new IntegrationTelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings());

            collector.GetData();
            collector.HasChanges().Should().BeFalse();

            collector.IntegrationRunning(info.Info);
            collector.HasChanges().Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(Integrations))]
        public void DoesNotHaveChangesWhenSameIntegrationRunning(SerializableIntegrationInfo info)
        {
            var collector = new IntegrationTelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings());

            collector.GetData();
            collector.HasChanges().Should().BeFalse();

            collector.IntegrationRunning(info.Info);
            collector.HasChanges().Should().BeTrue();
            collector.GetData();

            collector.IntegrationRunning(info.Info);
            collector.HasChanges().Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(Integrations))]
        public void HasChangesWhenNewIntegrationGeneratedSpan(SerializableIntegrationInfo info)
        {
            var collector = new IntegrationTelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings());

            collector.GetData();
            collector.HasChanges().Should().BeFalse();

            collector.IntegrationGeneratedSpan(info.Info);
            collector.HasChanges().Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(Integrations))]
        public void DoesNotHaveChangesWhenSameIntegrationGeneratedSpan(SerializableIntegrationInfo info)
        {
            var collector = new IntegrationTelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings());

            collector.GetData();
            collector.HasChanges().Should().BeFalse();

            collector.IntegrationGeneratedSpan(info.Info);
            collector.HasChanges().Should().BeTrue();
            collector.GetData();

            collector.IntegrationGeneratedSpan(info.Info);
            collector.HasChanges().Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(Integrations))]
        public void HasChangesWhenNewIntegrationDisabled(SerializableIntegrationInfo info)
        {
            var collector = new IntegrationTelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings());

            collector.GetData();
            collector.HasChanges().Should().BeFalse();

            collector.IntegrationDisabledDueToError(info.Info, "Testing!");
            collector.HasChanges().Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(Integrations))]
        public void DoesNotHaveChangesWhenSameIntegrationDisabled(SerializableIntegrationInfo info)
        {
            var collector = new IntegrationTelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings());

            collector.GetData();
            collector.HasChanges().Should().BeFalse();

            collector.IntegrationDisabledDueToError(info.Info, "Testing");
            collector.HasChanges().Should().BeTrue();
            collector.GetData();

            collector.IntegrationDisabledDueToError(info.Info, "Another error");
            collector.HasChanges().Should().BeFalse();
        }

        [Fact]
        public void WhenKnownIntegrationDoesNotRunHasExpectedValues()
        {
            var info = new SerializableIntegrationInfo(IntegrationIds.Kafka);
            var collector = new IntegrationTelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings());

            var data = collector.GetData();
            var integration = data.FirstOrDefault(x => x.Name == info.IntegrationName);
            integration.Should().NotBeNull();
            integration.AutoEnabled.Should().BeFalse();
            integration.Enabled.Should().BeFalse();
            integration.Error.Should().BeNull();
        }

        [Fact]
        public void WhenKnownIntegrationDoesNotRunDoesNotAppearInData()
        {
            var info = new SerializableIntegrationInfo("Custom integration");
            var collector = new IntegrationTelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings());

            var data = collector.GetData();
            var integration = data.FirstOrDefault(x => x.Name == info.IntegrationName);
            integration.Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(Integrations))]
        public void WhenIntegrationRunsSuccessfullyHasExpectedValues(SerializableIntegrationInfo info)
        {
            var collector = new IntegrationTelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings());

            collector.IntegrationRunning(info.Info);
            collector.IntegrationGeneratedSpan(info.Info);

            var data = collector.GetData();
            var integration = data.FirstOrDefault(x => x.Name == info.IntegrationName);
            integration.Should().NotBeNull();
            integration.AutoEnabled.Should().BeTrue();
            integration.Enabled.Should().BeTrue();
            integration.Error.Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(Integrations))]
        public void WhenIntegrationRunsButDoesNotGenerateSpanHasExpectedValues(SerializableIntegrationInfo info)
        {
            var collector = new IntegrationTelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings());

            collector.IntegrationRunning(info.Info);

            var data = collector.GetData();
            var integration = data.FirstOrDefault(x => x.Name == info.IntegrationName);
            integration.Should().NotBeNull();
            integration.AutoEnabled.Should().BeTrue();
            integration.Enabled.Should().BeFalse();
            integration.Error.Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(Integrations))]
        public void WhenIntegrationErrorsHasExpectedValues(SerializableIntegrationInfo info)
        {
            const string error = "Some error";
            var collector = new IntegrationTelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings());

            collector.IntegrationRunning(info.Info);
            collector.IntegrationDisabledDueToError(info.Info, error);

            var data = collector.GetData();
            var integration = data.FirstOrDefault(x => x.Name == info.IntegrationName);
            integration.Should().NotBeNull();
            integration.AutoEnabled.Should().BeTrue();
            integration.Enabled.Should().BeFalse();
            integration.Error.Should().Be(error);
        }

        [Theory]
        [MemberData(nameof(Integrations))]
        public void WhenIntegrationRunsThenErrorsHasExpectedValues(SerializableIntegrationInfo info)
        {
            const string error = "Some error";
            var collector = new IntegrationTelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings());

            collector.IntegrationRunning(info.Info);
            collector.IntegrationGeneratedSpan(info.Info);
            collector.IntegrationDisabledDueToError(info.Info, error);

            var data = collector.GetData();
            var integration = data.FirstOrDefault(x => x.Name == info.IntegrationName);
            integration.Should().NotBeNull();
            integration.AutoEnabled.Should().BeTrue();
            integration.Enabled.Should().BeFalse();
            integration.Error.Should().Be(error);
        }

        public class SerializableIntegrationInfo : IXunitSerializable
        {
            private int? _id;
            private string _name;

            public SerializableIntegrationInfo()
            {
            }

            internal SerializableIntegrationInfo(IntegrationIds id) => _id = (int)id;

            internal SerializableIntegrationInfo(string name) => _name = name;

            internal IntegrationInfo Info => _id.HasValue
                                                 ? new IntegrationInfo(_id.Value)
                                                 : new IntegrationInfo(_name);

            internal string IntegrationName => IntegrationRegistry.GetName(Info);

            public void Deserialize(IXunitSerializationInfo info)
            {
                _id = info.GetValue<int?>("id");
                _name = info.GetValue<string>("name");
            }

            public void Serialize(IXunitSerializationInfo info)
            {
                info.AddValue("id", _id?.ToString());
                info.AddValue("name", _name);
            }

            public override string ToString()
            {
                return $"{IntegrationName} ({_id})";
            }
        }
    }
}
