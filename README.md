# Product Management System API

A RESTful backend API built with **.NET 8 / ASP.NET Core** for managing products and their line-items, with JWT-based authentication, role-based authorization, and refresh-token support.

---

## Table of Contents

- [Overview](#overview)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Database Schema](#database-schema)
- [Authentication Flow](#authentication-flow)
- [API Reference](#api-reference)
- [Environment Setup](#environment-setup)
- [Running the Application](#running-the-application)
- [Running Tests](#running-tests)
- [Postman Collection](#postman-collection)
- [Security Measures](#security-measures)
- [Performance Considerations](#performance-considerations)
- [Deployment](#deployment)
- [Known Limitations & Future Improvements](#known-limitations--future-improvements)

---

## Overview

The Product Management System exposes a versioned REST API (`/api/v1/`) that allows:

- **User management** — register, login, logout, refresh token, CRUD (admin only)
- **Product management** — create, read, update, delete products with nested line-items
- **Role-based access** — `Admin` users can manage other users; all authenticated users can manage products

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | .NET 8, ASP.NET Core Web API |
| Database | PostgreSQL via Npgsql |
| ORM | Entity Framework Core 8 |
| Authentication | JWT Bearer + Refresh Token |
| Validation | FluentValidation 11 |
| Mapping | AutoMapper 13 |
| Logging | Serilog (console + rolling file) |
| DI Scanning | Scrutor |
| Testing | xUnit + Moq |
| Documentation | Swagger / OpenAPI (Swashbuckle) |
| Compression | Brotli + Gzip (built-in middleware) |

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        HTTP Request                         │
└───────────────────────────┬─────────────────────────────────┘
                            │
              ┌─────────────▼──────────────┐
              │   Middleware Pipeline       │
              │  • HTTPS Redirect           │
              │  • Response Compression     │
              │  • Exception Handler        │
              │  • CORS                     │
              │  • JWT Authentication       │
              │  • Authorization            │
              │  • JwtMiddleware            │
              │    (UserId → HttpContext)   │
              └─────────────┬──────────────┘
                            │
              ┌─────────────▼──────────────┐
              │        Controllers          │
              │  AuthController             │
              │  ProductController          │
              └─────────────┬──────────────┘
                            │
              ┌─────────────▼──────────────┐
              │       Service Layer         │
              │  AuthService                │
              │  ProductService             │
              │  TokenService               │
              │  JwtService                 │
              └─────────────┬──────────────┘
                            │
              ┌─────────────▼──────────────┐
              │     Repository Layer        │
              │  AuthRepository             │
              │  ProductRepository          │
              │  TokenRepository            │
              └─────────────┬──────────────┘
                            │
              ┌─────────────▼──────────────┐
              │    AppDbContext (EF Core)   │
              │         PostgreSQL          │
              └─────────────────────────────┘
```

### Design Patterns Used

| Pattern | Where |
|---|---|
| Repository Pattern | `Repositories/Data/`, `Repositories/Interface/` |
| Service Layer | `Services/Data/`, `Services/Interface/` |
| DTO Pattern | `DTOs/` — all API input/output uses DTOs, never raw models |
| Middleware Pipeline | `Middleware/` — exception handling, JWT extraction |
| Extension Methods | `Common/ControllerExtensions.cs` — consistent `ApiResponse<T>` |

---

## Project Structure

```
ProductManagementSystem/
│
├── Common/
│   ├── ControllerExtensions.cs     # ApiResponse<T> helper, ValidationError helper
│   ├── Enums.cs                    # StringValue attribute
│   ├── ExtensionMethods.cs         # GetStringValue, GetValByName, IsSimpleType
│   └── QueryableExtensions.cs      # OrderByProperty (dynamic sorting)
│
├── Context/
│   └── AppDbContext.cs             # EF Core DbContext, OnModelCreating (lowercase columns)
│
├── Controllers/
│   ├── AuthController.cs           # Register, Login, Logout, Refresh, User CRUD
│   └── ProductController.cs        # Product CRUD
│
├── DTOs/
│   ├── ApiResponseDto.cs           # Standard response envelope { status, message, data }
│   ├── ItemDto.cs
│   ├── LoginDto.cs
│   ├── LogoutDto.cs
│   ├── OrderParamDto.cs            # Pagination + sorting + search
│   ├── ProductDto.cs
│   ├── RefreshTokenDto.cs
│   ├── SysUserDto.cs
│   └── TokenResponseDto.cs
│
├── Mapping/
│   └── MappingProfile.cs           # AutoMapper: Entity ↔ DTO, Password ignored on Entity→DTO
│
├── Middleware/
│   ├── ApiExceptionMiddleware.cs   # Global unhandled exception handler
│   └── JwtMiddleware.cs            # Extracts UserId from token → HttpContext.Items
│
├── Models/
│   ├── Item.cs
│   ├── Product.cs
│   ├── SysUser.cs
│   ├── UserEntity.cs               # Abstract base: CreatedBy, UpdatedBy audit fields
│   └── UserToken.cs                # JWT + refresh token revocation store
│
├── Repositories/
│   ├── Data/
│   │   ├── AuthRepository.cs
│   │   ├── ProductRepository.cs
│   │   └── TokenRepository.cs
│   └── Interface/
│       ├── IAuthRepository.cs
│       ├── IProductRepository.cs
│       └── ITokenRepository.cs
│
├── Services/
│   ├── Data/
│   │   ├── AuthService.cs          # Credential validation, user CRUD
│   │   ├── JwtService.cs           # Token parsing, GetCurrentUser
│   │   ├── ProductService.cs       # Product CRUD with logging
│   │   └── TokenService.cs         # Token generation with role claims, revocation
│   └── Interface/
│       ├── IAuthService.cs
│       ├── IJwtService.cs
│       ├── IProductService.cs
│       └── ITokenService.cs
│
├── Validators/
│   ├── ItemDtoValidator.cs
│   ├── LoginDtoValidator.cs
│   ├── ProductDtoValidator.cs
│   └── SysUserDtoValidator.cs
│
├── Program.cs                      # DI, middleware pipeline, Serilog, versioning
├── appsettings.json                # Config keys (no secrets — use user-secrets)
│
└── ProductManagementSystem.Tests/
    ├── Services/
    │   ├── AuthServiceTests.cs     # 9 tests
    │   └── ProductServiceTests.cs  # 10 tests
    ├── Controllers/
    │   └── ProductControllerTests.cs # 10 tests
    └── ProductManagementSystem.Tests.csproj
```

---

## Database Schema

Four tables in PostgreSQL. All names lowercase (set by `OnModelCreating`).

```
┌─────────────────────────────────────────────┐
│                  sysuser                    │
├──────────────┬──────────────┬───────────────┤
│ userid  PK   │ INT SERIAL   │               │
│ loginid      │ VARCHAR(255) │ UNIQUE (lower)│
│ name         │ VARCHAR(255) │               │
│ password     │ VARCHAR(512) │ PBKDF2 hash   │
│ salt         │ VARCHAR(255) │ base64        │
│ isactive     │ BOOLEAN      │ default TRUE  │
│ usertype     │ VARCHAR(50)  │ Admin / User  │
│ fcm          │ VARCHAR(512) │ nullable      │
│ createdat    │ TIMESTAMP    │ nullable      │
│ updatedat    │ TIMESTAMP    │ nullable      │
│ createdby    │ INT FK→self  │ nullable      │
│ updatedby    │ INT FK→self  │ nullable      │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│                  product                    │
├──────────────┬──────────────┬───────────────┤
│ productid PK │ INT SERIAL   │               │
│ productname  │ VARCHAR(255) │               │
│ createdat    │ TIMESTAMP    │ nullable      │
│ updatedat    │ TIMESTAMP    │ nullable      │
│ createdby    │ INT FK→user  │ nullable      │
│ updatedby    │ INT FK→user  │ nullable      │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│                    item                     │
├──────────────┬──────────────┬───────────────┤
│ itemid    PK │ INT SERIAL   │               │
│ quantity     │ INT          │ CHECK >= 0    │
│ price        │ NUMERIC(18,2)│ CHECK >= 0    │
│ productid    │ INT FK→prod  │ CASCADE delete│
│ createdat    │ TIMESTAMP    │ nullable      │
│ updatedat    │ TIMESTAMP    │ nullable      │
│ createdby    │ INT FK→user  │ nullable      │
│ updatedby    │ INT FK→user  │ nullable      │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│                 usertoken                   │
├───────────────────────┬─────────────────────┤
│ id               PK   │ INT SERIAL          │
│ userid           FK   │ INT → sysuser       │
│ tokenid               │ VARCHAR UNIQUE      │ ← jti claim
│ refreshtokenid        │ VARCHAR             │
│ refreshtoken          │ VARCHAR             │
│ createdat             │ TIMESTAMP           │
│ expiresat             │ TIMESTAMP           │
│ refreshtokenexpiresat │ TIMESTAMP           │
│ revokedat             │ TIMESTAMP nullable  │ ← NULL = active
│ deviceinfo            │ VARCHAR nullable    │
│ ipaddress             │ VARCHAR nullable    │
└─────────────────────────────────────────────┘
```

Apply schema:
```bash
psql -U postgres -d ProductDB -f database/schema.sql
```

---

## Authentication Flow

```
┌──────────┐       POST /api/v1/auth/login         ┌─────────────┐
│  Client  │ ─────────────────────────────────────► │ AuthControl │
└──────────┘  { userName, password }                └──────┬──────┘
                                                           │ 1. ValidateDto (FluentValidation)
                                                           │ 2. AuthService.GenerateJwtToken()
                                                           │    → verifies password hash
                                                           │    → returns SysUser on success
                                                           │ 3. TokenService.GenerateTokenForUserAsync()
                                                           │    → fetches UserType from DB
                                                           │    → builds JWT with ClaimTypes.Role
                                                           │    → sets Issuer + Audience
                                                           │    → stores token in usertoken table
                                                           ▼
                                                  { token, refreshToken, expiresIn }

┌──────────┐   GET /api/v1/product  Bearer {token}  ┌─────────────┐
│  Client  │ ─────────────────────────────────────► │ JWT Middlew │
└──────────┘                                        └──────┬──────┘
                                                           │ Extracts userId → HttpContext.Items
                                                           ▼
                                                    ┌─────────────┐
                                                    │[Authorize]  │
                                                    │Reads        │
                                                    │ClaimTypes   │
                                                    │.Role claim  │
                                                    └─────────────┘

┌──────────┐  POST /api/v1/auth/refresh-token       ┌─────────────┐
│  Client  │ ─────────────────────────────────────► │ TokenService│
└──────────┘  { refreshToken }                      └──────┬──────┘
                                                           │ 1. Validate refresh token in DB
                                                           │ 2. Revoke old token
                                                           │ 3. Generate new token pair
                                                           ▼
                                                  { token, refreshToken, expiresIn }

Token expiry:   access token  = 10 minutes
                refresh token = 15 minutes
```

**Important:** Each login revokes all previous tokens for that user (single-device policy). Logging out revokes the current access token and optionally the refresh token.

---

## API Reference

Base URL: `http://localhost:5000/api/v1`

All responses use the envelope:
```json
{
  "status": true,
  "message": "...",
  "data": { ... }
}
```

### Auth Endpoints

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/auth/register` | None | Register a new user |
| POST | `/auth/login` | None | Login, get token + refreshToken |
| POST | `/auth/logout` | Bearer | Revoke current token |
| POST | `/auth/refresh-token` | None | Exchange refresh token for new token pair |
| POST | `/auth/getAll` | Admin | Paginated list of all users |
| PUT | `/auth/{id}` | Admin | Update a user |
| DELETE | `/auth/{id}` | Admin | Delete a user |

#### POST /auth/register
```json
// Request
{
  "loginId": "user@example.com",
  "name": "John Doe",
  "password": "Secret@123",
  "isActive": true,
  "userType": "Admin"   // "Admin" | "User"
}

// Response 200
{
  "status": true,
  "message": "Registration successful.",
  "data": { "loginid": "user@example.com", "name": "John Doe" }
}
```

#### POST /auth/login
```json
// Request
{ "userName": "user@example.com", "password": "Secret@123" }

// Response 200
{
  "status": true,
  "message": "Login successful.",
  "data": {
    "token": "<JWT>",
    "refreshToken": "<opaque string>",
    "expiresIn": 599
  }
}
```

#### POST /auth/getAll  *(Admin)*
```json
// Request
{
  "pageNumber": 1,
  "pageSize": 10,
  "orderColumn": "name",
  "order": "asc",
  "searchText": ""
}

// Response 200
{
  "status": true,
  "message": "Users fetched successfully.",
  "data": {
    "page": 1,
    "size": 10,
    "total_Records": 5,
    "data": [ { "userId": 1, "loginId": "...", "name": "...", ... } ]
  }
}
```

---

### Product Endpoints

All product endpoints require `Authorization: Bearer <token>`.

| Method | Route | Description |
|---|---|---|
| POST | `/product/getAll` | Paginated product list with items |
| GET | `/product/{id}` | Single product with items |
| POST | `/product` | Create product (with optional items) |
| PUT | `/product/{id}` | Update product |
| DELETE | `/product/{id}` | Delete product and its items |

#### POST /product/getAll
```json
// Request
{
  "pageNumber": 1,
  "pageSize": 10,
  "orderColumn": "productName",
  "order": "asc",
  "searchText": "widget"
}

// Response 200
{
  "page": 1,
  "size": 10,
  "total_Records": 3,
  "data": [
    {
      "productId": 1,
      "productName": "Widget A",
      "items": [
        { "itemId": 1, "quantity": 5, "price": 9.99, "productId": 1 }
      ]
    }
  ]
}
```

#### POST /product  (Create)
```json
// Request
{
  "productName": "New Widget",
  "items": [
    { "quantity": 10, "price": 4.99, "productId": 0 },
    { "quantity": 3,  "price": 14.99,"productId": 0 }
  ]
}

// Response 201
{
  "productId": 7,
  "productName": "New Widget",
  "items": [
    { "itemId": 12, "quantity": 10, "price": 4.99, "productId": 7 },
    { "itemId": 13, "quantity": 3,  "price": 14.99,"productId": 7 }
  ]
}
```

---

### Common HTTP Status Codes

| Code | Meaning |
|---|---|
| 200 | Success |
| 201 | Resource created |
| 204 | Deleted (no content) |
| 400 | Validation error or bad request |
| 401 | Missing or invalid token |
| 403 | Valid token but insufficient role |
| 404 | Resource not found |
| 500 | Unhandled server error |

---

## Environment Setup

### Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 8.0+ |
| PostgreSQL | 14+ |
| (Optional) Docker | any recent |

### 1. Clone the repository

```bash
git clone https://github.com/your-org/ProductManagementSystem.git
cd ProductManagementSystem
```

### 2. Create the database

```bash
psql -U postgres -c "CREATE DATABASE ProductDB;"
psql -U postgres -d ProductDB -f database/schema.sql
```

### 3. Store secrets (do NOT edit appsettings.json)

```bash
cd ProductManagementSystem

dotnet user-secrets init

# PostgreSQL connection string
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=ProductDB;Username=postgres;Password=yourpassword"

# JWT signing key — must be at least 32 characters
dotnet user-secrets set "Jwt:key" "YourSuperSecretKeyAtLeast32CharsLong!!"
```

### 4. Install NuGet packages

```bash
# API versioning
dotnet add package Microsoft.AspNetCore.Mvc.Versioning --version 5.1.0
dotnet add package Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer --version 5.1.0

# Structured logging
dotnet add package Serilog.AspNetCore --version 8.0.2
dotnet add package Serilog.Sinks.Console --version 5.0.1
dotnet add package Serilog.Sinks.File --version 5.0.0

# Scrutor (DI scanning)
dotnet add package Scrutor --version 4.2.2
```

### 5. Restore all packages

```bash
dotnet restore
```

---

## Running the Application

```bash
cd ProductManagementSystem
dotnet run
```

The API starts at:
- HTTP  → `http://localhost:5000`
- HTTPS → `https://localhost:5001`
- Swagger UI → `http://localhost:5000/swagger`

Logs are written to:
- Console (colored, structured)
- `logs/app-<date>.log` (rolling daily files)

---

## Running Tests

```bash
# From solution root
dotnet test

# With detailed output
dotnet test --logger "console;verbosity=detailed"

# Single test class
dotnet test --filter "FullyQualifiedName~ProductServiceTests"
dotnet test --filter "FullyQualifiedName~AuthServiceTests"
dotnet test --filter "FullyQualifiedName~ProductControllerTests"
```

### Test Coverage Summary

| File | Tests | Covers |
|---|---|---|
| `ProductServiceTests.cs` | 10 | GetById, GetAll, Create, Update, Delete |
| `AuthServiceTests.cs` | 9 | Register, Login (valid + invalid), Update, Delete, GetAll |
| `ProductControllerTests.cs` | 10 | HTTP status codes, validation gating, 404 handling |

**Total: 29 tests**

All tests use real `AutoMapper` (via `MappingProfile`) and `Moq` for repository/service mocking.

---

## Postman Collection

Import both files into Postman:

```
postman/ProductManagementSystem.postman_collection.json
postman/PMS_Local.postman_environment.json
```

**Quick start:**
1. Select the `PMS Local` environment
2. Run **Register Admin** once
3. Run **Login as Admin** — the test script auto-saves `{{token}}` and `{{refreshToken}}`
4. All other requests use `Bearer {{token}}` automatically

**To test role-based access:**
1. Run **Login as Regular User** to overwrite `{{token}}` with a User-role token
2. Run **Get All Users [Admin] → 200** — you should now receive **403**
3. Run **Login as Admin** to restore the Admin token
4. Rerun **Get All Users** — you should receive **200**

---

## Security Measures

| Measure | Implementation |
|---|---|
| Password hashing | PBKDF2-HMACSHA256, 10 000 iterations, 16-byte random salt |
| JWT signing | HMAC-SHA256, configurable signing key via user-secrets |
| Token expiry | Access token: 10 min · Refresh token: 15 min |
| Token revocation | Every token stored in `usertoken` table; logout sets `revokedat` |
| Single-device login | Login revokes all previous tokens for the user |
| Role-based auth | `ClaimTypes.Role` embedded in JWT; `[Authorize(Roles = "Admin")]` |
| Input validation | FluentValidation on all request DTOs |
| CORS | Configurable policy (currently AllowAll for development) |
| HTTPS | `UseHttpsRedirection()` first in pipeline |
| Secrets management | JWT key and DB password via `dotnet user-secrets` (never in source) |
| Response envelope | Consistent `{ status, message, data }` — no raw model leakage |
| Password not in response | `SysUserDto.Password` ignored in `SysUser → SysUserDto` AutoMapper map |

---

## Performance Considerations

| Consideration | Implementation |
|---|---|
| Read-only queries | `AsNoTracking()` on all `GET` operations in both repositories |
| Pagination | All list endpoints require `pageNumber` + `pageSize` |
| Async/await | Used throughout — controllers, services, repositories |
| Response compression | Brotli (preferred) + Gzip via `UseResponseCompression()` |
| Eager loading | `Include(p => p.Items)` only where needed |
| Indexing | See `database/schema.sql` — indexes on `loginid`, `tokenid`, `productname`, `userid+revokedat` |
| EF connection pooling | Built-in via `AddDbContext` scoped lifetime |

---

## Deployment

### Environment Variables (Production)

Set these instead of `appsettings.json` or user-secrets:

```bash
# Linux / Docker
export ConnectionStrings__DefaultConnection="Host=prod-db;Port=5432;Database=ProductDB;Username=app;Password=STRONG_PASS"
export Jwt__key="YourProductionSecretKeyAtLeast32Chars"
export Jwt__Issuer="YourProductionIssuer"
export Jwt__Audience="YourProductionAudience"
```

> ASP.NET Core maps double-underscore `__` to colon `:` in configuration keys.

### Publish

```bash
dotnet publish -c Release -o ./publish
```

### Docker (example)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish ProductManagementSystem/ProductManagementSystem.csproj \
    -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProductManagementSystem.dll"]
```

```bash
docker build -t pms-api .
docker run -p 8080:80 \
  -e ConnectionStrings__DefaultConnection="..." \
  -e Jwt__key="..." \
  pms-api
```

### CORS for Production

Update `Program.cs` before deploying — replace `AllowAnyOrigin()` with your actual client domain:

```csharp
options.AddPolicy("AllowAll", policy =>
    policy.WithOrigins("https://your-frontend.com")
          .AllowAnyMethod()
          .AllowAnyHeader());
```

---

## Known Limitations & Future Improvements

| Area | Current State | Suggested Improvement |
|---|---|---|
| Token expiry | Access: 10 min, Refresh: 15 min | Make configurable via `appsettings.json` |
| Single device | Login revokes all other sessions | Add device management — allow N sessions |
| Refresh token storage | Stored as plain text | Hash the refresh token value before storing |
| Roles | Hardcoded `"Admin"` / `"User"` | Move to a `roles` table for flexible RBAC |
| Item ProductId on create | Client sends `productId: 0` | Set server-side after product is saved |
| Integration tests | None | Add `WebApplicationFactory<Program>` tests |
| API rate limiting | Not implemented | Add `AspNetCoreRateLimit` package |
| Health check endpoint | Not implemented | Add `app.MapHealthChecks("/health")` |
| Soft delete | Records hard-deleted | Add `IsDeleted` flag + global query filter |
| FCM push notifications | Column exists, unused | Wire up Firebase SDK |

---

## License

This project was created as part of a technical assessment. Not licensed for production use without review.
