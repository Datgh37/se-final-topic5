using Microsoft.EntityFrameworkCore;
using WebUITopic5_Team4.Data;

namespace WebUITopic5_Team4
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var defaultCulture = new System.Globalization.CultureInfo("vi-VN");
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

            var builder = WebApplication.CreateBuilder(args);
            var myConnectionString = builder.Configuration.GetConnectionString("MyConnectString"); 
            
            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Add DbContext
            builder.Services.AddDbContext<ElectronicShopContext>(options =>
                options.UseSqlServer(myConnectionString));

            // Add Authentication and Session (Required for Cart/Order logic)
            builder.Services.AddAuthentication("CookieAuth")
                .AddCookie("CookieAuth", options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Home/Error";
                });
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
