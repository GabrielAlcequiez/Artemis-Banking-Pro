# Instrucciones para agentes — Artemis Banking Pro

## Fuentes y contexto

- `ABP_Document.md` define los requisitos funcionales y académicos.
- `ABP_Technical_Work_Distribution_Plan.md` define el stack, la arquitectura,
  los contratos y las decisiones técnicas. Antes de modificar código, consulta
  solo las secciones relevantes de ambos documentos; no es necesario leerlos
  completos.
- No uses ni actualices `graphify-out/` ni ningún índice de contexto. Inspecciona
  directamente los archivos relevantes del repositorio.
- Conserva cambios ajenos a la tarea. Revisa `git status` y el diff antes y
  después de editar.
- No cambies silenciosamente el stack, los contratos ni las rutas de la API.
  Si una decisión técnica debe cambiar, actualiza primero el plan técnico y
  deja explícito el impacto.

## Stack obligatorio

- .NET 9, ASP.NET Core MVC para `ABP.WebApp` y ASP.NET Core Web API para
  `ABP.WebApi`, bajo Onion Architecture.
- EF Core Code First con SQL Server.
- ASP.NET Identity: cookies en WebApp y JWT Bearer en Web API; la autorización
  debe validarse en servidor mediante roles/policies, no solo ocultando menús.
- API: CQRS + MediatR; los Controllers son delgados y despachan Commands/Queries
  mediante `ISender`. FluentValidation se ejecuta mediante MediatR Behaviors.
- WebApp MVC: servicios tradicionales de Application y ViewModels; no debe
  despachar Commands/Queries mediante MediatR.
- AutoMapper mediante Profiles, con prueba de configuración.
- Swagger/OpenAPI, Problem Details y manejo global de excepciones.
- Serilog para diagnóstico y auditoría; nunca registrar secretos, contraseñas,
  tokens, CVC, hashes de CVC, números completos de tarjeta ni cadenas de
  conexión.
- Tailwind CSS 4 es el estándar actual de la WebApp. `wwwroot/css/output.css`
  es generado: modifica las fuentes CSS, no el archivo generado manualmente.
- Azure Functions para `LoanDelinquency` es parte del alcance obligatorio; si
  aún falta integración, trátala como pendiente y no la sustituyas
  silenciosamente por otro mecanismo.

## Arquitectura y organización

Dependencias permitidas:

```text
Domain       -> ninguna capa interna
Application  -> Domain
Infrastructure -> Application + Domain
WebApp/WebApi/Functions -> Application; Infrastructure solo al componer DI
Tests        -> proyecto bajo prueba
```

- `ABP.Domain` no depende de ASP.NET Identity, EF Core, JWT, SMTP, Serilog ni
  detalles de infraestructura.
- Organiza Application por vertical en `Features/{Feature}` con `Commands`,
  `Queries`, `Services`, `DTOs` y `Validation` cuando correspondan.
- Reutiliza validadores DTO entre servicios MVC y Commands; no dupliques reglas
  de validación. Usa repositorios/servicios genéricos solo para CRUD sencillo;
  los flujos financieros requieren servicios especializados.
- Mantén las reglas de negocio en Domain/Application, no en Controllers ni
  Views. Respeta la propiedad vertical y usa contratos/fakes para integraciones
  entre módulos.

## Reglas financieras y de seguridad

- Usa `decimal` para dinero, nunca `double`. Persiste fechas en UTC y calcula
  “hoy” con la zona bancaria configurable `America/La_Paz`.
- Los identificadores de entidades del dominio son `Guid`; la excepción es
  `User.Id`, que es `string` por Identity. Los números de cuenta y préstamo son
  identificadores de negocio de 9 dígitos, almacenados como texto y únicos entre
  ambos tipos.
- Una operación que cambie varios agregados debe ser atómica. Revalida saldo o
  deuda dentro de la transacción y aplica concurrencia optimista (`rowversion`)
  en productos financieros.
- Los POST financieros y confirmaciones MVC deben ser idempotentes mediante
  `OperationId` o `Idempotency-Key`. Una operación rechazada no modifica
  balances/deudas; registra el intento cuando exista un producto origen válido.
- El correo se publica después del commit mediante Outbox; un fallo de correo
  nunca revierte una operación financiera.
- El CVC nunca se almacena ni se expone en texto plano. Usa el mecanismo seguro
  acordado en el plan (actualmente HMAC-SHA-256 con secreto externo), y muestra
  tarjetas solo enmascaradas o por sus últimos cuatro dígitos.
- La expiración `MM/AA` de una tarjeta representa el último día calendario del
  mes; la tarjeta sigue válida durante todo ese día según la fecha bancaria.
- WebApp contempla Administrador, Cajero y Cliente; API contempla Administrador
  y Comercio. Un Comercio solo opera sobre su propio comercio y no inicia sesión
  en MVC.

## Contratos, persistencia y verificación

- Trata `Application/Common/Contracts` como contrato compartido: un cambio
  incompatible requiere revisar sus consumidores. Conserva respuestas paginadas
  uniformes, Problem Details (RFC 7807) con status HTTP + mensaje según
  `ABP_Document` (sin `errorCode` como mecanismo de control) y rutas de endpoint
  documentadas.
- Cada vertical aporta sus configuraciones EF (`IEntityTypeConfiguration<T>`);
  las migraciones consolidadas se integran de forma centralizada. No edites una
  migración compartida sin coordinación.
- Para cada cambio relevante agrega o actualiza pruebas xUnit: reglas de
  Domain/Application, adapters de infraestructura y pruebas de integración
  cuando corresponda. En operaciones financieras cubre caso feliz, fondos o
  crédito insuficiente, ownership, estado inválido, rollback, concurrencia e
  idempotencia.
- Mantén Swagger actualizado y verifica, como mínimo según el alcance, con:

  ```text
  dotnet build ArtemisBankingPro.slnx
  dotnet test ArtemisBankingPro.slnx
  ```

## Forma de trabajo

Antes de cambios importantes explica el objetivo, la razón y el patrón o
principio aplicado. Al terminar, resume los archivos modificados, los riesgos o
decisiones relevantes y cómo verificar el resultado.
