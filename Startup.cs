using HP_Detailing.Data;
using HP_Detailing.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HP_Detailing
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            // Configure Entity Framework Core with SQL Server
            services.AddDbContext<HP_DetailingDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection") ??
                                     "Server=(localdb)\\MSSQLLocalDB;Database=HP_DetailingDb;User Id=sa;Password=sa;MultipleActiveResultSets=true;TrustServerCertificate=True"));

            // Register ASP.NET Core Identity
            services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                // Configure password policy
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            })
            .AddEntityFrameworkStores<HP_DetailingDbContext>()
            .AddDefaultTokenProviders();

            // Configure Application Cookie
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Home/Error";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Enable Authentication before Authorization (Crucial for security)
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                // --- Custom routes matching React router paths ---
                endpoints.MapControllerRoute(name: "login",           pattern: "login",                   defaults: new { controller = "Account", action = "Login" });
                endpoints.MapControllerRoute(name: "tickets-new",     pattern: "tickets/new",             defaults: new { controller = "Tickets", action = "Create" });
                endpoints.MapControllerRoute(name: "tickets-detail",  pattern: "tickets/{id}",            defaults: new { controller = "Tickets", action = "Detail" });
                endpoints.MapControllerRoute(name: "tickets",         pattern: "tickets",                 defaults: new { controller = "Tickets", action = "Index" });
                endpoints.MapControllerRoute(name: "appointments",    pattern: "appointments",            defaults: new { controller = "Appointments", action = "Index" });
                endpoints.MapControllerRoute(name: "analytics",       pattern: "analytics",               defaults: new { controller = "Analytics", action = "Index" });
                endpoints.MapControllerRoute(name: "cars",            pattern: "cars",                    defaults: new { controller = "Cars", action = "Index" });
                endpoints.MapControllerRoute(name: "staff-detail",    pattern: "staff/{id}",              defaults: new { controller = "Staff", action = "Detail" });
                endpoints.MapControllerRoute(name: "staff",           pattern: "staff",                   defaults: new { controller = "Staff", action = "Index" });
                endpoints.MapControllerRoute(name: "wh-import-new",   pattern: "warehouse/imports/new",   defaults: new { controller = "Warehouse", action = "ImportCreate" });
                endpoints.MapControllerRoute(name: "wh-import-detail",pattern: "warehouse/imports/{id}",  defaults: new { controller = "Warehouse", action = "ImportDetail" });
                endpoints.MapControllerRoute(name: "wh-imports",      pattern: "warehouse/imports",       defaults: new { controller = "Warehouse", action = "Imports" });
                endpoints.MapControllerRoute(name: "wh-audit-history", pattern: "warehouse/audit/history-page", defaults: new { controller = "Warehouse", action = "AuditHistoryPage" });
                endpoints.MapControllerRoute(name: "wh-audit",          pattern: "warehouse/audit",              defaults: new { controller = "Warehouse", action = "Audit" });
                endpoints.MapControllerRoute(name: "warehouse",       pattern: "warehouse",               defaults: new { controller = "Warehouse", action = "Index" });
                endpoints.MapControllerRoute(name: "financial-invoice", pattern: "financial/invoice/{id}", defaults: new { controller = "Financial", action = "Invoice" });
                endpoints.MapControllerRoute(name: "financial-detail", pattern: "financial/{id}",          defaults: new { controller = "Financial", action = "Invoice" });
                endpoints.MapControllerRoute(name: "financial",       pattern: "financial",               defaults: new { controller = "Financial", action = "Index" });
                endpoints.MapControllerRoute(name: "settings",        pattern: "settings",                defaults: new { controller = "Settings", action = "Index" });
                endpoints.MapControllerRoute(name: "support",         pattern: "support",                 defaults: new { controller = "Support", action = "Index" });
                endpoints.MapControllerRoute(name: "profile",         pattern: "profile",                 defaults: new { controller = "Profile", action = "Index" });

                // Default MVC route
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
