using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Angular.Net.Core.XSRF.Models;
using Angular.Net.Core.XSRF.Repository;
using Microsoft.AspNet.Antiforgery;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Angular.Net.Core.XSRF
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigureAntiforgery(options => options.FormFieldName = "X-XSRF-TOKEN");
            services.AddAntiforgery();
            services.AddSingleton<TransactionRepository>();
        }

        public void Configure(IApplicationBuilder app, IAntiforgery antiforgery, IOptions<AntiforgeryOptions> options, IHostingEnvironment env, ILoggerFactory loggerFactory, TransactionRepository transactionRepository)
        {
            app.Use(next => context =>
            {
                if (
                    string.Equals(context.Request.Path.Value, "/", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(context.Request.Path.Value, "/index.html", StringComparison.OrdinalIgnoreCase))
                {
                    // We can send the request token as a JavaScript-readable cookie, and Angular will use it by default.
                    var tokens = antiforgery.GetAndStoreTokens(context);
                    context.Response.Cookies.Append("XSRF-TOKEN", tokens.FormToken, new CookieOptions() { HttpOnly = false });
                }

                return next(context);
            });

            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseIISPlatformHandler();

            app.UseDefaultFiles();

            app.UseStaticFiles();

            app.Map("/api/transaction", a => a.Run(async context =>
            {
                if (string.Equals("GET", context.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    var transactions = transactionRepository.GetTransactions();
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(transactions, new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    }));
                }
                else if (string.Equals("POST", context.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    /* This code wont be needed in RC2 */

                    var dictionary = new Dictionary<string, StringValues>
                    {
                        {options.Value.FormFieldName, context.Request.Headers["X-XSRF-TOKEN"]}
                    };
                    context.Request.Form = new FormCollection(dictionary);

                    /* =============================== */

                    await antiforgery.ValidateRequestAsync(context);

                    var serializer = new JsonSerializer();
                    using (var reader = new JsonTextReader(new StreamReader(context.Request.Body)))
                    {
                        var transaction = serializer.Deserialize<Transaction>(reader);
                        transactionRepository.AddTransaction(transaction);
                    }

                    context.Response.StatusCode = 204;
                }
            }));
        }

        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}
