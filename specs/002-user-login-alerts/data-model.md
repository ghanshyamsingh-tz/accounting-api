# Data Model: User Login Alerts System

**Feature**: 002-user-login-alerts  
**Phase**: 1 - Design & Contracts  
**Date**: 2026-03-13  
**Status**: Complete

## Overview

This document defines the data model for the Identity bounded context following Domain-Driven Design principles. All entities follow Clean Architecture patterns with domain entities separate from persistence entities per Constitution Principle VII.

## Bounded Context

**Identity Context**: Responsible for user authentication, login tracking, security monitoring, and notification management.

**Ubiquitous Language**:
- **User Account**: A registered user with credentials
- **Login Attempt**: A single authentication request (successful or failed)
- **Security Event**: A significant security incident requiring attention
- **Notification Preference**: User-configurable settings for alerts
- **Authentication Token**: Short-lived JWT for API access

## Aggregates

### 1. UserAccount Aggregate

**Aggregate Root**: `UserAccount`

**Purpose**: Manages user identity, credentials, and account lifecycle

**Invariants**:
- Email must be unique across all users
- Password must meet complexity requirements (8+ chars, mixed case, numbers)
- Account status transitions must follow state machine rules
- Only unlocked accounts can authenticate
- Email must be verified before full account access

**Domain Entity Structure**:

```csharp
public class UserAccount : AggregateRoot<UserAccountId>
{
    // Value Objects
    public UserAccountId Id { get; private set; }
    public EmailAddress Email { get; private set; }
    public PasswordHash PasswordHash { get; private set; }
    public FullName Name { get; private set; }
    
    // Enums
    public AccountStatus Status { get; private set; }
    
    // Timestamps
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime? LockedAt { get; private set; }
    
    // Business Methods
    public Result<AuthenticationToken> Authenticate(string password)
    {
        // Validate account not locked
        // Verify password hash
        // Update LastLoginAt
        // Raise UserAuthenticated domain event
        // Return JWT token
    }
    
    public Result LockAccount(string reason)
    {
        // Change status to Locked
        // Set LockedAt timestamp
        // Raise AccountLocked domain event
    }
    
    public Result UnlockAccount()
    {
        // Verify authorized to unlock
        // Change status to Active
        // Clear LockedAt
        // Raise AccountUnlocked domain event
    }
    
    public Result UpdateProfile(FullName newName)
    {
        // Validate name
        // Update Name
        // Raise ProfileUpdated domain event
    }
    
    public static Result<UserAccount> Register(
        EmailAddress email, 
        string password, 
        FullName name)
    {
        // Validate email uniqueness (checked by repository)
        // Validate password complexity
        // Hash password
        // Create new UserAccount
        // Set status to PendingVerification
        // Raise UserRegistered domain event
    }
}
```

**Value Objects**:

```csharp
public record UserAccountId(Guid Value);

public record EmailAddress
{
    public string Value { get; init; }
    
    public static Result<EmailAddress> Create(string email)
    {
        // Validate email format
        // Normalize to lowercase
    }
}

public record PasswordHash
{
    public string Value { get; init; }
    
    public static PasswordHash FromPlainText(string password)
    {
        // Use BCrypt with cost factor 12
    }
    
    public bool Verify(string password)
    {
        // Verify password against hash
    }
}

public record FullName
{
    public string FirstName { get; init; }
    public string LastName { get; init; }
    
    public string DisplayName => $"{FirstName} {LastName}";
}
```

**Enums**:

```csharp
public enum AccountStatus
{
    PendingVerification = 0,  // Email not verified
    Active = 1,               // Normal state
    Locked = 2,               // Locked due to suspicious activity
    Suspended = 3,            // Administratively suspended
    Closed = 4                // Permanently closed
}
```

**Domain Events**:
- `UserRegistered` - New user account created
- `UserAuthenticated` - Successful login
- `LoginAttemptFailed` - Failed login attempt
- `AccountLocked` - Account locked due to security issue
- `AccountUnlocked` - Account unlocked by admin
- `ProfileUpdated` - User profile information changed

