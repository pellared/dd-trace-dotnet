// <copyright file="TelemetryTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Linq;
using Datadog.Trace.Configuration;
using Datadog.Trace.Telemetry;
using Datadog.Trace.TestHelpers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class TelemetryTests : TestHelper
    {
        public TelemetryTests(ITestOutputHelper output)
            : base("Telemetry", output)
        {
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [Trait("Category", "EndToEnd")]
        public void Telemetry_IsSentOnAppClose(bool enableCallTarget)
        {
            const string expectedOperationName = "http.request";
            const int expectedSpanCount = 1;
            const string serviceVersion = "1.0.0";

            int telemetryPort = TcpPortProvider.GetOpenPort();
            Output.WriteLine($"Assigning port {telemetryPort} for telemetry.");
            using var telemetry = new MockTelemetryAgent<TelemetryData>(telemetryPort);

            int agentPort = TcpPortProvider.GetOpenPort();
            Output.WriteLine($"Assigning port {agentPort} for the agentPort.");
            using var agent = new MockTracerAgent(agentPort);

            SetCallTargetSettings(enableCallTarget);
            SetServiceVersion(serviceVersion);
            SetEnvironmentVariable("DD_TRACE_DEBUG", "1");
            SetEnvironmentVariable("DD_TRACE_TELEMETRY_ENABLED", "true");
            SetEnvironmentVariable("DD_TRACE_TELEMETRY_URL", $"http://localhost:{telemetryPort}");

            int httpPort = TcpPortProvider.GetOpenPort();
            Output.WriteLine($"Assigning port {httpPort} for the httpPort.");
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent.Port, arguments: $"Port={httpPort}"))
            {
                Assert.True(processResult.ExitCode == 0, $"Process exited with code {processResult.ExitCode}");

                var spans = agent.WaitForSpans(expectedSpanCount, operationName: expectedOperationName);
                Assert.Equal(expectedSpanCount, spans.Count);

                var latestData = telemetry.WaitForLatestTelemetry(_ => true);

                latestData.Should().NotBeNull();
                latestData.ServiceVersion.Should().Be(serviceVersion);
                latestData.ServiceName.Should().Be("Samples.Telemetry");
                var httpHandler = latestData.Integrations.FirstOrDefault(x => x.Name == nameof(IntegrationIds.HttpMessageHandler));
                httpHandler.Enabled.Should().BeTrue();
                httpHandler.AutoEnabled.Should().BeTrue();
            }
        }
    }
}
