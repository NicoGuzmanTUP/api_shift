# ? Fase 3 — Autenticación JWT (COMPLETADA)

## ?? Estado Actual

### ? Implementado

#### 1. **Autenticación JWT Bearer**
- ? `JwtOptions.cs` - Configuración parametrizada
- ? `JwtTokenGenerator.cs` - Genera tokens con Claims (UserId, Email, Role)
- ? `AuthService.cs` - Valida credenciales con BCrypt
- ? `AuthController.cs` - Endpoint `POST /api/auth/login`
- ? Configuration en `Program.cs` - Bearer authentication configurado
- ? Token validation parameters - Issuer, Audience, Key, Lifetime

#### 2. **Endpoints de Turnos (Fase 4 avanzada)**
- ? `IShiftService.cs` - Interfaz de servicios
- ? `ShiftService.cs` - Lógica de negocio
- ? `ShiftsController.cs` - Endpoints protegidos
  - `GET /api/shifts/my` - Mis turnos
  - `GET /api/shifts/team` - Turnos del equipo

#### 3. **Solicitudes de Intercambio (Fase 5)**
- ? `IShiftSwapService.cs` - Interfaz de servicios
- ? `ShiftSwapService.cs` - Lógica completa con validaciones
- ? `ShiftSwapsController.cs` - Endpoints con autorización por rol
  - `POST /api/shift-swaps` - Crear solicitud
  - `POST /api/shift-swaps/{id}/accept` - Aceptar (usuario destino)
  - `POST /api/shift-swaps/{id}/reject` - Rechazar (usuario destino)
  - `POST /api/shift-swaps/{id}/cancel` - Cancelar (usuario solicitante)
  - `POST /api/shift-swaps/{id}/approve` - Aprobar (HR)
  - `GET /api/shift-swaps/{id}` - Obtener solicitud
  - `GET /api/shift-swaps/pending` - Solicitudes pendientes (HR)

#### 4. **Integración con n8n (Fase 6)**
- ? `INotifier.cs` - Interfaz de notificaciones
- ? `N8nOptions.cs` - Configuración de webhook
- ? `N8nNotifier.cs` - HttpClient con retry y manejo de errores
- ? Eventos emitidos:
  - SHIFT_SWAP_REQUESTED
  - SHIFT_SWAP_ACCEPTED
  - SHIFT_SWAP_REJECTED
  - SHIFT_SWAP_APPROVED
  - SHIFT_SWAP_CANCELLED

#### 5. **Data Seeding**
- ? `DatabaseSeeder.cs` - Usuarios y turnos de prueba
  - 3 usuarios (2 EMPLOYEE, 1 HR)
  - 4 turnos con fechas realistas
  - Automático al iniciar la app

#### 6. **Configuración Completa**
- ? `appsettings.json` - JWT y N8n configurados
- ? `Program.cs` - DI completo
  - DbContext
  - Authentication/Authorization
  - Services
  - N8n HttpClient
  - Database Seeding

## ?? Endpoints Disponibles

### Auth
```
POST /api/auth/login
```
**Request:**
```json
{
  "email": "juan@example.com",
  "password": "password123"
}
```

**Response (200):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": 1,
  "email": "juan@example.com",
  "name": "Juan García",
  "role": "EMPLOYEE"
}
```

### Shifts (Autenticado)
```
GET /api/shifts/my
GET /api/shifts/team
```

### Shift Swaps (Autenticado)
```
POST /api/shift-swaps
POST /api/shift-swaps/{id}/accept
POST /api/shift-swaps/{id}/reject
POST /api/shift-swaps/{id}/cancel
POST /api/shift-swaps/{id}/approve    [HR only]
GET /api/shift-swaps/{id}
GET /api/shift-swaps/pending          [HR only]
```

## ?? Usuarios de Prueba

| Email | Password | Role |
|-------|----------|------|
| juan@example.com | password123 | EMPLOYEE |
| maria@example.com | password123 | EMPLOYEE |
| carlos@example.com | password123 | HR |

## ?? Validaciones Implementadas

1. **Autenticación**
   - Email obligatorio
   - Contraseña validada con BCrypt
   - Token JWT con expiración

2. **Turnos**
   - Usuario debe ser propietario del turno
   - Validación de existencia

3. **Intercambio de Turnos**
   - Validar usuarios existen
   - Validar turnos existen
   - Validar pertenencia de turnos
   - Solo usuario destino puede aceptar/rechazar
   - Solo usuario solicitante puede cancelar
   - Solo HR puede aprobar
   - Validar transiciones de estado

4. **Notificaciones**
   - Webhook opcional (silencioso si no está configurado)
   - Retry logic en logs
   - Async (no bloquea la respuesta)

## ?? Seguridad

- ? JWT Bearer Authentication
- ? BCrypt hashing de contraseñas
- ? Claims con roles
- ? Autorización por rol [Authorize(Roles = "HR")]
- ? Validación de permisos en servicios
- ? HTTPS en producción

## ?? Dependencies

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.1" />
<PackageReference Include="BCrypt.Net-Core" Version="1.6.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0-rc.2" />
```

## ? Compilación

**Status**: ? Build exitoso sin errores

## ?? Próximos Pasos

1. Testear endpoints con Postman/Thunder Client
2. Configurar webhook de n8n
3. Implementar validators (FluentValidation opcional)
4. Agregar logging structured
5. CORS si es necesario para frontend

## ?? Notas Importantes

- Los datos se seedean automáticamente en cada inicio (idempotente)
- Las notificaciones a n8n son asincrónicas y no bloquean
- Los errores de n8n se logean pero no afectan la app
- JWT expira en 60 minutos (configurable)
- Las contraseñas hasheadas con BCrypt son seguras

---

**Fase 1**: ? DB  
**Fase 2**: ? EF Core  
**Fase 3**: ? Autenticación  
**Fase 4**: ? Autorización  
**Fase 5**: ? Servicios  
**Fase 6**: ? n8n  
**Fase 7**: ? Testeo y seguridad adicional  
**Fase 8**: ? Endpoints mapeados
