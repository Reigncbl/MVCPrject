# Recipe Management Application

A full-stack web application for recipe discovery, meal planning, and nutrition tracking with AI-powered suggestions.

## Features

- **Recipe Management**: Browse, search, and save recipes with detailed ingredients and instructions
- **Meal Planning**: Interactive calendar for weekly meal planning with drag-and-drop functionality
- **AI-Powered Chat**: Get personalized recipe recommendations using Mistral AI
- **Nutrition Tracking**: Monitor daily nutritional information and summaries
- **Social Features**: Follow users and view their profiles
- **Grocery Lists**: Auto-generate shopping lists from meal plans

## Tech Stack

### Backend
- **ASP.NET Core 9.0** (MVC)
- **Entity Framework Core** with SQL Server
- **ASP.NET Identity** for authentication
- **Redis** for distributed caching
- **Azure Blob Storage** for file management
- **Mistral AI** via Semantic Kernel for chat and suggestions

### Frontend
- **Razor Views** (CSHTML)
- **JavaScript** (ES6+)
- **Bootstrap** & custom CSS
- **jQuery** for DOM manipulation

## Quick Start

```bash
# Clone the repository
git clone https://github.com/Lorieta/MVCPrject.git
cd MVCPrject

# Restore dependencies
dotnet restore

# Update database
cd MVCPrject
dotnet ef database update

# Run the application
dotnet run
```

## Configuration

Create `appsettings.json` in `/MVCPrject` directory:

```json
{
  "ConnectionStrings": {
    "RecipeDbConnection": "Server=your-server;Database=RecipeDB;..."
  },
  "Mistral": {
    "ApiKey": "your-mistral-api-key"
  },
  "AzureBlobStorage": {
    "ConnectionString": "your-azure-storage-connection-string"
  },
  "Redis": {
    "ConnectionString": "your-redis-connection-string",
    "InstanceName": "RecipeApp_"
  }
}
```

## Project Structure

```
MVCPrject/
└── MVCPrject/
    ├── Controllers/        # Backend - MVC Controllers
    ├── Models/            # Backend - Data models
    ├── Data/              # Backend - Services & DB context
    ├── Views/             # Frontend - Razor templates
    └── wwwroot/           # Frontend - Static assets (CSS, JS, images)
```
