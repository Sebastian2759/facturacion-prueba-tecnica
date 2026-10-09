# Graph Report - .  (2026-07-26)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 314 nodes · 441 edges · 21 communities
- Extraction: 99% EXTRACTED · 1% INFERRED · 0% AMBIGUOUS · INFERRED: 6 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Generated from the complete feature working tree before opening the pull request.
- Regenerate the graph after structural code changes to keep this report aligned.

## Community Hubs (Navigation)
- [[_COMMUNITY_ResponseBase|ResponseBase]]
- [[_COMMUNITY_Domain.Entities|Domain.Entities]]
- [[_COMMUNITY_Domain.Base|Domain.Base]]
- [[_COMMUNITY_API - perfiles de ejecucion|API - perfiles de ejecucion]]
- [[_COMMUNITY_RepositoryGeneric|RepositoryGeneric]]
- [[_COMMUNITY_.AddPersistenceServices|.AddPersistenceServices]]
- [[_COMMUNITY_Domain - puerto IRepositoryGeneric|Domain - puerto IRepositoryGeneric]]
- [[_COMMUNITY_Program.cs|Program.cs]]
- [[_COMMUNITY_.Handle|.Handle]]
- [[_COMMUNITY_.TryHandleAsync|.TryHandleAsync]]
- [[_COMMUNITY_Api.csproj|Api.csproj]]
- [[_COMMUNITY_.AddAssemblyServices|.AddAssemblyServices]]
- [[_COMMUNITY_.BuildProvider|.BuildProvider]]
- [[_COMMUNITY_GetDemoArchitectureByIdValidator|GetDemoArchitectureByIdValidator]]
- [[_COMMUNITY_Tests - proyecto y herramientas|Tests - proyecto y herramientas]]
- [[_COMMUNITY_Persistence - dependencias EF Core|Persistence - dependencias EF Core]]
- [[_COMMUNITY_Application - dependencias CQRS|Application - dependencias CQRS]]
- [[_COMMUNITY_Domain e Infrastructure - proyectos base|Domain e Infrastructure - proyectos base]]
- [[_COMMUNITY_GetDemoArchitectureByIdQueryTests|GetDemoArchitectureByIdQueryTests]]
- [[_COMMUNITY_Adapters.csproj|Adapters.csproj]]

## God Nodes (most connected - your core abstractions)
1. `ResponseBase` - 19 edges
2. `RepositoryGeneric` - 14 edges
3. `IRepositoryGeneric` - 13 edges
4. `Application.UseCases.DemoArchitectures.GetDemoArchitectureById` - 9 edges
5. `GetDemoArchitectureByIdRequest` - 9 edges
6. `Domain.Base` - 9 edges
7. `Domain.Entities` - 8 edges
8. `Api` - 7 edges
9. `Application.Dtos` - 7 edges
10. `Domain.Base.Interface` - 7 edges

## Surprising Connections (you probably didn't know these)
- `Reutilización del repositorio genérico` --references--> `RepositoryGeneric`  [EXTRACTED]
  README.md → Infraestructure/Persistence/Base/RepositoryGeneric.cs
- `Reutilización del repositorio genérico` --references--> `IRepositoryGeneric`  [EXTRACTED]
  README.md → Core/Domain/Base/Interface/IRepositoryGeneric.cs
- `AutoMapper por convención` --references--> `AutoConventionProfile`  [EXTRACTED]
  README.md → Infraestructure/Adapters/Mapper/AutoConventionProfile.cs
- `AutoMapper por convención` --references--> `AutoMapperAdapter`  [EXTRACTED]
  README.md → Infraestructure/Adapters/Mapper/AutoMapperAdapter.cs
- `Once pruebas automatizadas` --references--> `GetDemoArchitectureByIdQueryTests`  [EXTRACTED]
  README.md → Test/DemoArchitectures/GetDemoArchitectureByIdQueryTests.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Ciclo de vida del request DemoArchitecture** — api_api_controllers_v1_demoarchitecturescontroller_api_controllers_v1_demoarchitecturescontroller_getbyid, core_application_behaviors_validationbehavior_application_behaviors_validationbehavior_handle, core_application_usecases_demoarchitectures_getdemoarchitecturebyid_getdemoarchitecturebyidquery_application_usecases_demoarchitectures_getdemoarchitecturebyid_getdemoarchitecturebyidquery_handle, core_domain_base_interface_irepositorygeneric_domain_base_interface_irepositorygeneric, infraestructure_persistence_base_repositorygeneric_persistence_base_repositorygeneric, infraestructure_persistence_context_appdbcontext_persistence_context_appdbcontext, infraestructure_adapters_mapper_automapperadapter_adapters_mapper_automapperadapter, api_api_controllers_apicontrollerbase_api_controllers_apicontrollerbase_toresult [EXTRACTED 1.00]
