// <copyright file="TelemetryCollectorTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using Datadog.Trace.AppSec;
using Datadog.Trace.Configuration;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.PlatformHelpers;
using Datadog.Trace.Telemetry;
using Datadog.Trace.Tests.PlatformHelpers;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.Tests.Telemetry
{
    public class TelemetryCollectorTests
    {
        private const string ServiceName = "serializer-test-app";
        private static readonly AzureAppServices EmptyAasSettings = new(new Dictionary<string, string>());

        public static TheoryData<SerializableIntegrationInfo> Integrations()
            => new()
            {
                new SerializableIntegrationInfo(IntegrationIds.Kafka),
                new SerializableIntegrationInfo("Custom integration"),
            };

        [Fact]
        public void HasChangesAfterEachTracerSettingsAdded()
        {
            var settings = new TracerSettings();

            var collector = new TelemetryCollector();

            collector.RecordTracerSettings(settings, ServiceName, EmptyAasSettings);

            collector.HasChanges().Should().BeTrue();
            var data = collector.GetData();
            data.Configuration.TracerInstanceCount = 1;
            collector.HasChanges().Should().BeFalse();

            collector.RecordTracerSettings(settings, ServiceName, EmptyAasSettings);
            collector.HasChanges().Should().BeTrue();

            data = collector.GetData();
            data.Configuration.TracerInstanceCount = 2;
            collector.HasChanges().Should().BeFalse();
        }

        [Fact]
        public void HasChangesAfterAssemblyLoaded()
        {
            var collector = new TelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings(), ServiceName, EmptyAasSettings);

            var data = collector.GetData();
            data.Dependencies.Should().BeEmpty();
            collector.HasChanges().Should().BeFalse();

            var assembly = typeof(TelemetryCollectorTests).Assembly;
            collector.AssemblyLoaded(assembly.GetName());

            collector.HasChanges().Should().BeTrue();

            data = collector.GetData();
            data.Dependencies.Should()
                .HaveCount(1)
                .And.ContainSingle(x => x.Name == "Datadog.Trace.Tests");
            collector.HasChanges().Should().BeFalse();
        }

        [Fact]
        public void DoesNotHaveChangesWhenSameAssemblyAddedTwice()
        {
            var assembly = typeof(TelemetryCollectorTests).Assembly.GetName();
            var collector = new TelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings(), ServiceName, EmptyAasSettings);
            collector.AssemblyLoaded(assembly);

            collector.GetData();
            collector.HasChanges().Should().BeFalse();

            collector.AssemblyLoaded(assembly);
            collector.HasChanges().Should().BeFalse();
        }

        [Fact]
        public void HasChangesWhenAddingSameAssemblyWithDifferentVersion()
        {
            var assemblyV1 = CreateAssemblyName(new Version(1, 0));
            var assemblyV2 = CreateAssemblyName(new Version(2, 0));
            var collector = new TelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings(), ServiceName, EmptyAasSettings);
            collector.AssemblyLoaded(assemblyV1);

            collector.GetData();
            collector.HasChanges().Should().BeFalse();

            collector.AssemblyLoaded(assemblyV2);
            collector.HasChanges().Should().BeTrue();
            var data = collector.GetData();
            data.Should().NotBeNull();
            data.Dependencies.Should()
                .NotBeNullOrEmpty()
                .And.HaveCount(2)
                .And.OnlyHaveUniqueItems();
        }

        [Theory]
        [MemberData(nameof(Integrations))]
        public void HasChangesWhenNewIntegrationRunning(SerializableIntegrationInfo info)
        {
            var collector = new TelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings(), ServiceName, EmptyAasSettings);

            collector.GetData();
            collector.HasChanges().Should().BeFalse();

            collector.IntegrationRunning(info.Info);
            collector.HasChanges().Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(Integrations))]
        public void DoesNotHaveChangesWhenSameIntegrationRunning(SerializableIntegrationInfo info)
        {
            var collector = new TelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings(), ServiceName, EmptyAasSettings);

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
            var collector = new TelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings(), ServiceName, EmptyAasSettings);

            collector.GetData();
            collector.HasChanges().Should().BeFalse();

            collector.IntegrationGeneratedSpan(info.Info);
            collector.HasChanges().Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(Integrations))]
        public void DoesNotHaveChangesWhenSameIntegrationGeneratedSpan(SerializableIntegrationInfo info)
        {
            var collector = new TelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings(), ServiceName, EmptyAasSettings);

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
            var collector = new TelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings(), ServiceName, EmptyAasSettings);

            collector.GetData();
            collector.HasChanges().Should().BeFalse();

            collector.IntegrationDisabledDueToError(info.Info, "Testing!");
            collector.HasChanges().Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(Integrations))]
        public void DoesNotHaveChangesWhenSameIntegrationDisabled(SerializableIntegrationInfo info)
        {
            var collector = new TelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings(), ServiceName, EmptyAasSettings);

            collector.GetData();
            collector.HasChanges().Should().BeFalse();

            collector.IntegrationDisabledDueToError(info.Info, "Testing");
            collector.HasChanges().Should().BeTrue();
            collector.GetData();

            collector.IntegrationDisabledDueToError(info.Info, "Another error");
            collector.HasChanges().Should().BeFalse();
        }

        [Fact]
        public void TelemetryDataShouldIncludeExpectedValues()
        {
            const string env = "serializer-tests";
            const string serviceVersion = "1.2.3";
            var settings = new TracerSettings() { ServiceName = ServiceName, Environment = env, ServiceVersion = serviceVersion };

            var collector = new TelemetryCollector();

            collector.RecordTracerSettings(settings, ServiceName, EmptyAasSettings);

            var data = collector.GetData();

            data.RuntimeId.Should().NotBeNullOrEmpty().And.Be(Tracer.RuntimeId);
            data.SeqId.Should().Be(1);
            data.ServiceName.Should().NotBeNullOrEmpty().And.Be(ServiceName);
            data.Env.Should().NotBeNullOrEmpty().And.Be(env);
            data.StartedAt.Should().BeInRange(1, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            data.TracerVersion.Should().NotBeNullOrEmpty().And.Be(TracerConstants.AssemblyVersion);
            data.LanguageName.Should().NotBeNullOrEmpty().And.Be(FrameworkDescription.Instance.Name);
            data.ServiceVersion.Should().NotBeNullOrEmpty().And.Be(serviceVersion);
            data.LanguageVersion.Should().NotBeNullOrEmpty().And.Be(FrameworkDescription.Instance.ProductVersion);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void TelemetryDataShouldIncludeExpectedSecurityValues(bool enabled, bool blockingEnabled)
        {
            var collector = new TelemetryCollector();

            collector.RecordTracerSettings(new TracerSettings(), ServiceName, EmptyAasSettings);
            var source = new NameValueConfigurationSource(new NameValueCollection
            {
                { ConfigurationKeys.AppSecEnabled, enabled.ToString() },
                { ConfigurationKeys.AppSecBlockingEnabled, blockingEnabled.ToString() },
            });
            collector.RecordSecuritySettings(new SecuritySettings(source));

            var data = collector.GetData();

            data.Configuration.SecurityEnabled.Should().Be(enabled);
            data.Configuration.SecurityBlockingEnabled.Should().Be(blockingEnabled);
        }

        [Fact]
        public void TelemetryDataShouldIncludeAzureValuesWhenInAzureAndSafeToTrace()
        {
            const string env = "serializer-tests";
            const string serviceVersion = "1.2.3";
            var settings = new TracerSettings() { ServiceName = ServiceName, Environment = env, ServiceVersion = serviceVersion };
            var aas = new AzureAppServices(new Dictionary<string, string>
            {
                { ConfigurationKeys.ApiKey, "SomeValue" },
                { AzureAppServices.AzureAppServicesContextKey, "1" },
                { AzureAppServices.SiteExtensionVersionKey, "1.5.0" },
                { AzureAppServices.FunctionsExtensionVersionKey, "~3" },
            });

            var collector = new TelemetryCollector();

            collector.RecordTracerSettings(settings, ServiceName, aas);

            var data = collector.GetData();

            using var scope = new AssertionScope();
            data.Configuration.AasConfigurationError.Should().BeFalse();
            data.Configuration.CloudHosting.Should().Be("Azure");
            data.Configuration.AasAppType.Should().Be("function");
            data.Configuration.AasFunctionsRuntimeVersion.Should().Be("~3");
            data.Configuration.AasSiteExtensionVersion.Should().Be("1.5.0");
        }

        [Fact]
        public void TelemetryDataShouldNotIncludeAzureValuesWhenInAzureAndNotSafeToTrace()
        {
            const string env = "serializer-tests";
            const string serviceVersion = "1.2.3";
            var settings = new TracerSettings() { ServiceName = ServiceName, Environment = env, ServiceVersion = serviceVersion };
            var aas = new AzureAppServices(new Dictionary<string, string>
            {
                // Without a DD_API_KEY, AAS does not consider it safe to trace
                // { ConfigurationKeys.ApiKey, "SomeValue" },
                { AzureAppServices.AzureAppServicesContextKey, "1" },
                { AzureAppServices.SiteExtensionVersionKey, "1.5.0" },
                { AzureAppServices.FunctionsExtensionVersionKey, "~3" },
            });

            var collector = new TelemetryCollector();

            collector.RecordTracerSettings(settings, ServiceName, aas);

            var data = collector.GetData();

            using var scope = new AssertionScope();
            data.Configuration.AasConfigurationError.Should().BeTrue();
            data.Configuration.CloudHosting.Should().Be("Azure");
            // TODO: Don't we want to collect these anyway? If so, need to update AzureAppServices behaviour
            data.Configuration.AasAppType.Should().BeNullOrEmpty();
            data.Configuration.AasFunctionsRuntimeVersion.Should().BeNullOrEmpty();
            data.Configuration.AasSiteExtensionVersion.Should().BeNullOrEmpty();
        }

        [Fact]
        public void TelemetryDataShouldNotIncludeAzureValuesWhenNotInAzure()
        {
            const string env = "serializer-tests";
            const string serviceVersion = "1.2.3";
            var settings = new TracerSettings() { ServiceName = ServiceName, Environment = env, ServiceVersion = serviceVersion };

            var collector = new TelemetryCollector();

            collector.RecordTracerSettings(settings, ServiceName, EmptyAasSettings);

            var data = collector.GetData();

            using var scope = new AssertionScope();
            data.Configuration.CloudHosting.Should().BeNullOrEmpty();
            data.Configuration.AasAppType.Should().BeNullOrEmpty();
            data.Configuration.AasFunctionsRuntimeVersion.Should().BeNullOrEmpty();
            data.Configuration.AasSiteExtensionVersion.Should().BeNullOrEmpty();
        }

        [Fact]
        public void SeqShouldIncrementWhenHaveChanges()
        {
            var collector = new TelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings(), "Default service name", EmptyAasSettings);

            collector.HasChanges().Should().BeTrue();
            var data = collector.GetData();
            data.SeqId.Should().Be(1);

            // Counts as a "change"
            collector.RecordTracerSettings(new TracerSettings(), "Service name 2", EmptyAasSettings);

            collector.HasChanges().Should().BeTrue();
            data = collector.GetData();
            data.SeqId.Should().Be(2);
        }

        [Fact]
        public void WhenKnownIntegrationDoesNotRunHasExpectedValues()
        {
            var info = new SerializableIntegrationInfo(IntegrationIds.Kafka);
            var collector = new TelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings(), "Default Service Name", EmptyAasSettings);

            var data = collector.GetData();
            var integration = data.Integrations.FirstOrDefault(x => x.Name == info.IntegrationName);
            integration.Should().NotBeNull();
            integration.AutoEnabled.Should().BeFalse();
            integration.Enabled.Should().BeFalse();
            integration.Error.Should().BeNull();
        }

        [Fact]
        public void WhenKnownIntegrationDoesNotRunDoesNotAppearInData()
        {
            var info = new SerializableIntegrationInfo("Custom integration");
            var collector = new TelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings(), "Default Service Name", EmptyAasSettings);

            var data = collector.GetData();
            var integration = data.Integrations.FirstOrDefault(x => x.Name == info.IntegrationName);
            integration.Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(Integrations))]
        public void WhenIntegrationRunsSuccessfullyHasExpectedValues(SerializableIntegrationInfo info)
        {
            var collector = new TelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings(), "Default Service Name", EmptyAasSettings);

            collector.IntegrationRunning(info.Info);
            collector.IntegrationGeneratedSpan(info.Info);

            var data = collector.GetData();
            var integration = data.Integrations.FirstOrDefault(x => x.Name == info.IntegrationName);
            integration.Should().NotBeNull();
            integration.AutoEnabled.Should().BeTrue();
            integration.Enabled.Should().BeTrue();
            integration.Error.Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(Integrations))]
        public void WhenIntegrationRunsButDoesNotGenerateSpanHasExpectedValues(SerializableIntegrationInfo info)
        {
            var collector = new TelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings(), "Default Service Name", EmptyAasSettings);

            collector.IntegrationRunning(info.Info);

            var data = collector.GetData();
            var integration = data.Integrations.FirstOrDefault(x => x.Name == info.IntegrationName);
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
            var collector = new TelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings(), "Default Service Name", EmptyAasSettings);

            collector.IntegrationRunning(info.Info);
            collector.IntegrationDisabledDueToError(info.Info, error);

            var data = collector.GetData();
            var integration = data.Integrations.FirstOrDefault(x => x.Name == info.IntegrationName);
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
            var collector = new TelemetryCollector();
            collector.RecordTracerSettings(new TracerSettings(), "Default Service Name", EmptyAasSettings);

            collector.IntegrationRunning(info.Info);
            collector.IntegrationGeneratedSpan(info.Info);
            collector.IntegrationDisabledDueToError(info.Info, error);

            var data = collector.GetData();
            var integration = data.Integrations.FirstOrDefault(x => x.Name == info.IntegrationName);
            integration.Should().NotBeNull();
            integration.AutoEnabled.Should().BeTrue();
            integration.Enabled.Should().BeFalse();
            integration.Error.Should().Be(error);
        }

        private static AssemblyName CreateAssemblyName(Version version = null)
        {
            return new AssemblyName()
            {
                Name = "Datadog.Trace.Test.DynamicAssembly",
                Version = version,
            };
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