---

### 2. LoginAttempt Aggregate

**Aggregate Root**: `LoginAttempt`

**Purpose**: Tracks all authentication attempts for audit and security analysis

**Invariants**:
- Each attempt must reference a valid email address (may not be existing user)
- Timestamp must be set when attempt recorded
- IP address must be valid format
- Attempt status must be either Success or Failure

**Domain Entity Structure**:

```csharp
public class LoginAttempt : AggregateRoot<LoginAttemptId>
{
    // Identity
    public LoginAttemptId Id { get; private set; }
    public UserAccountId? UserId { get; private set; }  // Null if email doesn't exist
    public EmailAddress AttemptedEmail { get; private set; }
    
    // Context
    public IPAddress IpAddress { get; private set; }
    public GeographicLocation Location { get; private set; }
    public UserAgent UserAgent { get; private set; }
    
    // Status
    public AttemptStatus Status { get; private set; }
    public FailureReason? FailureReason { get; private set; }
    
    // Timing
    public DateTime AttemptedAt { get; private set; }
    
    // Business Methods
    public static LoginAttempt RecordSuccess(
        UserAccountId userId,
        EmailAddress email,
        IPAddress ipAddress,
        UserAgent userAgent)
    {
        // Create attempt with Success status
        // Trigger geolocation lookup (async)
        // Raise LoginAttemptRecorded domain event
    }
    
    public static LoginAttempt RecordFailure(
        EmailAddress email,
        IPAddress ipAddress,
        UserAgent userAgent,
        FailureReason reason)
    {
        // Create attempt with Failure status
        // Trigger geolocation lookup (async)
        // Raise LoginAttemptRecorded domain event
    }
    
    public void UpdateLocation(GeographicLocation location)
    {
        // Set location after async lookup completes
    }
}
```

**Value Objects**:

```csharp
public record LoginAttemptId(Guid Value);

public record IPAddress
{
    public string Value { get; init; }
    
    public static Result<IPAddress> Create(string ip)
    {
        // Validate IPv4 or IPv6 format
    }
}

public record GeographicLocation
{
    public string Country { get; init; }
    public string? Region { get; init; }
    public string? City { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    
    public static GeographicLocation Unknown => new() { Country = "Unknown" };
}

public record UserAgent
{
    public string Value { get; init; }
    public string Browser { get; init; }
    public string OS { get; init; }
    public string Device { get; init; }
    
    public static UserAgent Parse(string userAgentString)
    {
        // Parse user agent string to extract components
    }
}
```

**Enums**:

```csharp
public enum AttemptStatus
{
    Success = 0,
    Failure = 1
}

public enum FailureReason
{
    InvalidCredentials = 0,
    AccountLocked = 1,
    AccountNotFound = 2,
    EmailNotVerified = 3,
    TooManyAttempts = 4
}
```

**Domain Events**:
- `LoginAttemptRecorded` - Attempt logged (triggers notification)
- `SuspiciousPatternDetected` - Multiple failures from same IP

---

### 3. SecurityEvent Aggregate

**Aggregate Root**: `SecurityEvent`

**Purpose**: Represents high-severity security incidents requiring admin attention

**Invariants**:
- Each event must have a severity level
- Events must be timestamped
- Critical events must have resolution tracking

**Domain Entity Structure**:

```csharp
public class SecurityEvent : AggregateRoot<SecurityEventId>
{
    // Identity
    public SecurityEventId Id { get; private set; }
    public UserAccountId UserId { get; private set; }
    
    // Classification
    public ThreatType ThreatType { get; private set; }
    public EventSeverity Severity { get; private set; }
    
    // Details
    public string Description { get; private set; }
    public Dictionary<string, string> Metadata { get; private set; }
    
    // Resolution
    public bool Resolved { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? ResolutionNotes { get; private set; }
    
    // Timing
    public DateTime CreatedAt { get; private set; }
    
    // Business Methods
    public static SecurityEvent Create(
        UserAccountId userId,
        ThreatType threatType,
        EventSeverity severity,
        string description,
        Dictionary<string, string> metadata = null)
    {
        // Create security event
        // Raise SecurityEventCreated domain event
        // If Critical severity, raise AlertAdministrators event
    }
    
    public Result Resolve(string notes)
    {
        // Mark as resolved
        // Set resolution timestamp
        // Record resolution notes
        // Raise SecurityEventResolved domain event
    }
}
```

**Value Objects**:

```csharp
public record SecurityEventId(Guid Value);
```

**Enums**:

```csharp
public enum ThreatType
{
    BruteForce = 0,           // Multiple failed attempts
    GeographicAnomaly = 1,    // Login from unusual location
    VelocityAnomaly = 2,      // Too many logins too fast
    KnownBadActor = 3,        // IP on blocklist
    CredentialStuffing = 4,   // Pattern of automated attempts
    AccountTakeover = 5       // Successful login after suspicious activity
}

public enum EventSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
```

**Domain Events**:
- `SecurityEventCreated` - New security incident
- `AlertAdministrators` - Critical event requires immediate attention
- `SecurityEventResolved` - Incident resolved

---

## Entity Relationships

```
┌─────────────────┐
│  UserAccount    │
│  (Aggregate)    │
│  - Id           │
│  - Email    UK  │
│  - PasswordHash │
│  - Status       │
└────────┬────────┘
         │
         │ 1:N
         │
         ▼
┌─────────────────┐
│  LoginAttempt   │
│  (Aggregate)    │
│  - Id           │
│  - UserId   FK  │
│  - IpAddress    │
│  - Status       │
│  - AttemptedAt  │
└────────┬────────┘
         │
         │ Triggers
         │
         ▼
┌─────────────────┐
│ SecurityEvent   │
│ (Aggregate)     │
│ - Id            │
│ - UserId    FK  │
│ - ThreatType    │
│ - Severity      │
│ - Resolved      │
└─────────────────┘

┌──────────────────────┐
│ NotificationPreference│
│ (Value Object/Entity) │
│ - UserId         FK  │
│ - EmailEnabled       │
│ - SmsEnabled         │
│ - Frequency          │
└──────────────────────┘
```

**Notes**:
- FK = Foreign Key (references by ID only, not object reference per DDD)
- UK = Unique Key
- All relationships are "reference by ID" to maintain aggregate boundaries
- Cross-aggregate changes use domain events

---

## Persistence Entities (Infrastructure Layer)

Per Constitution Principle VII, persistence entities are SEPARATE from domain entities:

```csharp
// Infrastructure/Persistence/Entities/UserAccountEntity.cs
public class UserAccountEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LockedAt { get; set; }
    
    // EF Core navigation properties (NOT in domain)
    public ICollection<LoginAttemptEntity> LoginAttempts { get; set; }
}

// Infrastructure/Persistence/Entities/LoginAttemptEntity.cs
public class LoginAttemptEntity
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string AttemptedEmail { get; set; }
    public string IpAddress { get; set; }
    public string? LocationCountry { get; set; }
    public string? LocationCity { get; set; }
    public string UserAgent { get; set; }
    public string Status { get; set; }
    public string? FailureReason { get; set; }
    public DateTime AttemptedAt { get; set; }
    
    // EF Core navigation (NOT in domain)
    public UserAccountEntity User { get; set; }
}

// Similar pattern for SecurityEventEntity
```

**EF Core Configuration**:

```csharp
// Infrastructure/Persistence/Configurations/UserAccountEntityConfiguration.cs
public class UserAccountEntityConfiguration : IEntityTypeConfiguration<UserAccountEntity>
{
    public void Configure(EntityTypeBuilder<UserAccountEntity> builder)
    {
        builder.ToTable("users", "identity");
        
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(255);
        builder.HasIndex(u => u.Email).IsUnique();
        
        builder.Property(u => u.Status).IsRequired().HasMaxLength(50);
        builder.Property(u => u.CreatedAt).IsRequired();
        
        // Exclude navigation properties from queries by default
        builder.Navigation(u => u.LoginAttempts).AutoInclude(false);
    }
}
```

---

## Query Models (CQRS Read Side)

For optimized queries, use flattened DTOs:

```csharp
// Application/Queries/GetLoginHistory/LoginHistoryDto.cs
public record LoginHistoryDto
{
    public Guid Id { get; init; }
    public DateTime AttemptedAt { get; init; }
    public string IpAddress { get; init; }
    public string Location { get; init; }
    public string Device { get; init; }
    public string Status { get; init; }
    public string? FailureReason { get; init; }
}

// Query uses projections for performance
var history = await context.LoginAttempts
    .AsNoTracking()
    .Where(la => la.UserId == userId)
    .OrderByDescending(la => la.AttemptedAt)
    .Select(la => new LoginHistoryDto
    {
        Id = la.Id,
        AttemptedAt = la.AttemptedAt,
        IpAddress = la.IpAddress,
        Location = la.LocationCity ?? la.LocationCountry ?? "Unknown",
        Device = la.UserAgent,
        Status = la.Status,
        FailureReason = la.FailureReason
    })
    .Take(pageSize)
    .Skip(pageNumber * pageSize)
    .ToListAsync(cancellationToken);
```

---

## Database Indexes

Performance-critical indexes per Constitution Principle VII:

```sql
-- User lookups
CREATE UNIQUE INDEX ix_users_email ON identity.users(email);
CREATE INDEX ix_users_status ON identity.users(status) WHERE status IN ('Active', 'PendingVerification');

-- Login attempt queries
CREATE INDEX ix_login_attempts_user_id_attempted_at 
  ON identity.login_attempts(user_id, attempted_at DESC);
CREATE INDEX ix_login_attempts_ip_address_attempted_at 
  ON identity.login_attempts(ip_address, attempted_at DESC) 
  WHERE status = 'Failure';
CREATE INDEX ix_login_attempts_status_attempted_at 
  ON identity.login_attempts(status, attempted_at DESC);

-- Security events
CREATE INDEX ix_security_events_user_id_created_at 
  ON identity.security_events(user_id, created_at DESC);
CREATE INDEX ix_security_events_resolved 
  ON identity.security_events(resolved, severity) 
  WHERE resolved = false;

-- Outbox queries
CREATE INDEX ix_outbox_messages_published_created_at 
  ON identity.outbox_messages(published, created_at) 
  WHERE published = false;
```

---

## State Transitions

### UserAccount Status State Machine

```
[New] → PendingVerification
            ↓ (email confirmed)
         Active
            ↓ (suspicious activity)
         Locked
            ↓ (admin unlocks)
         Active
            ↓ (admin suspends)
        Suspended
            ↓ (user requests)
         Closed (terminal)
```

**Valid Transitions**:
- PendingVerification → Active (email confirmed)
- Active → Locked (suspicious activity detected)
- Active → Suspended (admin action)
- Locked → Active (admin unlocks)
- Suspended → Active (admin reinstates)
- Any → Closed (permanent, irreversible)

---

## Summary

This data model follows DDD principles with three main aggregates:
1. **UserAccount** - Identity and authentication
2. **LoginAttempt** - Audit trail and security analysis
3. **SecurityEvent** - Incident tracking

All domain entities are separate from persistence entities with explicit mappers. The model supports:
- ✅ Aggregate invariant enforcement
- ✅ Domain event-driven notifications
- ✅ Eventual consistency patterns
- ✅ High-performance queries with projections
- ✅ Complete audit trail
- ✅ Security monitoring and alerting

Ready for contract definition (OpenAPI, event schemas) in next phase.