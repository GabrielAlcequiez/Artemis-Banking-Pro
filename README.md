# Artemis Banking Pro (ABP)

## Autores

- Gabriel Alcequiez (2025-1062)
- Jose Rincon (2025-1426)
- Manuel Fernandez (2025-1085)
- Angel Dany (2025-1039)

## Descripción del Proyecto

**Artemis Banking Pro** es una aplicación bancaria completa desarrollada con .NET 9. Gestiona de forma integral clientes, cuentas, tarjetas de crédito, préstamos, comercios, pagos (Hermes Pay) y el control de morosidad.

### Roles del sistema

- **WebApp (MVC)** — Administrador, Cajero y Cliente.
- **WebApi (REST)** — Administrador y Comercio. Un Comercio solo opera sobre su propio comercio y no inicia sesión en MVC.

La aplicación implementa **doble orquestación por requisito académico**: los casos de uso coexisten implementados con **CQRS + MediatR** (WebApi) y con **Servicios Tradicionales MVC** (WebApp) en paralelo, compartiendo DTOs y reglas de dominio.

## Tecnologías Usadas

- **Framework**: .NET 9.0 (ASP.NET Core MVC + WebApi)
- **Lenguaje**: C# 13
- **ORM**: Entity Framework Core 9.0 (Code First)
- **Base de Datos**: SQL Server
- **Validación**: FluentValidation (+ MediatR Behaviors en WebApi)
- **Mapeo**: AutoMapper
- **Mensajería / CQRS**: MediatR (solo WebApi)
- **Seguridad WebApp**: ASP.NET Core Identity + Cookie Authentication
- **Seguridad WebApi**: ASP.NET Core Identity + JWT Bearer
- **Email**: MailKit & MimeKit (SMTP)
- **Diagnóstico**: Serilog
- **Frontend**: Razor Views, Tailwind CSS 4 (HTML5, CSS3, JavaScript)
- **Orquestación**: Azure Functions (`LoanDelinquency`)
- **Testing**: xUnit

## Arquitectura

El proyecto sigue una **Arquitectura Onion** con las siguientes capas/proyectos:

| Capa | Proyecto | Responsabilidad |
|------|----------|----------------|
| **Domain** | `ABP.Domain` | Entidades, enums, errores, reglas de negocio, interfaces de repositorio |
| **Application** | `ABP.Application` | Servicios (tradicionales y CQRS), Commands/Queries, DTOs, validadores, perfiles AutoMapper |
| **Infrastructure.Identity** | `ABP.Infrastructure.Identity` | Identity DbContext (`IdentityContext`), JWT, cookies, seeds, account services |
| **Infrastructure.Persistence** | `ABP.Infrastructure.Persistence` | DbContext (`AppDbContext`), configuraciones EF, repositorios, Unit of Work, transacciones |
| **Shared** | `ABP.Shared` | Servicios compartidos (email, reloj bancario, etc.) |
| **WebApp** | `ABP.WebApp` | Aplicación MVC (frontend web) |
| **WebApi** | `ABP.WebApi` | API RESTful protegida con JWT (CQRS + MediatR) |
| **Functions** | `ABP.Functions` | Azure Functions (control de morosidad de préstamos) |

Dependencias permitidas:

```text
Domain        -> ninguna capa interna
Application   -> Domain
Infrastructure -> Application + Domain
WebApp/WebApi/Functions -> Application; Infrastructure solo al componer DI
Tests         -> proyecto bajo prueba
```

