using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using MVCPrject.Data;


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
                apiKey: builder.Configuration["Mistral:ApiKey"]
            );

    
            builder.Services.AddTransient<Kernel>(serviceProvider =>
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
            app.Run();
         
        }
    }
}