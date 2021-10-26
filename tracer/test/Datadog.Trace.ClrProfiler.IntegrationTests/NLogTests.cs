// <copyright file="NLogTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Configuration;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Logging.DirectSubmission;
using Datadog.Trace.TestHelpers;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class NLogTests : LogsInjectionTestBase
    {
        private readonly LogFileTest[] _nlogPre40LogFileTests =
            {
                new LogFileTest()
                {
                    FileName = "log-textFile.log",
                    RegexFormat = @"{0}: {1}",
                    UnTracedLogTypes = UnTracedLogTypes.EmptyProperties,
                    PropertiesUseSerilogNaming = false
                }
            };

        private readonly LogFileTest[] _nlog40LogFileTests =
            {
                new LogFileTest()
                {
                    FileName = "log-textFile.log",
                    RegexFormat = @"{0}: {1}",
                    UnTracedLogTypes = UnTracedLogTypes.EmptyProperties,
                    PropertiesUseSerilogNaming = false
                },
                new LogFileTest()
                {
                    FileName = "log-jsonFile.log",
                    RegexFormat = @"""{0}"": {1}",
                    UnTracedLogTypes = UnTracedLogTypes.EmptyProperties,
                    PropertiesUseSerilogNaming = false
                }
            };

        private readonly LogFileTest[] _nlogPost40LogFileTests =
            {
                new LogFileTest()
                {
                    FileName = "log-textFile.log",
                    RegexFormat = @"{0}: {1}",
                    UnTracedLogTypes = UnTracedLogTypes.EmptyProperties,
                    PropertiesUseSerilogNaming = false
                },
                new LogFileTest()
                {
                    FileName = "log-jsonFile.log",
                    RegexFormat = @"""{0}"": {1}",
                    UnTracedLogTypes = UnTracedLogTypes.None,
                    PropertiesUseSerilogNaming = false
                }
            };

        public NLogTests(ITestOutputHelper output)
            : base(output, "LogsInjection.NLog")
        {
            SetServiceVersion("1.0.0");
        }

        public static IEnumerable<object[]> GetTestData()
        {
            foreach (var item in PackageVersions.NLog)
            {
                yield return item.Concat(false);
                yield return item.Concat(true);
            }
        }

        [SkippableTheory]
        [MemberData(nameof(GetTestData))]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void InjectsLogsWhenEnabled(string packageVersion, bool enableLogShipping)
        {
            SetEnvironmentVariable("DD_LOGS_INJECTION", "true");
            using var logsIntake = new MockLogsIntake();
            if (enableLogShipping)
            {
                EnableDirectLogSubmission(logsIntake.Port, nameof(IntegrationIds.NLog), nameof(InjectsLogsWhenEnabled));
            }

            int agentPort = TcpPortProvider.GetOpenPort();
            using (var agent = new MockTracerAgent(agentPort))
            using (RunSampleAndWaitForExit(agent.Port, packageVersion: packageVersion))
            {
                var spans = agent.WaitForSpans(1, 2500);
                Assert.True(spans.Count >= 1, $"Expecting at least 1 span, only received {spans.Count}");

                var testFiles = GetTestFiles(packageVersion);
                ValidateLogCorrelation(spans, testFiles, packageVersion);
            }
        }

        [SkippableTheory]
        [MemberData(nameof(GetTestData))]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void DoesNotInjectLogsWhenDisabled(string packageVersion, bool enableLogShipping)
        {
            SetEnvironmentVariable("DD_LOGS_INJECTION", "false");
            using var logsIntake = new MockLogsIntake();
            if (enableLogShipping)
            {
                EnableDirectLogSubmission(logsIntake.Port, nameof(IntegrationIds.NLog), nameof(InjectsLogsWhenEnabled));
            }

            int agentPort = TcpPortProvider.GetOpenPort();
            using (var agent = new MockTracerAgent(agentPort))
            using (RunSampleAndWaitForExit(agent.Port, packageVersion: packageVersion))
            {
                var spans = agent.WaitForSpans(1, 2500);
                Assert.True(spans.Count >= 1, $"Expecting at least 1 span, only received {spans.Count}");

                var testFiles = GetTestFiles(packageVersion);
                ValidateLogCorrelation(spans, testFiles, packageVersion, disableLogCorrelation: true);
            }
        }

        [SkippableTheory]
        [MemberData(nameof(PackageVersions.NLog), MemberType = typeof(PackageVersions))]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void DirectlyShipsLogs(string packageVersion)
        {
            var hostName = "integration_nlog_tests";
            using var logsIntake = new MockLogsIntake();

            SetEnvironmentVariable("DD_LOGS_INJECTION", "true");
            EnableDirectLogSubmission(logsIntake.Port, nameof(IntegrationIds.NLog), hostName);

            var agentPort = TcpPortProvider.GetOpenPort();
            using var agent = new MockTracerAgent(agentPort);
            using var processResult = RunSampleAndWaitForExit(agent.Port, packageVersion: packageVersion);

            Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode} and exception: {processResult.StandardError}");

            var logs = logsIntake.Logs;

            using var scope = new AssertionScope();
            logs.Should().NotBeNull();
            logs.Should().HaveCountGreaterOrEqualTo(3);
            logs.Should()
                .OnlyContain(x => x.Service == "LogsInjection.NLog")
                .And.OnlyContain(x => x.Env == "integration_tests")
                .And.OnlyContain(x => x.Version == "1.0.0")
                .And.OnlyContain(x => x.Host == hostName)
                .And.OnlyContain(x => x.Source == "csharp")
                .And.OnlyContain(x => x.Exception == null)
                .And.OnlyContain(x => x.LogLevel == DirectSubmissionLogLevel.Information);

            logs
               .Where(x => !x.Message.Contains(ExcludeMessagePrefix))
               .Should()
               .NotBeEmpty()
               .And.OnlyContain(x => !string.IsNullOrEmpty(x.TraceId))
               .And.OnlyContain(x => !string.IsNullOrEmpty(x.SpanId));
        }

        private LogFileTest[] GetTestFiles(string packageVersion)
            => packageVersion switch
            {
                null or "" => _nlogPre40LogFileTests,
                _ when new Version(packageVersion) >= new Version("4.1.0") => _nlogPost40LogFileTests,
                _ => _nlog40LogFileTests,
            };
    }
}
