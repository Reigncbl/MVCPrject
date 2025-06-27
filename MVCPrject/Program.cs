using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.AspNetCore.Identity;
using MVCPrject.Data;
using MVCPrject.Models;
using StackExchange.Redis;


namespace MVCPrject
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
#pragma warning disable SKEXP0070 
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.AddControllersWithViews();

            //Redis
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration["Redis:ConnectionString"];
                options.InstanceName = builder.Configuration["Redis:InstanceName"];
            });


            //Mistral AI API
            var apikey = builder.Configuration["Mistral:ApiKey"];
            if (string.IsNullOrEmpty(apikey))
            {
                throw new InvalidOperationException("Mistral API key is not configured.");
            }
            builder.Services.AddMistralChatCompletion(
                modelId: "mistral-large-latest",
                apiKey: apikey
            );
            //Azure DB Connection
            builder.Services.AddScoped<Kernel>();
            builder.Services.AddDbContext<DBContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("RecipeDbConnection"),
                    sqlOptions => sqlOptions.EnableRetryOnFailure()
                ));

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
                options.LoginPath = "/Landing/Login";
                options.AccessDeniedPath = "/Landing/AccessDenied";
            });

            //Class Built
            builder.Services.AddScoped<RecipeRetrieverService>();
            builder.Services.AddScoped<RecipeManipulationService>();
            builder.Services.AddScoped<RecipeLabelingService>();
            builder.Services.AddScoped<UserService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            //Prepopulate Recipes
            using (var scope = app.Services.CreateScope())
            {
                var recipeService = scope.ServiceProvider.GetRequiredService<RecipeManipulationService>();
                await recipeService.PrepopulateCacheAsync();
            }

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Landing}/{action=Index}/{id?}");


            await Task.Delay(10);
            app.Run();
        }
    }
}