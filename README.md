# 📸 Instagram Clone API

A robust, enterprise-ready, scalable backend API for an Instagram-like social media platform, built with **.NET 8** adhering strictly to **Clean Architecture** principles.

[![NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Docker Compose](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)](https://www.docker.com/)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-8.0-512BD4)](https://docs.microsoft.com/en-us/ef/core/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## 🚀 Key Features

- **🔐 Authentication & Authorization**: JWT Token authentication, Refresh Tokens, ASP.NET Core Identity with custom security policies.
- **👤 User Management & Profiles**: User profile management, avatar uploads with automated ImageSharp resizing and compression.
- **👥 Social Interactions**: Follow/Unfollow system, user activity feeds, notifications.
- **📸 Content & Media**: Posts creation with multiple image uploads, post likes, comments, and nested comment chains.
- **💬 Real-time Messaging (SignalR)**: Direct messaging system, SignalR hub for real-time notifications, typing indicators, and message media uploads.
- **⚙️ Configurable Media Processing**: Configurable image dimensions and file size limits via `MediaSettings` (`appsettings.json`).
- **⚡ Performance & Caching**: Redis / In-Memory caching layer, Rate Limiting middleware.
- **📊 Observability & Health Monitoring**: Integrated Health Checks (UI at `/healthchecks-ui`), structured logging with **Serilog** & **Seq**.

---

## 🏗 Architecture & Project Structure

The project follows **Clean Architecture** and the **Repository & Unit of Work Patterns** to ensure high testability, maintainability, and clean separation of concerns.

```text
InstagramClone/
├── InstagramClone.API/            # Presentation Layer: Controllers, Middlewares, SignalR Hubs, Filters
├── InstagramClone.Application/    # Business Logic: DTOs, Services, Feature Handlers, Mappings, Interfaces
├── InstagramClone.Infrastructure/ # Infrastructure: EF Core DbContext, Repositories, Caching, External Services
├── InstagramClone.Domain/         # Domain Layer: Entities, Enums, Domain Constants
├── InstagramClone.Common/         # Shared: Result wrapper, Helper utilities, Configuration Options
├── nginx/                         # Reverse Proxy configuration & SSL
└── docker-compose.yml             # Orchestration for API, SQL Server 2022, Redis, Seq & Nginx
```

---

## 🛠 Tech Stack

| Category | Technology |
| :--- | :--- |
| **Framework** | .NET 8 (ASP.NET Core Web API) |
| **Database** | SQL Server 2022 / Entity Framework Core 8 |
| **Caching** | Redis / In-Memory Cache |
| **Real-time** | ASP.NET Core SignalR |
| **Object Mapping** | AutoMapper |
| **Validation** | FluentValidation |
| **Image Processing** | SixLabors ImageSharp |
| **Logging & Monitoring** | Serilog, Seq Logs, ASP.NET Core HealthChecks UI |
| **Reverse Proxy** | Nginx |
| **Containerization** | Docker & Docker Compose |

---

## ⚙️ Quick Start (1-Click Docker Setup)

The entire application stack is containerized for seamless 1-click startup without manual software installation.

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running.

### 1. Clone & Environment Setup
```bash
git clone https://github.com/manhdoduc/InstagramClone.git
cd InstagramClone

# Copy the example environment variables file
cp .env.example .env
```

### 2. Launch with Docker Compose
Run the following single command to build and start all containers (API, SQL Server, Redis, Seq Logs, Nginx):

```bash
docker compose up -d --build
```

> **Note**: Database migrations apply automatically during application startup in containerized environments. No manual `dotnet ef database update` is required!

---

## 🌐 Service Endpoints

Once the containers are running, you can access the services at the following URLs:

| Service | Access URL | Description |
| :--- | :--- | :--- |
| **Swagger UI** | [http://localhost:5063/swagger](http://localhost:5063/swagger) | Interactive API Documentation |
| **HealthChecks UI** | [http://localhost:5063/healthchecks-ui](http://localhost:5063/healthchecks-ui) | Real-time System & DB Health Dashboard |
| **Seq Log Console** | [http://localhost:5342](http://localhost:5342) | Centralized Structured Log Viewer |
| **Nginx HTTPS Proxy** | [https://localhost:44391](https://localhost:44391) | Reverse Proxy Entry Point |

---

## 🔧 Configuration (`MediaSettings`)

Media limits and image resize target dimensions can be adjusted centrally in `InstagramClone.API/appsettings.json`:

```json
"MediaSettings": {
  "MaxFileSizeBytes": 5242880,
  "Avatar": {
    "MaxWidth": 500,
    "MaxHeight": 500
  },
  "Post": {
    "MaxWidth": 1080,
    "MaxHeight": 1350
  },
  "ChatImage": {
    "MaxWidth": 400,
    "MaxHeight": 400
  }
}
```

---

## 💻 Manual Local Development (Without Docker)

If you prefer running the API locally via `dotnet run`:

1. Ensure a local SQL Server / Redis instance is running.
2. Update connection strings in `InstagramClone.API/appsettings.Development.json`.
3. Apply database migrations:
   ```bash
   dotnet ef database update --project InstagramClone.Infrastructure --startup-project InstagramClone.API
   ```
4. Run the API project:
   ```bash
   dotnet run --project InstagramClone.API
   ```

---

## 📝 License

This project is licensed under the [MIT License](LICENSE).
