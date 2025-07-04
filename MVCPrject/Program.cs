using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.AspNetCore.Identity;
using MVCPrject.Data;
using MVCPrject.Models;
using StackExchange.Redis;
using MVCPrject.Services;
using Azure.Storage.Blobs;


namespace MVCPrject
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
#pragma warning disable SKEXP0070 
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.AddControllersWithViews();
            //Azure storage
            var azureBlobConnectionString = builder.Configuration.GetSection("AzureBlobStorage")["DefaultEndpointsProtocol=https;AccountName=imageurlappdev;AccountKey=fZXCipQtMInu9SjS/ENdwuXhkTlrc5A7wGUzwsBb9HXgyZbgzzMMXgfzu/CJt1502fYUUG4J0oQV+AStRqd1qg==;EndpointSuffix=core.windows.net"];

            // Register BlobServiceClient with the connection string from configuration
            builder.Services.AddSingleton(x => new BlobServiceClient(azureBlobConnectionString));
            //Redis
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration["recipe.redis.cache.windows.net:6380,password=mt5YOtwgtfQEhuKkr7ElUr5vYRUizkzipAzCaOPQhUM=,ssl=True,abortConnect=False"];
                options.InstanceName = builder.Configuration["recipe"];

            });


            //Mistral AI API
            var apikey = builder.Configuration["VAJ6bCJPUh4Tmd9ybgE53EVHGjUCj7Q"];
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
                    builder.Configuration.GetConnectionString("Server=tcp:recipedev.database.windows.net,1433;Initial Catalog=AIrecipeDB;Persist Security Info=False;User ID=sarapmaster;Password=Welcome23;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"),
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
            builder.Services.AddMemoryCache(); // If not already added
            builder.Services.AddScoped<IUserCacheService, UserCacheService>();
            //Class Built
            builder.Services.AddScoped<RecipeRetrieverService>();
            builder.Services.AddScoped<RecipeManipulationService>();
            builder.Services.AddScoped<RecipeLabelingService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<MealLogService>();
            builder.Services.AddScoped<SuggestionService>();

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

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Landing}/{action=Index}/{id?}");


            await Task.Delay(10);
            app.Run();
        }
    }
}