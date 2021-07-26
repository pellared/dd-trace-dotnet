// <copyright file="TracerInstanceTestCollection.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Reflection;
using Xunit;
using Xunit.Sdk;

namespace Datadog.Trace.Tests
{
    [CollectionDefinition(nameof(TracerInstanceTestCollection), DisableParallelization = true)]
    public class TracerInstanceTestCollection
    {
        [AttributeUsage(AttributeTargets.Class, Inherited = true)]
        public class TracerRestorerAttribute : BeforeAfterTestAttribute
        {
            private Tracer _tracer;

            public override void Before(MethodInfo methodUnderTest)
            {
                _tracer = Tracer.Instance;
                base.Before(methodUnderTest);
            }

            public override void After(MethodInfo methodUnderTest)
            {
                Tracer.Instance = _tracer;
                base.After(methodUnderTest);
            }
        }
    }
}
