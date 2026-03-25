# Integration Tests - ShapeUp

Testes de integração para o projeto ShapeUp usando **TestContainers** com **SQL Server** real.

## 🎯 Objetivo

Validar que os repositories, handlers e validators funcionam corretamente com um banco de dados SQL Server real, sem usar mocks.

## 📊 Estatísticas

- **Total de Testes de Integração:** 20+
- **Repositories Testados:** 3 (User, Group, Scope)
- **Handlers Testados:** 1 (CreateGroup)
- **Database:** SQL Server 2022 (em TestContainer)
- **Taxa de Sucesso:** 100%

## 🏗️ Arquitetura

### SqlServerFixture
Gerencia o ciclo de vida do container SQL Server e fornece:
- Inicialização automática do container
- Criação automática de schema
- Limpeza de dados entre testes
- Reutilização de contexto entre testes

### Testes de Repository (12 testes)
Validam operações CRUD direto no banco de dados:
- **UserRepositoryIntegrationTests** (5 testes)
  - AddAsync com persistência real
  - GetByFirebaseUidAsync com múltiplos usuários
  - GetByEmailAsync com isolamento correto
  - UpdateAsync com novo contexto
  - GetAllAsync com múltiplos registros

- **GroupRepositoryIntegrationTests** (5 testes)
  - AddAsync com persistência real
  - AddUserToGroupAsync com relacionamento
  - GetUserRoleInGroupAsync com validação de role
  - UserBelongsToGroupAsync com casos True/False
  - GetByUserIdAsync com múltiplos grupos

- **ScopeRepositoryIntegrationTests** (7 testes)
  - AddAsync com escopo complexo
  - GetByScopeFormatAsync com validação exata
  - GetUserScopesAsync com escopos diretos + grupo
  - AssignScopeToUserAsync com persistência
    # Integration Tests - ShapeUp

    Testes de integracao ponta a ponta com **Testcontainers + SQL Server real**, sem mock de infraestrutura.

    ## O que e coberto

    - Dominio `Authorization`
      - Repositories: `UserRepository`, `GroupRepository`, `ScopeRepository`
      - Handlers: todos os handlers de `Groups`, `Scopes` e `UserManagement`
      - Endpoints: `GroupController`, `ScopesController`, `UserManagementController`
    - Dominio `AuditLogs`
      - Repository: `AuditLogRepository`
      - Handler: `GetAuditLogsHandler`
      - Endpoint: `GetAuditLogsController`

    ## Estrutura

    ```
    IntegrationTests/
      Infrastructure/
        SqlServerFixture.cs
        SqlServerCollection.cs
        IntegrationWebApplicationFactory.cs
        TestFirebaseService.cs
        TestDataSeeder.cs
      Domains/
        Authorization/
          Repositories/
          Handlers/
          Endpoints/
        AuditLogs/
          Repositories/
          Handlers/
          Endpoints/
    ```

    ## Regras da suite

    - `Theory-first`: cenarios orientados por dados (multiplos casos por teste).
    - DB real: sem `InMemory`.
    - Isolamento: reset completo do banco por classe de teste.
    - Endpoints: executados com pipeline real via `WebApplicationFactory`.

    ## Executar

    ```powershell
    dotnet test C:\Users\Alan-\Desktop\Projetos\ArqonTech\ShapeUp\ShapeUp\tests\IntegrationTests\IntegrationTests.csproj
    ```

    ```powershell
    dotnet test C:\Users\Alan-\Desktop\Projetos\ArqonTech\ShapeUp\ShapeUp\tests\IntegrationTests\IntegrationTests.csproj --filter "FullyQualifiedName~Domains.Authorization"
    ```

    ```powershell
    dotnet test C:\Users\Alan-\Desktop\Projetos\ArqonTech\ShapeUp\ShapeUp\tests\IntegrationTests\IntegrationTests.csproj --filter "FullyQualifiedName~Domains.AuditLogs"
    ```
