# Plano: Trocar a conexão com o banco de AWS SecretsManager → local (appsettings)

## Contexto

A conexão MySQL é obtida via **AWS SecretsManager** em dois lugares:
1. **WebApi (runtime):** `DbContext.OnConfiguring` → `OptionsHelper.UseMySqlWithSecrets()` →
   `Techer.Aws.Secrets.SecretsHelper.GetSecret("DefaultDb")` → monta a connection string.
2. **Deployment.Database (migrações DbUp):** `Program.cs` → `SecretsHelper.GetSecret(...)`.

Objetivo: ler a connection string de um **appsettings local** (`ConnectionStrings:DefaultConnection`),
sem depender da AWS. Manter as abstrações DbContext/UnitOfWork/Repository intactas — só troca a
**fonte da credencial**.

Escopo confirmado: **WebApi + migrações**. Connection string padrão: `localhost + root/root`.

## Achado pré-existente (bloqueia o build do Deployment.Database)
O `.csproj` referencia `Scripts\Script001 - Cartao.sql` e `Scripts\Script002 - Otimizador.sql`,
mas no working tree esses dois foram **deletados** e existe só `Scripts\Script001 - Otimizador.sql`
(novo, não rastreado). Hoje o `dotnet build` do Deployment.Database falha com `CS1566` (recurso
embutido não encontrado). Vou apontar o `EmbeddedResource` para o arquivo que existe.

## Alterações

### 1. `Arauco.Otimizador.Data.MySql/OptionsHelper.cs` — fonte: appsettings, não Secrets
Substituir toda a lógica de `SecretsHelper`/`Newtonsoft.Json` por leitura de `IConfiguration`:
```csharp
public const string ConnectionStringName = "DefaultConnection";

public static string GetConnectionString(IConfiguration config)
{
    var connString = config.GetConnectionString(ConnectionStringName);
    if (string.IsNullOrWhiteSpace(connString))
        throw new InvalidOperationException(
            $"Connection string '{ConnectionStringName}' ausente. " +
            $"Configure 'ConnectionStrings:{ConnectionStringName}' no appsettings.json.");
    return connString;
}

public static DbContextOptionsBuilder UseMySqlLocal(
    this DbContextOptionsBuilder options, IConfiguration config)
{
    var connString = GetConnectionString(config);
    return options.UseMySql(connString, ServerVersion.AutoDetect(connString));
}
```
- Remover `enum MySqlSecretOption`, `UseMySqlWithSecrets`, `GetConnectionString()` antigo,
  `GetSecretName`.
- Remover usings `Techer.Aws.Secrets` e `Newtonsoft.Json`.
- Manter comportamento "lazy" (AutoDetect por instância), igual ao original.

### 2. `Arauco.Otimizador.Data.MySql/DbContext.cs`
- Remover o override `OnConfiguring` (que chamava `UseMySqlWithSecrets()`). A conexão passa a ser
  configurada via DI no `Startup`.
- Remover o `static DbContext Create()` (morto, dependia de `OnConfiguring`; sem provider quebraria).
- Manter `DbSet`s, `OnModelCreating` e o construtor `(DbContextOptions<DbContext>)` intactos.

### 3. `Arauco.Otimizador.Data.MySql/Arauco.Otimizador.Data.MySql.csproj`
- Remover `<ProjectReference Include="..\Techer.Aws.Secrets\..." />` (não usado mais).
- Adicionar `<PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.10" />`
  (necessário p/ `IConfiguration` em `OptionsHelper`).

### 4. `Arauco.Otimizador.WebApi/Startup.cs`
Trocar:
```csharp
services.AddDbContext<DbContext>();
```
por:
```csharp
services.AddDbContext<DbContext>(options => options.UseMySqlLocal(Configuration));
```
(usa `Arauco.Otimizador.Data.MySql.OptionsHelper`).

### 5. `Arauco.Otimizador.WebApi/appsettings.json`
Adicionar:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=otimizador;Uid=root;Pwd=root;CharSet=utf8;Connection Timeout=30"
}
```
(Base — vale para qualquer ambiente local. Edite conforme seu MySQL.)

### 6. `Arauco.Otimizador.Deployment.Database/Program.cs` — ler appsettings local
Trocar `SecretsHelper` por `ConfigurationBuilder` lendo `appsettings.json` + env vars, e obter a
connection string via `OptionsHelper.GetConnectionString(config)`. Remover `using Techer.Aws.Secrets`
e `using Newtonsoft.Json`. O restante do DbUp (`DeployChanges.To.MySqlDatabase(connString)...`)
permanece.

### 7. `Arauco.Otimizador.Deployment.Database/` — novo `appsettings.json`
Criar `appsettings.json` com a mesma `ConnectionStrings:DefaultConnection`, marcado
`CopyToOutputDirectory` (para o exe achar ao rodar).

### 8. `Arauco.Otimizador.Deployment.Database/Arauco.Otimizador.Deployment.Database.csproj`
- Trocar os dois `EmbeddedResource` inexistentes por:
  `<EmbeddedResource Include="Scripts\Script001 - Otimizador.sql" />`
- Remover `<ProjectReference Include="..\Techer.Aws.Secrets\..." />`.
- Adicionar `<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.10" />`
  (para `ConfigurationBuilder`/`AddJsonFile` no Program).

## O que NÃO muda
- `UnitOfWork`, `GenericRepository`, `IUnitOfWork`, entidades, `DbContext.OnModelCreating`,
  controllers, services — tudo intacto. Só a origem da connection string muda.
- O projeto `Techer.Aws.Secrets` continua na solution (helper company-wide, mantido intacto),
  apenas deixa de ser referenciado por Data.MySql e Deployment.Database.

## Achado extra (não resolvido aqui — só registro)
`Script001 - Otimizador.sql` (consolidado) **não cria a tabela `Cartao`** — só Cenario/Parametro/
ParametroValor/CenarioParametro/Demanda/Pedido. Mas o `DbContext` tem `DbSet<Cartao>`. Se a tabela
`Cartao` não existir no banco local, qualquer operação de Cartão falha em runtime. Como você deletou
o `Script001 - Cartao.sql`, decida se quer recriá-lo (posso restaurar do git). É decisão de schema,
não de conexão — por isso deixei fora do escopo. Me avise se quiser incluir.

## Verificação
1. `dotnet build Arauco.Otimizador.Data.MySql/...csproj` (biblioteca) → 0 erros.
2. `dotnet build Arauco.Otimizador.Deployment.Database/...csproj` → 0 erros (corrige o CS1566).
3. `dotnet build arauco-otimizador-api.sln --configuration Debug` (com a WebApi parada) → 0 erros.
4. (Opcional) rodar `Deployment.Database` contra o MySQL local p/ aplicar o schema; rodar a WebApi e
   chamar um endpoint que use o banco.