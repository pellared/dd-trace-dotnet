// <copyright file="SerilogTests.cs" company="Datadog">
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
    public class SerilogTests : LogsInjectionTestBase
    {
        private readonly LogFileTest[] _log200FileTests =
            {
                new LogFileTest()
                {
                    FileName = "log-textFile.log",
                    RegexFormat = @"{0}: {1}",
                    UnTracedLogTypes = UnTracedLogTypes.EmptyProperties,
                    PropertiesUseSerilogNaming = true
                },
                new LogFileTest()
                {
                    FileName = "log-jsonFile.log",
                    RegexFormat = @"""{0}"":{1}",
                    UnTracedLogTypes = UnTracedLogTypes.None,
                    PropertiesUseSerilogNaming = true
                }
            };

        private readonly LogFileTest[] _logPre200FileTests =
        {
            new LogFileTest()
            {
                FileName = "log-textFile.log",
                RegexFormat = @"{0}: {1}",
                TracedLogTypes = TracedLogTypes.NotCorrelated,
                UnTracedLogTypes = UnTracedLogTypes.EmptyProperties,
                PropertiesUseSerilogNaming = true
            }
        };

        public SerilogTests(ITestOutputHelper output)
            : base(output, "LogsInjection.Serilog")
        {
            SetServiceVersion("1.0.0");
        }

        public static IEnumerable<object[]> GetTestData()
        {
            foreach (var item in PackageVersions.Serilog)
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
                EnableDirectLogSubmission(logsIntake.Port, nameof(IntegrationIds.Log4Net), nameof(InjectsLogsWhenEnabled));
            }

            int agentPort = TcpPortProvider.GetOpenPort();
            using (var agent = new MockTracerAgent(agentPort))
            using (RunSampleAndWaitForExit(agent.Port, packageVersion: packageVersion))
            {
                var spans = agent.WaitForSpans(1, 2500);
                Assert.True(spans.Count >= 1, $"Expecting at least 1 span, only received {spans.Count}");

                if (PackageSupportsLogsInjection(packageVersion))
                {
                    ValidateLogCorrelation(spans, _log200FileTests, packageVersion);
                }
                else
                {
                    // We do not expect logs injection for Serilog versions < 2.0.0 so filter out all logs
                    ValidateLogCorrelation(spans, _logPre200FileTests, packageVersion);
                }
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
                EnableDirectLogSubmission(logsIntake.Port, nameof(IntegrationIds.Log4Net), nameof(InjectsLogsWhenEnabled));
            }

            int agentPort = TcpPortProvider.GetOpenPort();
            using (var agent = new MockTracerAgent(agentPort))
            using (RunSampleAndWaitForExit(agent.Port, packageVersion: packageVersion))
            {
                var spans = agent.WaitForSpans(1, 2500);
                Assert.True(spans.Count >= 1, $"Expecting at least 1 span, only received {spans.Count}");

                if (PackageSupportsLogsInjection(packageVersion))
                {
                    ValidateLogCorrelation(spans, _log200FileTests, packageVersion, disableLogCorrelation: true);
                }
                else
                {
                    // We do not expect logs injection for Serilog versions < 2.0.0 so filter out all logs
                    ValidateLogCorrelation(spans, _logPre200FileTests, packageVersion, disableLogCorrelation: true);
                }
            }
        }

        [SkippableTheory]
        [MemberData(nameof(PackageVersions.Serilog), MemberType = typeof(PackageVersions))]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void DirectlyShipsLogs(string packageVersion)
        {
            var hostName = "integration_serilog_tests";
            using var logsIntake = new MockLogsIntake();

            SetEnvironmentVariable("DD_LOGS_INJECTION", "true");
            EnableDirectLogSubmission(logsIntake.Port, nameof(IntegrationIds.Serilog), hostName);

            var agentPort = TcpPortProvider.GetOpenPort();
            using var agent = new MockTracerAgent(agentPort);
            using var processResult = RunSampleAndWaitForExit(agent.Port, packageVersion: packageVersion);

            Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode} and exception: {processResult.StandardError}");

            var logs = logsIntake.Logs;

            using var scope = new AssertionScope();
            logs.Should().NotBeNull();
            logs.Should().HaveCountGreaterOrEqualTo(3);
            logs.Should()
                .OnlyContain(x => x.Service == "LogsInjection.Serilog")
                .And.OnlyContain(x => x.Env == "integration_tests")
                .And.OnlyContain(x => x.Version == "1.0.0")
                .And.OnlyContain(x => x.Host == hostName)
                .And.OnlyContain(x => x.Source == "csharp")
                .And.OnlyContain(x => x.Exception == null)
                .And.OnlyContain(x => x.LogLevel == DirectSubmissionLogLevel.Information);

            if (PackageSupportsLogsInjection(packageVersion))
            {
                logs
                   .Where(x => !x.Message.Contains(ExcludeMessagePrefix))
                   .Should()
                   .NotBeEmpty()
                   .And.OnlyContain(x => !string.IsNullOrEmpty(x.TraceId))
                   .And.OnlyContain(x => !string.IsNullOrEmpty(x.SpanId));
            }
        }

        private static bool PackageSupportsLogsInjection(string packageVersion)
#if NETCOREAPP
            // enabled in default version for .NET Core
            => string.IsNullOrWhiteSpace(packageVersion) || new Version(packageVersion) >= new Version("2.0.0");
#else
            // disabled dor default version in .NET Framework
            => !string.IsNullOrWhiteSpace(packageVersion) && new Version(packageVersion) >= new Version("2.0.0");
#endif

    }
}
