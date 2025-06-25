using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using MVCPrject.Data;
using Microsoft.AspNetCore.Identity; // 
using MVCPrject.Models; //


namespace MVCPrject
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
#pragma warning disable SKEXP0070
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.AddControllersWithViews();

            // Configure Mistral AI with Semantic Kernel
            builder.Services.AddMistralChatCompletion(
                modelId: "mistral-large-latest",
                apiKey: builder.Configuration["Mistral:AIzaSyBhTIw3N_NmeqxUYdWP5ardejMHen_LLjU"]
            );


            builder.Services.AddTransient<Kernel>(serviceProvider =>
            {
                return new Kernel(serviceProvider);
            });

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

            builder.Services.AddScoped<RecipeRetrieverService>();
            builder.Services.AddScoped<RecipeManipulationService>();


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
            app.UseAuthentication(); // 
            app.UseAuthorization();
            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Landing}/{action=Index}/{id?}")
                .WithStaticAssets();

            /* using (var scope = app.Services.CreateScope())
            {
                var migrationService = scope.ServiceProvider.GetRequiredService<RecipeRetrieverService>();
                Console.WriteLine("Scraping data!!!");

                // Run both tasks concurrently
                var scrapeTask = migrationService.ScrapeAndUpdateRecipesAsync();



                await Task.WhenAll(scrapeTask);

                Console.WriteLine("Scraping and migration completed!");
            }
*/

            app.Run();

        }
    }
}