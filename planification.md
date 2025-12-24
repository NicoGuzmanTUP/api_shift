# 🧠 Objetivo del Backend

El backend será el único punto de verdad para:

- Autenticación y autorización
- Reglas de negocio
- Estados de solicitudes
- Persistencia
- Emisión de eventos hacia n8n

# 🧱 Stack Técnico

- .NET 8 Web API
- PostgreSQL
- EF Core (Scaffold / Reverse Engineering)
- JWT Bearer Authentication
- BCrypt / PBKDF2
- HttpClient (n8n notifier)

# 🗂️ Estructura del Proyecto (Recomendada)

```
src
├── Api
│   ├── Controllers
│   ├── Program.cs
│   └── appsettings.json
│
├── Application
│   ├── Interfaces
│   ├── Services
│   ├── DTOs
│   └── Validators
│
├── Domain
│   ├── Entities
│   ├── Enums
│   └── Exceptions
│
├── Infrastructure
│   ├── Data
│   │   └── DbContext
│   ├── Auth
│   ├── Notifications (n8n)
│   └── Repositories
```

📌 No es overengineering, es orden.

# 🔁 Fase 1 — Base de Datos (Antes del Backend)

- ✔ Tablas creadas
- ✔ Relaciones definidas
- ✔ Constraints
- ✔ Datos dummy

👉 La DB es la fuente de verdad.

# 🔄 Fase 2 — Ingeniería Inversa (EF Core)

Comando base:

```bash
dotnet ef dbcontext scaffold \
  "Host=localhost;Database=shifts_db;Username=postgres;Password=xxx" \
  Npgsql.EntityFrameworkCore.PostgreSQL \
  -o Infrastructure/Data/Models \
  -c AppDbContext \
  --no-onconfiguring
```

✔ Genera:

- Entidades
- DbContext
- Relaciones

📌 No edites estos modelos (se regeneran).

# 🔐 Fase 3 — Autenticación (JWT Bearer)

Flujo:

1. Login → email + password
2. Validar hash
3. Emitir JWT
4. Usar token en endpoints protegidos

Configuración JWT (Program.cs):

```csharp
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });
```

Endpoint de login:

```
POST /auth/login
```

Request:

```json
{
  "email": "juan@mail.com",
  "password": "1234"
}
```

Response:

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

# 🧑‍💼 Fase 4 — Autorización por Rol

Usás claims:

```csharp
[Authorize(Roles = "HR")]
```

Roles:

- EMPLOYEE
- HR

# 🔄 Fase 5 — Casos de Uso (Servicios)

Acá vive la lógica real, NO en controllers.

Servicios clave:

- AuthService
- ShiftService
- ShiftSwapService
- NotificationService (n8n)

# 📡 Fase 6 — Integración con n8n

Concepto: El backend emite eventos, no espera respuesta.

Eventos definidos:

- SHIFT_SWAP_REQUESTED
- SHIFT_SWAP_ACCEPTED
- SHIFT_SWAP_REJECTED
- SHIFT_SWAP_APPROVED
- SHIFT_SWAP_CANCELLED

Servicio notifier:

```csharp
public interface INotifier
{
    Task NotifyAsync(string eventType, object payload);
}
```

Implementación:

- HttpClient
- Header con API Key
- Endpoint n8n

# 🧪 Fase 7 — Seguridad Mínima

- ✔ JWT
- ✔ HTTPS
- ✔ API Key para n8n
- ✔ Validaciones
- ✔ No exponer DB

# 🧠 Fase 8 — Endpoints (Mapa Final)

## Auth

- POST /auth/login

## Turnos

- GET /shifts/my
- GET /shifts/team

## Solicitudes

- POST /shift-swaps
- POST /shift-swaps/{id}/accept
- POST /shift-swaps/{id}/reject
- POST /shift-swaps/{id}/cancel
- POST /shift-swaps/{id}/approve (HR)

# 🧭 Orden Real de Implementación (IMPORTANTE)

1. DB terminada
2. Scaffold EF Core
3. Auth JWT
4. Login endpoint
5. Endpoint /shifts/my
6. Crear solicitud
7. n8n webhook
8. Resto de estados

Si respetás este orden, no te trabás.

# 🏁 Resultado Final

Vas a tener:

- Backend profesional
- Auth real
- DB consistente
- n8n bien usado
- Proyecto 100% defendible en entrevistas