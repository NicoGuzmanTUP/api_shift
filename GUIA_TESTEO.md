# ?? GUÍA DE TESTEO - FASE 3

## ? Inicio Rápido

1. **Actualizar connection string** en `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=shift_change_bd;Username=postgres;Password=tu_password"
}
```

2. **Ejecutar la app**:
```bash
dotnet run
```

La base de datos se creará automáticamente y se seedeará con datos de prueba.

## ?? Usuarios de Prueba Disponibles

```
Email: juan@example.com          | Password: password123  | Role: EMPLOYEE
Email: maria@example.com         | Password: password123  | Role: EMPLOYEE  
Email: carlos@example.com        | Password: password123  | Role: HR
```

## ?? Test Cases

### Test 1: Login Exitoso

**Endpoint**: `POST /api/auth/login`

**Request**:
```json
{
  "email": "juan@example.com",
  "password": "password123"
}
```

**Response esperada (200 OK)**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": 1,
  "email": "juan@example.com",
  "name": "Juan García",
  "role": "EMPLOYEE"
}
```

**Guardar**: El `token` para usar en pruebas posteriores

---

### Test 2: Login Fallido (Credenciales Inválidas)

**Endpoint**: `POST /api/auth/login`

**Request**:
```json
{
  "email": "juan@example.com",
  "password": "password_incorrecto"
}
```

**Response esperada (401 Unauthorized)**:
```json
{
  "message": "Email o contraseña inválidos"
}
```

---

### Test 3: Obtener Mis Turnos

**Endpoint**: `GET /api/shifts/my`

**Headers**:
```
Authorization: Bearer {token_juan}
```

**Response esperada (200 OK)**:
```json
[
  {
    "id": 1,
    "userId": 1,
    "shiftDate": "2024-01-16",
    "startTime": "09:00",
    "endTime": "17:00",
    "status": "ACTIVE"
  },
  {
    "id": 2,
    "userId": 1,
    "shiftDate": "2024-01-17",
    "startTime": "14:00",
    "endTime": "22:00",
    "status": "ACTIVE"
  }
]
```

---

### Test 4: Obtener Turnos del Equipo

**Endpoint**: `GET /api/shifts/team`

**Headers**:
```
Authorization: Bearer {token_juan}
```

**Response esperada (200 OK)**:
```json
[
  {
    "id": 1,
    "userId": 1,
    "shiftDate": "2024-01-16",
    "startTime": "09:00",
    "endTime": "17:00",
    "status": "ACTIVE"
  },
  {
    "id": 2,
    "userId": 1,
    "shiftDate": "2024-01-17",
    "startTime": "14:00",
    "endTime": "22:00",
    "status": "ACTIVE"
  },
  {
    "id": 3,
    "userId": 2,
    "shiftDate": "2024-01-16",
    "startTime": "14:00",
    "endTime": "22:00",
    "status": "ACTIVE"
  },
  {
    "id": 4,
    "userId": 2,
    "shiftDate": "2024-01-18",
    "startTime": "09:00",
    "endTime": "17:00",
    "status": "ACTIVE"
  }
]
```

---

### Test 5: Crear Solicitud de Intercambio

**Endpoint**: `POST /api/shift-swaps`

**Headers**:
```
Authorization: Bearer {token_juan}
Content-Type: application/json
```

**Request**:
```json
{
  "targetUserId": 2,
  "requesterShiftId": 1,
  "targetShiftId": 3,
  "reason": "Conflicto familiar"
}
```

**Response esperada (201 Created)**:
```json
{
  "id": 1,
  "requesterId": 1,
  "targetUserId": 2,
  "requesterShiftId": 1,
  "targetShiftId": 3,
  "reason": "Conflicto familiar",
  "status": "PENDING",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**Nota**: Se emite evento `SHIFT_SWAP_REQUESTED` a n8n

---

### Test 6: Aceptar Solicitud (Usuario Destino)

**Endpoint**: `POST /api/shift-swaps/{id}/accept`

**Headers**:
```
Authorization: Bearer {token_maria}
```

**Request**: (vacío, solo URL)

**Response esperada (200 OK)**:
```json
{
  "id": 1,
  "requesterId": 1,
  "targetUserId": 2,
  "requesterShiftId": 1,
  "targetShiftId": 3,
  "reason": "Conflicto familiar",
  "status": "ACCEPTED",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**Nota**: Se emite evento `SHIFT_SWAP_ACCEPTED` a n8n

---

### Test 7: Aprobar Solicitud (Solo HR)

**Endpoint**: `POST /api/shift-swaps/{id}/approve`

**Headers**:
```
Authorization: Bearer {token_carlos}
```

**Request**: (vacío, solo URL)

**Response esperada (200 OK)**:
```json
{
  "id": 1,
  "requesterId": 1,
  "targetUserId": 2,
  "requesterShiftId": 1,
  "targetShiftId": 3,
  "reason": "Conflicto familiar",
  "status": "APPROVED",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**Nota**: Se emite evento `SHIFT_SWAP_APPROVED` a n8n

---

### Test 8: Intentar Aprobar sin ser HR (Debe fallar)

**Endpoint**: `POST /api/shift-swaps/{id}/approve`

**Headers**:
```
Authorization: Bearer {token_juan}
```

**Response esperada (403 Forbidden)**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403
}
```

---

### Test 9: Rechazar Solicitud

**Endpoint**: `POST /api/shift-swaps/2/reject`

**Headers**:
```
Authorization: Bearer {token_maria}
```

**Request**: (vacío)

**Response esperada (200 OK)**:
```json
{
  "id": 2,
  "status": "REJECTED",
  ...
}
```

---

### Test 10: Cancelar Solicitud

**Endpoint**: `POST /api/shift-swaps/1/cancel`

**Headers**:
```
Authorization: Bearer {token_juan}
```

**Response esperada (200 OK)**:
```json
{
  "id": 1,
  "status": "CANCELLED",
  ...
}
```

---

### Test 11: Ver Solicitudes Pendientes (Solo HR)

**Endpoint**: `GET /api/shift-swaps/pending`

**Headers**:
```
Authorization: Bearer {token_carlos}
```

**Response esperada (200 OK)**:
```json
[
  {
    "id": 3,
    "requesterId": 1,
    "targetUserId": 2,
    "status": "PENDING",
    "createdAt": "2024-01-15T10:45:00Z"
  }
]
```

---

### Test 12: Endpoint sin Autenticación (Debe fallar)

**Endpoint**: `GET /api/shifts/my`

**Headers**: (ninguno)

**Response esperada (401 Unauthorized)**:
```
WWW-Authenticate: Bearer error="invalid_token"
```

---

## ?? Errores Esperados

| Caso | Status | Mensaje |
|------|--------|---------|
| Email no existe | 401 | Email o contraseña inválidos |
| Password incorrecto | 401 | Email o contraseña inválidos |
| Sin token | 401 | Unauthorized |
| Token expirado | 401 | Token expired |
| Sin permisos (Approve sin HR) | 403 | Forbidden |
| Solicitud no existe | 404 | Solicitud con ID X no encontrada |
| Usuario no destino en accept | 400 | No tienes permisos para aceptar esta solicitud |

---

## ??? Herramientas Recomendadas

### Postman
1. Instalar Postman
2. Crear colección "Shift Swaps API"
3. Guardar el token en una variable global
4. Importar tests

### Thunder Client (VS Code)
1. Instalar extensión "Thunder Client"
2. Más ligero que Postman
3. Integrado en VS Code

### cURL (Línea de Comandos)
```bash
# Login
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"juan@example.com","password":"password123"}' \
  | jq -r '.token')

