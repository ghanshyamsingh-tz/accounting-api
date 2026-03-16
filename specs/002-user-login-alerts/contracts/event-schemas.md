# Domain Event Schemas

**Purpose**: Define schemas for domain events published by Identity service to Kafka event bus.

**Event Format**: All events follow CloudEvents 1.0 specification with JSON payload.

**Kafka Topics**:
- `identity.user-events` - User lifecycle events (registered, profile updated)
- `identity.auth-events` - Authentication events (login attempts, account locks)
- `identity.security-events` - Security incidents

**Consumer Idempotency**: All consumers MUST implement idempotency using `eventId` as deduplication key.

---

## Event: UserRegistered

**Topic**: `identity.user-events`  
**Trigger**: New user account created via registration  
**Consumers**: Email service (verification email), Analytics service

### Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "UserRegistered",
  "type": "object",
  "required": ["specversion", "type", "source", "id", "time", "datacontenttype", "data"],
  "properties": {
    "specversion": {
      "type": "string",
      "const": "1.0",
      "description": "CloudEvents version"
    },
    "type": {
      "type": "string",
      "const": "com.nemtaccounting.identity.user.registered.v1"
    },
    "source": {
      "type": "string",
      "format": "uri",
      "example": "https://api.nemtaccounting.example.com/identity"
    },
    "id": {
      "type": "string",
      "format": "uuid",
      "description": "Unique event identifier (for idempotency)"
    },
    "time": {
      "type": "string",
      "format": "date-time",
      "description": "Event timestamp (ISO 8601)"
    },
    "datacontenttype": {
      "type": "string",
      "const": "application/json"
    },
    "data": {
      "type": "object",
      "required": ["userId", "email", "firstName", "lastName", "registeredAt"],
      "properties": {
        "userId": {
          "type": "string",
          "format": "uuid"
        },
        "email": {
          "type": "string",
          "format": "email"
        },
        "firstName": {
          "type": "string"
        },
        "lastName": {
          "type": "string"
        },
        "registeredAt": {
          "type": "string",
          "format": "date-time"
        },
        "tenantId": {
          "type": "string",
          "format": "uuid",
          "description": "Multi-tenant identifier"
        }
      }
    }
  }
}
```

### Example

```json
{
  "specversion": "1.0",
  "type": "com.nemtaccounting.identity.user.registered.v1",
  "source": "https://api.nemtaccounting.example.com/identity",
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "time": "2026-03-13T10:30:00Z",
  "datacontenttype": "application/json",
  "data": {
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "email": "john.doe@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "registeredAt": "2026-03-13T10:30:00Z",
    "tenantId": "tenant-abc-123"
  }
}
```

---

## Event: UserAuthenticated

**Topic**: `identity.auth-events`  
**Trigger**: Successful user login  
**Consumers**: Notification service (send login alert), Analytics service, Audit log

### Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "UserAuthenticated",
  "type": "object",
  "required": ["specversion", "type", "source", "id", "time", "datacontenttype", "data"],
  "properties": {
    "specversion": { "type": "string", "const": "1.0" },
    "type": {
      "type": "string",
      "const": "com.nemtaccounting.identity.user.authenticated.v1"
    },
    "source": { "type": "string", "format": "uri" },
    "id": { "type": "string", "format": "uuid" },
    "time": { "type": "string", "format": "date-time" },
    "datacontenttype": { "type": "string", "const": "application/json" },
    "data": {
      "type": "object",
      "required": ["userId", "email", "attemptId", "ipAddress", "userAgent", "authenticatedAt"],
      "properties": {
        "userId": { "type": "string", "format": "uuid" },
        "email": { "type": "string", "format": "email" },
        "attemptId": { "type": "string", "format": "uuid" },
        "ipAddress": { "type": "string" },
        "location": {
          "type": "object",
          "properties": {
            "country": { "type": "string" },
            "city": { "type": "string" }
          }
        },
        "userAgent": { "type": "string" },
        "authenticatedAt": { "type": "string", "format": "date-time" }
      }
    }
  }
}
```

### Example