## Requisitos Previos

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) o superior.
- Una instancia de SQL Server funcionando. En Windows puede ser LocalDB (viene con Visual Studio o SQL Server Express) o una instancia completa. 
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local) para compilar y ejecutar `ABP.Functions` localmente.
- [Azurite](https://learn.microsoft.com/azure/storage/common/storage-install-azurite) o un Azure Storage real para el host y el `TimerTrigger` de la Function.
- *(Solo para contribuir con los tests de integración)* Eres libre de usar LocalDB o cualquier SQL Server; el proyecto detecta el servidor automáticamente (ver sección Tests).

## Instrucciones de Configuración

### 1. Clonar el repositorio

```bash
git clone https://github.com/tu-usuario/Artemis-Banking-Pro.git
cd Artemis-Banking-Pro
```

### 2. Configurar valores sensibles

El proyecto usa `dotnet user-secrets` para desarrollo. Los valores también pueden colocarse directamente en `appsettings.json` de cada proyecto de inicio, pero **se recomienda usar user-secrets** para evitar exponer contraseñas y claves en el repositorio (y nunca commitearlas).

> **Importante:** no subas `appsettings.json` con secretos reales ni los commits reales de conexión. Usa user-secrets o placeholders.

#### Opción A (recomendada): User Secrets

Inicializa los secrets para cada proyecto de inicio:

```bash
dotnet user-secrets init --project ABP.WebApp
dotnet user-secrets init --project ABP.WebApi
```

`ABP.Functions` ya contiene un `UserSecretsId`; no es necesario ejecutar `dotnet user-secrets init` para ese proyecto.

**Connection string** (ambos contexts usan `DefaultConnection`):

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=ArtemisBankingPro;Trusted_Connection=True;TrustServerCertificate=True;" --project ABP.WebApp
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=ArtemisBankingPro;Trusted_Connection=True;TrustServerCertificate=True;" --project ABP.WebApi
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=ArtemisBankingPro;Trusted_Connection=True;TrustServerCertificate=True;" --project ABP.Functions
```

Si usas SQL Server Express, cambia `Server=.` por `Server=.\SQLEXPRESS`. Si usas una instancia en Docker en otro puerto, por ejemplo `Server=127.0.0.1,14333;User ID=sa;Password=...;`.

**Password por defecto para los usuarios semilla** (`SeedUsers:DefaultPassword`) — **requerida**: el seed lanza una excepción si falta:

```bash
dotnet user-secrets set "SeedUsers:DefaultPassword" "UnaPasswordSegura123$" --project ABP.WebApp
dotnet user-secrets set "SeedUsers:DefaultPassword" "UnaPasswordSegura123$" --project ABP.WebApi
```

**Zona horaria bancaria (por defecto `America/La_Paz`)**:

```bash
dotnet user-secrets set "BankingTime:TimeZoneId" "America/La_Paz" --project ABP.WebApp
dotnet user-secrets set "BankingTime:TimeZoneId" "America/La_Paz" --project ABP.WebApi
dotnet user-secrets set "BankingTime:TimeZoneId" "America/La_Paz" --project ABP.Functions
```

**CVC (secreto HMAC-SHA-256)** — base64 de al menos 32 bytes:

```bash
dotnet user-secrets set "Security:Cvc:SecretBase64" "I5UqiUzpnSjOY4Mr/PP/+cSZm+/Oh+hc0eSGyZZWZPw=" --project ABP.WebApp
dotnet user-secrets set "Security:Cvc:SecretBase64" "I5UqiUzpnSjOY4Mr/PP/+cSZm+/Oh+hc0eSGyZZWZPw=" --project ABP.WebApi
dotnet user-secrets set "Security:Cvc:SecretBase64" "I5UqiUzpnSjOY4Mr/PP/+cSZm+/Oh+hc0eSGyZZWZPw=" --project ABP.Functions
```

> Aunque `LoanDelinquencyFunction` no procesa tarjetas, la composición actual de Persistence valida `Security:Cvc:SecretBase64` al iniciar. Por ello `ABP.Functions` necesita temporalmente el mismo valor que WebApp y WebApi.

**Email SMTP (Gmail)** — requerido para el envío de correos de activación, recuperación de contraseña y notificaciones:

```bash
dotnet user-secrets set "EmailSettings:SenderName" "Artemis Banking Pro" --project ABP.WebApp
dotnet user-secrets set "EmailSettings:SenderEmail" "tu_correo@gmail.com" --project ABP.WebApp
dotnet user-secrets set "EmailSettings:SmtpHost" "smtp.gmail.com" --project ABP.WebApp
dotnet user-secrets set "EmailSettings:SmtpPort" "587" --project ABP.WebApp
dotnet user-secrets set "EmailSettings:SmtpUser" "tu_correo@gmail.com" --project ABP.WebApp
dotnet user-secrets set "EmailSettings:SmtpPassword" "tu_app_password_de_gmail" --project ABP.WebApp
dotnet user-secrets set "EmailSettings:UseSsl" "true" --project ABP.WebApp
dotnet user-secrets set "EmailSettings:RequiresAuthentication" "true" --project ABP.WebApp
```

Repite los mismos valores para `ABP.WebApi` si la API también envía correos.

**JWT Settings (WebApi)** — requerido para la autenticación de la API (clave de al menos 32 bytes):

```bash
dotnet user-secrets set "JwtSettings:SecretKey" "UnaClaveSuperSeguraDe128BitsOMasAqui1234567890" --project ABP.WebApi
dotnet user-secrets set "JwtSettings:Issuer" "Artemis Banking Pro" --project ABP.WebApi
dotnet user-secrets set "JwtSettings:Audience" "ArtemisBankingProUsers" --project ABP.WebApi
dotnet user-secrets set "JwtSettings:ExpiryInMinutes" "60" --project ABP.WebApi
```

Verificar secrets configurados:

```bash
dotnet user-secrets list --project ABP.WebApp
dotnet user-secrets list --project ABP.WebApi
dotnet user-secrets list --project ABP.Functions
```

#### Opción B: appsettings.json (alternativa rápida)

Si prefieres no usar user-secrets, agrega las secciones directamente en `ABP.WebApp/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=ArtemisBankingPro;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "SeedUsers": {
    "DefaultPassword": "UnaPasswordSegura123$"
  },
  "BankingTime": {
    "TimeZoneId": "America/La_Paz"
  },
  "Security": {
    "Cvc": {
      "SecretBase64": "I5UqiUzpnSjOY4Mr/PP/+cSZm+/Oh+hc0eSGyZZWZPw="
    }
  },
  "EmailSettings": {
    "SenderName": "Artemis Banking Pro",
    "SenderEmail": "tu_correo@gmail.com",
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "tu_correo@gmail.com",
    "SmtpPassword": "tu_app_password_de_gmail",
    "UseSsl": true,
    "RequiresAuthentication": true
  }
}
```

Y en `ABP.WebApi/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=ArtemisBankingPro;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "SeedUsers": {
    "DefaultPassword": "UnaPasswordSegura123$"
  },
  "JwtSettings": {
    "SecretKey": "UnaClaveSuperSeguraDe128BitsOMasAqui1234567890",
    "Issuer": "Artemis Banking Pro",
    "Audience": "ArtemisBankingProUsers",
    "ExpiryInMinutes": 60
  },
  "BankingTime": {
    "TimeZoneId": "America/La_Paz"
  },
  "Security": {
    "Cvc": {
      "SecretBase64": "I5UqiUzpnSjOY4Mr/PP/+cSZm+/Oh+hc0eSGyZZWZPw="
    }
  },
  "EmailSettings": {
    "SenderName": "Artemis Banking Pro",
    "SenderEmail": "tu_correo@gmail.com",
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "tu_correo@gmail.com",
    "SmtpPassword": "tu_app_password_de_gmail",
    "UseSsl": true,
    "RequiresAuthentication": true
  }
}
```

### 2.1. Configurar Azure Functions

`ABP.Functions` ejecuta diariamente `LoanDelinquencyFunction`, que marca como atrasadas las cuotas vencidas con saldo y elimina la marca cuando una cuota queda totalmente pagada. El proceso usa la fecha bancaria configurada y es idempotente.

#### Separación de configuración

La configuración del host y la configuración del worker se cargan en momentos diferentes:

| Clave | Desarrollo local recomendado | Azure Function App |
|------|------------------------------|--------------------|
| `AzureWebJobsStorage` | `local.settings.json` | Application Setting con un Azure Storage real |
| `FUNCTIONS_WORKER_RUNTIME` | `local.settings.json` | `dotnet-isolated` |
| `LoanDelinquencySchedule` | `local.settings.json` | Application Setting |
| `ConnectionStrings:DefaultConnection` | User Secrets | `ConnectionStrings__DefaultConnection` |
| `BankingTime:TimeZoneId` | `appsettings.json` o User Secrets | `BankingTime__TimeZoneId` |
| `Security:Cvc:SecretBase64` | User Secrets | `Security__Cvc__SecretBase64` |

`AzureWebJobsStorage` y `LoanDelinquencySchedule` son resueltos por el host de Functions y **no deben depender únicamente de User Secrets**. Los User Secrets se cargan dentro del worker y se usan para la conexión SQL, la zona bancaria y el secreto CVC.

#### Configuración local del host

Desde la raíz del repositorio, crea tu archivo local a partir del ejemplo:

```powershell
Copy-Item ABP.Functions/local.settings.example.json ABP.Functions/local.settings.json
```

El archivo debe contener como mínimo:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AZURE_FUNCTIONS_ENVIRONMENT": "Development",
    "LoanDelinquencySchedule": "0 5 4 * * *"
  }
}
```

