# Research: User Login Alerts System

**Feature**: 002-user-login-alerts  
**Phase**: 0 - Technical Research  
**Date**: 2026-03-13  
**Status**: Complete

## Purpose

This document captures technical research decisions for implementing the User Login Alerts System within the NEMT Accounting System. All research aligns with constitutional principles, particularly Production-Ready Code (Principle I), DDD & Clean Architecture (Principle II), and PostgreSQL & EF Core Standards (Principle VII).

## Technical Decisions

### Decision 1: Identity as Separate Bounded Context

**Question**: Should user management be part of the Accounting bounded context or a separate Identity context?

**Decision**: Implement as separate Identity bounded context (microservice)

**Rationale**:
- **Ubiquitous Language**: Identity domain has distinct language (authentication, authorization, login attempts) vs. Accounting domain (ledgers, journals, transactions)
- **Independent Scaling**: Authentication services have different scaling patterns than accounting operations
- **Security Isolation**: Sensitive user credentials and authentication logic should be isolated
- **Team Autonomy**: Identity can evolve independently without coupling to accounting changes
- **Database Isolation**: Constitution Principle II requires database-per-service; identity data isolated in `identity` schema

**Alternatives Considered**:
- **Shared Accounting Context**: Rejected due to tight coupling and mixed concerns
- **Separate Repository**: Rejected; single monorepo with multiple bounded contexts maintains deployment simplicity while preserving logical boundaries

**References**: 
- Constitution Principle II: "Bounded Context = Microservice"
- DDD patterns: Bounded Context isolation

---

### Decision 2: Authentication Strategy

**Question**: What authentication mechanism should be used? JWT, session-based, or hybrid?

**Decision**: JWT tokens with Keycloak integration for centralized identity management

**Rationale**:
- **Constitution Requirement**: Mandatory technology stack specifies Keycloak for authentication (OAuth2/OIDC)
- **Stateless**: JWT enables stateless authentication, critical for microservice scalability
- **Centralized Identity**: Keycloak provides enterprise-grade identity management with MFA, SSO capabilities
- **Cross-Service**: JWT tokens can be validated by multiple services without shared state
- **Standards-Based**: OAuth2/OIDC are industry standards with robust security properties

**Implementation Approach**:
- Identity service issues JWT tokens after successful authentication
- Tokens contain claims: user ID, email, roles, tenant ID
- Token expiration: 15 minutes (access token), 7 days (refresh token)
- Keycloak handles token validation, refresh, and revocation

**Alternatives Considered**:
- **Session-Based**: Rejected; requires sticky sessions, doesn't scale horizontally
- **Custom Token System**: Rejected; reinventing the wheel, missing enterprise features

**References**:
- Constitution: Technology Stack Constraints (Keycloak mandatory)
- OAuth 2.0 RFC 6749, OIDC Core 1.0

---

### Decision 3: Notification Delivery Architecture

**Question**: How should real-time notifications (<60 second SLA) be delivered reliably?

**Decision**: Event-driven architecture with Outbox pattern and dedicated notification worker service

**Rationale**:
- **Reliability**: Outbox pattern ensures notifications aren't lost during database transactions
- **Performance**: Async processing prevents authentication requests from blocking on email/SMS delivery
- **Resilience**: Dedicated worker with Polly retry policies handles transient failures
- **Scale**: Kafka event streaming handles high-volume notification events
- **Constitution Compliance**: Principle V mandates Outbox pattern for reliable messaging

**Architecture**:
```
[Authentication] → [Domain Event] → [Outbox Table] → [Outbox Processor] → [Kafka] → [Notification Worker] → [Email/SMS Provider]
```

**Flow**:
1. UserAuthenticated domain event raised during login
2. Event persisted to outbox table in same transaction as login attempt
3. Background OutboxProcessor polls outbox, publishes to Kafka
4. Notification worker consumes events, sends notifications with retry logic
5. Idempotency key prevents duplicate notifications

**Alternatives Considered**:
- **Synchronous Email**: Rejected; violates 60s SLA if email provider is slow
- **Direct Kafka Publishing**: Rejected; dual-write problem (DB + Kafka can diverge)
- **Azure/AWS Service Bus**: Rejected; constitution mandates Kafka

**References**:
- Constitution Principle IV: Resilience & Observability (Outbox pattern)
- Constitution Principle V: Eventual Consistency (reliable messaging)
- Microservices Patterns by Chris Richardson: Outbox Pattern

---

### Decision 4: Suspicious Activity Detection Algorithm

**Question**: What algorithm should detect suspicious login patterns effectively?

**Decision**: Rules-based detection with configurable thresholds + IP reputation scoring

**Rationale**:
- **Simplicity**: Rules-based system is explainable, auditable, and predictable
- **Compliance**: Security rules must be transparent for audit/compliance
- **Performance**: Simple rules process in <10ms vs. ML models requiring seconds
- **Maintenance**: Non-ML engineers can tune thresholds without retraining models
- **Start Simple**: Can evolve to ML-based detection in future if needed

