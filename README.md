# MVCPrject - Recipe Management Application

A comprehensive ASP.NET Core MVC web application for managing recipes, meal planning, and nutrition tracking with AI-powered recipe suggestions.

## Table of Contents
- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
  - [Backend](#backend)
  - [Frontend](#frontend)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Configuration](#configuration)
- [Usage](#usage)
- [Project Structure](#project-structure)

## Overview

MVCPrject is a full-stack recipe management platform that allows users to discover, save, and organize recipes. The application features AI-powered meal suggestions, nutrition tracking, social features for following other users, and a comprehensive meal planning system.

## Features

- **User Authentication & Authorization**: Secure user registration and login with ASP.NET Identity
- **Recipe Management**: Browse, search, save, and manage recipes
- **Meal Planning**: Plan meals for the week with an interactive calendar
- **Nutrition Tracking**: Monitor nutritional information and daily summaries
- **AI-Powered Suggestions**: Get personalized recipe recommendations using Mistral AI
- **Social Features**: Follow other users and view their profiles
- **Chat Functionality**: AI-powered chat for recipe assistance
- **Grocery List**: Generate shopping lists from meal plans
- **Recipe Scraping**: Automated recipe collection from external sources

## Architecture

### Backend

The backend is built using ASP.NET Core 9.0 following the MVC (Model-View-Controller) pattern.

#### Technologies

- **Framework**: ASP.NET Core 9.0
- **Language**: C# with .NET 9.0
- **Database**: 
  - SQL Server (Azure SQL Database) with Entity Framework Core 9.0
  - Redis for distributed caching
- **Authentication**: ASP.NET Core Identity
- **AI Integration**: 
  - Microsoft Semantic Kernel 1.57.0
  - Mistral AI (mistral-large-latest model) for chat and suggestions
- **Storage**: Azure Blob Storage for file management
- **Web Scraping**: HtmlAgilityPack for recipe extraction

#### Backend Components

1. **Controllers** (`/MVCPrject/Controllers/`)
   - `HomeController.cs`: Main dashboard and suggestions
   - `RecipeController.cs`: Recipe browsing and management
   - `MealPlannerController.cs`: Meal planning functionality
   - `ProfileController.cs`: User profile management
   - `ChatController.cs`: AI chat interface
   - `LandingController.cs`: Authentication (login/register)
   - `NutritionSummariesController.cs`: Nutrition tracking
   - `GroceryListController.cs`: Shopping list generation

2. **Services** (`/MVCPrject/Data/`)
   - `RecipeRetrieverService.cs`: Recipe scraping and retrieval
   - `RecipeManipulationService.cs`: Recipe CRUD operations
   - `UserService.cs`: User management
   - `MealLogService.cs`: Meal planning and logging
   - `SuggestionService.cs`: AI-powered recipe suggestions
   - `CacheService.cs`: Redis caching abstraction
   - `AIServices.cs`: AI integration utilities

3. **Models** (`/MVCPrject/Models/`)
   - `Recipe.cs`: Recipe data model with ingredients and instructions
   - `User.cs`: User identity model
   - `NutritionSummary.cs`: Nutritional information
   - `Chat.cs`: Chat history
   - View models for authentication and data transfer

4. **Data Context** (`/MVCPrject/Data/`)
   - `DBContext.cs`: Entity Framework database context

#### API Endpoints

The application uses standard MVC routing with the default pattern: `{controller=Landing}/{action=Index}/{id?}`

### Frontend

The frontend is built using Razor Views with JavaScript for dynamic functionality.

#### Technologies

- **Template Engine**: Razor (CSHTML)
- **JavaScript**: Vanilla JavaScript (ES6+)
- **CSS**: Custom CSS with responsive design
- **Libraries**: 
  - jQuery (via CDN/lib)
  - Bootstrap (via lib folder)
  - jQuery Validation

#### Frontend Components

1. **Views** (`/MVCPrject/Views/`)
   - `Landing/`: Landing page, login, and registration
   - `Home/`: Dashboard with recipe suggestions
   - `Recipe/`: Recipe browsing and details
   - `MealPlanner/`: Interactive meal planning calendar
   - `Profile/`: User profile pages
   - `Chat/`: AI chat interface
   - `Shared/`: Layout and shared components

2. **Static Assets** (`/MVCPrject/wwwroot/`)
   
   **JavaScript** (`/js/`)
   - `MealPlanner.js`: Meal planning calendar functionality (78KB)
   - `AddRecipeModal.js`: Recipe addition interface (42KB)
   - `dashboardCards.js`: Dashboard card interactions (24KB)
   - `profile.js`: Profile management (16KB)
   - `profileOthers.js`: Other users' profiles (15KB)
   - `EditModal.js`: Recipe editing interface (10KB)
   - `toggle.js`: UI toggle utilities (5KB)

   **CSS** (`/css/`)
   - `AddRecipeModal.css`: Recipe modal styling
   - `MealPlanner/`: Meal planner specific styles
   - `profile.css`: Profile page styling
   - `profileOthers.css`: Other profiles styling
   - `recipe.css`: Recipe pages styling
   - `landing.css`: Landing page styling
   - `home.css`: Dashboard styling
   - `UserLogin.css`, `UserRegister.css`: Authentication pages
   - `site.css`: Global styles

   **Images** (`/img/`)
   - User avatars and recipe images
   - UI assets and icons

   **Libraries** (`/lib/`)
   - Bootstrap
   - jQuery
   - jQuery Validation

## Prerequisites

- .NET 9.0 SDK or later
- SQL Server (or Azure SQL Database)
- Redis Server
- Azure Storage Account (for blob storage)
- Mistral AI API Key

## Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/Lorieta/MVCPrject.git
   cd MVCPrject
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure the application**
   - Create an `appsettings.json` file in the `MVCPrject` directory (see [Configuration](#configuration) section)

4. **Apply database migrations**
   ```bash
   cd MVCPrject
   dotnet ef database update
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Access the application**
   - Navigate to `https://localhost:5001` (or the port specified in your launch settings)

## Configuration

Create an `appsettings.json` file in the `/MVCPrject` directory with the following structure:

```json
{
  "ConnectionStrings": {
    "RecipeDbConnection": "Server=your-server;Database=RecipeDB;User Id=your-user;Password=your-password;TrustServerCertificate=True;"
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
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Required Configuration Values

1. **SQL Server Connection String**: Database for storing recipes, users, and meal plans
2. **Mistral AI API Key**: For AI-powered chat and recipe suggestions
3. **Azure Blob Storage Connection String**: For storing user-uploaded images
4. **Redis Connection String**: For distributed caching and session management

## Usage

### For Users

1. **Registration/Login**
   - Navigate to the landing page
   - Register a new account or login with existing credentials

2. **Browse Recipes**
   - View recipe suggestions on the dashboard
   - Search and filter recipes by category, type, or ingredients
   - View detailed recipe information including ingredients and instructions

3. **Meal Planning**
   - Access the Meal Planner from the navigation
   - Drag and drop recipes onto calendar days
   - Generate grocery lists from your meal plan

4. **AI Chat**
   - Use the chat feature for recipe recommendations
   - Ask questions about cooking or nutrition

5. **Profile Management**
   - Upload a profile picture
   - Follow other users
   - View your saved recipes and meal history

### For Developers

#### Running in Development Mode

```bash
dotnet run --environment Development
```

#### Building for Production

```bash
dotnet publish -c Release -o ./publish
```

#### Database Migrations

**Create a new migration:**
```bash
dotnet ef migrations add MigrationName
```

**Update database:**
```bash
dotnet ef database update
```

## Project Structure

```
MVCPrject/
├── MVCPrject.sln                 # Visual Studio solution file
├── README.md                     # This file
├── azure-pipelines.yml           # CI/CD pipeline configuration
├── clear_redis_cache.ps1         # Redis cache cleanup script
└── MVCPrject/                    # Main application project
    ├── MVCPrject.csproj          # Project file
    ├── Program.cs                # Application entry point
    ├── appsettings.json          # Configuration (not in source control)
    │
    ├── Controllers/              # MVC Controllers (Backend)
    │   ├── HomeController.cs
    │   ├── RecipeController.cs
    │   ├── MealPlannerController.cs
    │   ├── ProfileController.cs
    │   ├── ChatController.cs
    │   ├── LandingController.cs
    │   ├── NutritionSummariesController.cs
    │   └── GroceryListController.cs
    │
    ├── Models/                   # Data Models (Backend)
    │   ├── Recipe.cs
    │   ├── User.cs
    │   ├── NutritionSummary.cs
    │   ├── Chat.cs
    │   └── ViewModels/
    │
    ├── Data/                     # Services & Data Access (Backend)
    │   ├── DBContext.cs
    │   ├── RecipeRetrieverService.cs
    │   ├── RecipeManipulationService.cs
    │   ├── UserService.cs
    │   ├── MealLogService.cs
    │   ├── SuggestionService.cs
    │   ├── CacheService.cs
    │   └── AIServices.cs
    │
    ├── Views/                    # Razor Views (Frontend)
    │   ├── Shared/
    │   │   └── _Layout.cshtml
    │   ├── Landing/
    │   ├── Home/
    │   ├── Recipe/
    │   ├── MealPlanner/
    │   ├── Profile/
    │   └── Chat/
    │
    ├── wwwroot/                  # Static Files (Frontend)
    │   ├── css/                  # Stylesheets
    │   ├── js/                   # JavaScript files
    │   ├── img/                  # Images
    │   ├── lib/                  # Third-party libraries
    │   └── favicon.ico
    │
    └── Properties/
        └── launchSettings.json
```

### Key Files

- **Program.cs**: Application configuration and startup, including dependency injection setup
- **DBContext.cs**: Entity Framework database context with all entity configurations
- **_Layout.cshtml**: Main layout template used across all pages

## Contributing

This is a private project. For questions or contributions, please contact the repository owner.

## License

All rights reserved.
