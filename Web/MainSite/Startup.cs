using Certes;
using FluffySpoon.AspNet.LetsEncrypt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Globalization;
using System.Linq;
using TerritoryTools.Entities;
using TerritoryTools.Web.Data;
using TerritoryTools.Web.Data.Services;
using TerritoryTools.Web.MainSite.Services;

namespace TerritoryTools.Web.MainSite
{
    public class Startup
    {
        static public bool NoSsl = false;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<MainDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("MainDbContextConnection")));

            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    options.ClientId = Configuration["Authentication:Google:ClientId"];
                    options.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
                });

            //services.AddAuthentication()
            //    .AddMicrosoftAccount(options =>
            //    {
            //        options.ClientId = Configuration["Authentication:Microsoft:ClientId"];
            //        options.ClientSecret = Configuration["Authentication:Microsoft:ClientSecret"];
            //    });

            services.AddLocalization(options => options.ResourcesPath = "Resources");

            services.AddMvc()
            //  .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
              .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
              .AddDataAnnotationsLocalization();

            services.AddScoped<IAlbaCredentials>(ac => new AlbaCredentials(
                Configuration["AlbaAccount"],
                Configuration["AlbaUser"],
                Configuration["AlbaPassword"]));

            services.AddScoped<IShortUrlService, ShortUrlService>();
            services.AddScoped<IQRCodeActivityService, QRCodeActivityService>();

            services.Configure<WebUIOptions>(Configuration);

            var users = (Configuration["Users"] ?? string.Empty)
             .Split(';')
             .ToList();

            var adminUsers = (Configuration["AdminUsers"] ?? string.Empty)
                .Split(';')
                .ToList();

            services.AddSingleton<IAccountLists>(l => new AccountLists(
                Configuration["TT_AreaNames"] ?? string.Empty));

            services.AddScoped<IAuthorizationService>(s =>
                new TerritoryAuthorizationService(users, adminUsers));

            services.AddScoped<IAlbaCredentialService, AlbaCredentialAzureVaultService>();

            // New with .NET Core 3.1
            services.AddControllersWithViews();
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                ////////app.UseHsts();
            }

            var supportedCultures = new[]
            {
                new CultureInfo("en-US"),
                new CultureInfo("en-AU"),
                new CultureInfo("zh"),
                new CultureInfo("zh-Hant"),
            };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-US"),
                // Formatting numbers, dates, etc.
                SupportedCultures = supportedCultures,
                // UI strings that we have localized.
                SupportedUICultures = supportedCultures
            });

            ////////app.UseHttpsRedirection();

            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".kml"] = "application/vnd.google-earth.kml+xml";
            provider.Mappings[".kmz"] = "application/vnd.google-earth.kmz";

            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = provider
            });

            UpdateDatabase(app);

            // New with .NET Core 3.1
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // New with .NET Core 3.1
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                // Added areas
                //////endpoints.MapControllerRoute(
                //////    name: "areas",
                //////    pattern: "{area:exists}/{controller=ShortUrls}/{action=Index}");
                endpoints.MapRazorPages();
            });

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            //app.UseMvc(routes =>
            //{
            //    routes.MapRoute(
            //        name: "areas",
            //        template: "{area:exists}/{controller=ShortUrls}/{action=Index}");

            //    routes.MapRoute(
            //        name: "default",
            //        template: "{controller=Home}/{action=Index}/{id?}");
            //});
        }


        private static void UpdateDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetService<MainDbContext>())
                {
                    context.Database.Migrate();
                }
            }
        }

        void ConfigureLetsEncryptServices(IServiceCollection services)
        {
            // Configuration code copied from: 
            // https://github.com/ffMathy/FluffySpoon.AspNet.LetsEncrypt/blob/master/README.md
            // The following line adds the automatic renewal service.
            services.AddFluffySpoonLetsEncryptRenewalService(
                new LetsEncryptOptions()
                {
                    // LetsEncrypt will send you an e-mail here when the 
                    // certificate is about to expire
                    Email = Configuration["LetsEncrypt:Email"],
                    UseStaging = bool.Parse(Configuration["LetsEncrypt:UseStaging"]), //switch to true for testing
                    Domains = Configuration["LetsEncrypt:Domains"]
                        .ToString()
                        .Split(',', StringSplitOptions.RemoveEmptyEntries),
                    // Renew automatically 30 days before expiry
                    TimeUntilExpiryBeforeRenewal = TimeSpan.FromDays(30),
                    //these are your certificate details
                    CertificateSigningRequest = new CsrInfo()
                    {
                        CountryName = Configuration["LetsEncrypt:CountryName"],
                        Locality = Configuration["LetsEncrypt:Locality"],
                        Organization = Configuration["LetsEncrypt:Organization"],
                        OrganizationUnit = Configuration["LetsEncrypt:OrganizationUnit"],
                        State = Configuration["LetsEncrypt:State"]
                    }
                });

            // The following line tells the library to persist the certificate
            // to a file, so that if the server restarts, the certificate can 
            // be re-used without generating a new one.
            //services.AddFluffySpoonLetsEncryptFileCertificatePersistence("/data/certificate");

            // The following line tells the library to persist challenges 
            // in-memory. challenges are the "/.well-known" URL codes that 
            // LetsEncrypt will call.
            //services.AddFluffySpoonLetsEncryptMemoryChallengePersistence();
        }
    }
}
