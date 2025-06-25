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
#pragma warning disable SKEXP0070 // Suppress warning for experimental Semantic Kernel API
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.AddControllersWithViews();

         
            builder.Services.AddMistralChatCompletion(
                modelId: "mistral-large-latest",
                apiKey: builder.Configuration["Mistral:ApiKey"]
            );

            // Register the Semantic Kernel instance. AddScoped is appropriate for web requests.
            builder.Services.AddScoped<Kernel>();

            // Add DbContext for Entity Framework Core
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
            .AddEntityFrameworkStores<DBContext>() // Link Identity to your DBContext
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

            using (var scope = app.Services.CreateScope())
              {
                  var serviceProvider = scope.ServiceProvider;
                  var labelingService = serviceProvider.GetRequiredService<RecipeLabelingService>();
                  var configuration = serviceProvider.GetRequiredService<IConfiguration>(); // Get configuration from scope

                  // Get API Delay from configuration, defaulting to 2 seconds if not found
                  int apiDelayMs = configuration.GetValue<int>("ApiSettings:DelayBetweenRequestsMs", 2000);

                  // Re-instantiate the service with the explicit delay
                  // (Alternatively, you could configure the service with options directly in DI setup)
                  var logger = serviceProvider.GetRequiredService<ILogger<RecipeLabelingService>>();
                  var dbContext = serviceProvider.GetRequiredService<DBContext>();
                  var kernel = serviceProvider.GetRequiredService<Kernel>();

                  var labelingServiceWithDelay = new RecipeLabelingService(dbContext, kernel, logger, apiDelayMs);

                  Console.WriteLine("Starting recipe labeling process on application startup...");
                  await labelingServiceWithDelay.LabelAllRecipesAsync();
                  Console.WriteLine("Recipe labeling process completed on application startup.");
              }


            app.Run(); // Starts the web application
        }
    }
}
