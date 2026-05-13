# Instagram Clone API

A robust, scalable backend API for an Instagram-like social media application, built with .NET Core and adhering to Clean Architecture principles.

## 🚀 Features

- **User Management**: Authentication, authorization (JWT), user profiles.
- **Social Interactions**: Following/followers system.
- **Content**: Create posts, comment on posts, like/react.
- **Real-time Chat**: Direct messaging using SignalR, with typing indicators and online status.
- **Media Upload**: File upload system for images/avatars (ready for Azure Blob Storage migration).
- **Performance**: In-memory cache and Rate Limiting for high performance and security.

## 🏗 Architecture

This project is structured using **Clean Architecture** and incorporates the **Repository & Unit of Work Patterns** to ensure a highly decoupled, testable, and maintainable codebase.

- **API Layer**: ASP.NET Core Web API (Controllers, Middlewares, SignalR Hubs).
- **Application Layer**: Business logic, Services, DTOs, FluentValidation, AutoMapper profiles.
- **Domain Layer**: Core entities, interfaces.
- **Infrastructure Layer**: Entity Framework Core DbContext, Repository implementations, external services.

## 🛠 Tech Stack

- **Framework**: .NET 8 (or your current version)
- **Database**: SQL Server (LocalDB for dev) & Entity Framework Core
- **Real-time**: SignalR
- **Caching**: In-memory cache
- **Mapping**: AutoMapper
- **Validation**: FluentValidation
- **Logging**: Serilog (with Seq support)

## ⚙️ Local Development Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/manhdoduc/InstagramClone.git
   cd InstagramClone
   ```

2. **Configuration**
   - Copy `InstagramClone.API/appsettings.example.json` to `InstagramClone.API/appsettings.Development.json`.
   - Update the `DefaultConnection` string with your local SQL Server details.
   - Update the `JwtSettings:Key` with a strong secret key.

3. **Database Migration**
   ```bash
   dotnet ef database update --project InstagramClone.Infrastructure --startup-project InstagramClone.API
   ```

4. **Run the Application**
   ```bash
   cd InstagramClone.API
   dotnet run
   ```

## 📝 Git Workflow

- Features are developed on separate branches and merged into `master`.
- Ensure all sensitive files (like `appsettings.Development.json` and local media uploads) are properly ignored via `.gitignore` (already configured).
