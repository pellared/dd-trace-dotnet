// <copyright file="RequestDataV6.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1600 // Elements should be documented
using System;
using System.ComponentModel;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Elasticsearch.V6
{
    /// <summary>
    /// Duck-copy struct for RequestData
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct RequestDataV6 : IRequestData
    {
        private Proxy _data;

        public RequestDataV6(object source)
        {
            _data = source.DuckCast<Proxy>();
        }

        public string Path => _data.PathAndQuery;

        public Uri Uri => _data.Uri;

        public string Method => _data.Method.ToString();

        [DuckCopy]
        public struct Proxy
        {
            public string PathAndQuery;
            public HttpMethod Method;
            public Uri Uri;
        }
    }
}
