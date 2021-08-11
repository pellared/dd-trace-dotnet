// <copyright file="TelemetryData.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;

namespace Datadog.Trace.Telemetry
{
    /// <summary>
    /// DTO that is serialized.
    /// Be aware that the property names control serialization
    /// </summary>
    internal class TelemetryData
    {
        public string RuntimeId { get; set; }

        public int SeqId { get; set; }

        public string ServiceName { get; set; }

        public string Env { get; set; }

        public long StartedAt { get; set; }

        public string ServiceVersion { get; set; }

        public string TracerVersion { get; set; }

        public string LanguageName { get; set; }

        public string LanguageVersion { get; set; }

        public List<Integration> Integrations { get; set; } = new();

        public List<Dependency> Dependencies { get; set; } = new();

        public Config Configuration { get; set; } = new();

        public Dictionary<string, object> AdditionalPayload { get; set; } = new();

        public class Integration
        {
            public string Name { get; set; }

            public bool Enabled { get; set; }

            public bool? AutoEnabled { get; set; }

            public bool? Compatible { get; set; }

            public string Error { get; set; }
        }

        public class Dependency
        {
            public string Name { get; set; }

            public string Version { get; set; }
        }

        public class Config
        {
            public string OsName { get; set; }

            public string OsVersion { get; set; }

            public string Platform { get; set; }

            public bool? Enabled { get; set; }

            public string AgentUrl { get; set; }

            public bool? Debug { get; set; }

            public bool? AnalyticsEnabled { get; set; }

            public double? SampleRate { get; set; }

            public string SamplingRules { get; set; }

            public bool? LogInjectionEnabled { get; set; }

            public bool? RuntimeMetricsEnabled { get; set; }

            public bool? NetstandardEnabled { get; set; }

            public bool? RoutetemplateResourcenamesEnabled { get; set; }

            public bool? PartialflushEnabled { get; set; }

            public int? PartialflushMinspans { get; set; }

            public int? TracerInstanceCount { get; set; }

            public string CloudHosting { get; set; }

            public bool? AasConfigurationError { get; set; }

            public string AasSiteExtensionVersion { get; set; }

            public string AasAppType { get; set; }

            public string AasFunctionsRuntimeVersion { get; set; }

            public bool? SecurityEnabled { get; set; }

            public bool? SecurityBlockingEnabled { get; set; }
        }
    }
}
