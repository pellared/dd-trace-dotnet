// <copyright file="TelemetryCollector.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using Datadog.Trace.AppSec;
using Datadog.Trace.Configuration;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.PlatformHelpers;

namespace Datadog.Trace.Telemetry
{
    internal class TelemetryCollector
    {
        private readonly ConcurrentDictionary<Tuple<string, string>, string> _assemblies = new();
        private readonly ConcurrentDictionary<string, IntegrationDetail> _integrationsByName = new();

        private IntegrationDetail[] _integrationsById;
        private int _tracerInstanceCount = 0;
        private int _hasChangesFlag = 0;
        private TracerSettings _settings;
        private SecuritySettings _securitySettings;
        private string _defaultServiceName;
        private AzureAppServices _azureApServicesMetadata;
        private bool _isInitialized = false;
        private long _startedAt;
        private int _sequence = 0;

        public void RecordTracerSettings(
            TracerSettings settings,
            string defaultServiceName,
            AzureAppServices appServicesMetadata)
        {
            // Increment number of tracer instances
            var tracerCount = Interlocked.Increment(ref _tracerInstanceCount);
            if (tracerCount != 1)
            {
                // We only record configuration telemetry from the first Tracer created
                SetHasChanges();
                return;
            }

            _settings = settings;
            _defaultServiceName = defaultServiceName;
            _azureApServicesMetadata = appServicesMetadata;
            _startedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _integrationsById = new IntegrationDetail[IntegrationRegistry.Names.Length];

            for (var i = 0; i < IntegrationRegistry.Names.Length; i++)
            {
                _integrationsById[i] = new IntegrationDetail { Name = IntegrationRegistry.Names[i] };
            }

            foreach (var integration in _settings.DisabledIntegrationNames)
            {
                if (IntegrationRegistry.Ids.TryGetValue(integration, out var id))
                {
                    _integrationsById[id].WasExplicitlyDisabled = 1;
                }

                _integrationsByName.AddOrUpdate(
                    integration,
                    name => new IntegrationDetail { Name = name, WasExplicitlyDisabled = 1 },
                    (_, previous) =>
                    {
                        Interlocked.Exchange(ref previous.WasExplicitlyDisabled, 1);
                        return previous;
                    });
            }

            // Not sure if this is worth doing, but seems useful to record them?
            foreach (var adoNetType in _settings.AdoNetExcludedTypes)
            {
                _integrationsByName.AddOrUpdate(
                    adoNetType,
                    name => new IntegrationDetail { Name = name, WasExplicitlyDisabled = 1 },
                    (_, previous) =>
                    {
                        Interlocked.Exchange(ref previous.WasExplicitlyDisabled, 1);
                        return previous;
                    });
            }

            _isInitialized = true;
            SetHasChanges();
        }

        public void RecordSecuritySettings(SecuritySettings securitySettings)
        {
            _securitySettings = securitySettings;
            SetHasChanges();
        }

        /// <summary>
        /// Called when an assembly is loaded
        /// </summary>
        public void AssemblyLoaded(AssemblyName assembly)
        {
            // TODO: Filter out assemblies we don't care about
            var key = new Tuple<string, string>(assembly.Name, assembly.Version?.ToString());
            if (_assemblies.TryAdd(key, null))
            {
                SetHasChanges();
            }
        }

        /// <summary>
        /// Should be called when an integration is first executed (not necessarily successfully)
        /// </summary>
        public void IntegrationRunning(IntegrationInfo info)
        {
            var previousValue = 0;
            if (info.Name is null)
            {
                previousValue = Interlocked.Exchange(ref _integrationsById[info.Id].WasExecuted, 1);
            }
            else
            {
                _integrationsByName.AddOrUpdate(
                    info.Name,
                    name => new IntegrationDetail { Name = name, WasExecuted = 1, },
                    (_, previous) =>
                    {
                        previousValue = Interlocked.Exchange(ref previous.WasExecuted, 1);
                        return previous;
                    });
            }

            if (previousValue == 0)
            {
                SetHasChanges();
            }
        }

        /// <summary>
        /// Should be called when an integration successfully generates a span
        /// </summary>
        public void IntegrationGeneratedSpan(IntegrationInfo info)
        {
            var previousWasExecuted = 0;
            var previousHasGeneratedSpan = 0;
            if (info.Name is null)
            {
                previousWasExecuted = Interlocked.Exchange(ref _integrationsById[info.Id].WasExecuted, 1);
                previousHasGeneratedSpan = Interlocked.Exchange(ref _integrationsById[info.Id].HasGeneratedSpan, 1);
            }
            else
            {
                _integrationsByName.AddOrUpdate(
                    info.Name,
                    name => new IntegrationDetail { Name = name, HasGeneratedSpan = 1, WasExecuted = 1, },
                    (_, previous) =>
                    {
                        previousWasExecuted = Interlocked.Exchange(ref previous.WasExecuted, 1);
                        previousHasGeneratedSpan = Interlocked.Exchange(ref previous.HasGeneratedSpan, 1);
                        return previous;
                    });
            }

            if (previousWasExecuted == 0 || previousHasGeneratedSpan == 0)
            {
                SetHasChanges();
            }
        }

