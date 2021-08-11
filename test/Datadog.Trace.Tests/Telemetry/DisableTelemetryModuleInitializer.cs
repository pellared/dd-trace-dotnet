// <copyright file="DisableTelemetryModuleInitializer.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Runtime.CompilerServices;
using Datadog.Trace.Telemetry;

namespace Datadog.Trace.Tests.Telemetry
{
    public static class DisableTelemetryModuleInitializer
    {
        [ModuleInitializer]
        public static void SetGlobalTelemetryToNull()
        {
            // As we have a single shared instance across everything, set to the null telemetry instance
            Datadog.Trace.Telemetry.Telemetry.Instance = NullTelemetryController.Instance;
        }
    }
}
