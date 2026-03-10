# SecureApiProject

A secure **ASP.NET Core 8 Web API** built with Microsoft Copilot, demonstrating security best practices across three activities:

| Activity | Topic |
|---|---|
| Activity 1 | Writing Secure Code |
| Activity 2 | Authentication & Authorization |
| Activity 3 | Debugging & Resolving Security Issues |

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Any IDE: Visual Studio 2022, VS Code, or Rider

### Run Locally

```bash
# Clone the repo
git clone https://github.com/YOUR_USERNAME/SecureApiProject.git
cd SecureApiProject

# Restore packages
dotnet restore

# Run (uses in-memory database for dev)
dotnet run
```

Open Swagger UI: `https://localhost:5001/swagger`

---

## 🔐 Activity 1: Writing Secure Code

Key security practices implemented:

- **Password Hashing** — BCrypt with work factor 12 (never plain text)
- **No hardcoded secrets** — JWT key loaded from `appsettings` / environment variables
- **Input Validation** — Data annotations on all DTOs (`[Required]`, `[StringLength]`, `[RegularExpression]`)
- **Parameterized Queries** — Entity Framework Core (no raw SQL → no SQL injection)
- **Soft Delete** — Records marked `IsDeleted` instead of permanently removed (audit trail preserved)

```csharp
// ✅ Secure: BCrypt hash
PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12)

// ❌ Never do this
// password = request.Password  // plain text
```

---

## 🛡️ Activity 2: Authentication & Authorization

### JWT Authentication
- Tokens signed with HMAC-SHA256
- Short expiry (60 minutes) with `ClockSkew = Zero`
- Claims include: `userId`, `email`, `username`, `role`, `jti` (unique token ID)

### Role-Based Authorization

| Endpoint | Role Required |
|---|---|
| `GET /api/products` | User or Admin |
| `POST /api/products` | Admin only |
| `PUT /api/products/{id}` | Admin only |
| `DELETE /api/products/{id}` | Admin only |
| `GET /api/auth/profile` | Any authenticated user |

### Account Lockout
- 5 failed login attempts → account locked for 15 minutes
- Prevents brute-force attacks

### Test Accounts (seeded)

| Email | Password | Role |
|---|---|---|
| admin@secureapi.com | Admin@123456 | Admin |
| john@secureapi.com | User@123456 | User |

---

## 🔍 Activity 3: Debugging & Resolving Security Issues

### Security Middleware Pipeline

**1. SecurityHeadersMiddleware**
Adds HTTP response headers on every request:
```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Content-Security-Policy: default-src 'self'
Referrer-Policy: strict-origin-when-cross-origin
```

**2. RequestLoggingMiddleware**
- Logs all requests with IP, method, path, and response time
- Detects suspicious patterns: SQL injection (`' OR`, `DROP TABLE`), XSS (`<script>`), path traversal (`../`)

**3. RateLimitingMiddleware**
- Limits `/api/auth/*` to **10 requests/minute per IP**
- Returns `429 Too Many Requests` with `Retry-After: 60` header

### Security Issues Fixed

| Issue | Fix Applied |
|---|---|
| Timing attack on login | Always run BCrypt.Verify even if user not found |
| Username enumeration | Generic error: "Invalid email or password" |
| Brute-force login | Account lockout + rate limiting |
| Missing security headers | `SecurityHeadersMiddleware` |
| Server version disclosure | `Server` header removed |

---

## 📁 Project Structure

```
SecureApiProject/
├── Controllers/
│   ├── AuthController.cs        # Register, Login, Profile
│   └── ProductsController.cs    # CRUD with role-based auth
├── Data/
│   ├── AppDbContext.cs           # EF Core DbContext
│   └── DbSeeder.cs              # Seed users & products
├── DTOs/
│   └── DTOs.cs                  # Request/Response models with validation
├── Middleware/
│   ├── SecurityHeadersMiddleware.cs
│   ├── RequestLoggingMiddleware.cs
│   └── RateLimitingMiddleware.cs
├── Models/
│   └── Models.cs                # User, Product, AuditLog entities
├── Services/
│   ├── AuthService.cs           # Registration & login logic
│   ├── TokenService.cs          # JWT generation & validation
│   └── ProductService.cs        # Product CRUD
├── Program.cs                   # App configuration & DI
└── appsettings.json
```

---

## 🧪 API Endpoints

### Auth
```
POST /api/auth/register    Register new user
POST /api/auth/login       Login → returns JWT token
GET  /api/auth/profile     Get current user (requires token)
```

### Products
```
GET    /api/products         List all products
GET    /api/products/{id}    Get product by ID
POST   /api/products         Create product (Admin)
PUT    /api/products/{id}    Update product (Admin)
DELETE /api/products/{id}    Delete product (Admin)
```

---

## ⚙️ Configuration

For production, set these as environment variables (never commit real secrets):

```bash
Jwt__Key=your_production_secret_key_minimum_32_chars
Jwt__Issuer=SecureApiProject
Jwt__Audience=SecureApiProjectUsers
```

---

## 🛠️ Built With

- ASP.NET Core 8
- Entity Framework Core 8 (InMemory for dev, SQL Server for prod)
- JWT Bearer Authentication (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- BCrypt.Net-Next (password hashing)
- Swashbuckle / Swagger UI
