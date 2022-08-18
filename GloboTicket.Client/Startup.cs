using Dapr.Client;
using GloboTicket.Web.Models;
using GloboTicket.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            var builder = services.AddControllersWithViews();

            if (environment.IsDevelopment())
                builder.AddRazorRuntimeCompilation();

            services.AddDaprClient();

            services.AddSingleton(new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
            });            

            services.AddSingleton<IEventCatalogService>(c =>
                new EventCatalogService(DaprClient.CreateInvokeHttpClient("catalog")));
            services.AddSingleton<IOrderService>(c =>
                new OrderService(DaprClient.CreateInvokeHttpClient("order")));

            services.AddScoped<IShoppingBasketService, ShoppingBasketDaprService>();
            services.AddScoped<IDiscountService, DiscountDaprService>();            

            services.AddSingleton<Settings>();

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            }).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.Authority = "https://login.microsoftonline.com/{guid}/v2.0";
                options.ClientId = "";

                //Web apps that call web APIs are confidential client applications.
                //That's why they register a secret (an application password or certificate) with Azure Active Directory (Azure AD).
                //This secret is passed in during the call to Azure AD to get a token.
                options.ClientSecret = "";
                options.ResponseType = "code";

                //options.ResponseType = "id_token"; // only for web app that is used to signs in user. Also we need to check ID Tokens in AAD registration page
                options.Scope.Add("api://catalog/FullAccess");
                options.Scope.Add("offline_access"); // to get refresh tokens
                options.SaveTokens = true;
            });
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
            //app.UseHttpsRedirection();
            app.UseStaticFiles();

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
