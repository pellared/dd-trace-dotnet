// <copyright file="Telemetry.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Threading;
using Datadog.Trace.Logging;

namespace Datadog.Trace.Telemetry
{
    /// <summary>
    /// The <see cref="Telemetry"/> class is responsible for coordinating internal telemetry
    /// </summary>
    internal class Telemetry
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<Tracer>();

        private static ITelemetryController _instance;
        private static bool _globalInstanceInitialized;
        private static object _globalInstanceLock = new object();

        /// <summary>
        /// Gets or sets the global <see cref="ITelemetryController"/> instance.
        /// </summary>
        public static ITelemetryController Instance
        {
            get
            {
                return LazyInitializer.EnsureInitialized(
                    ref _instance,
                    ref _globalInstanceInitialized,
                    ref _globalInstanceLock,
                    CreateTelemetryController);
            }

            set
            {
                lock (_globalInstanceLock)
                {
                    _globalInstanceInitialized = true;
                    _instance = value;
                }
            }
        }

        private static ITelemetryController CreateTelemetryController()
            => CreateTelemetryController(TelemetrySettings.FromDefaultSources());

        internal static ITelemetryController CreateTelemetryController(TelemetrySettings settings)
        {
            if (settings.TelemetryEnabled)
            {
                try
                {
                    var controller = new TelemetryController(
                        new TelemetryCollector(),
                        new TelemetryTransportFactory(settings.TelemetryUrl).Create(),
                        TelemetryConstants.RefreshInterval);

                    RegisterShutdownTasks();

                    return controller;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error initializing telemetry. Telemetry collection disabled.");
                }
            }

            return NullTelemetryController.Instance;
        }

        private static void RegisterShutdownTasks()
        {
            // Register callbacks to make sure we flush the traces before exiting
            AppDomain.CurrentDomain.ProcessExit += ProcessExit;
            AppDomain.CurrentDomain.DomainUnload += DomainUnload;

            try
            {
                // Registering for the AppDomain.UnhandledException event cannot be called by a security transparent method
                // This will only happen if the Tracer is not run full-trust
                AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to register a callback to the AppDomain.UnhandledException event.");
            }

            try
            {
                // Registering for the cancel key press event requires the System.Security.Permissions.UIPermission
                Console.CancelKeyPress += CancelKeyPress;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to register a callback to the Console.CancelKeyPress event.");
            }
        }

        private static void ProcessExit(object sender, EventArgs e)
        {
            AppDomain.CurrentDomain.ProcessExit -= ProcessExit;
            RunShutdownTasks();
        }

        private static void DomainUnload(object sender, EventArgs e)
        {
            AppDomain.CurrentDomain.DomainUnload -= DomainUnload;
            RunShutdownTasks();
        }

        private static void CancelKeyPress(object sender, EventArgs e)
        {
            Console.CancelKeyPress -= CancelKeyPress;
            RunShutdownTasks();
        }

        private static void UnhandledException(object sender, EventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException -= UnhandledException;
            RunShutdownTasks();
        }

        private static void RunShutdownTasks()
        {
            try
            {
                (_instance as IDisposable)?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error flushing telemetry on shutdown.");
            }
        }
    }
}
