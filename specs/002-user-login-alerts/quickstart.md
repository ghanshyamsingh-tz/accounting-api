# Identity Service Quickstart Guide

Get the Identity Service up and running in **5 minutes**.

---

## Prerequisites

### Required Software

| Tool | Version | Purpose |
|------|---------|---------|
| **.NET SDK** | 10.0+ | Runtime and build tools |
| **PostgreSQL** | 17.0+ | Identity database |
| **Keycloak** | 26.0+ | OAuth2/OIDC provider |
| **Docker** | 27.0+ | Container runtime |
| **Docker Compose** | 2.30+ | Multi-container orchestration |
| **Redis** | 7.4+ | Caching layer |
| **Apache Kafka** | 3.8+ | Event streaming |

### Verify Installation

```powershell
# Check .NET version
dotnet --version  # Should be 10.0.100 or higher

# Check Docker version
docker --version  # Should be 27.0.0 or higher
docker compose version  # Should be 2.30.0 or higher

# Check PostgreSQL (if running locally)
psql --version  # Should be 17.0 or higher
```

---

## Quick Start (Docker Compose)

**Fastest way to run the complete system:**

```powershell
# 1. Clone repository (if not already)
git clone https://github.com/nemtaccounting/accounting-api.git
cd accounting-api

# 2. Checkout feature branch
git checkout 002-user-login-alerts

# 3. Start all services
docker compose -f docker/docker-compose.yml up -d

# 4. Verify services are running
docker compose -f docker/docker-compose.yml ps

# 5. Run database migrations
docker compose exec identity-api dotnet ef database update

# 6. Health check
curl http://localhost:5001/health
# Expected: {"status":"Healthy","totalDuration":"00:00:00.123"}
```

**Services will be available at:**

- Identity API: `http://localhost:5001`
- Keycloak Admin: `http://localhost:8080` (admin/admin)
- PostgreSQL: `localhost:5432` (postgres/postgres)
- Redis: `localhost:6379`
- Kafka: `localhost:9092`

---

## Local Development Setup

### Step 1: Database Setup

#### Create Identity Schema

```powershell
# Connect to PostgreSQL
psql -U postgres -h localhost

# Create database and user
CREATE DATABASE nemtaccounting;
CREATE USER identity_service WITH PASSWORD 'SecurePassword123!';
GRANT ALL PRIVILEGES ON DATABASE nemtaccounting TO identity_service;

# Connect to nemtaccounting database
\c nemtaccounting

# Create identity schema
CREATE SCHEMA IF NOT EXISTS identity;
GRANT ALL ON SCHEMA identity TO identity_service;

# Exit psql
\q
```

#### Apply Migrations

```powershell
cd src/Accounting.Identity.API

# Add EF Core tools (if not installed)
dotnet tool install --global dotnet-ef

# Run migrations
dotnet ef database update --project ../Accounting.Identity.Infrastructure
```

#### Verify Tables

```sql
-- Connect to database
psql -U identity_service -d nemtaccounting -h localhost

-- List tables in identity schema
\dt identity.*

-- Expected tables:
-- identity.user_accounts
-- identity.login_attempts
-- identity.security_events
-- identity.outbox_messages
```

---

### Step 2: Keycloak Configuration

#### Start Keycloak Container

```powershell
docker run -d \
  --name keycloak \
  -p 8080:8080 \
  -e KEYCLOAK_ADMIN=admin \
  -e KEYCLOAK_ADMIN_PASSWORD=admin \
  quay.io/keycloak/keycloak:26.0.0 start-dev
```

#### Create Realm and Client

1. **Access Admin Console**: http://localhost:8080
   - Username: `admin`
   - Password: `admin`

2. **Create Realm**:
   - Click dropdown at top-left → "Create Realm"
   - Name: `nemtaccounting`
   - Click "Create"

3. **Create Client**:
   - Clients → "Create client"
   - **General Settings**:
     - Client type: `OpenID Connect`
     - Client ID: `identity-service`
   - **Capability config**:
     - Client authentication: `ON`
     - Authorization: `OFF`
     - Authentication flow: Enable `Standard flow`, `Direct access grants`
   - **Valid redirect URIs**: `http://localhost:5001/*`
   - Click "Save"

4. **Get Client Secret**:
   - Go to "Credentials" tab
   - Copy "Client secret" value (needed for `.env` file)

---

### Step 3: Environment Configuration

#### Create `.env` File

