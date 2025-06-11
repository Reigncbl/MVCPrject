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
            builder.Services.AddScoped<UrlScraper>();



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

            app.Run();
        }
    }
}