# Usar token
curl http://localhost:5000/api/shifts/my \
  -H "Authorization: Bearer $TOKEN"
```

---

## ?? Verificar Base de Datos

```sql
-- Ver usuarios seededeados
SELECT * FROM users;

-- Ver turnos
SELECT * FROM shifts;

-- Ver solicitudes de intercambio
SELECT * FROM shift_swap_requests;
```

---

## ?? Troubleshooting

### Error: "Connection timeout"
- Verificar que PostgreSQL está corriendo
- Revisar connection string en appsettings.json

### Error: "Database does not exist"
- EF Core creará la DB automáticamente
- Si falla, crear manualmente: `CREATE DATABASE shift_change_bd;`

### Error: "Seeding failed"
- Verificar que la tabla `users` no existe previa
- Limpiar base de datos y reintentar

### Token inválido
- Copiar exactamente el token de la respuesta de login
- Verificar formato: `Bearer {token}`
- Verificar que no está expirado (60 minutos)

---

## ? Checklist Final

- [ ] DB PostgreSQL creada y corriendo
- [ ] Connection string actualizado
- [ ] App inicia sin errores
- [ ] Login exitoso
- [ ] Token obtenido
- [ ] Endpoints de shifts funcionan
- [ ] Crear intercambio funciona
- [ ] Aceptar/Rechazar/Cancelar funciona
- [ ] Aprobar solo funciona con HR
- [ ] Errores retornan status correcto
- [ ] n8n puede recibir webhooks (opcional)

---

**Estado**: Fase 3 lista para producción (falta testeo manual)