**Detection Rules**:
1. **Brute Force**: ≥5 failed attempts from same IP within 10 minutes → Account Locked
2. **Geographic Anomaly**: Login from country different from last 30 days → High-priority alert
3. **Velocity**: Successful logins from 2+ countries within 1 hour → Account locked
4. **Known Bad IPs**: Login from IP on threat intelligence blocklist → Blocked immediately
5. **Off-Hours**: ≥3 failed attempts during 2am-5am local time → Alert administrator

**IP Reputation**:
- Integrate with MaxMind GeoIP2 for location data
- Integrate with IPQualityScore or similar for reputation scoring
- Cache reputation data in Redis (TTL: 1 hour) to avoid API limits

**Alternatives Considered**:
- **Machine Learning**: Rejected for MVP; adds complexity, requires training data, slower prediction
- **Third-Party Fraud Detection**: Rejected; cost prohibitive, vendor lock-in, data privacy concerns
- **No Detection**: Rejected; violates security requirements (FR-006)

**References**:
- OWASP Authentication Cheat Sheet: Rate limiting and anomaly detection
- Constitution Principle I: Production-Ready Code (no placeholders)

---

### Decision 5: Entity Separation Strategy

**Question**: How should domain entities be separated from persistence entities as required by Constitution Principle VII?

**Decision**: Explicit separation with dedicated Mapper classes for bidirectional translation

**Rationale**:
- **Constitution Mandate**: Principle VII explicitly requires domain entities separate from persistence entities
- **Domain Purity**: Domain entities enforce business rules without EF Core pollution (no navigation properties, no public setters for EF)
- **Testing**: Domain tests don't require EF Core, run faster, more focused
- **Flexibility**: Can change ORM or switch to NoSQL without touching domain layer

**Pattern**:
```csharp
// Domain Layer
public class UserAccount {
    private readonly EmailAddress _email;
    private readonly PasswordHash _passwordHash;
    private AccountStatus _status;
    
    public Result<AuthenticationToken> Authenticate(string password) { }
    public Result LockAccount(string reason) { }
}

// Infrastructure Layer - Persistence Entity
public class UserAccountEntity {
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Infrastructure Layer - Mapper
public class UserAccountMapper {
    public static UserAccount ToDomain(UserAccountEntity entity) { }
    public static UserAccountEntity ToPersistence(UserAccount domain) { }
}
```

**Mapping Strategy**:
- Repositories return domain entities after mapping from persistence entities
- Repositories accept domain entities and map to persistence entities before saving
- Mappers handle conversion of value objects to primitive types
- Mapping occurs ONLY in Infrastructure layer

**Alternatives Considered**:
- **Single Entity**: Rejected; violates Constitution Principle VII
- **AutoMapper**: Rejected; implicit mapping hides transformation logic, slower
- **Direct EF Core in Domain**: Rejected; introduces infrastructure dependency

**References**:
- Constitution Principle VII: PostgreSQL & EF Core Standards (entity separation mandatory)
- Constitution Principle II: Domain Layer must have ZERO infrastructure dependencies

---

### Decision 6: Database Schema Design

**Question**: How should PostgreSQL schema be structured for the Identity context?

**Decision**: Dedicated `identity` schema with lowercase_snake_case naming following PostgreSQL conventions

**Schema Structure**:
```sql
-- Schema isolation
CREATE SCHEMA identity;

-- Core tables
identity.users (id, email, password_hash, status, created_at, updated_at)
identity.login_attempts (id, user_id, ip_address, user_agent, location_country, 
                         location_city, status, failure_reason, attempted_at)
identity.security_events (id, user_id, event_type, severity, description, 
                          threat_type, resolved, created_at)
identity.notification_preferences (id, user_id, email_enabled, sms_enabled, 
                                    in_app_enabled, frequency, updated_at)
identity.outbox_messages (id, event_type, payload, published, created_at)

-- Indexes for query performance
CREATE INDEX ix_users_email ON identity.users(email);
CREATE INDEX ix_login_attempts_user_id_attempted_at ON identity.login_attempts(user_id, attempted_at DESC);
CREATE INDEX ix_login_attempts_ip_address_attempted_at ON identity.login_attempts(ip_address, attempted_at DESC);
CREATE INDEX ix_security_events_user_id_created_at ON identity.security_events(user_id, created_at DESC);
CREATE INDEX ix_outbox_messages_published_created_at ON identity.outbox_messages(published, created_at) WHERE published = false;
```

**Rationale**:
- **Constitution Compliance**: Principle VII mandates lowercase_snake_case, plural table names, `id` for PK
- **Query Performance**: Indexes on all WHERE, ORDER BY, and JOIN columns
- **Schema Isolation**: Separate schema prevents accidental cross-context queries
- **Audit Trail**: All tables have created_at/updated_at timestamps
- **Event Sourcing**: login_attempts and security_events provide complete audit log

