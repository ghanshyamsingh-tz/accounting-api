# Implementation Plan: User Login Alerts System

**Branch**: `002-user-login-alerts` | **Date**: 2026-03-13 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-user-login-alerts/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

This feature implements a comprehensive user management system with real-time security notifications for login attempts. The system must authenticate users securely, log all authentication attempts with detailed context (IP, location, device), detect suspicious activity patterns, and send multi-channel notifications within 60 seconds. The implementation follows Clean Architecture with DDD principles, separating the Identity bounded context as an independent microservice with its own database schema. All operations use the Result pattern for error handling, implement resilience policies via Polly, and maintain eventual consistency through domain events and the Outbox pattern.

## Technical Context

**Language/Version**: .NET 10 (C# with nullable reference types enabled)  
**Primary Dependencies**: ASP.NET Core 10, EF Core 10, FluentValidation, Polly, Serilog, OpenTelemetry  
**Storage**: PostgreSQL 17+ with identity schema, Redis for session/cache, Kafka for events  
**Testing**: xUnit, FluentAssertions, Testcontainers for integration tests, WireMock.Net for contract tests  
**Target Platform**: Linux containers (Docker), Kubernetes deployment  
**Project Type**: Microservice (Identity bounded context) within distributed system  
**Performance Goals**: <50ms p50 API latency, <200ms p95 latency, 1000 concurrent users, <60s notification delivery  
**Constraints**: 99.5% notification delivery SLA, <2% false positive rate for suspicious activity detection, 90-day audit retention  
**Scale/Scope**: Multi-tenant system, 10k+ users, high-security requirements, real-time event processing

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Reference**: See `.specify/memory/constitution.md` for governing principles.

### ✅ I. Production-Ready Code
- [ ] Parallel processing for bulk operations (login history queries, notification batch sends)
- [ ] Resilience patterns configured (email/SMS service retries, circuit breakers)
- [ ] CancellationToken propagation through all async operations
- [ ] Thread-safe concurrent login attempt tracking
- [ ] Proper resource disposal (DbContext, HttpClient, Redis connections)
- [ ] Performance-optimized queries (AsNoTracking, projections, indexed columns)
- [ ] Structured logging with correlation IDs for all operations
- [ ] Result pattern for all domain and application layer error handling

### ✅ II. Domain-Driven Design & Clean Architecture
- [ ] **Domain Layer**: UserAccount, LoginAttempt, SecurityEvent aggregates with zero infrastructure dependencies
- [ ] **Application Layer**: RegisterUserHandler, AuthenticateUserHandler, DetectSuspiciousActivityHandler with FluentValidation
- [ ] **Infrastructure Layer**: IdentityDbContext, EmailNotificationService, IPGeolocationService with separate persistence entities
- [ ] **API Layer**: AccountsController, AuthenticationController with global exception middleware, Problem Details responses
- [ ] Aggregate rules enforced (UserAccount root modifies internal state, reference by ID only)
- [ ] Identity bounded context isolated with own database schema
- [ ] Cross-context events for account events (UserRegistered, SuspiciousActivityDetected)

### ✅ III. Test-First Development
- [ ] Acceptance criteria from spec.md converted to test scenarios
- [ ] Domain tests verify aggregate invariants (password policy, lockout rules)
- [ ] Application tests verify handlers return correct Result types
- [ ] Integration tests verify database persistence and queries
- [ ] Contract tests verify API endpoints match OpenAPI specification
- [ ] Contract tests verify event schemas for cross-service communication

### ✅ IV. Resilience & Observability
- [ ] **Service Isolation**: Identity service with dedicated PostgreSQL schema
- [ ] **Resilience Patterns**: Email/SMS retry (3 attempts, exponential backoff), circuit breaker for external services
- [ ] **Eventual Consistency**: Login notifications via domain events, Outbox pattern for cross-service events
- [ ] **Idempotent Operations**: Login attempts use unique request IDs, notification deduplication
- [ ] **Reliable Messaging**: Outbox pattern for UserRegistered, AccountLocked events
- [ ] **Observability**: Correlation IDs, OpenTelemetry traces, structured logs for authentication flows
- [ ] **Graceful Degradation**: System continues authentication even if notification delivery fails

### ✅ V. Eventual Consistency
- [ ] No distributed transactions (2PC forbidden)
- [ ] Outbox pattern mandatory for UserRegistered, AccountLocked, SuspiciousActivityDetected events
- [ ] Domain events for cross-aggregate consistency (UserAccount → LoginAttempt → SecurityEvent)
- [ ] All event consumers idempotent (notification handlers check if already sent)

### ✅ VI. Result Pattern for Error Handling
- [ ] Domain methods return Result or Result<T> (e.g., UserAccount.Authenticate returns Result<AuthenticationToken>)
- [ ] Application handlers return Result<TResponse>
- [ ] Controllers map Result to HTTP status codes (validation→400, NotFound→404, Conflict→409)
- [ ] Global exception middleware handles only infrastructure exceptions (DbException, NetworkException)
- [ ] No try-catch in controllers

### ✅ VII. PostgreSQL & EF Core Standards
- [ ] Domain entities (UserAccount, LoginAttempt) separate from persistence entities (UserAccountEntity, LoginAttemptEntity)
- [ ] PostgreSQL naming: `identity.users`, `identity.login_attempts`, `identity.security_events` (lowercase_snake_case)
- [ ] AsNoTracking() for all read queries (login history, notification preferences)
- [ ] Projections for DTOs (Select only needed fields)
- [ ] Pagination for list queries (GetLoginHistory with page size limits)
- [ ] Indexes on query columns (email, timestamp, ip_address, user_id)
- [ ] Idempotent migrations with Up() and Down()
- [ ] Identity schema isolated from other service schemas

**Status**: ✅ **PASS** - All constitutional principles align with feature requirements. No violations or exceptions needed.

---

## Post-Design Constitution Re-check

*Completed: 2026-03-13 after Phase 1 design artifacts (data-model.md, api-spec.yaml, event-schemas.md, quickstart.md)*

**Verification**: All design artifacts reviewed against constitutional principles. Detailed design maintains compliance.

### ✅ I. Production-Ready Code

**Evidence from Design Artifacts**:
- **data-model.md** specifies performance indexes on `email`, `ip_address`, `created_at`, `user_id` columns (lines 430-470)
- **data-model.md** defines `AsNoTracking()` queries for read-only projections (LoginHistoryQueryModel, SecurityEventQueryModel)
- **event-schemas.md** includes idempotency guidance using CloudEvents `id` field for deduplication
- **api-spec.yaml** specifies rate limiting (5 req/10s for auth, 100 req/min for other endpoints)

**Compliance**: ✅ **PASS** - Design includes all production-ready patterns (indexes, projections, idempotency, rate limiting).

### ✅ II. Domain-Driven Design & Clean Architecture

**Evidence from Design Artifacts**:
- **data-model.md** separates domain entities (UserAccount, LoginAttempt, SecurityEvent) from persistence entities (UserAccountEntity, LoginAttemptEntity, SecurityEventEntity) with explicit Mapper classes
- **data-model.md** defines 3 aggregate roots with clear boundaries, invariants enforced via methods (Authenticate(), LockAccount(), RecordFailure())
- **data-model.md** specifies Identity bounded context with dedicated PostgreSQL schema `identity.*`
- **event-schemas.md** defines cross-context events (UserRegistered, AccountLocked) following CloudEvents 1.0 spec

**Compliance**: ✅ **PASS** - Design strictly follows DDD patterns with explicit aggregate boundaries, domain/persistence separation, and bounded context isolation.

### ✅ III. Test-First Development

**Evidence from Design Artifacts**:
- **data-model.md** lists testable aggregate invariants (password policy enforcement, lockout threshold of 5 failures, email uniqueness)
- **api-spec.yaml** provides complete OpenAPI 3.0.3 contract for contract testing (9 endpoints with request/response schemas)
- **event-schemas.md** defines JSON Schema 7 contracts for all domain events with example payloads
- **event-schemas.md** includes contract test example (xUnit + FluentAssertions + JSON Schema validation)
- **quickstart.md** documents test execution commands for unit, integration, contract, and E2E tests

**Compliance**: ✅ **PASS** - Design enables contract-first development with complete API and event schemas for testing before implementation.

### ✅ IV. Resilience & Observability

**Evidence from Design Artifacts**:
- **data-model.md** specifies Outbox pattern with `outbox_messages` table for reliable event publishing
- **event-schemas.md** uses CloudEvents format with correlation via `id` field for distributed tracing
- **api-spec.yaml** includes Problem Details (RFC 9457) error format with `type`, `title`, `status`, `detail` fields
- **quickstart.md** configures OpenTelemetry, Serilog, and correlation IDs in environment setup
- **api-spec.yaml** specifies rate limiting and 429 Too Many Requests responses for overload protection

**Compliance**: ✅ **PASS** - Design includes resilience patterns (Outbox, rate limiting), observability (CloudEvents, Problem Details, correlation IDs), and graceful degradation.

### ✅ V. Eventual Consistency

**Evidence from Design Artifacts**:
- **data-model.md** specifies Outbox pattern for cross-aggregate events (no 2PC)
- **event-schemas.md** mandates consumer idempotency: "All consumers MUST implement idempotency using eventId as deduplication key"
- **event-schemas.md** defines 5 domain events (UserRegistered, UserAuthenticated, LoginAttemptFailed, AccountLocked, SuspiciousActivityDetected) with CloudEvents spec
- **data-model.md** aggregate methods emit domain events (UserAccount.Register() → UserRegistered, LoginAttempt.RecordFailure() → LoginAttemptFailed)

**Compliance**: ✅ **PASS** - Design enforces eventual consistency via domain events, Outbox pattern, and mandatory consumer idempotency.

### ✅ VI. Result Pattern for Error Handling

**Evidence from Design Artifacts**:
- **data-model.md** aggregate methods designed to return Result types (e.g., "UserAccount.Authenticate() returns Result<AuthenticationToken> with InvalidCredentials, AccountLocked, or Success variants")
- **api-spec.yaml** maps validation errors to 400 Bad Request with Problem Details, business rule violations to 409 Conflict
- **api-spec.yaml** includes error response examples for 400, 401, 409, 429 status codes with RFC 9457 format
- No try-catch blocks in design; exceptions reserved for infrastructure failures (connection errors, not business logic)

**Compliance**: ✅ **PASS** - Design enforces Result pattern for domain/application layers and Problem Details for API error responses.

### ✅ VII. PostgreSQL & EF Core Standards

**Evidence from Design Artifacts**:
- **data-model.md** uses PostgreSQL lowercase_snake_case naming: `identity.user_accounts`, `identity.login_attempts`, `identity.security_events`
- **data-model.md** separates domain entities from persistence entities with ToEntity()/ToDomain() mappers
- **data-model.md** specifies `AsNoTracking()` for read queries (LoginHistoryQueryModel, SecurityEventQueryModel)
- **data-model.md** defines 10 performance indexes on query columns
- **data-model.md** includes pagination guidance for list queries (page size limits)
- **quickstart.md** documents idempotent migrations with `dotnet ef database update`

**Compliance**: ✅ **PASS** - Design adheres to all PostgreSQL and EF Core standards with explicit domain/persistence separation, performance optimizations, and naming conventions.

---

**Final Status**: ✅ **ALL PRINCIPLES SATISFIED** - Design artifacts maintain full constitutional compliance. No violations introduced during detailed design phase.

**Next Phase**: Proceed to task breakdown (`/speckit.tasks` command) for implementation planning.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── Identity.API/                      # Presentation Layer (NEW)
│   ├── Controllers/
│   │   ├── AccountsController.cs     # User registration, profile management
│   │   └── AuthenticationController.cs  # Login, logout, token refresh
│   ├── Middleware/
│   │   └── ExceptionMiddleware.cs    # Global exception handling
│   ├── Program.cs
│   └── appsettings.json
│
├── Identity.Application/              # Application Layer (NEW)
│   ├── Commands/
│   │   ├── RegisterUser/
│   │   │   ├── RegisterUserCommand.cs
│   │   │   ├── RegisterUserHandler.cs
│   │   │   └── RegisterUserValidator.cs
│   │   ├── AuthenticateUser/
│   │   │   ├── AuthenticateUserCommand.cs
│   │   │   ├── AuthenticateUserHandler.cs
│   │   │   └── AuthenticateUserValidator.cs
│   │   └── LockAccount/
│   ├── Queries/
│   │   ├── GetLoginHistory/
│   │   │   ├── GetLoginHistoryQuery.cs
│   │   │   └── GetLoginHistoryHandler.cs
│   │   └── GetUserProfile/
│   ├── DTOs/
│   │   ├── LoginAttemptDto.cs
│   │   └── UserProfileDto.cs
│   └── Interfaces/
│       ├── INotificationService.cs
│       └── IGeolocationService.cs
│
├── Identity.Domain/                   # Domain Layer (NEW)
│   ├── Aggregates/
│   │   ├── UserAccount/
│   │   │   ├── UserAccount.cs         # Aggregate Root
│   │   │   ├── UserAccountId.cs       # Value Object
│   │   │   ├── EmailAddress.cs        # Value Object
│   │   │   ├── PasswordHash.cs        # Value Object
│   │   │   └── AccountStatus.cs       # Enum
│   │   ├── LoginAttempt/
│   │   │   ├── LoginAttempt.cs        # Aggregate Root
│   │   │   ├── LoginAttemptId.cs
│   │   │   ├── IPAddress.cs           # Value Object
│   │   │   ├── GeographicLocation.cs  # Value Object
│   │   │   └── AttemptStatus.cs       # Enum
│   │   └── SecurityEvent/
│   │       ├── SecurityEvent.cs       # Aggregate Root
│   │       ├── SecurityEventId.cs
│   │       ├── EventSeverity.cs       # Enum
│   │       └── ThreatType.cs          # Enum
│   ├── Events/
│   │   ├── UserRegistered.cs
│   │   ├── UserAuthenticated.cs
│   │   ├── LoginAttemptFailed.cs
│   │   ├── SuspiciousActivityDetected.cs
│   │   └── AccountLocked.cs
│   └── Exceptions/
│       ├── InvalidCredentialsException.cs
│       └── AccountLockedException.cs
│
├── Identity.Infrastructure/           # Infrastructure Layer (NEW)
│   ├── Persistence/
│   │   ├── IdentityDbContext.cs
│   │   ├── Configurations/
│   │   │   ├── UserAccountEntityConfiguration.cs
│   │   │   ├── LoginAttemptEntityConfiguration.cs
│   │   │   └── SecurityEventEntityConfiguration.cs
│   │   ├── Entities/                  # Persistence Entities (separate from domain)
│   │   │   ├── UserAccountEntity.cs
│   │   │   ├── LoginAttemptEntity.cs
│   │   │   └── SecurityEventEntity.cs
│   │   ├── Mappers/
│   │   │   ├── UserAccountMapper.cs   # Domain ↔ Persistence
│   │   │   └── LoginAttemptMapper.cs
│   │   ├── Repositories/
│   │   │   ├── UserAccountRepository.cs
│   │   │   └── LoginAttemptRepository.cs
│   │   └── Migrations/                # EF Core migrations
│   ├── Services/
│   │   ├── EmailNotificationService.cs
│   │   ├── SmsNotificationService.cs
│   │   ├── IPGeolocationService.cs
│   │   └── SuspiciousActivityDetector.cs
│   └── Outbox/
│       ├── OutboxMessage.cs
│       └── OutboxProcessor.cs
│
tests/
├── Identity.Domain.Tests/             # Domain unit tests
│   ├── UserAccountTests.cs
│   ├── LoginAttemptTests.cs
│   └── SecurityEventTests.cs
│
├── Identity.Application.Tests/        # Application unit tests
│   ├── RegisterUserHandlerTests.cs
│   ├── AuthenticateUserHandlerTests.cs
│   └── GetLoginHistoryHandlerTests.cs
│
├── Identity.IntegrationTests/         # Integration tests
│   ├── AuthenticationFlowTests.cs
│   ├── NotificationDeliveryTests.cs
│   └── DatabasePersistenceTests.cs
│
└── Identity.ContractTests/            # Contract tests
    ├── ApiContractTests.cs            # OpenAPI spec validation
    └── EventContractTests.cs          # Event schema validation

# Existing Accounting Components (for reference)
src/
├── Accounting.API/                    # Existing ledger API
├── Accounting.Application/
├── Accounting.Domain/
└── Accounting.Infrastructure/
```

**Structure Decision**: 

This feature introduces a new **Identity bounded context** as a separate microservice within the existing accounting-api repository. The Identity context follows the same Clean Architecture layers as the existing Accounting context (Domain, Application, Infrastructure, API) but maintains complete isolation:

1. **Separate Domain**: Identity aggregates (UserAccount, LoginAttempt, SecurityEvent) are independent from Accounting domain
2. **Separate Database Schema**: Uses `identity` schema in PostgreSQL, isolated from `accounting` schema
3. **Independent Deployment**: Identity.API can be deployed separately from Accounting.API
4. **Event-Based Integration**: Cross-context communication via domain events (e.g., UserRegistered event can be consumed by Accounting context if needed)

The structure explicitly separates persistence entities from domain entities (as required by Constitution Principle VII), with dedicated Mappers to translate between layers. This maintains domain purity while accommodating EF Core's requirements.

## Complexity Tracking

**No violations to justify.** All constitutional principles are satisfied by the design:

- Clean Architecture with four distinct layers maintained
- Domain entities separated from persistence entities
- Result pattern for error handling
- Eventual consistency via domain events and Outbox pattern
- Production-ready code patterns (resilience, observability, performance optimization)
- Test-first development with comprehensive test coverage
- PostgreSQL standards with proper naming conventions and query optimization

The Identity bounded context follows the same architectural patterns as the existing Accounting context, ensuring consistency across the system.
