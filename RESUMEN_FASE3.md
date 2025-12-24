# ?? RESUMEN FASE 3 - AUTENTICACIÓN JWT & SERVICIOS

## ?? Estructura Final del Proyecto

```
Api/
??? ?? Controllers/
?   ??? AuthController.cs           ? Login endpoint
?   ??? ShiftsController.cs         ? Turnos (GET /my, /team)
?   ??? ShiftSwapsController.cs     ? Intercambios de turnos
?
??? ?? Application/
?   ??? DTOs/
?   ?   ??? Auth/
?   ?   ?   ??? LoginRequest.cs
?   ?   ?   ??? LoginResponse.cs
?   ?   ??? Shifts/
?   ?   ?   ??? ShiftDto.cs
?   ?   ??? ShiftSwaps/
?   ?       ??? CreateShiftSwapRequest.cs
?   ?       ??? ShiftSwapRequestDto.cs
?   ??? Interfaces/
?   ?   ??? IAuthService.cs
?   ?   ??? IJwtTokenGenerator.cs
?   ?   ??? IShiftService.cs
?   ?   ??? IShiftSwapService.cs
?   ?   ??? INotifier.cs
?   ??? Services/
?       ??? AuthService.cs
?       ??? ShiftService.cs
?       ??? ShiftSwapService.cs
?
??? ??? Domain/
?   ??? Enums/
?   ?   ??? UserRole.cs
?   ?   ??? ShiftStatus.cs
?   ?   ??? SwapRequestStatus.cs
?   ??? Exceptions/
?       ??? InvalidCredentialsException.cs
?       ??? EntityNotFoundException.cs
?
??? ??? Infrastructure/
?   ??? Auth/
?   ?   ??? JwtOptions.cs
?   ?   ??? JwtTokenGenerator.cs
?   ??? Data/
?   ?   ??? DatabaseSeeder.cs        ? Seed automático
?   ??? Notifications/
?       ??? N8nOptions.cs
?       ??? N8nNotifier.cs           ? Webhooks
?
??? ?? Entities/
?   ??? shift_change_bdContext.cs
?   ??? User.cs
?   ??? Shift.cs
?   ??? ShiftSwapRequest.cs
?
??? ?? Program.cs                     ? Completamente configurado
??? ?? appsettings.json              ? JWT + N8n
??? ?? Api.csproj                    ? Dependencias actualizadas
```

## ?? Flujo de Autenticación

```
1. Usuario POST /api/auth/login
   ?? email: "juan@example.com"
   ?? password: "password123"
         ?
2. AuthService valida
   ?? Busca usuario por email
   ?? Verifica BCrypt hash
   ?? Si es válido ? genera JWT
         ?
3. LoginResponse
   ?? token: "eyJhbGciOiJIUzI1NiI..."
   ?? userId: 1
   ?? email: "juan@example.com"
   ?? name: "Juan García"
   ?? role: "EMPLOYEE"
         ?
4. Cliente almacena token
   ?? Authorization: Bearer {token}
         ?
5. Endpoint protegido valida JWT
   ?? Extrae claims
   ?? Autoriza según rol si es necesario
```

## ?? Flujo de Intercambio de Turnos

```
Usuario A (EMPLOYEE)
    ?
    POST /api/shift-swaps
    {
      "targetUserId": 2,
      "requesterShiftId": 1,
      "targetShiftId": 3,
      "reason": "Necesito el turno de la tarde"
    }
    ?
ShiftSwapService.CreateSwapRequestAsync()
    ?? Valida usuarios
    ?? Valida turnos
    ?? Crea solicitud (PENDING)
    ?? Guarda en BD
    ?? Emite evento a n8n
    ?
Usuario B (EMPLOYEE) recibe notificación
    ?
    POST /api/shift-swaps/{id}/accept
    ?
Status ? ACCEPTED
    ?? Emite evento a n8n
    ?? HR puede aprobar
    ?
HR (EMPLOYEE) aprueba
    ?
    POST /api/shift-swaps/{id}/approve  [Authorize(Roles = "HR")]
    ?
Status ? APPROVED
    ?? Emite evento a n8n
    ?? Los turnos se pueden intercambiar
```

