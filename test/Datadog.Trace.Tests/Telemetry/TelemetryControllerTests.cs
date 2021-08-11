// <copyright file="TelemetryControllerTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.Configuration;
using Datadog.Trace.PlatformHelpers;
using Datadog.Trace.Telemetry;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Datadog.Trace.Tests.Telemetry
{
    public class TelemetryControllerTests
    {
        private static readonly AzureAppServices EmptyAasMetadata = new(new Dictionary<string, string>());
        private readonly TimeSpan _refreshInterval = TimeSpan.FromMilliseconds(100);
        private readonly TimeSpan _timeout = TimeSpan.FromMilliseconds(10_000); // definitely should receive telemetry by now

        [Fact]
        public async Task TelemetryControllerShouldSendTelemetry()
        {
            var collector = new TelemetryCollector();
            var transport = new TestTelemetryTransport();
            using var controller = new TelemetryController(collector, transport, _refreshInterval);
            controller.RecordTracerSettings(new TracerSettings(), "DefaultServiceName", EmptyAasMetadata);

            var data = await WaitForData(transport, 1, _timeout);
        }

        [Fact]
        public async Task TelemetryControllerAddsAllAssembliesToCollector()
        {
            var currentAssemblyNames = AppDomain.CurrentDomain
                                                .GetAssemblies()
                                                .Select(x => x.GetName());

            var collector = new TelemetryCollector();
            var transport = new TestTelemetryTransport();
            using var controller = new TelemetryController(collector, transport, _refreshInterval);
            controller.RecordTracerSettings(new TracerSettings(), "DefaultServiceName", EmptyAasMetadata);

            var allData = await WaitForData(transport, 1, _timeout);
            var data = allData.OrderBy(x => x.SeqId).Last();

            // should contain all the assemblies
            using var a = new AssertionScope();
            foreach (var assemblyName in currentAssemblyNames)
            {
                data.Dependencies.Should().ContainSingle(x => x.Name == assemblyName.Name && x.Version == assemblyName.Version.ToString());
            }
        }

        private async Task<List<TelemetryData>> WaitForData(TestTelemetryTransport transport, int requiredCount, TimeSpan timeout)
        {
            var deadline = DateTimeOffset.UtcNow.Add(timeout);
            while (DateTimeOffset.UtcNow < deadline)
            {
                var data = transport.GetData();
                if (data.Count >= requiredCount)
                {
                    return data;
                }

                await Task.Delay(_refreshInterval);
            }

            throw new Exception($"Transport did not receive {requiredCount} data before the timeout {timeout.TotalMilliseconds}ms");
        }

        internal class TestTelemetryTransport : ITelemetryTransport
        {
            private readonly ConcurrentStack<TelemetryData> _data = new();

            public List<TelemetryData> GetData()
            {
                return _data.ToList();
            }

            public Task PushTelemetry(TelemetryData data)
            {
                _data.Push(data);
                return Task.FromResult(0);
            }
        }
    }
}
