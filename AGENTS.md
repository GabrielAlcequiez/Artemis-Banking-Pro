## Graphify (optional context bootstrap)

This project has a knowledge graph at `graphify-out/`. Its purpose is to help a
new chat obtain initial architectural context quickly when the conversation does
not already provide enough information.

Rules:

* Do not use Graphify routinely for codebase questions.
* In a new chat, use a small `graphify query "<question>"` only when essential
  repository context is missing and the graph can avoid broad manual exploration.
* Use Graphify when the user explicitly requests it with `/graphify` or an
  equivalent instruction.
* Once the relevant files and context are known, inspect those files directly
  instead of continuing to query the graph.
* Do not run `graphify update .` automatically after code changes.
* Do not rebuild or update the graph unless the user explicitly requests it.

## Project technology baseline

Before changing project code, consult the **Tecnologías y estándares
obligatorios** section in `ABP_Technical_Work_Distribution_Plan.md` and the
technical requirements in `ABP_Document.md`.

The agreed stack is:

- .NET 9, ASP.NET Core MVC, ASP.NET Core Web API and Onion Architecture.
- Entity Framework Core Code First with SQL Server.
- ASP.NET Identity for users/roles, cookies in the WebApp and JWT in the API.
- CQRS with MediatR, FluentValidation through MediatR Behaviors, and AutoMapper Profiles.
- Swagger/OpenAPI, Serilog, xUnit and Azure Functions for loan delinquency.
- Tailwind CSS 4 is the current WebApp styling standard.

If a required technology is not yet present in a project file or feature, treat
it as pending implementation. Do not replace it silently with another library;
update the distribution plan first if the team intentionally changes the
stack.
