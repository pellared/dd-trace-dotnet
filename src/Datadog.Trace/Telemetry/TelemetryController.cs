// <copyright file="TelemetryController.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Reflection;
using System.Threading.Tasks;
using Datadog.Trace.AppSec;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;
using Datadog.Trace.PlatformHelpers;

namespace Datadog.Trace.Telemetry
{
    internal class TelemetryController : ITelemetryController, IDisposable
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<TelemetryCollector>();
        private readonly TelemetryCollector _collector;
        private readonly ITelemetryTransport _transport;
        private readonly TimeSpan _sendFrequency;
        private readonly TaskCompletionSource<bool> _processExit = new();
        private readonly Task _telemetryTask;

        internal TelemetryController(
            TelemetryCollector collector,
            ITelemetryTransport transport,
            TimeSpan sendFrequency)
        {
            _collector = collector ?? throw new ArgumentNullException(nameof(collector));
            _sendFrequency = sendFrequency;
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));

            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_OnAssemblyLoad;
            var assembliesLoaded = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var t in assembliesLoaded)
            {
                RecordAssembly(t);
            }

            _telemetryTask = Task.Run(PushTelemetryLoopAsync);
        }

        public void RecordTracerSettings(TracerSettings settings, string defaultServiceName, AzureAppServices appServicesMetadata)
            => _collector.RecordTracerSettings(settings, defaultServiceName, appServicesMetadata);

        public void RecordSecuritySettings(SecuritySettings settings)
            => _collector.RecordSecuritySettings(settings);

        public void IntegrationRunning(IntegrationInfo info)
            => _collector.IntegrationRunning(info);

        public void IntegrationGeneratedSpan(IntegrationInfo info)
            => _collector.IntegrationGeneratedSpan(info);

        public void IntegrationDisabledDueToError(IntegrationInfo info, string error)
            => _collector.IntegrationDisabledDueToError(info, error);

        public void Dispose()
        {
            _processExit.TrySetResult(true);
            AppDomain.CurrentDomain.AssemblyLoad -= CurrentDomain_OnAssemblyLoad;
            _telemetryTask.GetAwaiter().GetResult();
        }

        private void CurrentDomain_OnAssemblyLoad(object sender, AssemblyLoadEventArgs e)
        {
            RecordAssembly(e.LoadedAssembly);
        }

        private void RecordAssembly(Assembly assembly)
        {
            try
            {
                _collector.AssemblyLoaded(assembly.GetName());
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error recording loaded assembly");
            }
        }

        private async Task PushTelemetryLoopAsync()
        {
#if !NET5_0_OR_GREATER
            var tasks = new Task[2];
            tasks[0] = _processExit.Task;
#endif
            while (true)
            {
                await PushTelemetry().ConfigureAwait(false);

                if (_processExit.Task.IsCompleted)
                {
                    Log.Debug("Process exit requested, ending telemetry loop");
                    return;
                }

#if NET5_0_OR_GREATER
                // .NET 5.0 has an explicit overload for this
                await Task.WhenAny(
                               Task.Delay(_bucketDuration),
                               _processExit.Task)
                          .ConfigureAwait(false);
#else
                tasks[1] = Task.Delay(_sendFrequency);
                await Task.WhenAny(tasks).ConfigureAwait(false);
#endif
            }
        }

        private async Task PushTelemetry()
        {
            try
            {
                if (!_collector.HasChanges())
                {
                    Log.Debug("No telemetry changes found, skipping");
                    return;
                }

                var data = _collector.GetData();
                if (data is null)
                {
                    Log.Debug("No telemetry data found, skipping");
                    return;
                }

                Log.Debug("Pushing telemetry changes");
                await _transport.PushTelemetry(data).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error pushing telemetry");
            }
        }
    }
}