La expresión NCRONTAB usa seis campos. `0 5 4 * * *` ejecuta la Function cada día a las `04:05 UTC`, equivalente a las `00:05` en `America/La_Paz`.

`UseDevelopmentStorage=true` requiere que Azurite esté ejecutándose. Si no deseas usar User Secrets, también puedes colocar temporalmente los valores del worker dentro de `Values` usando claves de variables de entorno:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AZURE_FUNCTIONS_ENVIRONMENT": "Development",
    "LoanDelinquencySchedule": "0 5 4 * * *",
    "ConnectionStrings__DefaultConnection": "TU_CONNECTION_STRING",
    "BankingTime__TimeZoneId": "America/La_Paz",
    "Security__Cvc__SecretBase64": "TU_SECRETO_BASE64_DE_AL_MENOS_32_BYTES"
  }
}
```

`ABP.Functions/local.settings.json` está excluido por `.gitignore`; no elimines esa regla ni subas el archivo. Para trabajo normal se recomienda mantener los valores sensibles en User Secrets y dejar en `local.settings.json` solamente la configuración del host.

#### Instalar las herramientas locales

En Windows instala Azure Functions Core Tools v4 mediante el instalador oficial o Chocolatey:

```powershell
choco install azure-functions-core-tools -y
```

Azurite puede venir incluido con Visual Studio o instalarse mediante npm:

```powershell
npm.cmd install -g azurite
```

El sufijo `.cmd` evita bloqueos de `ExecutionPolicy` en PowerShell. No mezcles instalaciones MSI, npm y Chocolatey de Core Tools en una misma máquina; utiliza un solo mecanismo y verifica la instalación con `func --version` o `func.cmd --version`.

#### Ejecutar localmente

Mantén dos terminales abiertas. En la primera inicia Azurite fuera del repositorio para no generar archivos locales dentro del proyecto:

```powershell
$azuriteData = Join-Path $env:LOCALAPPDATA "ArtemisBankingPro\Azurite"
New-Item -ItemType Directory -Force -Path $azuriteData | Out-Null
azurite.cmd --location $azuriteData
```

En la segunda terminal inicia la Function desde su propio directorio:

```powershell
cd ABP.Functions
dotnet run
```

El host debe listar `LoanDelinquencyFunction: timerTrigger`. Cuando se ejecute, los logs mostrarán el inicio, la cantidad de cuotas actualizadas y la finalización. Un resultado de cero cuotas actualizadas también es una ejecución válida.

Para una prueba rápida, cambia temporalmente `LoanDelinquencySchedule` en `local.settings.json` a cada 30 segundos, reinicia el host y luego restaura el horario diario:

```json
"LoanDelinquencySchedule": "*/30 * * * * *"
```

La Function utiliza la base creada por las migraciones de `AppDbContext`; no crea un tercer contexto ni requiere una migración propia.

#### Configuración en Azure

User Secrets y `local.settings.json` no se publican. Antes de desplegar, registra estos Application Settings en la Function App:

```text
AzureWebJobsStorage=<CONNECTION_STRING_DE_AZURE_STORAGE>
FUNCTIONS_WORKER_RUNTIME=dotnet-isolated
LoanDelinquencySchedule=0 5 4 * * *
ConnectionStrings__DefaultConnection=<CONNECTION_STRING_SQL>
BankingTime__TimeZoneId=America/La_Paz
Security__Cvc__SecretBase64=<SECRETO_BASE64_DE_AL_MENOS_32_BYTES>
```

No uses `UseDevelopmentStorage=true` en Azure. Si se habilita Application Insights en la Function App, Azure agrega normalmente `APPLICATIONINSIGHTS_CONNECTION_STRING`; también puede registrarse manualmente como Application Setting. JWT, usuarios semilla y SMTP no son necesarios para ejecutar `LoanDelinquencyFunction`.

### 3. Aplicar migraciones

Si no tienes la herramienta de comandos de EF Core instalada globalmente:

```bash
dotnet tool install --global dotnet-ef
```

Luego, aplica las migraciones a la base de datos para ambos contextos:

**Migración de Persistencia (`AppDbContext`):**

```bash
dotnet ef database update --project ABP.Infrastructure/ABP.Infrastructure.Persistence --startup-project ABP.WebApp --context AppDbContext
```

**Migración de Identidad (`IdentityContext`):**

```bash
dotnet ef database update --project ABP.Infrastructure/ABP.Infrastructure.Identity --startup-project ABP.WebApp --context IdentityContext
```

Ambos contextos usan la misma cadena de conexión (`DefaultConnection`). El contexto de Identidad mapea sus tablas bajo el esquema `idt`.

### 4. Ejecutar la aplicación

**WebApp (frontend MVC):**

```bash
dotnet run --project ABP.WebApp
```

**WebApi (API REST):**

```bash
dotnet run --project ABP.WebApi
```

La WebApi expone Swagger/OpenAPI en `/swagger`.

**Azure Functions (mora de préstamos):**

Con Azurite iniciado en otra terminal:

```powershell
cd ABP.Functions
dotnet run
```

## Usuarios por Defecto (Seed Data)

Al ejecutar la aplicación por primera vez se crean automáticamente los usuarios de prueba. La contraseña de **todos** es la configurada en `SeedUsers:DefaultPassword`.

| Usuario | Rol | Correo |
|---------|-----|--------|
| `admin` | Administrador (WebApp) | admin@artemisbanking.com |
| `cashier` | Cajero (WebApp) | cashier@artemisbanking.com |
| `client` | Cliente (WebApp) | client@artemisbanking.com |
| `adminapi` | Administrador (WebApi) | adminapi@artemisbanking.com |
| `commerceapi` | Comercio (WebApi) | commerceapi@artemisbanking.com |

## Tests

El proyecto usa **xUnit**. Para ejecutar toda la suite:

```text
dotnet build ArtemisBankingPro.slnx
dotnet test ArtemisBankingPro.slnx
```

### Tests de integración y Base de Datos

Los tests de integración de `ABP.Infrastructure.IntegrationTests` y `ABP.WebApi.IntegrationTests` crean y destruyen bases de datos reales por cada test (nombres únicos) contra un SQL Server accesible.

**Windows**: no requieren configuración. El helper `TestDatabase` detecta automáticamente el servidor: usa `(localdb)\MSSQLLocalDB` si está disponible; si no, usa la instancia por defecto (`localhost`).

**Linux/Mac** (u otro SQL Server, por ejemplo en Docker): define la variable de entorno antes de ejecutar los tests:

```bash
export ABP_TEST_SQL_CONNECTION="Server=127.0.0.1,14333;User ID=sa;Password=tu_password;TrustServerCertificate=True"
```

O, si solo quieres cambiar el servidor conservando la autenticación integrada:

```bash
export ABP_TEST_SQL_SERVER="127.0.0.1,14333"
```

Prioridad de resolución: `ABP_TEST_SQL_CONNECTION` → `ABP_TEST_SQL_SERVER` → auto-detección en Windows.

> **Nota:** Los tests usan bases de datos independientes por test (`ABP_*_{guid}`) y no tocan la base `ArtemisBankingPro` de desarrollo.

## Notas sobre la WebApi

- La API utiliza autenticación **JWT Bearer**.
- El endpoint de login es público; el resto requiere un token válido en el header `Authorization: Bearer {token}`.
- La autorización se valida en el servidor mediante roles/policies, no solo ocultando opciones en la interfaz.
- Los Controllers son delgados: despachan Commands/Queries mediante `ISender` (MediatR) y la validación FluentValidation se ejecuta vía Behaviors.

