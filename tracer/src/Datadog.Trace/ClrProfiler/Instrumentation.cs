// <copyright file="Instrumentation.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Datadog.Trace.AppSec;
using Datadog.Trace.Ci;
using Datadog.Trace.Configuration;
using Datadog.Trace.DiagnosticListeners;
using Datadog.Trace.Logging;
using Datadog.Trace.ServiceFabric;

namespace Datadog.Trace.ClrProfiler
{
    /// <summary>
    /// Provides access to the profiler CLSID and whether it is attached to the process.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class Instrumentation
    {
        /// <summary>
        /// Indicates whether we're initializing Instrumentation for the first time
        /// </summary>
        private static int _firstInitialization = 1;

        /// <summary>
        /// Gets the CLSID for the Datadog .NET profiler
        /// </summary>
        public static readonly string ProfilerClsid = "{846F5F1C-F9AE-4B07-969E-05C26BC060D8}";

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(Instrumentation));

        /// <summary>
        /// Gets a value indicating whether Datadog's profiler is attached to the current process.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the profiler is currently attached; <c>false</c> otherwise.
        /// </value>
        public static bool ProfilerAttached
        {
            get
            {
                try
                {
                    return NativeMethods.IsProfilerAttached();
                }
                catch (DllNotFoundException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Initializes global instrumentation values.
        /// </summary>
        public static void Initialize()
        {
            if (Interlocked.Exchange(ref _firstInitialization, 0) != 1)
            {
                // Initialize() was already called before
                return;
            }

            Log.Debug("Initialization started.");

            try
            {
                Log.Debug("Sending CallTarget integration definitions to native library.");
                var payload = InstrumentationDefinitions.GetAllDefinitions();
                NativeMethods.InitializeProfiler(payload.DefinitionsId, payload.Definitions);
                foreach (var def in payload.Definitions)
                {
                    def.Dispose();
                }

                Log.Information<int>("The profiler has been initialized with {count} definitions.", payload.Definitions.Length);

                var asm = typeof(Instrumentation).Assembly;
                Log.Information($"[Assembly metadata] Location: {asm.Location}, CodeBase: {asm.CodeBase}, GAC: {asm.GlobalAssemblyCache}, HostContext: {asm.HostContext}, SecurityRuleSet: {asm.SecurityRuleSet}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
            }

            try
            {
                // ensure global instance is created if it's not already
                if (CIVisibility.Enabled)
                {
                    CIVisibility.Initialize();
                }
                else
                {
                    Log.Debug("Initializing tracer singleton instance.");
                    _ = Tracer.Instance;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
            }

            try
            {
                Log.Debug("Initializing security singleton instance.");
                _ = Security.Instance;
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
            }

#if !NETFRAMEWORK
            try
            {
                if (GlobalSettings.Source.DiagnosticSourceEnabled)
                {
                    // check if DiagnosticSource is available before trying to use it
                    var type = Type.GetType("System.Diagnostics.DiagnosticSource, System.Diagnostics.DiagnosticSource", throwOnError: false);

                    if (type == null)
                    {
                        Log.Warning("DiagnosticSource type could not be loaded. Skipping diagnostic observers.");
                    }
                    else
                    {
                        // don't call this method unless DiagnosticSource is available
                        StartDiagnosticManager();
                    }
                }
            }
            catch
            {
                // ignore
            }

            // we only support Service Fabric Service Remoting instrumentation on .NET Core (including .NET 5+)
            if (string.Equals(FrameworkDescription.Instance.Name, ".NET Core", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(FrameworkDescription.Instance.Name, ".NET", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    ServiceRemotingClient.StartTracing();
                }
                catch
                {
                    // ignore
                }

                try
                {
                    ServiceRemotingService.StartTracing();
                }
                catch
                {
                    // ignore
                }
            }
#endif

            Log.Debug("Initialization finished.");
        }

#if !NETFRAMEWORK
        private static void StartDiagnosticManager()
        {
            var observers = new List<DiagnosticObserver>();

            if (!PlatformHelpers.AzureAppServices.Metadata.IsFunctionsApp)
            {
                // Not adding the `AspNetCoreDiagnosticObserver` is particularly important for Azure Functions.
                // The AspNetCoreDiagnosticObserver will be loaded in a separate Assembly Load Context, breaking the connection of AsyncLocal
                // This is because user code is loaded within the functions host in a separate context
                observers.Add(new AspNetCoreDiagnosticObserver());
            }

            var diagnosticManager = new DiagnosticManager(observers);
            diagnosticManager.Start();
            DiagnosticManager.Instance = diagnosticManager;
        }
#endif
    }
}
