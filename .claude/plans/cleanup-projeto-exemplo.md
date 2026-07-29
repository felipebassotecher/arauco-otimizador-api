# Plano: Limpar diretórios órfãos do projeto de exemplo

## Contexto

O repositório foi derivado de um projeto de exemplo. Você já migrou para a arquitetura de **única WebApi**
(`Arauco.Otimizador.WebApi/`) e está removendo os módulos legados. O estado atual do git mostra isso em andamento:
6 projetos WebApi "split" já foram apagados do disco (constam como `D` não-stageado), e 11 diretórios
órfãos ainda existem em disco **e** no git, mas **fora da `.sln`** e **não referenciados** por nenhum projeto
que será mantido.

### Verificações feitas (seguras para prosseguir)
- A `.sln` referencia 33 projetos; nenhum deles referencia qualquer um dos 11 órfãos.
- Scan de código-fonte nos projetos mantidos (WebApi, WebApi.Base, Common.Domain, Data.Entities, Data.MySql,
  Service.CartaoService/CenarioService/DemandaService/ParametroService, Function.EmailOutbound): **zero**
  referências a namespaces dos órfãos.
- Única referência "externa" a um órfão: `deploy_api.sh` faz `cd Arauco.Otimizador.Function.Cognito`
  (órfão). Como você quer **manter** os scripts de deploy, vou remover apenas o bloco do Cognito desse script.

## Escopo (decidido com você)

**Remover** — 11 diretórios órfãos (em disco + rastreados no git, fora da .sln, não referenciados):
1. `Arauco.Otimizador.DataApi` (lib de integração de dados)
2. `Arauco.Otimizador.Function.Cognito`
3. `Arauco.Otimizador.Function.DataSource`
4. `Arauco.Otimizador.Function.Florestal`
5. `Arauco.Otimizador.Function.Senior`
6. `Arauco.Otimizador.Service.AuthService`
7. `Arauco.Otimizador.Service.CognitoService`
8. `Arauco.Otimizador.Service.ColaboradorService`
9. `Arauco.Otimizador.Service.ContaService`
10. `Arauco.Otimizador.Service.LogService`
11. `Arauco.Otimizador.Service.UsuarioService`

**Stagear** — 6 projetos WebApi "split" já apagados do disco (deleções `D` pendentes no git):
- `Arauco.Otimizador.WebApi.AuthApi`
- `Arauco.Otimizador.WebApi.Cartao` (antigo; a WebApi unificada já o substitui)
- `Arauco.Otimizador.WebApi.ColaboradorApi`
- `Arauco.Otimizador.WebApi.ContaApi`
- `Arauco.Otimizador.WebApi.DataApi`
- `Arauco.Otimizador.WebApi.WebHook`

**Mantém** (conforme sua escolha): `Database/` (scripts SQL legados), `Arauco.Otimizador.Function.Test/`,
scripts raiz de deploy/CFN (`deploy_api.sh`, `buildspec.yml`, `start_deploy_*.sh`, `setup.yml`,
`resources*.yml`, `build.sh`), `Docs/`.

## Passos

### 1. Build de baseline
`dotnet build arauco-otimizador-api.sln --configuration Debug` — registrar estado atual (compila ou não).
Isso é só referência; se houver erros pré-existentes das suas modificações em andamento, serão reportados
mas não são causados por esta limpeza.

### 2. Stagear as 6 deleções pendentes dos WebApi split
Já estão fisicamente apagadas. Stagear com `git add -A` sobre esses caminhos (ou `git add` de cada path)
para registrar a remoção no índice. Sem commit.

### 3. Remover os 11 órfãos do disco e do git
`git rm -r <dir>` para cada um dos 11. Isso apaga do disco **e** stagea a remoção. Sem commit.

### 4. Ajustar `deploy_api.sh` (mantido, mas referencia órfão)
Remover o bloco "Functions" que faz `cd Arauco.Otimizador.Function.Cognito` e o
`dotnet lambda deploy-serverless ... stack-cognito-function` (linhas ~26-42), pois `Function.Cognito`
está sendo removido. Manter o bloco "WebApi" e o bloco "Resources" intactos.

### 5. Build de verificação
`dotnet build arauco-otimizador-api.sln --configuration Debug` — deve manter o **mesmo** resultado do
baseline (os órfãos não faziam parte do build da .sln, então a compilação da solução não muda).
Reporto o resultado fielmente.

### 6. (Não commitar)
Conforme política, não farei commit. Deixo as remoções stageadas para você revisar com `git status` e
commitar quando quiser (posso fazer o commit se você pedir).

## Fora de escopo (não tocar)
- Suas modificações em andamento (IUnitOfWork, DbContext, UnitOfWork, TipoLogEnum, IAuthService,
  CartaoService, BaseController, AppUserIdentity, etc.) — são seu WIP.
- `CLAUDE.md` (em estado modificado; seu documento).
- Repositórios/entidades possivelmente "órfãos" dentro de IUnitOfWork/DbContext (ex.: UsuarioRepository,
  ColaboradorRepository) — viram código morto após a remoção dos Services, mas **não quebram o build**
  (as entidades continuam em Data.Entities). Limpeza disso fica para uma etapa futura se você quiser.
- `.vs/` (artefatos do Visual Studio) — não é rastreado pelo git, é local da IDE.

## Riscos
- **Baixo**: os órfãos não são referenciados por nada que fica, então a remoção não altera o build da .sln.
- `deploy_api.sh` perderá a etapa de deploy do Cognito — é o esperado, já que a função foi removida.