        public void IntegrationDisabledDueToError(IntegrationInfo info, string error)
        {
            string previousValue = null;
            if (info.Name is null)
            {
                previousValue = Interlocked.CompareExchange(ref _integrationsById[info.Id].Error, error, null);
            }
            else
            {
                _integrationsByName.AddOrUpdate(
                    info.Name,
                    name => new IntegrationDetail { Name = name, Error = error, },
                    (_, previous) =>
                    {
                        previousValue = Interlocked.CompareExchange(ref previous.Error, error, null);
                        return previous;
                    });
            }

            if (previousValue is null)
            {
                SetHasChanges();
            }
        }

        public bool HasChanges()
        {
            return _isInitialized && _hasChangesFlag == 1;
        }

        /// <summary>
        /// Get the latest data to send to the intake.
        /// </summary>
        /// <returns>Null if there are no changes, or the collector is not yet initialized</returns>
        public TelemetryData GetData()
        {
            var hasChanges = Interlocked.CompareExchange(ref _hasChangesFlag, 0, 1) == 1;
            if (!_isInitialized || !hasChanges)
            {
                return null;
            }

            _sequence++;

            var data = new TelemetryData
            {
                RuntimeId = Tracer.RuntimeId,
                SeqId = _sequence,
                StartedAt = _startedAt,
                ServiceName = _defaultServiceName,
                Env = _settings.Environment,
                ServiceVersion = _settings.ServiceVersion,
                TracerVersion = TracerConstants.AssemblyVersion,
                LanguageName = FrameworkDescription.Instance.Name,
                LanguageVersion = FrameworkDescription.Instance.ProductVersion,
            };

            var integrations = _integrationsById.Concat(
                _integrationsByName.ToArray().Select(x => x.Value));

            foreach (var integration in integrations)
            {
                var error = integration.Error;

                data.Integrations.Add(new TelemetryData.Integration
                {
                    Name = integration.Name,
                    Enabled = integration.HasGeneratedSpan > 0
                           && integration.WasExplicitlyDisabled == 0
                           && string.IsNullOrEmpty(error),
                    AutoEnabled = integration.WasExecuted > 0,
                    Error = error
                });
            }

            foreach (var kvp in _assemblies.ToArray())
            {
                var dependency = kvp.Key;
                data.Dependencies.Add(new TelemetryData.Dependency { Name = dependency.Item1, Version = dependency.Item2, });
            }

            data.Configuration.OsName = FrameworkDescription.Instance.OSPlatform;
            data.Configuration.OsVersion = Environment.OSVersion.ToString();
            data.Configuration.Platform = FrameworkDescription.Instance.ProcessArchitecture;
            data.Configuration.Enabled = _settings.TraceEnabled;
            data.Configuration.AgentUrl = _settings.AgentUri.ToString();
            data.Configuration.Debug = GlobalSettings.Source.DebugEnabled;
            data.Configuration.AnalyticsEnabled = _settings.AnalyticsEnabled;
            data.Configuration.SampleRate = _settings.GlobalSamplingRate;
            data.Configuration.SamplingRules = _settings.CustomSamplingRules;
            data.Configuration.LogInjectionEnabled = _settings.LogsInjectionEnabled;
            data.Configuration.RuntimeMetricsEnabled = _settings.RuntimeMetricsEnabled;
            data.Configuration.NetstandardEnabled = _settings.IsNetStandardFeatureFlagEnabled();
            data.Configuration.RoutetemplateResourcenamesEnabled = _settings.RouteTemplateResourceNamesEnabled;
            data.Configuration.PartialflushEnabled = _settings.PartialFlushEnabled;
            data.Configuration.PartialflushMinspans = _settings.PartialFlushMinSpans;
            data.Configuration.TracerInstanceCount = _tracerInstanceCount;
            data.Configuration.AasConfigurationError = _azureApServicesMetadata.IsUnsafeToTrace;

            if (_azureApServicesMetadata.IsRelevant)
            {
                data.Configuration.CloudHosting = "Azure";
                data.Configuration.AasSiteExtensionVersion = _azureApServicesMetadata.SiteExtensionVersion;
                data.Configuration.AasAppType = _azureApServicesMetadata.SiteType;
                data.Configuration.AasFunctionsRuntimeVersion = _azureApServicesMetadata.FunctionsExtensionVersion;
            }

            if (_securitySettings is not null)
            {
                data.Configuration.SecurityEnabled = _securitySettings.Enabled;
                data.Configuration.SecurityBlockingEnabled = _securitySettings.BlockingEnabled;
            }

            // data.Configuration["agent_reachable"] = agentError == null;
            // data.Configuration["agent_error"] = agentError ?? string.Empty;

            // Global tags?
            // Agent reachable
            // Agent error
            // Is CallTarget

            // additional values
            // Native metrics

            return data;
        }

        private void SetHasChanges()
        {
            Interlocked.Exchange(ref _hasChangesFlag, 1);
        }

        internal struct IntegrationDetail
        {
            /// <summary>
            /// Gets or sets the integration info of the integration
            /// </summary>
            public string Name;

            /// <summary>
            /// Gets or sets a value indicating whether an integration successfully generated a span
            /// 0 = not generated, 1 = generated
            /// </summary>
            public int HasGeneratedSpan;

            /// <summary>
            /// Gets or sets a value indicating whether the integration ever executed
            /// 0 = not generated, 1 = generated
            /// </summary>
            public int WasExecuted;

            /// <summary>
            /// Gets or sets a value indicating whether the integration was disabled by a user
            /// 0 = not generated, 1 = generated
            /// </summary>
            public int WasExplicitlyDisabled;

            /// <summary>
            /// Gets or sets a value indicating whether an integration was disabled due to a fatal error
            /// </summary>
            public string Error;
        }
    }
}