- **Composición por capas** — api_api_program, core_application_applicationserviceregistration_application_applicationserviceregistration_addapplicationservices, infraestructure_adapters_adapterserviceregistration_adapters_adapterserviceregistration_addadapterservices, infraestructure_persistence_persistenceserviceregistration_persistence_persistenceserviceregistration_addpersistenceservices, infraestructure_infraestructure_dependencyinjection_dependencyinjectionhelper_infraestructure_dependencyinjection_dependencyinjectionhelper [EXTRACTED 1.00]
- **Quality gate y publicación de la API** — azure_pipelines_continuous_integration_pipeline, azure_pipelines_net10_release_quality_gate, azure_pipelines_artifactdrop, api_api_api [EXTRACTED 1.00]

## Communities (21 total, 0 thin omitted)

### Community 0 - "ResponseBase"
Cohesion: 0.07
Nodes (26): DemoArchitecturesController, CancellationToken, IActionResult, Task, ApiControllerBase, ResponseBase, GetDemoArchitectureByIdQuery, CancellationToken (+18 more)

### Community 1 - "Domain.Entities"
Cohesion: 0.10
Nodes (17): DemoArchitectureDto, Guid, Application.Dtos, Domain.Base.Interface, Test.Application, Domain.Entities, Persistence.Base, Domain.Contracts.Adapter.Mapper (+9 more)

### Community 2 - "Domain.Base"
Cohesion: 0.09
Nodes (17): IReadOnlyDictionary, DomainException, IReadOnlyDictionary, DomainValidation, Guid, EntityBase, DemoArchitectureEntity, Domain.Base (+9 more)

### Community 3 - "API - perfiles de ejecucion"
Cohesion: 0.10
Nodes (21): applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, launchUrl, ASPNETCORE_ENVIRONMENT, commandName (+13 more)

### Community 4 - "RepositoryGeneric"
Cohesion: 0.13
Nodes (12): DbContext, CancellationToken, DbSet, Expression, Func, Guid, IEnumerable, Task (+4 more)

### Community 5 - ".AddPersistenceServices"
Cohesion: 0.12
Nodes (12): ApplicationServiceRegistration, IServiceCollection, Adapters.Mapper, Application, IConfiguration, AutoConventionProfile, AutoMapperAdapter, IEnumerable (+4 more)

### Community 6 - "Domain - puerto IRepositoryGeneric"
Cohesion: 0.18
Nodes (8): CancellationToken, Expression, Func, Guid, IEnumerable, Task, IRepositoryGeneric, Reutilización del repositorio genérico

### Community 7 - "Program.cs"
Cohesion: 0.15
Nodes (12): ApiKeyAuthHandler, ApiKeyDefaults, string, Task, ApiKeyOptions, Program, AuthenticateResult, AuthenticationHandler (+4 more)

### Community 8 - ".Handle"
Cohesion: 0.13
Nodes (12): ValidationBehavior, CancellationToken, Task, Application.Behaviors, IPipelineBehavior, IRequest, Use case CQRS: Request, Validator, Query o Command y Response, RequestHandlerDelegate (+4 more)

### Community 9 - ".TryHandleAsync"
Cohesion: 0.14
Nodes (11): ApiControllerBase, IActionResult, GlobalExceptionHandler, CancellationToken, Exception, ControllerBase, Api.Controllers, HttpContext (+3 more)

### Community 10 - "Api.csproj"
Cohesion: 0.15
Nodes (12): net10.0, FluentValidation (12.1.1), Publicación de artifactDrop, Pipeline CI de la plantilla, .NET 10 Release: restore auditado, build, test y formato, Asp.Versioning.Mvc (10.0.0), Asp.Versioning.Mvc.ApiExplorer (10.0.0), Microsoft.AspNetCore.Authentication.JwtBearer (10.0.10) (+4 more)