```powershell
# In repository root
New-Item -Path .env -ItemType File

# Add configuration (replace placeholders)
@"
# Database
ConnectionStrings__IdentityDb=Host=localhost;Port=5432;Database=nemtaccounting;Username=identity_service;Password=SecurePassword123!;SearchPath=identity

# Keycloak
Authentication__Authority=http://localhost:8080/realms/nemtaccounting
Authentication__Audience=identity-service
Authentication__ClientId=identity-service
Authentication__ClientSecret=<YOUR_CLIENT_SECRET_FROM_STEP_2>

# Redis
Redis__ConnectionString=localhost:6379,abortConnect=false
Redis__InstanceName=identity:

# Kafka
Kafka__BootstrapServers=localhost:9092
Kafka__GroupId=identity-service-group
Kafka__EnableAutoCommit=true

# Email (for notifications)
Email__SmtpHost=smtp.ethereal.email
Email__SmtpPort=587
Email__Username=<your-ethereal-username>
Email__Password=<your-ethereal-password>
Email__FromAddress=noreply@nemtaccounting.example.com

# Observability
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft.EntityFrameworkCore=Warning
Seq__ServerUrl=http://localhost:5341

# Application
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:5001
"@ | Out-File -FilePath .env -Encoding UTF8
```

**Get Ethereal Email Credentials** (for testing):
1. Go to https://ethereal.email
2. Click "Create Ethereal Account"
3. Copy username and password to `.env`

---

### Step 4: Run Identity Service

#### Restore Dependencies

```powershell
cd src/Accounting.Identity.API
dotnet restore
```

#### Run Service

```powershell
# Development mode (hot reload enabled)
dotnet watch run

# Or standard run
dotnet run

# Service will start at http://localhost:5001
```

#### Verify Service

```powershell
# Health check
curl http://localhost:5001/health

# OpenAPI documentation
# Open browser: http://localhost:5001/swagger
```

---

## Running Tests

### Unit Tests

```powershell
# Run all unit tests
dotnet test tests/Accounting.Identity.Domain.Tests
dotnet test tests/Accounting.Identity.Application.Tests

# With code coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportsDirectory=./coverage

# View coverage report
# Open: tests/Accounting.Identity.Domain.Tests/coverage/index.html
```

### Integration Tests

```powershell
# Requires Docker for Testcontainers
cd tests/Accounting.Identity.IntegrationTests

# Run integration tests
dotnet test

# Tests will automatically:
# - Start PostgreSQL container
# - Start Redis container
# - Run migrations
# - Execute tests
# - Clean up containers
```

### Contract Tests

```powershell
# Verify API contracts match OpenAPI spec
cd tests/Accounting.Identity.ContractTests
dotnet test

# Verify event contracts match schemas
dotnet test --filter Category=EventContracts
```

### End-to-End Tests

```powershell
# Requires all services running (use Docker Compose)
docker compose -f docker/docker-compose.yml up -d

# Run E2E tests
cd tests/Accounting.Identity.E2ETests
dotnet test
```

---

## Docker Compose Configuration

### Full Stack Setup

**File**: `docker/docker-compose.yml`

```yaml
services:
  postgres:
    image: postgres:17-alpine
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: nemtaccounting
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  keycloak:
    image: quay.io/keycloak/keycloak:26.0.0
    command: start-dev
    environment:
      KEYCLOAK_ADMIN: admin
      KEYCLOAK_ADMIN_PASSWORD: admin
    ports:
      - "8080:8080"
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:8080/health/ready || exit 1"]
      interval: 30s
      timeout: 10s
      retries: 5

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  kafka:
    image: confluentinc/cp-kafka:7.7.0
    ports:
      - "9092:9092"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://kafka:29092,PLAINTEXT_HOST://localhost:9092
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT
      KAFKA_INTER_BROKER_LISTENER_NAME: PLAINTEXT
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
    depends_on:
      - zookeeper

  zookeeper:
    image: confluentinc/cp-zookeeper:7.7.0
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
      ZOOKEEPER_TICK_TIME: 2000

  identity-api:
    build:
      context: ../src/Accounting.Identity.API
      dockerfile: Dockerfile
    ports:
      - "5001:8080"
    environment:
      ConnectionStrings__IdentityDb: "Host=postgres;Port=5432;Database=nemtaccounting;Username=postgres;Password=postgres;SearchPath=identity"
      Authentication__Authority: "http://keycloak:8080/realms/nemtaccounting"
      Redis__ConnectionString: "redis:6379"
      Kafka__BootstrapServers: "kafka:29092"
    depends_on:
      postgres:
        condition: service_healthy
      keycloak:
        condition: service_healthy
      redis:
        condition: service_healthy
      kafka:
        condition: service_started

volumes:
  postgres_data:
```

