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

The application is containerized and can be easily run using Docker Compose.

1. **Clone the repository**
   ```bash
   git clone https://github.com/manhdoduc/InstagramClone.git
   cd InstagramClone
   ```

2. **Run with Docker Compose**
   Build and start all services (API, SQL Server, Seq Logs, Nginx) in detached mode:
   ```bash
   docker-compose up -d --build
   ```

3. **Database Migration**
   The database connection is automatically configured to point to the SQL Server container. Apply the migrations to create the database schema:
   ```bash
   dotnet ef database update --project InstagramClone.Infrastructure --startup-project InstagramClone.API
   ```
   *(Ensure you have the .NET EF tools installed: `dotnet tool install --global dotnet-ef`)*

4. **Access the Services**
   - **API (HTTP)**: `http://localhost:5063`
   - **Nginx (HTTPS)**: `https://localhost:44391`
   - **Seq Logs**: `http://localhost:5342`

## 📝 Git Workflow

- Features are developed on separate branches and merged into `master`.
- Ensure all sensitive files (like `appsettings.Development.json` and local media uploads) are properly ignored via `.gitignore` (already configured).
