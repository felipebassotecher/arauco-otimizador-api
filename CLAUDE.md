# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Arauco Otimizador API** is a serverless microservices application built with ASP.NET Core (.NET 10.0) running on AWS Lambda. This repository is used as a .NET base project containing reusable structures and existing implementations that can be adapted for new projects.

The current code still carries domain-specific implementations (Auth, Cartão, Colaborador, Conta, Data API, Flow/WebHook, Functions, etc.) that are planned to be reviewed and removed in a later cleanup. The base infrastructure, common libraries, and AWS helpers are kept intact.

## Build and Run Commands

```bash
# Build entire solution
dotnet build arauco-otimizador-api.sln --configuration Debug

# Run a WebApi locally (uses LocalEntryPoint.cs with Kestrel)
cd Arauco.Otimizador.WebApi.AuthApi
dotnet run

# Build for release/deployment
dotnet build arauco-otimizador-api.sln --configuration Release

# Install Lambda deployment tools
dotnet tool install -g Amazon.Lambda.Tools

# Deploy individual API via SAM (requires AWS credentials)
dotnet lambda deploy-serverless --configuration Release --framework net10.0 --region sa-east-1 --template serverless.yml
```

## Architecture

### Layered Structure
```
Controllers (WebApi) → Services → UnitOfWork → Repositories → DbContext → MySQL
```

**Project Organization:**
- `Arauco.Otimizador.Common.*` - Domain models, enums, events, session, email/pdf helpers
- `Arauco.Otimizador.Data.*` - Entity definitions, EF Core DbContexts, repositories, DynamoDB docs
- `Arauco.Otimizador.Service.*` - Business logic layer, all inherit from `ServiceBase`
- `Arauco.Otimizador.WebApi.*` - HTTP API endpoints as Lambda functions
- `Arauco.Otimizador.Function.*` - Event-driven Lambda functions (email, Cognito triggers, etc.)
- `Arauco.Otimizador.Aws.*` - AWS service integrations (CloudFront, shared resources)
- `Techer.Common.*` - Company-wide reusable common libraries (kept unchanged)
- `Techer.Aws.*` - Company-wide reusable AWS helpers (kept unchanged)
- `Techer.Data.MySql` - Generic MySQL repository and DbContext utilities (kept unchanged)

### Key Patterns

**Unit of Work:** `ISeniorUnitOfWork` and `IUnitOfWork` manage transactions with generic repositories
```csharp
await unitOfWork.ColaboradorRepository.Add(entity);
await unitOfWork.SaveAsync();
```

**Service Base Class:** All services inherit from `ServiceBase` with injected `IUnitOfWork` and `IEnvironmentVariables`

**Dual Entry Points:** Each WebApi has `LambdaEntryPoint.cs` (AWS) and `LocalEntryPoint.cs` (local development)

**Environment Variables:** Access via `IEnvironmentVariables` interface with `IsLocal()`, `IsDevelopment()`, `IsProduction()` methods

### Database Contexts
- `SeniorDbContext` - Senior HR system entities (Colaborador, Cargo, etc.)
- `HubDbContext` - Hub/Otimizador-specific entities (Cartao, etc.)

Database credentials come from AWS Secrets Manager via `UseMySqlWithSecrets()` extension.

## AWS Infrastructure

- **Lambda:** All APIs and functions run as Lambda functions with runtime `dotnet10`
- **API Gateway:** HTTP API routes to Lambda
- **Cognito:** User authentication and management
- **DynamoDB:** Key-value storage (`OtimizadorKeyValue`), logging (`OtimizadorLog`), workflows (`OtimizadorFlow`)
- **S3:** File storage (`arauco-otimizador-{ENV}`, `arauco-otimizador-temp-{ENV}`)
- **SQS:** Async email processing
- **Secrets Manager:** Database credentials and API keys

## Deployment

Deployment uses AWS SAM with CloudFormation. Each WebApi/Function has its own `serverless.yml`.

```bash
# Set environment
export AWS_REGION=sa-east-1
export ENVIRONMENT=dev  # dev, test, or prod
export APIDOMAIN=api.otimizador.arauco.app.br

# Deploy all APIs
./deploy_api.sh

# Deploy infrastructure
./start_deploy_setup.sh
```

**CloudFormation templates:**
- `setup.yml` - VPC, subnets, security groups, Route53
- `resources.yml` - DynamoDB tables, S3 buckets, IAM
- `resources_cognito.yml` - Cognito user pools

## Domain Enumerations

**CartaoTipoEnum:** 1=Segurança, 2=Excelência/Inovação, 3=Trabalho em Equipe, 4=Bom Cidadão, 5=Compromisso

**TipoColaboradorEnum:** 1=Empregado, 2=Terceiro

**AplicacaoEnum:** 1=Hub

## Database

SQL migration scripts in `Database/Scripts/`:
1. `01_CreateTables.sql` - Schema creation
2. `02_InitialData.sql` - Initial seed data
3. `03_SampleData.sql` - Test data (optional)
4. `04_CreateViews.sql` - Database views
5. `05_StoredProcedures.sql` - Stored procedures

Key tables: Colaborador, Usuario, Cartao, Documento, AssinaturaDocumento, Perfil, Regra
