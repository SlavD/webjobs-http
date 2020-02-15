using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PactNet;
using PactNet.Infrastructure.Outputters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

namespace ProviderContractTest
{
    public class ProviderApiTests : IDisposable
    {
        private string _providerUri { get; }
        private string _pactServiceUri { get; }
        private IWebHost _webHost { get; }
        private IHost _host { get; }
        private JobHost _jobHost { get { return _host.Services.GetService<IJobHost>() as JobHost; } }

        private ITestOutputHelper _outputHelper { get; }

        public ProviderApiTests(ITestOutputHelper output)
        {
            _outputHelper = output;
            _providerUri = "http://localhost:9000";
            _pactServiceUri = "http://localhost:9001";


            var builder = Host.CreateDefaultBuilder()
                .ConfigureServices(s =>
                {
                    s.AddSingleton<ITypeLocator>(new FakeTypeLocator(new[] { typeof(FunctionApp1.Function1) }));
                })
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.Configure(app=> {
                        app.Map("/sut", s => {
                            s.Run(async c => {
                                HttpRequest request = HttpTestHelpers.CreateHttpRequest("GET", "http://functions.com/api/abc?name=RobZombie");
                                var method = typeof(FunctionApp1.Function1).GetMethod(nameof(FunctionApp1.Function1.Run));
                                await _jobHost.CallAsync(method, new { req = request });

                                //await (request.HttpContext.Items["$ret"] as IActionResult).ExecuteResultAsync(new ActionContext(c, null, null));

                                await c.Response.WriteAsync((request.HttpContext.Items["$ret"] as OkObjectResult).Value.ToString());
                            });
                        });
                        app.Map("/state", s => {
                            s.UseMiddleware<ProviderStateMiddleware>();
                            //s.UseMvc();
                        });
                    });
                    //webBuilder.UseStartup<TestStartup>();
                    webBuilder.UseUrls(_pactServiceUri);
                })
                .ConfigureWebJobs(b =>
                {
                    b.AddHttp(o =>
                    {
                        o.SetResponse = (r, o) => { 
                            r.HttpContext.Items["$ret"] = o; 
                        };
                    });
                })
                .ConfigureLogging((context, b) =>
                {
                    b.AddConsole();
                });
            ;

            _host = builder.Build();
            _host.Start();

            /*
            _webHost = WebHost.CreateDefaultBuilder()
                .UseUrls(_pactServiceUri)
                .UseStartup<TestStartup>()
                .Build();

            _webHost.Start();*/
        }

        [Fact]
        public async Task EnsureProviderApiHonoursPactWithConsumerAsync()
        {
            // Arrange
            var config = new PactVerifierConfig
            {

                // NOTE: We default to using a ConsoleOutput,
                // however xUnit 2 does not capture the console output,
                // so a custom outputter is required.
                Outputters = new List<IOutput>
                                {
                                    new XUnitOutput(_outputHelper)
                                },

                // Output verbose verification logs to the test output
                Verbose = true
            };

            var tcs = new TaskCompletionSource<bool>();

            // complete task in event
            //tcs.SetResult(fa);

            // wait for task somewhere else
            await tcs.Task;

        /*
        //Act / Assert
        IPactVerifier pactVerifier = new PactVerifier(config);
        pactVerifier.ProviderState($"{_pactServiceUri}/provider-states")
            .ServiceProvider("Provider", _providerUri)
            .HonoursPactWith("Consumer")
            .PactUri(@"..\..\..\..\..\pacts\consumer-provider.json")
            .Verify();*/
    }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _host.StopAsync().GetAwaiter().GetResult();
                    _host.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
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
}
