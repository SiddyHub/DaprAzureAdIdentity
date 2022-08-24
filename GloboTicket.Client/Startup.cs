using Dapr.Client;
using GloboTicket.Web.Models;
using GloboTicket.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.Net.Http.Headers;
using System;
using System.Text.Json;

namespace GloboTicket.Web
{
    public class Startup
    {
        private readonly IHostEnvironment environment;
        private readonly IConfiguration config;

        public Startup(IConfiguration configuration, IHostEnvironment environment)
        {
            config = configuration;
            this.environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDistributedMemoryCache();

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.Unspecified;
                // Handling SameSite cookie according to https://docs.microsoft.com/en-us/aspnet/core/security/samesite?view=aspnetcore-3.1
                options.HandleSameSiteCookieCompatibility();
            });

            services.AddOptions();

            services.AddMicrosoftIdentityWebAppAuthentication(config)
                    .EnableTokenAcquisitionToCallDownstreamApi(
                        config.GetSection("EventCatalog:EventCatalogScopes").Get<string>().Split(" ", System.StringSplitOptions.RemoveEmptyEntries)
                     )
                    .AddInMemoryTokenCaches();

            var requireAuthenticatedUserPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            var builder = services.AddControllersWithViews(configure =>
            {
                configure.Filters.Add(new AuthorizeFilter(requireAuthenticatedUserPolicy));
            }).AddMicrosoftIdentityUI();

            if (environment.IsDevelopment())
                builder.AddRazorRuntimeCompilation();

            services.AddDaprClient();

            services.AddSingleton(new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
            });

            var daprPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT");            
            services.AddHttpClient<IEventCatalogService, EventCatalogService>(c=> 
                c.BaseAddress = new Uri($"http://localhost:{daprPort}/v1.0/invoke/catalog/method"));
            services.AddSingleton<IOrderService>(c =>
                new OrderService(DaprClient.CreateInvokeHttpClient("order")));

            services.AddScoped<IShoppingBasketService, ShoppingBasketDaprService>();
            services.AddScoped<IDiscountService, DiscountDaprService>();            

            services.AddSingleton<Settings>();            
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=EventCatalog}/{action=Index}/{id?}");
            });
        }
    }
}
