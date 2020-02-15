using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.Http;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http.Features;
using System.Text;

namespace WebJobSDKSample
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {

            var builder = new HostBuilder()
                /*.ConfigureAppConfiguration((context, config) => {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })*/
                .ConfigureServices(s =>
                {
                    s.AddSingleton<ITypeLocator>(new FakeTypeLocator(new[] { typeof(TestFunctions), typeof(FunctionApp1.Function1) }));
                })
                .ConfigureWebJobs(b =>
                {
                    b.AddHttp(o =>
                    {
                        o.SetResponse = SetResultHook;
                    });
                    //.AddAzureStorage();
                })
                .ConfigureLogging((context, b) =>
                {
                    b.AddConsole();
                });

            var host = builder.Build();
            /*using (host)
            {
                host.Run();
            }*/


            
            var jobHost = host.Services.GetService<IJobHost>() as JobHost;
            await CallEchoFunc(jobHost);

            await CallRunFunc(jobHost);
        }

        private static async Task CallEchoFunc(JobHost jobHost)
        {
            HttpRequest request = HttpTestHelpers.CreateHttpRequest("GET", "http://functions.com/api/abc");
            var method = typeof(TestFunctions).GetMethod(nameof(TestFunctions.TestResponse));
            await jobHost.CallAsync(method, new { req = request });

            Console.WriteLine(request.HttpContext.Items["$ret"]);
        }

        private static async Task CallRunFunc(JobHost jobHost)
        {
            HttpRequest request = HttpTestHelpers.CreateHttpRequest("GET", "http://functions.com/api/abc?name=RobZombie");
            var method = typeof(FunctionApp1.Function1).GetMethod(nameof(FunctionApp1.Function1.Run));
            await jobHost.CallAsync(method, new { req = request });

            Console.WriteLine(request.HttpContext.Items["$ret"]);
        }

        private static void SetResultHook(HttpRequest request, object result)
        {
            request.HttpContext.Items["$ret"] = result;
        }

    }
}
public class FakeTypeLocator : ITypeLocator
    {
        private readonly Type[] _types;

        public FakeTypeLocator(params Type[] types)
        {
            _types = types;
        }

        public IReadOnlyList<Type> GetTypes()
        {
            return _types;
        }
    }


public class HttpTestHelpers
{
    public static HttpRequest CreateHttpRequest(string method, string uriString, IHeaderDictionary headers = null, string body = null)
    {
        var uri = new Uri(uriString);
        var request = new DefaultHttpContext().Request;
        var requestFeature = request.HttpContext.Features.Get<IHttpRequestFeature>();
        requestFeature.Method = method;
        requestFeature.Scheme = uri.Scheme;
        requestFeature.PathBase = uri.Host;
        requestFeature.Path = uri.GetComponents(UriComponents.KeepDelimiter | UriComponents.Path, UriFormat.Unescaped);
        requestFeature.PathBase = "/";
        requestFeature.QueryString = uri.GetComponents(UriComponents.KeepDelimiter | UriComponents.Query, UriFormat.Unescaped);

        headers = headers ?? new HeaderDictionary();

        if (!string.IsNullOrEmpty(uri.Host))
        {
            headers.Add("Host", uri.Host);
        }

        if (body != null)
        {
            requestFeature.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            request.ContentLength = request.Body.Length;
            headers.Add("Content-Length", request.Body.Length.ToString());
        }

        requestFeature.Headers = headers;

        return request;
    }
}

public static class TestFunctions
{
    public static Task<string> TestResponse(
        [HttpTrigger("get", "post")] HttpRequest req)
    {
        // Return value becomes the HttpResponseMessage.
        return Task.FromResult("test-response");
    }
}