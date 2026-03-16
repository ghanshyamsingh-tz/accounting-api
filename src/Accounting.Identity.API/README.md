# Accounting Identity Service

A secure, scalable identity and user management microservice built with Clean Architecture and Domain-Driven Design.

## Overview

The Identity service handles user registration, authentication, login attempt tracking, security monitoring, and notification management for the NEMT Accounting System. It implements the Identity bounded context as an independent microservice with its own database schema.

## Architecture

### Clean Architecture Layers

```
┌─────────────────────────────────────────────┐
│           API Layer (Presentation)           │
│  Controllers, Middleware, OpenAPI Docs       │
└──────────────────┬──────────────────────────┘
                   │ depends on
┌──────────────────▼──────────────────────────┐
│         Application Layer (Use Cases)        │
│  Commands, Queries, Handlers, DTOs           │
└──────────────────┬──────────────────────────┘
                   │ depends on
┌──────────────────▼──────────────────────────┐
│          Domain Layer (Business Logic)       │
│  Aggregates, Value Objects, Domain Events    │
│  NO external dependencies                    │
└──────────────────▲──────────────────────────┘
                   │ implements
┌──────────────────┴──────────────────────────┐
│    Infrastructure Layer (External Concerns)  │
│  Database, Caching, Messaging, External APIs │
└─────────────────────────────────────────────┘
```

### Key Components

- **Domain Aggregates**: UserAccount, LoginAttempt, SecurityEvent
- **Value Objects**: EmailAddress, PasswordHash, IPAddress, GeographicLocation
- **Persistence**: PostgreSQL with `identity` schema, EF Core
- **Caching**: Redis for session data and geolocation lookups
- **Messaging**: Kafka for domain events (Outbox pattern)
- **Authentication**: JWT tokens with Keycloak integration
- **Observability**: Serilog, OpenTelemetry, correlation IDs

## Features

### User Story 1: User Account Registration and Management
- User registration with email/password
- Secure authentication with JWT tokens
- Profile management
- Account locking/unlocking

### User Story 2: Real-time Login Attempt Notifications
- Track all authentication attempts (successful and failed)
- Send notifications within 60 seconds
- Login history with IP, location, device details
- "This wasn't me" self-service account locking

### User Story 3: Suspicious Activity Detection and Alerts
- Brute force detection (5+ failures in 10 minutes)
- Geographic anomaly detection
- Velocity anomaly detection (multiple locations)
- Admin security dashboard

### User Story 4: Notification Preferences and Management
- Multi-channel notifications (Email, SMS, In-App)
- Customizable notification frequency
- Per-event-type preferences
- Daily digest option

## Technology Stack

- **.NET 9.0** - Application framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - ORM
- **PostgreSQL 17+** - Primary database
- **Redis** - Caching layer
- **Apache Kafka** - Event streaming
- **Keycloak** - OAuth2/OIDC provider
- **FluentValidation** - Input validation
- **MediatR** - CQRS pattern
- **Polly** - Resilience policies
- **Serilog** - Structured logging
- **OpenTelemetry** - Distributed tracing
- **xUnit** - Testing framework
- **FluentAssertions** - Test assertions
- **Testcontainers** - Integration testing

## Quick Start

### Prerequisites

- .NET 9 SDK or higher
- Docker & Docker Compose
- PostgreSQL 17+
- Keycloak 26+

### Run with Docker Compose

```powershell
# Start all services
docker compose -f docker/docker-compose.yml up -d

# Apply database migrations
docker compose exec identity-api dotnet ef database update

# Health check
curl http://localhost:5001/health
```

### Run Locally

```powershell
cd src/Accounting.Identity.API

# Configure environment (see quickstart.md)
# Create .env file with connection strings

# Run migrations
dotnet ef database update --project ../Accounting.Identity.Infrastructure

# Start service
dotnet run
```

Service will be available at `http://localhost:5001`

## Testing

