// <copyright file="NUnitSkipCommandExecuteIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.ComponentModel;
using Datadog.Trace.ClrProfiler.CallTarget;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.NUnit
{
    /// <summary>
    /// NUnit.Framework.Internal.Commands.SkipCommand.Execute() calltarget instrumentation
    /// </summary>
    [InstrumentMethod(
        AssemblyName = "nunit.framework",
        TypeName = "NUnit.Framework.Internal.Commands.SkipCommand",
        MethodName = "Execute",
        ReturnTypeName = "NUnit.Framework.Internal.TestResult",
        ParameterTypeNames = new[] { "NUnit.Framework.Internal.TestExecutionContext" },
        MinimumVersion = "3.0.0",
        MaximumVersion = "3.*.*",
        IntegrationName = NUnitIntegration.IntegrationName)]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class NUnitSkipCommandExecuteIntegration
    {
        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TContext">ExecutionContext type</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="executionContext">Execution context instance</param>
        /// <returns>Calltarget state value</returns>
        public static CallTargetState OnMethodBegin<TTarget, TContext>(TTarget instance, TContext executionContext)
            where TContext : ITestExecutionContext
        {
            if (!NUnitIntegration.IsEnabled)
            {
                return CallTargetState.GetDefault();
            }

            return new CallTargetState(NUnitIntegration.CreateScope(executionContext.CurrentTest, typeof(TTarget)));
        }

        /// <summary>
        /// OnMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TResult">TestResult type</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="returnValue">Original method return value</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>Return value of the method</returns>
        public static CallTargetReturn<TResult> OnMethodEnd<TTarget, TResult>(TTarget instance, TResult returnValue, Exception exception, CallTargetState state)
        {
            Scope scope = state.Scope;
            if (scope != null)
            {
                NUnitIntegration.FinishScope(scope, exception);
                scope.Dispose();
            }

            return new CallTargetReturn<TResult>(returnValue);
        }
    }
}