### Community 11 - ".AddAssemblyServices"
Cohesion: 0.18
Nodes (8): Assembly, Infraestructure.DependencyInjection, Adapters, AdapterServiceRegistration, IServiceCollection, IServiceCollection, DependencyInjectionHelper, ServiceLifetime

### Community 12 - ".BuildProvider"
Cohesion: 0.25
Nodes (6): IEnumerable, IMapperAdapter, ServiceProvider, Fact, Task, ApplicationServiceRegistrationTests

### Community 13 - "GetDemoArchitectureByIdValidator"
Cohesion: 0.24
Nodes (7): AbstractValidator, AsyncValidationRequest, GetDemoArchitectureByIdValidator, AsyncValidationRequestValidator, Fact, Task, GetDemoArchitectureByIdValidatorTests

### Community 14 - "Tests - proyecto y herramientas"
Cohesion: 0.22
Nodes (8): coverlet.collector (10.0.1), FluentAssertions (8.10.0), Microsoft.NET.Test.Sdk (18.8.1), Moq (4.20.72), xunit.runner.visualstudio (3.1.5), xunit.v3 (3.2.2), net10.0, Microsoft.NET.Sdk

### Community 15 - "Persistence - dependencias EF Core"
Cohesion: 0.25
Nodes (7): net10.0, Microsoft.Extensions.DependencyInjection.Abstractions (10.0.10), Microsoft.NET.Sdk, Microsoft.EntityFrameworkCore (10.0.10), Microsoft.EntityFrameworkCore.Relational (10.0.10), Microsoft.EntityFrameworkCore.SqlServer (10.0.10), Microsoft.Extensions.Configuration.Abstractions (10.0.10)

### Community 16 - "Application - dependencias CQRS"
Cohesion: 0.29
Nodes (6): net10.0, FluentValidation (12.1.1), Microsoft.Extensions.DependencyInjection.Abstractions (10.0.10), Microsoft.NET.Sdk, FluentValidation.DependencyInjectionExtensions (12.1.1), MediatR (14.2.0)

### Community 17 - "Domain e Infrastructure - proyectos base"
Cohesion: 0.29
Nodes (5): net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.Extensions.DependencyInjection.Abstractions (10.0.10), Microsoft.NET.Sdk

### Community 18 - "GetDemoArchitectureByIdQueryTests"
Cohesion: 0.47
Nodes (4): Once pruebas automatizadas, Fact, Task, GetDemoArchitectureByIdQueryTests

### Community 20 - "Adapters.csproj"
Cohesion: 0.40
Nodes (4): net10.0, Microsoft.Extensions.DependencyInjection.Abstractions (10.0.10), Microsoft.NET.Sdk, AutoMapper (16.2.0)

## Knowledge Gaps
- **56 isolated node(s):** `net10.0`, `Asp.Versioning.Mvc (10.0.0)`, `Asp.Versioning.Mvc.ApiExplorer (10.0.0)`, `FluentValidation (12.1.1)`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.10)` (+51 more)
  These have ≤1 connection - possible missing edges or undocumented components.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `GetDemoArchitectureByIdRequest` connect `ResponseBase` to `.Handle`?**
  _High betweenness centrality (0.001) - this node is a cross-community bridge._
- **What connects `net10.0`, `Asp.Versioning.Mvc (10.0.0)`, `Asp.Versioning.Mvc.ApiExplorer (10.0.0)` to the rest of the system?**
  _58 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `ResponseBase` be split into smaller, more focused modules?**
  _Cohesion score 0.0743321718931475 - nodes in this community are weakly interconnected._
- **Should `Domain.Entities` be split into smaller, more focused modules?**
  _Cohesion score 0.0967741935483871 - nodes in this community are weakly interconnected._
- **Should `Domain.Base` be split into smaller, more focused modules?**
  _Cohesion score 0.09057971014492754 - nodes in this community are weakly interconnected._
- **Should `API - perfiles de ejecucion` be split into smaller, more focused modules?**
  _Cohesion score 0.09523809523809523 - nodes in this community are weakly interconnected._
- **Should `RepositoryGeneric` be split into smaller, more focused modules?**
  _Cohesion score 0.12987012987012986 - nodes in this community are weakly interconnected._