## ?? Matriz de Roles y Permisos

| Acción | EMPLOYEE | HR |
|--------|----------|-----|
| Login | ? | ? |
| Ver mis turnos | ? | ? |
| Ver turnos del equipo | ? | ? |
| Crear intercambio | ? | ? |
| Aceptar intercambio | ?* | ?* |
| Rechazar intercambio | ?* | ?* |
| Cancelar intercambio | ?** | ?** |
| Aprobar intercambio | ? | ? |
| Ver solicitudes pendientes | ? | ? |

*Solo si eres el usuario destino  
**Solo si eres el usuario solicitante

## ?? Comandos de Prueba

### 1?? Login
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "juan@example.com",
    "password": "password123"
  }'
```

### 2?? Obtener mis turnos (con token)
```bash
curl http://localhost:5000/api/shifts/my \
  -H "Authorization: Bearer {token}"
```

### 3?? Crear intercambio
```bash
curl -X POST http://localhost:5000/api/shift-swaps \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "targetUserId": 2,
    "requesterShiftId": 1,
    "targetShiftId": 3,
    "reason": "Conflicto personal"
  }'
```

## ?? Eventos Emitidos a n8n

```javascript
// SHIFT_SWAP_REQUESTED
{
  "eventType": "SHIFT_SWAP_REQUESTED",
  "timestamp": "2024-01-15T10:30:00Z",
  "data": {
    "id": 1,
    "requesterId": 1,
    "targetUserId": 2,
    "requesterName": "Juan García",
    "targetName": "María López"
  }
}

// SHIFT_SWAP_ACCEPTED
{
  "eventType": "SHIFT_SWAP_ACCEPTED",
  "timestamp": "2024-01-15T10:35:00Z",
  "data": {
    "id": 1,
    "requesterId": 1,
    "targetUserId": 2
  }
}

// SHIFT_SWAP_APPROVED
{
  "eventType": "SHIFT_SWAP_APPROVED",
  "timestamp": "2024-01-15T10:40:00Z",
  "data": {
    "id": 1,
    "requesterId": 1,
    "targetUserId": 2
  }
}
```

## ? Checklist de Completitud

- [x] Autenticación JWT Bearer
- [x] Login endpoint
- [x] Password hashing (BCrypt)
- [x] Token generation con Claims
- [x] Endpoints protegidos con [Authorize]
- [x] Autorización por rol [Authorize(Roles = "HR")]
- [x] Servicios de turnos
- [x] Servicios de intercambios
- [x] Validaciones de negocio
- [x] Integración con n8n
- [x] Database seeding
- [x] Manejo de errores
- [x] DTOs y mapeos
- [x] Configuration en appsettings
- [x] Dependency injection completo
- [x] Build exitoso

## ?? Status General

```
Fase 1: ? DB
Fase 2: ? EF Core
Fase 3: ? AUTENTICACIÓN JWT ? YOU ARE HERE
Fase 4: ? AUTORIZACIÓN
Fase 5: ? SERVICIOS
Fase 6: ? N8N
Fase 7: ? Testeo (próximo paso)
Fase 8: ? Endpoints
```

## ?? Lo Que Aprendiste

1. ? JWT Bearer Authentication con .NET
2. ? Validación de tokens
3. ? Claims y roles
4. ? BCrypt para passwords
5. ? Inyección de dependencias
6. ? DTOs y mapeos
7. ? Servicios de dominio
8. ? Validaciones de negocio
9. ? Notificaciones asincrónicas
10. ? Manejo de errores

---

**Próximo paso**: Testear los endpoints y ajustar configuración de n8n
