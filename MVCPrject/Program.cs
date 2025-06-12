using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using MVCPrject.Data;
namespace MVCPrject
{
    public class Program
    {
        public static async Task Main(string[] args)
        {


#pragma warning disable SKEXP0070

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();


            IServiceCollection serviceCollection = builder.Services.AddSingleton<Kernel>(serviceProvider =>
            {
                var kernelBuilder = Kernel.CreateBuilder();

                kernelBuilder.AddGoogleAIGeminiChatCompletion(
                    modelId: "gemini-1.5-flash",
                    apiKey: builder.Configuration["Gemini:ApiKey"],
                    apiVersion: GoogleAIVersion.V1
                );

                return kernelBuilder.Build();
            });

            builder.Services.AddDbContext<DBContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("RecipeDbConnection")));

            builder.Services.AddTransient<RecipeScraper>();


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

            using (var scope = app.Services.CreateScope())
            {
                var scraper = scope.ServiceProvider.GetRequiredService<RecipeScraper>();

                var categories = new[]
                {
        "chicken-recipes",
        "pork-recipes",
        "dessert-and-pastry-recipes",
        "beef-recipes",
        "vegetable-recipes",
        "fish-recipes-recipes",
        "pasta-recipes",
        "rice-recipes",
        "eggs",
        "tofu-recipes-recipes",
        "noodle-recipes"
    };

                foreach (var category in categories)
                {
                    Console.WriteLine($"Scraping category: {category}");
                    await scraper.ScrapeAndSaveUrlsAsync(category);
                }
            }

            Console.WriteLine(" Scraping complete.");
            app.Run();


        }
    }
}