```json
{
  "specversion": "1.0",
  "type": "com.nemtaccounting.identity.user.authenticated.v1",
  "source": "https://api.nemtaccounting.example.com/identity",
  "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "time": "2026-03-13T10:35:00Z",
  "datacontenttype": "application/json",
  "data": {
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "email": "john.doe@example.com",
    "attemptId": "att-9876-5432-1098",
    "ipAddress": "203.0.113.45",
    "location": {
      "country": "USA",
      "city": "San Francisco"
    },
    "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0",
    "authenticatedAt": "2026-03-13T10:35:00Z"
  }
}
```

---

## Event: LoginAttemptFailed

**Topic**: `identity.auth-events`  
**Trigger**: Failed login attempt  
**Consumers**: Notification service (send alert), Security monitoring, Threat detection

### Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "LoginAttemptFailed",
  "type": "object",
  "required": ["specversion", "type", "source", "id", "time", "datacontenttype", "data"],
  "properties": {
    "specversion": { "type": "string", "const": "1.0" },
    "type": {
      "type": "string",
      "const": "com.nemtaccounting.identity.login.failed.v1"
    },
    "source": { "type": "string", "format": "uri" },
    "id": { "type": "string", "format": "uuid" },
    "time": { "type": "string", "format": "date-time" },
    "datacontenttype": { "type": "string", "const": "application/json" },
    "data": {
      "type": "object",
      "required": ["userId", "email", "attemptId", "ipAddress", "failureReason", "failedAt"],
      "properties": {
        "userId": { 
          "type": "string", 
          "format": "uuid",
          "description": "Null if email doesn't exist"
        },
        "email": { "type": "string", "format": "email" },
        "attemptId": { "type": "string", "format": "uuid" },
        "ipAddress": { "type": "string" },
        "location": {
          "type": "object",
          "properties": {
            "country": { "type": "string" },
            "city": { "type": "string" }
          }
        },
        "userAgent": { "type": "string" },
        "failureReason": {
          "type": "string",
          "enum": ["InvalidCredentials", "AccountLocked", "AccountNotFound", "EmailNotVerified", "TooManyAttempts"]
        },
        "failedAt": { "type": "string", "format": "date-time" }
      }
    }
  }
}
```

### Example

```json
{
  "specversion": "1.0",
  "type": "com.nemtaccounting.identity.login.failed.v1",
  "source": "https://api.nemtaccounting.example.com/identity",
  "id": "c3d4e5f6-a7b8-9012-cdef-123456789012",
  "time": "2026-03-13T10:40:00Z",
  "datacontenttype": "application/json",
  "data": {
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "email": "john.doe@example.com",
    "attemptId": "att-8765-4321-0987",
    "ipAddress": "198.51.100.23",
    "location": {
      "country": "USA",
      "city": "New York"
    },
    "userAgent": "Mozilla/5.0 (iPhone; CPU iPhone OS 16_0)",
    "failureReason": "InvalidCredentials",
    "failedAt": "2026-03-13T10:40:00Z"
  }
}
```

---

## Event: AccountLocked

**Topic**: `identity.security-events`  
**Trigger**: Account locked due to suspicious activity  
**Consumers**: Notification service (urgent alert), Security team dashboard

### Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "AccountLocked",
  "type": "object",
  "required": ["specversion", "type", "source", "id", "time", "datacontenttype", "data"],
  "properties": {
    "specversion": { "type": "string", "const": "1.0" },
    "type": {
      "type": "string",
      "const": "com.nemtaccounting.identity.account.locked.v1"
    },
    "source": { "type": "string", "format": "uri" },
    "id": { "type": "string", "format": "uuid" },
    "time": { "type": "string", "format": "date-time" },
    "datacontenttype": { "type": "string", "const": "application/json" },
    "data": {
      "type": "object",
      "required": ["userId", "email", "reason", "lockedAt"],
      "properties": {
        "userId": { "type": "string", "format": "uuid" },
        "email": { "type": "string", "format": "email" },
        "reason": { "type": "string" },
        "triggeringAttemptId": { "type": "string", "format": "uuid" },
        "lockedAt": { "type": "string", "format": "date-time" }
      }
    }
  }
}
```

### Example

```json
{
  "specversion": "1.0",
  "type": "com.nemtaccounting.identity.account.locked.v1",
  "source": "https://api.nemtaccounting.example.com/identity",
  "id": "d4e5f6a7-b8c9-0123-def1-234567890123",
  "time": "2026-03-13T10:45:00Z",
  "datacontenttype": "application/json",
  "data": {
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "email": "john.doe@example.com",
    "reason": "5 consecutive failed login attempts detected",
    "triggeringAttemptId": "att-7654-3210-9876",
    "lockedAt": "2026-03-13T10:45:00Z"
  }
}
```