**Alternatives Considered**:
- **Shared Schema**: Rejected; violates isolation principle
- **PascalCase Names**: Rejected; requires quoted identifiers, not PostgreSQL convention
- **NoSQL**: Rejected; ACID guarantees critical for authentication

**References**:
- Constitution Principle VII: PostgreSQL naming conventions
- PostgreSQL documentation: Schema best practices

---

### Decision 7: Performance Optimization Strategy

**Question**: How to meet <50ms p50, <200ms p95 latency requirements for authentication API?

**Decision**: Multi-layered caching + query optimization + async processing

**Optimization Techniques**:

1. **Redis Caching**:
   - Cache user lookup by email (TTL: 5 minutes)
   - Cache IP geolocation data (TTL: 1 hour)
   - Cache IP reputation scores (TTL: 1 hour)
   - Reduces database queries by ~80% for returning users

2. **Database Query Optimization**:
   - Use `AsNoTracking()` for all read-only queries
   - Project to DTOs with `Select()` instead of loading full entities
   - Composite indexes on multi-column WHERE clauses
   - Connection pooling with max 100 connections

3. **Async Processing**:
   - Authentication check returns immediately after validation
   - Notification sending happens asynchronously via domain events
   - Geolocation lookup happens in background (not blocking authentication)
   - Suspicious activity detection runs in separate worker process

4. **Database Connection Efficiency**:
   - Use `ConfigureAwait(false)` to avoid context switching
   - Batch queries where possible (e.g., GetLoginHistory with pagination)
   - Keep transactions short (≤100ms)

5. **Monitoring**:
   - OpenTelemetry spans measure each operation (DB query, cache lookup, external API call)
   - Alert if p95 latency exceeds 150ms (buffer before 200ms limit)
   - Track cache hit rate (target: >80%)

**Alternatives Considered**:
- **Synchronous Processing**: Rejected; blocks authentication on external services
- **No Caching**: Rejected; can't meet latency SLAs without cache
- **CDN for Static Content**: N/A for authentication API (no static content)

**References**:
- Constitution Principle I: Performance-optimized queries mandatory
- Constitution Technical Stack: Redis for caching
- ASP.NET Core Performance Best Practices

---

## Technology Stack Summary

Based on constitution mandates and research decisions:

| Component | Technology | Version | Purpose |
|-----------|-----------|---------|---------|
| Runtime | .NET | 10 | Application platform |
| Database | PostgreSQL | 17+ | Primary data store (identity schema) |
| ORM | EF Core | 10 | Database access with migrations |
| Cache | Redis | Latest | User/IP data caching |
| Messaging | Apache Kafka | Latest | Event streaming for notifications |
| Auth | Keycloak | Latest | Identity provider (OAuth2/OIDC) |
| Validation | FluentValidation | Latest | Input validation |
| Resilience | Polly | Latest | Retry, circuit breaker policies |
| Logging | Serilog | Latest | Structured logging |
| Tracing | OpenTelemetry | Latest | Distributed tracing |
| Testing | xUnit | Latest | Unit/integration tests |
| API Docs | Swashbuckle | Latest | OpenAPI generation |
| Geolocation | MaxMind GeoIP2 | Latest | IP to location mapping |

## Best Practices Integration

### Node.js Best Practices Adapted for .NET

From the Node.js Best Practices guide fetched earlier, the following principles are integrated into this design:

1. **Component-Based Structure** → DDD Bounded Contexts (Identity as separate microservice)
2. **Layered Architecture** → Clean Architecture (Domain, Application, Infrastructure, API layers)
3. **Async/Await for Error Handling** → C# async/await with Result pattern (no exceptions for business logic)
4. **Centralized Error Handler** → Global exception middleware in API layer
5. **Distinguish Operational vs Programmer Errors** → Result pattern for expected errors, exceptions for infrastructure failures
6. **Use Environment-Aware Config** → appsettings.{Environment}.json with Keycloak for secrets
7. **AAA Testing Pattern** → Arrange-Act-Assert in all xUnit tests
8. **Secure Headers** → ASP.NET Core middleware for CORS, HSTS, CSP
9. **Input Validation** → FluentValidation at API boundary
10. **Rate Limiting** → ASP.NET Core rate limiting middleware (5 requests/10s for auth endpoints)
11. **Logging to stdout** → Serilog configured for Docker container logging
12. **Dependency Locking** → NuGet package lock files (packages.lock.json)

## Next Phase

All technical unknowns resolved. Ready to proceed to **Phase 1**:
- Generate data-model.md (entity relationships, aggregates, value objects)
- Generate contracts/ (OpenAPI spec, event schemas)
- Generate quickstart.md (setup instructions)
- Update agent context with Identity technology stack

No NEEDS CLARIFICATION items remaining.