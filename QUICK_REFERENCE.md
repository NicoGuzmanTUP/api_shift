# ?? QUICK REFERENCE - FASE 3

## ?? API Endpoints

```
AUTH
POST   /api/auth/login

SHIFTS
GET    /api/shifts/my          [Authorize]
GET    /api/shifts/team        [Authorize]

SHIFT SWAPS
POST   /api/shift-swaps                      [Authorize]
POST   /api/shift-swaps/{id}/accept          [Authorize]
POST   /api/shift-swaps/{id}/reject          [Authorize]
POST   /api/shift-swaps/{id}/cancel          [Authorize]
POST   /api/shift-swaps/{id}/approve         [Authorize(Roles = "HR")]
GET    /api/shift-swaps/{id}                 [Authorize]
GET    /api/shift-swaps/pending              [Authorize(Roles = "HR")]
```

## ?? Headers Requeridos

```
# Todos excepto POST /api/auth/login
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

## ?? DTOs

### LoginRequest
```csharp
public string Email { get; set; }
public string Password { get; set; }
```

### LoginResponse
```csharp
public string Token { get; set; }
public int UserId { get; set; }
public string Email { get; set; }
public string Name { get; set; }
public string Role { get; set; }
```

### ShiftDto
```csharp
public int Id { get; set; }
public int UserId { get; set; }
public DateOnly ShiftDate { get; set; }
public TimeOnly StartTime { get; set; }
public TimeOnly EndTime { get; set; }
public string Status { get; set; }
```

### CreateShiftSwapRequest
```csharp
public int TargetUserId { get; set; }
public int RequesterShiftId { get; set; }
public int TargetShiftId { get; set; }
public string Reason { get; set; }
```

### ShiftSwapRequestDto
```csharp
public int Id { get; set; }
public int RequesterId { get; set; }
public int TargetUserId { get; set; }
public int RequesterShiftId { get; set; }
public int TargetShiftId { get; set; }
public string Reason { get; set; }
public string Status { get; set; }
public DateTime CreatedAt { get; set; }
```

## ??? Database Schema

```sql
-- Users
CREATE TABLE users (
  id SERIAL PRIMARY KEY,
  name VARCHAR NOT NULL,
  email VARCHAR UNIQUE NOT NULL,
  password_hash VARCHAR NOT NULL,
  role VARCHAR NOT NULL
);

-- Shifts
CREATE TABLE shifts (
  id SERIAL PRIMARY KEY,
  user_id INTEGER REFERENCES users(id),
  shift_date DATE NOT NULL,
  start_time TIME NOT NULL,
  end_time TIME NOT NULL,
  status VARCHAR DEFAULT 'ACTIVE'
);

-- Shift Swap Requests
CREATE TABLE shift_swap_requests (
  id SERIAL PRIMARY KEY,
  requester_id INTEGER REFERENCES users(id),
  target_user_id INTEGER REFERENCES users(id),
  requester_shift_id INTEGER REFERENCES shifts(id),
  target_shift_id INTEGER REFERENCES shifts(id),
  reason VARCHAR,
  status VARCHAR NOT NULL,
  created_at TIMESTAMP DEFAULT now()
);
```

## ?? Status Válidos

```
Shift Status: ACTIVE, CANCELLED, COMPLETED
Swap Status: PENDING, ACCEPTED, REJECTED, APPROVED, CANCELLED
User Role: EMPLOYEE, HR
```

## ?? Estado Transitions (Swap)

```
PENDING
  ?? ? ACCEPTED (target user)
  ?? ? REJECTED (target user)
  ?? ? CANCELLED (requester)

ACCEPTED
  ?? ? APPROVED (HR)
  ?? ? CANCELLED (requester)

APPROVED
  ?? (terminal)

REJECTED
  ?? (terminal)

CANCELLED
  ?? (terminal)
```

## ?? Configuration Keys

```json
{
  "ConnectionStrings:DefaultConnection": "Connection string PostgreSQL",
  "Jwt:Key": "Secret key (min 32 chars)",
  "Jwt:Issuer": "ShiftChangeApi",
  "Jwt:Audience": "ShiftChangeClient",
  "Jwt:ExpirationMinutes": "60",
  "N8n:WebhookUrl": "http://localhost:5678/webhook/...",
  "N8n:ApiKey": "your-api-key"
}
```

## ?? Test Users

```
juan@example.com      | password123 | EMPLOYEE
maria@example.com     | password123 | EMPLOYEE
carlos@example.com    | password123 | HR
```

## ?? Servicios (DI)

```csharp
// En Program.cs
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IShiftService, ShiftService>();
builder.Services.AddScoped<IShiftSwapService, ShiftSwapService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddHttpClient<INotifier, N8nNotifier>();
```

## ?? Validaciones

### CreateSwapRequest
- ? targetUserId existe
- ? requesterShiftId existe y pertenece a requester
- ? targetShiftId existe y pertenece a targetUser

### AcceptSwap
- ? Swap existe
- ? User es targetUser
- ? Status es PENDING

### ApproveSwap (HR only)
- ? Swap existe
- ? Status es ACCEPTED

## ?? HTTP Status Codes

```
200 OK              Exitoso
201 Created         Recurso creado
400 Bad Request     Validación falló
401 Unauthorized    Sin autenticación/token inválido
403 Forbidden       Sin permisos (rol)
404 Not Found       Recurso no existe
500 Server Error    Error del servidor
```

## ?? Eventos n8n

```
SHIFT_SWAP_REQUESTED
SHIFT_SWAP_ACCEPTED
SHIFT_SWAP_REJECTED
SHIFT_SWAP_APPROVED
SHIFT_SWAP_CANCELLED
```

## ?? JWT Claims

```
ClaimTypes.NameIdentifier = UserId
ClaimTypes.Email = Email
ClaimTypes.Role = Role
```

## ??? Seguridad

- ? Contraseñas hasheadas con BCrypt
- ? JWT con expiración
- ? Validación de claims
- ? Autorización por rol
- ? Validación de pertenencia (usuario propietario)

## ?? NuGet Packages

```xml
Microsoft.AspNetCore.Authentication.JwtBearer (10.0.1)
BCrypt.Net-Core (1.6.0)
Npgsql.EntityFrameworkCore.PostgreSQL (10.0.0-rc.2)
```

## ?? Configuración Recomendada (Producción)

```json
{
  "Jwt": {
    "Key": "usar variable de entorno",
    "ExpirationMinutes": 30,
    "Issuer": "ShiftChangeApi"
  },
  "N8n": {
    "WebhookUrl": "https://tu-n8n.com/webhook/...",
    "ApiKey": "usar variable de entorno"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

## ?? Estructura Clean Architecture

```
Domain/
  - Entities (User, Shift, ShiftSwapRequest)
  - Enums (UserRole, ShiftStatus, SwapRequestStatus)
  - Exceptions (InvalidCredentialsException, EntityNotFoundException)

Application/
  - Services (AuthService, ShiftService, ShiftSwapService)
  - Interfaces (IAuthService, IShiftService, etc)
  - DTOs (LoginRequest, ShiftDto, CreateShiftSwapRequest)

Infrastructure/
  - Auth (JwtOptions, JwtTokenGenerator)
  - Data (DatabaseSeeder, DbContext)
  - Notifications (N8nOptions, N8nNotifier)

Api/
  - Controllers (AuthController, ShiftsController, ShiftSwapsController)
  - Program.cs (DI & Configuration)
```

---

**Build Status**: ? Compilación exitosa  
**Estado Fase 3**: ? Completada