### Start Services

```powershell
# Start all services
docker compose -f docker/docker-compose.yml up -d

# View logs
docker compose -f docker/docker-compose.yml logs -f identity-api

# Stop services
docker compose -f docker/docker-compose.yml down

# Stop and remove volumes (clean slate)
docker compose -f docker/docker-compose.yml down -v
```

---

## Common Tasks

### Register a User

```powershell
curl -X POST http://localhost:5001/api/v1/auth/register `
  -H "Content-Type: application/json" `
  -d '{
    "email": "test@example.com",
    "password": "SecurePassword123!",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

### Login

```powershell
curl -X POST http://localhost:5001/api/v1/auth/login `
  -H "Content-Type: application/json" `
  -d '{
    "email": "test@example.com",
    "password": "SecurePassword123123!"
  }'

# Save the accessToken from response
```

### Get User Profile

```powershell
$token = "<ACCESS_TOKEN_FROM_LOGIN>"

curl -X GET http://localhost:5001/api/v1/accounts/me `
  -H "Authorization: Bearer $token"
```

### View Login History

```powershell
curl -X GET "http://localhost:5001/api/v1/accounts/me/login-history?pageNumber=1&pageSize=10" `
  -H "Authorization: Bearer $token"
```

---

## Troubleshooting

### Service Won't Start

**Problem**: `Failed to connect to PostgreSQL`

**Solution**:
```powershell
# Verify PostgreSQL is running
docker ps | Select-String postgres

# Check connection string in .env file
cat .env | Select-String ConnectionStrings
```

---

### Keycloak Connection Failed

**Problem**: `Unable to obtain configuration from http://keycloak:8080/realms/nemtaccounting/.well-known/openid-configuration`

**Solution**:
```powershell
# Verify Keycloak is running
curl http://localhost:8080/realms/nemtaccounting/.well-known/openid-configuration

# Check realm name matches configuration
```

---

### Tests Failing

**Problem**: Integration tests fail with Docker errors

**Solution**:
```powershell
# Ensure Docker is running
docker ps

# Pull required images
docker pull postgres:17-alpine
docker pull redis:7-alpine
docker pull testcontainers/ryuk:0.10.0
```

---

### Migrations Not Applied

**Problem**: Tables don't exist in database

**Solution**:
```powershell
cd src/Accounting.Identity.API

# Check migration status
dotnet ef migrations list --project ../Accounting.Identity.Infrastructure

# Apply migrations
dotnet ef database update --project ../Accounting.Identity.Infrastructure

# If migrations don't exist, create initial migration
dotnet ef migrations add InitialCreate --project ../Accounting.Identity.Infrastructure --output-dir Migrations
```

---

## Next Steps

1. **Explore API**: Open Swagger UI at http://localhost:5001/swagger
2. **Review Architecture**: Read `/docs/architecture/` ADRs
3. **Implement Features**: See `tasks.md` for implementation tasks
4. **Write Tests**: Follow TDD approach (test → code → refactor)
5. **Monitor Events**: Use Kafka UI to view published events: http://localhost:9021

---

## Useful Commands Reference

```powershell
# Build solution
dotnet build

# Clean build artifacts
dotnet clean

# Format code
dotnet format

# Run all tests
dotnet test

# Generate code coverage report
dotnet test /p:CollectCoverage=true /p:CoverageReportsDirectory=./coverage

# Create new migration
dotnet ef migrations add <MigrationName> --project src/Accounting.Identity.Infrastructure

# View EF Core migration SQL
dotnet ef migrations script --project src/Accounting.Identity.Infrastructure

# Docker Compose shortcuts
docker compose up -d      # Start detached
docker compose down       # Stop and remove containers
docker compose ps         # List running services
docker compose logs -f    # Follow logs for all services
docker compose restart    # Restart all services
```

---

## Support

- **Documentation**: `/docs/`
- **Architecture decisions**: `/docs/architecture/ADR-*.md`
- **Feature specs**: `/specs/002-user-login-alerts/`
- **Constitution**: `/.specify/memory/constitution.md`