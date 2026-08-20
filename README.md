# 📸 Instagram Clone - Backend System & Architecture

An enterprise-ready, high-performance Social Media Backend API built with **.NET 8 (ASP.NET Core Web API)**, adhering strictly to **Clean Architecture** and **SOLID** principles. The system includes an integrated **Blazor Interactive Web Client** used as a reference UI to demonstrate and test the backend capabilities end-to-end.

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C# 12](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-8.0-512BD4)](https://docs.microsoft.com/en-us/ef/core/)
[![Docker Compose](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)](https://www.docker.com/)
[![SignalR](https://img.shields.io/badge/SignalR-Realtime%20WebSockets-512BD4)](https://dotnet.microsoft.com/apps/aspnet/signalr)
[![Hangfire](https://img.shields.io/badge/Hangfire-Background%20Jobs-FF4154)](https://www.hangfire.io/)
[![Redis](https://img.shields.io/badge/Redis-Distributed%20Cache-DC382D?logo=redis)](https://redis.io/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## 🏛️ System Architecture & Design

The backend is architected following **Clean Architecture (Onion Architecture)**, guaranteeing loose coupling, high maintainability, testability, and independence from external frameworks and database providers.

```text
InstagramClone/
├── InstagramClone.Domain/         # Core Domain: Entities, Value Objects, Domain Enums, Exceptions
├── InstagramClone.Application/    # Business Logic: CQRS/Services, DTOs, Mappings, FluentValidation, Interfaces
├── InstagramClone.Infrastructure/ # Data Access: EF Core 8, MSSQL Repositories, Redis Cache, Hangfire Jobs, Identity
├── InstagramClone.API/            # Presentation: RESTful Controllers, SignalR Hubs, Middlewares, Action Filters
├── InstagramClone.Common/         # Cross-Cutting Concerns: Result<T> Pattern, Constants, Helper Utilities
├── InstagramClone.Web/            # Demo Client: Blazor Server Web UI for live testing & interaction
├── nginx/                         # Reverse Proxy & SSL Gateway
└── docker-compose.yml             # Full-Stack Multi-Container Orchestration
```

### 📐 Architectural Patterns & Design Highlights
- **Repository & Unit of Work Pattern**: Abstraction over database operations ensuring transactional integrity and simplifying unit testing via mock repositories.
- **Result Pattern (`Result<T>`)**: Unified error handling across all application layers without relying on expensive exception throwing for standard business validation.
- **Cursor-based Pagination**: High-performance pagination for feeds, messages, and notifications to avoid performance degradation on large datasets.
- **Action Filters & Model Validation**: Centralized FluentValidation and custom authorization ownership filters (`[AuthorizeOwnership]`).

---

## ⚡ Core Backend Features & Capabilities

### 1. 🔐 Security & Identity Management
- **JWT Authentication & Token Lifecycle**: Access tokens (HMAC-SHA256) with secure Refresh Token rotation.
- **ASP.NET Core Identity**: Custom User & Role stores with password hashing (PBKDF2/BCrypt) and account lockout policies.
- **Resource-Based Authorization**: Attribute filters ensuring users can only edit/delete their own posts, comments, stories, and messages.

### 2. 📸 Media Processing Pipeline
- **Automated Processing with SixLabors ImageSharp**: Automatic image decompression, dimension normalization, aspect-ratio constraint enforcement, and quality optimization.
- **Modular Storage Abstraction**: Extensible storage interface (`ILocalStorageService` / Cloud Storage ready) with MIME-type verification and file size validation.

### 3. ⏱️ Distributed Background Processing (Hangfire)
- **24-Hour Story Expiration Engine**: Automatic background job scheduling to deactivate stories after exactly 24 hours.
- **Persistent Storage**: Jobs are persisted in SQL Server tables (`Hangfire.Job`, `Hangfire.State`), surviving application restarts.

### 4. 💬 Real-Time WebSockets Engine (SignalR)
- **ChatHub & NotificationHub**: Bi-directional communication for 1-1 Direct Messages and Group Chats.
- **Realtime Features**: Instant message delivery, typing indicators, realtime emoji reactions (❤️, 😂, 🔥, 👍, 😮, 😢), and live notification broadcasting.

### 5. 🚀 High-Performance Caching & Rate Limiting
- **Distributed Caching (Redis) & Memory Cache**: Cache-aside strategy with automatic cache invalidation on mutations (user profiles, active stories, followers).
- **Rate Limiting**: Built-in ASP.NET Core rate limiting middleware protecting API endpoints from spam and brute-force attacks.

### 6. 📊 Observability, Logging & Diagnostics
- **Structured Logging (Serilog & Seq)**: Enriched structured JSON logs with correlation IDs, request timing, and SQL execution tracing in Seq Console.
- **Health Checks & Monitoring**: Endpoint `/health` and visual dashboard `/healthchecks-ui` monitoring SQL Server, Redis, and storage health.

---

## 🖥️ Client Applications & Interfaces

While the core focus of this repository is the **Backend System & Architecture**, the solution provides two client interfaces for immediate testing and demonstration:

1. **Blazor Server Web Client (`InstagramClone.Web`)**: An integrated client web app (built with Blazor Server & TailwindCSS) demonstrating complete real-time flows, feed interactions, stories, and direct messaging.
2. **Swagger / OpenAPI (`/swagger`)**: Interactive documentation for exploring and testing RESTful endpoints.

---

## 🛠️ Backend Tech Stack

| Category | Technology |
| :--- | :--- |
| **Framework** | .NET 8.0 (ASP.NET Core Web API) |
| **Language** | C# 12 |
| **ORM & Database** | Entity Framework Core 8.0, Microsoft SQL Server 2022 |
| **Background Processing** | Hangfire (with SQL Server Storage) |
| **Realtime Gateway** | ASP.NET Core SignalR (WebSockets) |
| **Caching Layer** | Redis (Alpine) / Memory Cache |
| **Media Processing** | SixLabors ImageSharp |
| **Mapping & Validation** | AutoMapper, FluentValidation |
| **Logging & Diagnostics** | Serilog, Seq Logs, ASP.NET Core HealthChecks UI |
| **Containerization** | Docker & Docker Compose (Multi-Container Architecture) |
| **Demo Client UI** | Blazor Interactive Server (.NET 8) |

---

## 🐳 Quick Start with Docker Compose

The complete environment (Backend API, MSSQL Database, Redis, Seq Logs, and Blazor Web Client) is orchestrated with Docker Compose for seamless single-command setup.

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running.

### 1. Clone & Configuration
```bash
git clone https://github.com/manhdoduc/InstagramClone.git
cd InstagramClone

# Copy example environment configuration
cp .env.example .env
```

### 2. Start the Stack
```bash
docker compose up -d --build
```

> **Note**: Database schema migrations and media directory permissions (`/app/wwwroot/media`) are applied automatically upon container startup. No manual migration command is needed.

---

## 🌐 Service Endpoints

| Service | URL | Description |
| :--- | :--- | :--- |
| 🌐 **Web Client (Blazor UI Demo)** | **[http://localhost:5242](http://localhost:5242)** | Integrated Reference Client UI |
| 📑 **Swagger API Docs** | [http://localhost:5063/swagger](http://localhost:5063/swagger) | Interactive API Explorer & Schema Docs |
| 🩺 **HealthChecks UI** | [http://localhost:5063/healthchecks-ui](http://localhost:5063/healthchecks-ui) | Real-time System & Infrastructure Health |
| 📜 **Seq Log Server** | [http://localhost:5342](http://localhost:5342) | Centralized Structured Log Viewer |
| 🔒 **Nginx Proxy** | [https://localhost:44391](https://localhost:44391) | Reverse Proxy Gateway |

---

## 💻 Local Development Setup (Manual CLI)

To run the backend services without Docker:

1. Ensure local instances of **SQL Server** and **Redis** are active.
2. Configure connection strings in `InstagramClone.API/appsettings.Development.json`.
3. Apply Entity Framework migrations:
   ```bash
   dotnet ef database update --project InstagramClone.Infrastructure --startup-project InstagramClone.API
   ```
4. Start Backend API:
   ```bash
   dotnet run --project InstagramClone.API
   ```
5. Start Blazor Client (Optional - in a separate terminal):
   ```bash
   dotnet run --project InstagramClone.Web
   ```

---

## 🧪 Testing & Code Quality

The backend features extensive automated test suites:
- **Unit Tests (`InstagramClone.Application.UnitTests`)**: Testing business logic, services, and DTO mappings with Moq and xUnit.
- **Infrastructure Tests (`InstagramClone.Infrastructure.UnitTests`)**: Testing repository operations, caching, and storage handlers.

Run all tests via CLI:
```bash
dotnet test
```

---

## 📝 License

Distributed under the [MIT License](LICENSE).
