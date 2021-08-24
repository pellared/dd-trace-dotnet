// <copyright file="IntegrationTelemetryCollector.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Datadog.Trace.Configuration;

namespace Datadog.Trace.Telemetry
{
    internal class IntegrationTelemetryCollector
    {
        private readonly ConcurrentDictionary<string, IntegrationDetail> _integrationsByName = new();
        private readonly IntegrationDetail[] _integrationsById;

        private int _hasChangesFlag = 0;

        public IntegrationTelemetryCollector()
        {
            _integrationsById = new IntegrationDetail[IntegrationRegistry.Names.Length];

            for (var i = 0; i < IntegrationRegistry.Names.Length; i++)
            {
                _integrationsById[i] = new IntegrationDetail { Name = IntegrationRegistry.Names[i] };
            }
        }

        public void RecordTracerSettings(TracerSettings settings)
        {
            foreach (var integration in settings.DisabledIntegrationNames)
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
            foreach (var adoNetType in settings.AdoNetExcludedTypes)
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

            SetHasChanges();
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
            return _hasChangesFlag == 1;
        }

        /// <summary>
        /// Get the latest data to send to the intake.
        /// </summary>
        /// <returns>Null if there are no changes, or the collector is not yet initialized</returns>
        public ICollection<IntegrationTelemetryData> GetData()
        {
            var hasChanges = Interlocked.CompareExchange(ref _hasChangesFlag, 0, 1) == 1;
            if (!hasChanges)
            {
                return null;
            }

            var integrations = _integrationsById.Concat(
                _integrationsByName.ToArray().Select(x => x.Value));

            return integrations
                  .Select(
                       integration =>
                       {
                           var error = integration.Error;

                           return new IntegrationTelemetryData
                           {
                               Name = integration.Name,
                               Enabled = integration.HasGeneratedSpan > 0
                                      && integration.WasExplicitlyDisabled == 0
                                      && string.IsNullOrEmpty(error),
                               AutoEnabled = integration.WasExecuted > 0,
                               Error = error
                           };
                       })
                  .ToList();
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
