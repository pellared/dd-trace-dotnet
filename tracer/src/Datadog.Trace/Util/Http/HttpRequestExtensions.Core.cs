// <copyright file="HttpRequestExtensions.Core.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if !NETFRAMEWORK
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Datadog.Trace.AppSec;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Datadog.Trace.Util.Http
{
    internal static partial class HttpRequestExtensions
    {
        private const string NoHostSpecified = "UNKNOWN_HOST";

        internal static Dictionary<string, object> PrepareArgsForWaf(this HttpRequest request, RouteData routeDatas = null)
        {
            var url = GetUrl(request);
            var headersDic = new Dictionary<string, string>(request.Headers.Keys.Count);
            foreach (var k in request.Headers.Keys)
            {
                if (!k.Equals("cookie", System.StringComparison.OrdinalIgnoreCase))
                {
                    headersDic.Add(k.ToLowerInvariant(), request.Headers[k]);
                }
            }

            var cookiesDic = new Dictionary<string, string>(request.Cookies.Keys.Count);
            foreach (var k in request.Cookies.Keys)
            {
                cookiesDic.Add(k, request.Cookies[k]);
            }

            var queryStringDic = new Dictionary<string, List<string>>(request.Query.Count);
            foreach (var kvp in request.Query)
            {
                queryStringDic.Add(kvp.Key, kvp.Value.ToList());
            }

            var dict = new Dictionary<string, object>
            {
                { AddressesConstants.RequestMethod, request.Method },
                { AddressesConstants.RequestUriRaw, url },
                { AddressesConstants.RequestQuery, queryStringDic },
                { AddressesConstants.RequestHeaderNoCookies, headersDic },
                { AddressesConstants.RequestCookies, cookiesDic },
            };

            if (routeDatas != null && routeDatas.Values.Any())
            {
                var routeDataDict = HttpRequestUtils.ConvertRouteValueDictionary(routeDatas.Values);
                dict.Add(AddressesConstants.RequestPathParams, routeDataDict);
            }

            return dict;
        }

        internal static string GetUrl(this HttpRequest request)
        {
            if (request.Host.HasValue)
            {
                return $"{request.Scheme}://{request.Host.Value}{request.PathBase.ToUriComponent()}{request.Path.ToUriComponent()}";
            }

            // HTTP 1.0 requests are not required to provide a Host to be valid
            // Since this is just for display, we can provide a string that is
            // not an actual Uri with only the fields that are specified.
            // request.GetDisplayUrl(), used above, will throw an exception
            // if request.Host is null.
            return $"{request.Scheme}://{HttpRequestExtensions.NoHostSpecified}{request.PathBase.ToUriComponent()}{request.Path.ToUriComponent()}";
        }
    }
}
#endif
