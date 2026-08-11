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
- **Doble Orquestación por Requisito Académico**: Coexisten dos formas de implementar los casos de uso en `ABP.Application`:
  - **`ABP.WebApi` (CQRS + MediatR)**: Controllers delgados despachan `Commands`/`Queries` mediante `ISender`. FluentValidation se ejecuta mediante MediatR `ValidationBehavior`.
  - **`ABP.WebApp` (Servicios Tradicionales MVC)**: Controllers consumen exclusivamente servicios de aplicación tradicional (`Features/{Feature}/Services/`) y ViewModels. Está estrictamente prohibido usar MediatR o despachar Commands/Queries en WebApp.
  - **Independencia**: Servicios MVC y Handlers CQRS son implementaciones paralelas. Comparten DTOs y reglas de Dominio, pero no se llaman entre sí.
- AutoMapper mediante Profiles, con prueba de configuración.
- Swagger/OpenAPI, Problem Details y manejo global de excepciones.
- Serilog para diagnóstico y auditoría; nunca registrar secretos, contraseñas,
  tokens, CVC, hashes de CVC, números completos de tarjeta ni cadenas de
  conexión.
- Tailwind CSS 4 es el estándar actual de la WebApp. `wwwroot/css/output.css`
  es generado: modifica las fuentes CSS (`wwwroot/css/site.css`: `@theme` +
  `@layer components`), no el archivo generado manualmente. `dotnet build` lo
  regenera (target `Tailwind` del csproj); en desarrollo usa `npm run css:watch`
  o `npm run css:build` desde la raíz.
- Layouts MVC anidados: `_Layout` (público: Login, recuperación de contraseña,
  Acceso denegado, Error) ← `_AppLayout` (rail vertical oscuro + topbar;
  renderiza la sección `SidebarItems`) ← `_AdminLayout` / `_CashierLayout` /
  `_ClientLayout`, cada uno con los items de menú de su rol. Las vistas por rol
  viven en `Areas/{Admin,Cashier,Client}` con su propio `_ViewStart.cshtml`.
  Los iconos SVG se agregan en `Views/Shared/Partials/_Icon.cshtml`.
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
- **Estructura Interna por Vertical en `ABP.Application/Features/{Feature}/`**:
  - `Commands/`: Command, CommandHandlers y CommandValidators (CQRS para WebApi).
  - `Queries/`: Query y QueryHandlers (CQRS para WebApi).
  - `Services/`: Interfaces (`Interfaces/`) e Implementaciones (`Implementations/`) para Servicios Tradicionales (exclusivo para WebApp MVC).
  - `DTOs/`: Data Transfer Objects compartidos.
  - `Validation/`: Validadores base de FluentValidation para DTOs (`{Dto}Validator.cs`). Nombre estandarizado (no usar `Validators/`).
- **Estrategia de Validación Compartida (Sin duplicar reglas)**:
  - Las reglas de validación residen en los validadores DTO dentro de `Validation/`.
  - **En CQRS (WebApi)**: El `CommandValidator` reutiliza el validador del DTO mediante `SetValidator(...)` y MediatR `ValidationBehavior` lo ejecuta automáticamente.
  - **En Servicios Tradicionales (WebApp MVC)**: El servicio inyecta `IValidator<TDto>` y ejecuta `await _validator.ValidateAndThrowAsync(dto, cancellationToken)`.
- Usa repositorios/servicios genéricos solo para CRUD sencillo; los flujos financieros requieren servicios y handlers especializados.
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
