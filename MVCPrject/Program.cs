using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using MVCPrject.Data;

#pragma warning disable SKEXP0070
namespace MVCPrject
{
    public class Program
    {
        public async static Task Main(string[] args)
        {


            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();
            var apiKey = builder.Configuration["Mistral:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey), "Mistral API Key is missing or empty.");
            }


            // Configure Mistral AI with Semantic Kernel
            builder.Services.AddMistralChatCompletion(
                modelId: "mistral-large-latest",
                apiKey: apiKey
            );


            builder.Services.AddScoped<Kernel>(serviceProvider =>
            {
                return new Kernel(serviceProvider);
            });

            builder.Services.AddDbContext<DBContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("RecipeDbConnection")));

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
            app.UseRouting();
            app.UseAuthorization();
            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Home}/{id?}")
                .WithStaticAssets();
            /*   using (var scope = app.Services.CreateScope())
            {
              
                var migrationService = scope.ServiceProvider.GetRequiredService<RecipeRetrieverService>();
                Console.WriteLine("Scrapping data!!!");

                await migrationService.ScrapeAndUpdateRecipesAsync(); // Note: Your method name was MigrateToNormalizedTablesAsync, not MigrateRecipesToNormalizedTableAsync
                Console.WriteLine("Scraping completed!");
            }*/


            Console.WriteLine(" Scraping complete.");
            await Task.Delay(100);
            Console.WriteLine("Starting the application...");
            app.Run();

        }
    }
}