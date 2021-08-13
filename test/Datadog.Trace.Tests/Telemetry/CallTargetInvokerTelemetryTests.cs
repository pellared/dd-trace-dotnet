// <copyright file="CallTargetInvokerTelemetryTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net.Http;
using Datadog.Trace.Agent;
using Datadog.Trace.AppSec;
using Datadog.Trace.ClrProfiler.AutoInstrumentation.Http.HttpClient.HttpClientHandler;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.Configuration;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.PlatformHelpers;
using Datadog.Trace.Sampling;
using Datadog.Trace.Telemetry;
using FluentAssertions;
using Moq;
using Xunit;

namespace Datadog.Trace.Tests.Telemetry
{
    [Collection(nameof(TracerInstanceTestCollection))]
    [TracerInstanceTestCollection.TracerRestorer]
    public class CallTargetInvokerTelemetryTests
    {
        // This test tests a lot at once because everything is heavily statically cached,
        // which makes tests brittle to order and concurrency etc
        [Fact]
        public void RecordsRelevantTelemetry()
        {
            var settings = new TracerSettings() { ServiceName = "DefaultService" };
            var telemetry = new TestTelemetryController();
            var tracer = new Tracer(
                settings,
                new Mock<IAgentWriter>().Object,
                new Mock<ISampler>().Object,
                scopeManager: null,
                statsd: null);

            Tracer.Instance = tracer;
            Datadog.Trace.Telemetry.Telemetry.Instance = telemetry;
            telemetry.RunningInvocations.Should().BeEmpty();

            try
            {
                CallTargetInvoker.BeginMethod<HttpClientHandlerIntegration, HttpClientHandler>(new HttpClientHandler());
            }
            catch (Exception)
            {
                // this will throw but we don't actually care
            }

            telemetry.RunningInvocations.Should().ContainSingle();

            var runInvocation = telemetry.RunningInvocations[0];
            runInvocation.Id.Should().Be((int)IntegrationIds.HttpMessageHandler);

            try
            {
                CallTargetInvoker.BeginMethod<HttpClientHandlerIntegration, HttpClientHandler>(new HttpClientHandler());
            }
            catch (Exception)
            {
                // this will throw but we don't actually care
            }

            telemetry.RunningInvocations.Should().HaveCount(2);
            telemetry.RunningInvocations.Should().AllBeEquivalentTo(runInvocation);

            // Now track errors

            var untrackedException = new Exception("I'm not tracked");
            CallTargetInvoker.LogException<HttpClientHandlerIntegration, HttpClientHandler>(untrackedException);
            telemetry.ErrorInvocations.Should().BeEmpty();

            // Either of these should work, but we can't test both, as we only record the first exception for now
            var exception = new DuckTypeException("A DuckTypeException occured");
            // var exception = new CallTargetInvokerException(new Exception("A CallTargetInvokerException occurred"));

            CallTargetInvoker.LogException<HttpClientHandlerIntegration, HttpClientHandler>(exception);
            telemetry.ErrorInvocations.Should().ContainSingle();

            var invocation = telemetry.ErrorInvocations[0];
            invocation.Info.Id.Should().Be((int)IntegrationIds.HttpMessageHandler);
            invocation.Error.Should().Be(nameof(DuckTypeException));
        }

        internal class TestTelemetryController : ITelemetryController
        {
            public List<IntegrationInfo> RunningInvocations { get; } = new();

            public List<(IntegrationInfo Info, string Error)> ErrorInvocations { get; } = new();

            public void IntegrationRunning(IntegrationInfo info)
            {
                RunningInvocations.Add(info);
            }

            public void IntegrationGeneratedSpan(IntegrationInfo info)
            {
            }

            public void IntegrationDisabledDueToError(IntegrationInfo info, string error)
            {
                ErrorInvocations.Add((info, error));
            }

            public void RecordTracerSettings(TracerSettings settings, string defaultServiceName, AzureAppServices appServicesMetadata)
            {
            }

            public void RecordSecuritySettings(SecuritySettings settings)
            {
            }
        }
    }
}
