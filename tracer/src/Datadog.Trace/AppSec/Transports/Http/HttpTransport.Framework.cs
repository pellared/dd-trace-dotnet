// <copyright file="HttpTransport.Framework.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Web;
using Datadog.Trace.AppSec.EventModel;
using Datadog.Trace.AppSec.Transports.Http;
using Datadog.Trace.AppSec.Waf;

namespace Datadog.Trace.AppSec.Transports.Http
{
    internal class HttpTransport : ITransport
    {
        private const string WafKey = "waf";
        private readonly HttpContext context;

        public HttpTransport(HttpContext context)
        {
            this.context = context;
            var ipInfo = IpExtractor.ExtractAddressAndPort(context.Request.UserHostAddress, context.Request.IsSecureConnection);
            PeerAddressInfo = ipInfo;
        }

        public IpInfo PeerAddressInfo { get; }

        public bool IsSecureConnection => context.Request.IsSecureConnection;

        public Func<string, string> GetHeader => key => context.Request.Headers[key];

        public void Block()
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "text/html";
            context.Response.Write(SecurityConstants.AttackBlockedHtml);
            context.Response.Flush();
            context.ApplicationInstance.CompleteRequest();
        }

        public IContext GetAdditiveContext() => context.Items[WafKey] as IContext;

        public void SetAdditiveContext(IContext additiveContext)
        {
            context.DisposeOnPipelineCompleted(additiveContext);
            context.Items[WafKey] = additiveContext;
        }
    }
}
#endif
