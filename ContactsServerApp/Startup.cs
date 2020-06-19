using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContactsApp.BaseRepository;
using ContactsApp.Controls;
using ContactsApp.Controls.Grid;
using ContactsApp.DataAccess;
using ContactsApp.Model;
using ContactsApp.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ContactsServerApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(AzureADDefaults.AuthenticationScheme)
                .AddAzureAD(options => Configuration.Bind("AzureAd", options));

            services.AddControllersWithViews(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            });

            services.AddRazorPages();
            services.AddServerSideBlazor();

            services.AddDbContextFactory<ContactContext>(opt =>
                opt.UseSqlServer(
                    Configuration.GetConnectionString(ContactContext.BlazorContactsDb))
                .EnableSensitiveDataLogging());

            // add the repository
            services.AddScoped<IRepository<Contact, ContactContext>,
                ContactRepository>();
            services.AddScoped<IBasicRepository<Contact>>(sp =>
                sp.GetService<IRepository<Contact, ContactContext>>());
            services.AddScoped<IUnitOfWork<Contact>, UnitOfWork<ContactContext, Contact>>();

            // for seeding the first time
            services.AddScoped<SeedContacts>();

            services.AddScoped<IPageHelper, PageHelper>();
            services.AddScoped<IContactFilters, ContactFilters>();
            services.AddScoped<GridQueryAdapter>();
            services.AddScoped<EditService>();

            // set up authorized user
            services.AddScoped(sp =>
            {
                var provider = sp.GetService<AuthenticationStateProvider>();
                var state = provider.GetAuthenticationStateAsync().Result;
                return state.User.Identity.IsAuthenticated ?
                    state.User : null;
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
