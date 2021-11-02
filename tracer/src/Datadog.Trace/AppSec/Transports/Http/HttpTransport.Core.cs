// <copyright file="HttpTransport.Core.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if !NETFRAMEWORK
using System;
using Datadog.Trace.AppSec.Waf;
using Microsoft.AspNetCore.Http;

namespace Datadog.Trace.AppSec.Transports.Http
{
    internal class HttpTransport : ITransport
    {
        private readonly HttpContext context;

        public HttpTransport(HttpContext context)
        {
            this.context = context;
            PeerAddressInfo = new IpInfo(context.Connection.RemoteIpAddress.ToString(), context.Connection.RemotePort);
        }

        public IpInfo PeerAddressInfo { get; }

        public bool IsSecureConnection => context.Request.IsHttps;

        public Func<string, string> GetHeader => key => context.Request.Headers[key];

        public void Block()
        {
            if (context.Items.ContainsKey(SecurityConstants.InHttpPipeKey) && context.Items[SecurityConstants.InHttpPipeKey] is bool inHttpPipe && inHttpPipe)
            {
                throw new BlockActionException();
            }
            else
            {
                context.Items[SecurityConstants.KillKey] = true;
            }
        }

        public IContext GetAdditiveContext()
        {
            return context.Features.Get<IContext>();
        }

        public void SetAdditiveContext(IContext additiveContext)
        {
            context.Features.Set(additiveContext);
            context.Response.RegisterForDispose(additiveContext);
        }
    }
}
#endif
