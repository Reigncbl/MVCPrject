using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.AspNetCore.Identity;
using MVCPrject.Data;
using MVCPrject.Models;


namespace MVCPrject
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
#pragma warning disable SKEXP0070 
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.AddControllersWithViews();


            var apikey = builder.Configuration["Mistral:ApiKey"];
            if (string.IsNullOrEmpty(apikey))
            {
                throw new InvalidOperationException("Mistral API key is not configured.");
            }

            builder.Services.AddMistralChatCompletion(
                modelId: "mistral-large-latest",
                apiKey: apikey
            );



            builder.Services.AddScoped<Kernel>();


            builder.Services.AddDbContext<DBContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("RecipeDbConnection")));

            // Add Identity services
            builder.Services.AddIdentity<User, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            })
            .AddEntityFrameworkStores<DBContext>()
            .AddDefaultTokenProviders();

            // Configure authentication cookies
            builder.Services.AddAuthentication()
            .AddCookie(options =>
            {
                options.LoginPath = "/Landing/Login"; // Redirect to login page
                options.AccessDeniedPath = "/Landing/AccessDenied"; // Handle unauthorized access
            });

            // Register your custom application services
            builder.Services.AddScoped<RecipeRetrieverService>();
            builder.Services.AddScoped<RecipeManipulationService>();
            // Register RecipeLabelingService so it can be resolved from the DI container
            builder.Services.AddScoped<RecipeLabelingService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles(); // Serves static files from wwwroot
            app.UseRouting();

            app.UseAuthentication(); // IMPORTANT: Must be before UseAuthorization
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Landing}/{action=Index}/{id?}");

            await Task.Delay(10);
            app.Run();
        }
    }
}