---

## Event: SuspiciousActivityDetected

**Topic**: `identity.security-events`  
**Trigger**: Threat detection system identifies suspicious pattern  
**Consumers**: Security incident dashboard, Notification service, SIEM integration

### Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "SuspiciousActivityDetected",
  "type": "object",
  "required": ["specversion", "type", "source", "id", "time", "datacontenttype", "data"],
  "properties": {
    "specversion": { "type": "string", "const": "1.0" },
    "type": {
      "type": "string",
      "const": "com.nemtaccounting.identity.security.suspicious-activity.v1"
    },
    "source": { "type": "string", "format": "uri" },
    "id": { "type": "string", "format": "uuid" },
    "time": { "type": "string", "format": "date-time" },
    "datacontenttype": { "type": "string", "const": "application/json" },
    "data": {
      "type": "object",
      "required": ["securityEventId", "userId", "threatType", "severity", "description", "detectedAt"],
      "properties": {
        "securityEventId": { "type": "string", "format": "uuid" },
        "userId": { "type": "string", "format": "uuid" },
        "email": { "type": "string", "format": "email" },
        "threatType": {
          "type": "string",
          "enum": ["BruteForce", "GeographicAnomaly", "VelocityAnomaly", "KnownBadActor", "CredentialStuffing", "AccountTakeover"]
        },
        "severity": {
          "type": "string",
          "enum": ["Low", "Medium", "High", "Critical"]
        },
        "description": { "type": "string" },
        "metadata": {
          "type": "object",
          "description": "Additional context about the threat"
        },
        "detectedAt": { "type": "string", "format": "date-time" }
      }
    }
  }
}
```

### Example

```json
{
  "specversion": "1.0",
  "type": "com.nemtaccounting.identity.security.suspicious-activity.v1",
  "source": "https://api.nemtaccounting.example.com/identity",
  "id": "e5f6a7b8-c9d0-1234-ef12-345678901234",
  "time": "2026-03-13T10:50:00Z",
  "datacontenttype": "application/json",
  "data": {
    "securityEventId": "sec-event-1234-5678",
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "email": "john.doe@example.com",
    "threatType": "GeographicAnomaly",
    "severity": "High",
    "description": "Login attempt from Russia while user typically logs in from USA",
    "metadata": {
      "currentLocation": "Moscow, Russia",
      "typicalLocations": ["San Francisco, USA", "New York, USA"],
      "ipAddress": "185.220.101.45"
    },
    "detectedAt": "2026-03-13T10:50:00Z"
  }
}
```

---

## Contract Testing

All event consumers MUST implement contract tests to validate:

1. **Schema Compliance**: Events match JSON schema definitions
2. **Required Fields**: All required fields are present
3. **Field Types**: Data types match schema
4. **Idempotency**: Duplicate events with same `id` are handled correctly
5. **Version Compatibility**: Changes to event schemas are backward-compatible

### Example Contract Test (xUnit + FluentAssertions)

```csharp
[Fact]
public async Task UserRegisteredEvent_Should_MatchSchema()
{
    // Arrange
    var eventJson = await File.ReadAllTextAsync("test-data/user-registered.json");
    var schemaJson = await File.ReadAllTextAsync("contracts/user-registered-schema.json");
    var schema = JSchema.Parse(schemaJson);
    var eventObj = JObject.Parse(eventJson);
    
    // Act
    var isValid = eventObj.IsValid(schema, out IList<string> errors);
    
    // Assert
    isValid.Should().BeTrue(because: $"Event should match schema. Errors: {string.Join(", ", errors)}");
}
```

---

## Versioning Strategy

Event schemas follow semantic versioning in the `type` field:

- **Breaking changes** (v1 → v2): Change major version
  - Removing required fields
  - Changing field types
  - Renaming fields
  
- **Non-breaking changes** (v1.0 → v1.1): Change minor version
  - Adding optional fields
  - Expanding enum values

**Example**: `com.nemtaccounting.identity.user.registered.v1` → `com.nemtaccounting.identity.user.registered.v2`

Producers and consumers MUST support at least 2 major versions simultaneously during migration periods.