```powershell
# Unit tests (fast, no dependencies)
dotnet test tests/Accounting.Identity.Domain.Tests
dotnet test tests/Accounting.identity.Application.Tests

# Integration tests (requires Docker for Testcontainers)
dotnet test tests/Accounting.Identity.IntegrationTests

# Contract tests (API and event schema validation)
dotnet test tests/Accounting.Identity.ContractTests

# All tests with coverage
dotnet test /p:CollectCoverage=true
```

## API Documentation

- **Swagger UI**: http://localhost:5001/swagger
- **OpenAPI Spec**: [specs/002-user-login-alerts/contracts/api-spec.yaml](../../../specs/002-user-login-alerts/contracts/api-spec.yaml)
- **Event Schemas**: [specs/002-user-login-alerts/contracts/event-schemas.md](../../../specs/002-user-login-alerts/contracts/event-schemas.md)

## Project Structure

```
src/Accounting.Identity.API/
├── Controllers/           # HTTP endpoints
├── Middleware/           # Exception handling, logging
├── Program.cs            # Application entry point
└── appsettings.json      # Configuration

src/Accounting.Identity.Application/
├── Commands/             # Write operations (CQRS)
├── Queries/              # Read operations (CQRS)
├── DTOs/                 # Data transfer objects
└── Interfaces/           # Service contracts

src/Accounting.Identity.Domain/
├── Aggregates/           # Aggregate roots, entities
├── Events/               # Domain events
└── Interfaces/           # Repository contracts

src/Accounting.Identity.Infrastructure/
├── Persistence/          # EF Core, repositories
├── Services/             # External integrations
├── Outbox/              # Outbox pattern
└── Resilience/          # Polly policies

tests/
├── Accounting.Identity.Domain.Tests/
├── Accounting.Identity.Application.Tests/
├── Accounting.Identity.IntegrationTests/
└── Accounting.Identity.ContractTests/
```

## Configuration

### Environment Variables

```bash
# Database
ConnectionStrings__IdentityDb="Host=localhost;Database=nemtaccounting;Username=identity_service;Password=***"

# Keycloak
Authentication__Authority="http://localhost:8080/realms/nemtaccounting"
Authentication__ClientId="identity-service"
Authentication__ClientSecret="***"

# Redis
Redis__ConnectionString="localhost:6379"

# Kafka
Kafka__BootstrapServers="localhost:9092"

# Email (SMTP)
Email__SmtpHost="smtp.example.com"
Email__SmtpPort=587
Email__Username="noreply@example.com"
Email__Password="***"
```

See [quickstart.md](../../../specs/002-user-login-alerts/quickstart.md) for complete configuration guide.

## Constitutional Principles

This service adheres to core principles defined in `.specify/memory/constitution.md`:

- ✅ **I. Production-Ready Code**: Resilience, performance optimization, observability
- ✅ **II. Domain-Driven Design**: Clean Architecture, bounded context isolation
- ✅ **III. Test-First Development**: Comprehensive test coverage
- ✅ **IV. Resilience & Observability**: Outbox pattern, retry policies, tracing
- ✅ **V. Eventual Consistency**: Domain events, no distributed transactions
- ✅ **VI. Result Pattern**: No exceptions for business logic
- ✅ **VII. PostgreSQL & EF Core Standards**: Domain/persistence separation

## Contributing

1. Follow Test-Driven Development (TDD): Write tests first
2. Respect aggregate boundaries: No navigation properties
3. Use Result pattern: Return `Result<T>` instead of throwing exceptions
4. Separate domain from persistence: Never use EF entities in domain layer
5. Run all tests before committing: `dotnet test`

## Documentation

- **Feature Specification**: [specs/002-user-login-alerts/spec.md](../../../specs/002-user-login-alerts/spec.md)
- **Implementation Plan**: [specs/002-user-login-alerts/plan.md](../../../specs/002-user-login-alerts/plan.md)
- **Data Model**: [specs/002-user-login-alerts/data-model.md](../../../specs/002-user-login-alerts/data-model.md)
- **Quickstart Guide**: [specs/002-user-login-alerts/quickstart.md](../../../specs/002-user-login-alerts/quickstart.md)
- **Tasks**: [specs/002-user-login-alerts/tasks.md](../../../specs/002-user-login-alerts/tasks.md)

## License

Proprietary - NEMT Accounting System
