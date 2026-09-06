# Native Authorization Model Tasks

## Execution Protocol (MANDATORY -- do not skip)

Implement these tasks with the `tlc-spec-driven` skill: **activate it by name and follow its Execute flow and Critical Rules.** Do not search for skill files by filesystem path. The skill is the source of truth for the full flow (per-task cycle, sub-agent delegation, adequacy review, Verifier, discrimination sensor).

**If the skill cannot be activated, STOP and tell the user — do not proceed without it.**

---

**Design**: `.specs/features/native-authorization-model/design.md`
**Status**: Draft — aguardando confirmação do usuário (ver seção de tools/skills no fim)

---

## Test Coverage Matrix

> Gerado a partir de `src/AGENTS.md` (guidelines do próprio projeto) + amostragem de `tests/UnitTests` e `tests/IntegrationTests`. Guidelines encontradas: `src/AGENTS.md` (Result Pattern, CQRS, FluentValidation, CancellationToken mandatórios; nenhum threshold de cobertura numérico definido — aplicado default forte para lacunas). Nenhum workflow de CI encontrado (`find . -iname "*.yml"` só retorna `docker-compose.yml`) — os comandos abaixo rodam localmente até CI existir.

| Code Layer | Required Test Type | Coverage Expectation | Location Pattern | Run Command |
|---|---|---|---|---|
| Domain/business-logic (`CapabilityResolver`, `CapabilityAuthorizationHandler`, máquina de estado de `ProfessionalCredential`) | unit | Todos os branches; 1:1 com ACs AUTHZ-01 a AUTHZ-14; todo edge case listado no spec tem teste | `tests/UnitTests/Domains/Authorization/Resolver/*.cs`, `tests/UnitTests/Domains/Credentials/*.cs` | `dotnet test tests/UnitTests/UnitTests.csproj` |
| Repository/adapter (`IOrganizationMembershipRepository`, `IEntitlementRepository`, `IProfessionalCredentialRepository`, `IProfessionalClientRelationshipRepository`) | unit + integration | Unit: principais caminhos de query + tratamento de erro (padrão já usado em `AuditLogRepositoryTests.cs`); Integration: contra SQL Server real via Testcontainers | `tests/UnitTests/Domains/{Credentials,Relationships,Memberships,Entitlements}/*.cs`, `tests/IntegrationTests/Domains/{...}/Repositories/*.cs` | `dotnet test tests/UnitTests/UnitTests.csproj` / `dotnet test tests/IntegrationTests/IntegrationTests.csproj` |
| Controller/endpoint (GymManagement e Training migrados para `[Authorize(Policy=...)]`) | integration | Toda rota migrada: happy path + AUTHZ-04 (deny sem membership) + AUTHZ-05 (deny em falha de infra) + edge cases do spec (cross-gym, relacionamento encerrado) | `tests/IntegrationTests/Domains/{GymManagement,Training}/Endpoints/*.cs` | `dotnet test tests/IntegrationTests/IntegrationTests.csproj` |
| Entity/Config (`ProfessionalCredential`, `ProfessionalClientRelationship`, migrations EF, registro de Policies na DI) | none | Build gate apenas | — | `dotnet build` |

## Parallelism Assessment

> Gerado a partir de `tests/IntegrationTests/Infrastructure/SqlServerWriteCollection.cs` / `SqlServerReadCollection.cs` (coleções já separadas no repo por escrita vs leitura).

| Test Type | Parallel-Safe? | Isolation Model | Evidence |
|---|---|---|---|
| Unit (Moq + EF InMemory) | Yes | Cada teste usa `DbContext` InMemory isolado por instância/nome de banco | Padrão em `tests/UnitTests/Domains/AuditLogs/AuditLogRepositoryTests.cs` |
| Integration — leitura (`SqlServerReadCollection`) | Yes | Container/schema compartilhado, mas testes só leem, sem mutação cruzada | `tests/IntegrationTests/Infrastructure/SqlServerReadCollection.cs` |
| Integration — escrita (`SqlServerWriteCollection`) | No | Container/schema compartilhado com mutação — coleção já existe para serializar esses testes | `tests/IntegrationTests/Infrastructure/SqlServerWriteCollection.cs` |

## Gate Check Commands

| Gate Level | When to Use | Command |
|---|---|---|
| Quick | Após tasks só com unit tests | `dotnet test tests/UnitTests/UnitTests.csproj` |
| Full | Após tasks com integration/e2e | `dotnet test tests/UnitTests/UnitTests.csproj && dotnet test tests/IntegrationTests/IntegrationTests.csproj` |
| Build | Após task de entity/config/migration | `dotnet build src/ShapeUp.slnx` |

---

## Execution Plan

### Phase 1: Foundation — schema novo + resolver (Sequential)

```
T1 → T2 → T3 → T4 → T5 → T6 → T7
```

### Phase 2: GymManagement migrado (Parallel OK após T7)

```
T7 ──┬→ T8  [P]
     ├→ T9  [P]
     ├→ T10 [P]
     ├→ T11 [P]
     ├→ T12 [P]
     ├→ T13 [P]
     ├→ T14 [P]
     └→ T15 [P]
              └──→ T16 (integration cross-gym, depende de T8-T15)
```

### Phase 3: Training migrado (Parallel OK após T7; independente da Phase 2)

```
T7 ──┬→ T17 [P]
     ├→ T18 [P]
     ├→ T19 [P]
     ├→ T20 [P]
     ├→ T21 [P]
     ├→ T22 [P]
     └→ T23 [P]
              └──→ T24 (integration relacionamento treinador-cliente, depende de T17-T23)
```

### Phase 4: Descomissionamento do legado (Sequential, depende de T16 e T24)

```
T16, T24 → T25 → T26 → T27
```

---

## Task Breakdown

### T1: Criar entidades e `DbContext` de `Features/Credentials`

**What**: Criar `ProfessionalCredential` (com enum `CredentialStatus`) + `CredentialsDbContext` + migration inicial
**Where**: `src/Features/Credentials/Shared/Entities/ProfessionalCredential.cs`, `src/Features/Credentials/Shared/Data/CredentialsDbContext.cs`, `src/Features/Credentials/Shared/Data/Migrations/`
**Depends on**: None
**Reuses**: Padrão de `AuditLogsDbContext` (DbContext isolado por feature)
**Requirement**: AUTHZ-12

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Entidade e enum criados conforme Data Models do design
- [ ] Migration inicial gerada e aplica sem erro (`dotnet ef database update` local)
- [ ] Gate check passa: `dotnet build ShapeUp.slnx`

**Tests**: none (entity/config)
**Gate**: build

**Commit**: `feat(credentials): create ProfessionalCredential entity and DbContext`

---

### T2: Repositório + máquina de estado de `ProfessionalCredential` [depende de T1]

**What**: Implementar `IProfessionalCredentialRepository` (`GetVerifiedAsync`) + guard de transição de estado (`CredentialStatus`) que rejeita transições fora de `DRAFT→SUBMITTED→UNDER_REVIEW→VERIFIED→REJECTED`, `VERIFIED→EXPIRED/SUSPENDED/REVOKED`
**Where**: `src/Features/Credentials/Shared/Abstractions/IProfessionalCredentialRepository.cs`, `src/Features/Credentials/Infrastructure/Repositories/ProfessionalCredentialRepository.cs`, `src/Features/Credentials/Shared/StateMachine/CredentialStatusGuard.cs`
**Depends on**: T1
**Reuses**: nada existente — domínio novo
**Requirement**: AUTHZ-12, AUTHZ-13

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] `GetVerifiedAsync` retorna `null` se status != VERIFIED ou `expires_at` no passado (edge case de expiração)
- [ ] Guard rejeita toda transição não listada (teste cobre pelo menos uma transição inválida por estado)
- [ ] Gate check passa: `dotnet test tests/UnitTests/UnitTests.csproj`
- [ ] Test count: cobre todas as transições válidas + pelo menos 3 inválidas + o edge case de expiração (mínimo 8 testes)

**Tests**: unit
**Gate**: quick

**Commit**: `feat(credentials): add repository and state transition guard`

---

### T3: Criar entidade e `DbContext` de `Features/Relationships`

**What**: Criar `ProfessionalClientRelationship` (com enum `RelationshipStatus`) + `RelationshipsDbContext` + migration com índice único filtrado (`Active` por `ProfessionalUserId+ClientUserId+RelationshipType`)
**Where**: `src/Features/Relationships/Shared/Entities/ProfessionalClientRelationship.cs`, `src/Features/Relationships/Shared/Data/RelationshipsDbContext.cs`, `src/Features/Relationships/Shared/Data/Migrations/`
**Depends on**: None
**Reuses**: Padrão de `AuditLogsDbContext`
**Requirement**: AUTHZ-09

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Entidade e enum criados conforme Data Models do design
- [ ] Índice único filtrado por `Status = Active` presente na migration (previne duplicata concorrente — edge case do spec)
- [ ] Gate check passa: `dotnet build ShapeUp.slnx`

**Tests**: none (entity/config)
**Gate**: build

**Commit**: `feat(relationships): create ProfessionalClientRelationship entity and DbContext`

---

### T4: Repositório de `Features/Relationships` [depende de T3] [P]

**What**: Implementar `IProfessionalClientRelationshipRepository.GetActiveAsync` (retorna `null` se `ended_at` preenchido) + tratamento do `409` de conflito na criação duplicada
**Where**: `src/Features/Relationships/Shared/Abstractions/IProfessionalClientRelationshipRepository.cs`, `src/Features/Relationships/Infrastructure/Repositories/ProfessionalClientRelationshipRepository.cs`
**Depends on**: T3
**Reuses**: nada existente
**Requirement**: AUTHZ-09

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] `GetActiveAsync` retorna `null` para relacionamento encerrado (edge case)
- [ ] Criação duplicada concorrente retorna `409` mapeado via `Result`/`CommonErrors` (padrão do AGENTS.md)
- [ ] Gate check passa: `dotnet test tests/UnitTests/UnitTests.csproj` e `dotnet test tests/IntegrationTests/IntegrationTests.csproj`
- [ ] Test count: mínimo 5 unit + 2 integration (incluindo o teste de conflito de concorrência)

**Tests**: unit + integration
**Gate**: full

**Commit**: `feat(relationships): add repository with active-relationship lookup`

---

### T5: Adapter `Features/Memberships` sobre `GymManagement` [P]

**What**: Implementar `IOrganizationMembershipRepository.GetMembershipAsync` mapeando `Gym.OwnerId`→`Owner`, `GymStaff.Role`→demais papéis, sem tabela nova (AD-002)
**Where**: `src/Features/Memberships/Shared/Abstractions/IOrganizationMembershipRepository.cs`, `src/Features/Memberships/Infrastructure/OrganizationMembershipAdapter.cs`
**Depends on**: None (lê `GymManagement` existente, que já está pronto)
**Reuses**: `IGymRepository`, `IGymStaffRepository` de `GymManagement` (AD-002)
**Requirement**: AUTHZ-02, AUTHZ-03

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Retorna `Owner` quando `userId == Gym.OwnerId`, papel correspondente quando existe `GymStaff` ativo, `null` quando não há vínculo (edge case de "sem membership")
- [ ] Não introduz nenhuma tabela/migration nova
- [ ] Gate check passa: `dotnet test tests/UnitTests/UnitTests.csproj`
- [ ] Test count: mínimo 4 (owner, cada papel de staff, ausência de membership)

**Tests**: unit
**Gate**: quick

**Commit**: `feat(memberships): add OrganizationMembership adapter over GymManagement`

---

### T6: Adapter `Features/Entitlements` sobre `PlatformTier` [P]

**What**: Implementar `IEntitlementRepository.GetEntitlementAsync` derivando capabilities do `PlatformTier` atribuído; default `Free` quando ausente (AUTHZ-14)
**Where**: `src/Features/Entitlements/Shared/Abstractions/IEntitlementRepository.cs`, `src/Features/Entitlements/Infrastructure/EntitlementAdapter.cs`
**Depends on**: None
**Reuses**: `PlatformTier`/`UserPlatformRole` de `GymManagement` (AD-002)

> ⚠️ Antes de implementar, ler `src/Features/GymManagement/Shared/Entities/PlatformTier.cs` por completo — o design flagou (Risks & Concerns) que o campo de "capabilities concedidas" pode não existir explicitamente; se não existir, mapear tier→capabilities como constante em código e documentar como dívida da Fase 5.

**Requirement**: AUTHZ-14

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Usuário sem `PlatformTier` atribuído recebe entitlement `Free` (edge case AUTHZ-14)
- [ ] Mapeamento tier→capabilities documentado (constante em código ou campo existente, o que for encontrado)
- [ ] Gate check passa: `dotnet test tests/UnitTests/UnitTests.csproj`
- [ ] Test count: mínimo 3 (tier atribuído, tier ausente→Free, tier inexistente/expirado)

**Tests**: unit
**Gate**: quick

**Commit**: `feat(entitlements): add Entitlement adapter over PlatformTier`

---

### T7: `CapabilityResolver` + `CapabilityRequirement` + `CapabilityAuthorizationHandler` [depende de T2, T4, T5, T6]

**What**: Implementar o núcleo de resolução nativa (`ICapabilityResolver.Resolve`), o `IAuthorizationRequirement` e o `IAuthorizationHandler` do ASP.NET Core, registrar Policies na DI (`DependencyInjectionExtensions.cs`), com deny-by-default em falha de qualquer fonte (AUTHZ-05)
**Where**: `src/Features/Authorization/Resolver/{CapabilityRequirement.cs, CapabilityAuthorizationHandler.cs, CapabilityResolver.cs}`, `src/Configurations/DependencyInjectionExtensions.cs` (modify)
**Depends on**: T2, T4, T5, T6
**Reuses**: `UserContext` já populado por `AuthorizationMiddleware`; `AuditLogs` para registrar a decisão (AUTHZ-06)
**Requirement**: AUTHZ-01, AUTHZ-05, AUTHZ-06, AUTHZ-07

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] `Resolve` combina as quatro fontes (Membership, Credential, Relationship, Entitlement) sem nenhuma delas ficar hardcoded/pulada
- [ ] Exceção de qualquer fonte resulta em deny (nunca allow por omissão) — AUTHZ-05
- [ ] Toda decisão (allow e deny) é registrada no `AuditLogs` com motivo quando negada — AUTHZ-06
- [ ] `AuthorizationMiddleware`/fluxo Firebase permanece sem nenhuma mudança de comportamento — AUTHZ-07
- [ ] Gate check passa: `dotnet test tests/UnitTests/UnitTests.csproj`
- [ ] Test count: mínimo 10 (uma combinação allow por fonte, uma deny por fonte ausente, uma deny por falha de infra, uma auditoria registrada)

**Tests**: unit
**Gate**: quick

**Commit**: `feat(authorization): add native CapabilityResolver and policy-based handler`

---

### T8: Migrar `GymsController` para `[Authorize(Policy=...)]` [depende de T7] [P]

**What**: Substituir `[TypeFilter(RequireScopesAttribute, ...)]` por `[Authorize(Policy = "capability:gym.manage")]` (ou policy equivalente por endpoint) em todos os endpoints de `GymsController`; remover qualquer checagem ad-hoc de contexto que existir no handler correspondente
**Where**: `src/Features/GymManagement/Gyms/GymsController.cs` (modify), handlers associados (modify se houver checagem duplicada)
**Depends on**: T7
**Reuses**: Policies registradas em T7
**Requirement**: AUTHZ-01, AUTHZ-02, AUTHZ-04

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Nenhuma referência a `RequireScopesAttribute` restante neste controller
- [ ] Nenhuma checagem manual de membership restante nos handlers deste domínio (Risks & Concerns do design)
- [ ] Gate check passa: `dotnet test tests/IntegrationTests/IntegrationTests.csproj` (endpoints deste controller)
- [ ] Test count: mínimo 1 happy path + 1 deny por endpoint migrado

**Tests**: integration
**Gate**: full

**Commit**: `refactor(gym-management): migrate GymsController to capability policies`

---

### T9: Migrar `GymStaffController` para `[Authorize(Policy=...)]` [depende de T7] [P]

**What**: Mesmo padrão de T8 aplicado a `GymStaffController`; remover `IsOwnerOrReceptionistAsync` de `AddGymStaffHandler`/`RemoveGymStaffHandler` (Risks & Concerns do design aponta este arquivo especificamente)
**Where**: `src/Features/GymManagement/GymStaff/GymStaffController.cs`, `src/Features/GymManagement/GymStaff/AddGymStaff/AddGymStaffHandler.cs`, `src/Features/GymManagement/GymStaff/RemoveGymStaff/RemoveGymStaffHandler.cs`
**Depends on**: T7
**Reuses**: Policies de T7
**Requirement**: AUTHZ-01, AUTHZ-02, AUTHZ-03, AUTHZ-04

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] `IsOwnerOrReceptionistAsync` removido de `AddGymStaffHandler` — autorização decidida só pela policy
- [ ] Gate check passa: `dotnet test tests/IntegrationTests/IntegrationTests.csproj`
- [ ] Test count: mínimo 1 happy path + 1 deny por endpoint + o teste específico de cross-gym (staff de A não gerencia B)

**Tests**: integration
**Gate**: full

**Commit**: `refactor(gym-management): migrate GymStaffController to capability policies`

---

### T10: Migrar `GymPlansController` para `[Authorize(Policy=...)]` [depende de T7] [P]

**What**: Mesmo padrão de T8 aplicado a `GymPlansController`
**Where**: `src/Features/GymManagement/GymPlans/GymPlansController.cs`
**Depends on**: T7
**Reuses**: Policies de T7
**Requirement**: AUTHZ-01, AUTHZ-02, AUTHZ-04

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Nenhuma referência a `RequireScopesAttribute` restante
- [ ] Gate check passa: `dotnet test tests/IntegrationTests/IntegrationTests.csproj`
- [ ] Test count: mínimo 1 happy path + 1 deny por endpoint

**Tests**: integration
**Gate**: full

**Commit**: `refactor(gym-management): migrate GymPlansController to capability policies`

---

### T11: Migrar `GymClientsController` para `[Authorize(Policy=...)]` [depende de T7] [P]

**What**: Mesmo padrão de T8 aplicado a `GymClientsController`
**Where**: `src/Features/GymManagement/GymClients/GymClientsController.cs`, handlers associados se houver checagem duplicada
**Depends on**: T7
**Reuses**: Policies de T7
**Requirement**: AUTHZ-01, AUTHZ-02, AUTHZ-04

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Nenhuma referência a `RequireScopesAttribute` restante
- [ ] Gate check passa: `dotnet test tests/IntegrationTests/IntegrationTests.csproj`
- [ ] Test count: mínimo 1 happy path + 1 deny por endpoint

**Tests**: integration
**Gate**: full

**Commit**: `refactor(gym-management): migrate GymClientsController to capability policies`

---

### T12: Migrar `TrainerPlansController` para `[Authorize(Policy=...)]` [depende de T7] [P]

**What**: Mesmo padrão de T8 aplicado a `TrainerPlansController`
**Where**: `src/Features/GymManagement/TrainerPlans/TrainerPlansController.cs`
**Depends on**: T7
**Reuses**: Policies de T7
**Requirement**: AUTHZ-01, AUTHZ-02, AUTHZ-04

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Nenhuma referência a `RequireScopesAttribute` restante
- [ ] Gate check passa: `dotnet test tests/IntegrationTests/IntegrationTests.csproj`
- [ ] Test count: mínimo 1 happy path + 1 deny por endpoint

**Tests**: integration
**Gate**: full

**Commit**: `refactor(gym-management): migrate TrainerPlansController to capability policies`

---

### T13: Migrar `TrainerClientsController` para `[Authorize(Policy=...)]` [depende de T7] [P]

**What**: Mesmo padrão de T8 aplicado a `TrainerClientsController` — este controller já tem lógica de convite/transferência, cuidado extra para não quebrar o fluxo de invite
**Where**: `src/Features/GymManagement/TrainerClients/TrainerClientsController.cs`
**Depends on**: T7
**Reuses**: Policies de T7
**Requirement**: AUTHZ-01, AUTHZ-02, AUTHZ-04

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Nenhuma referência a `RequireScopesAttribute` restante
- [ ] Fluxo de convite/aceite/transferência continua funcionando (teste de regressão explícito)
- [ ] Gate check passa: `dotnet test tests/IntegrationTests/IntegrationTests.csproj`
- [ ] Test count: mínimo 1 happy path + 1 deny por endpoint + 1 teste de fluxo de convite ponta a ponta

**Tests**: integration
**Gate**: full

**Commit**: `refactor(gym-management): migrate TrainerClientsController to capability policies`

---

### T14: Migrar `UserRolesController` para `[Authorize(Policy=...)]` [depende de T7] [P]

**What**: Mesmo padrão de T8 aplicado a `UserRolesController`
**Where**: `src/Features/GymManagement/UserRoles/UserRolesController.cs`
**Depends on**: T7
**Reuses**: Policies de T7
**Requirement**: AUTHZ-01, AUTHZ-02, AUTHZ-04

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Nenhuma referência a `RequireScopesAttribute` restante
- [ ] Gate check passa: `dotnet test tests/IntegrationTests/IntegrationTests.csproj`
- [ ] Test count: mínimo 1 happy path + 1 deny por endpoint

**Tests**: integration
**Gate**: full

**Commit**: `refactor(gym-management): migrate UserRolesController to capability policies`

---

### T15: Migrar `PlatformTiersController` para `[Authorize(Policy=...)]` [depende de T7] [P]

**What**: Mesmo padrão de T8 aplicado a `PlatformTiersController`
**Where**: `src/Features/GymManagement/PlatformTiers/PlatformTiersController.cs`
**Depends on**: T7
**Reuses**: Policies de T7
**Requirement**: AUTHZ-01, AUTHZ-02, AUTHZ-04

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Nenhuma referência a `RequireScopesAttribute` restante
- [ ] Gate check passa: `dotnet test tests/IntegrationTests/IntegrationTests.csproj`
- [ ] Test count: mínimo 1 happy path + 1 deny por endpoint

**Tests**: integration
**Gate**: full

**Commit**: `refactor(gym-management): migrate PlatformTiersController to capability policies`

---

### T16: Teste de integração cross-gym dedicado (P1 Independent Test) [depende de T8-T15]

**What**: Escrever o teste de integração explícito que o spec pede como Independent Test da P1 — duas academias, dois staffs, confirma isolamento + auditoria — cobrindo o gap flagado em Risks & Concerns ("nenhum teste hoje cobre autorização cross-gym")
**Where**: `tests/IntegrationTests/Domains/GymManagement/Endpoints/CrossGymAuthorizationTests.cs`
**Depends on**: T8, T9, T10, T11, T12, T13, T14, T15
**Reuses**: `IntegrationWebApplicationFactory`, `SqlServerWriteCollection`
**Requirement**: AUTHZ-04, AUTHZ-06

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Staff de A recebe `403` ao agir sobre B; Owner de A age normalmente sobre A
- [ ] `AuditLogs` mostra as duas tentativas (permitida e negada) com motivo
- [ ] Gate check passa: `dotnet test tests/IntegrationTests/IntegrationTests.csproj`
- [ ] Test count: mínimo 3 (deny cross-gym, allow same-gym, auditoria)

**Tests**: integration
**Gate**: full

**Commit**: `test(gym-management): add cross-gym authorization isolation coverage`

---

### T17–T23: Migrar controllers de Training para `[Authorize(Policy=...)]` [depende de T7] [P]

Mesmo padrão de T8, um task por controller — mantidos como um bloco por serem mecanicamente idênticos entre si (mesma troca de atributo, sem lógica de negócio especial como o invite de T13):

| Task | Controller | Where |
|---|---|---|
| T17 | `ExercisesController` | `src/Features/Training/Exercises/ExercisesController.cs` |
| T18 | `EquipmentsController` | `src/Features/Training/Equipments/EquipmentsController.cs` |
| T19 | `WorkoutPlansController` | `src/Features/Training/WorkoutPlans/WorkoutPlansController.cs` |
| T20 | `WorkoutTemplatesController` | `src/Features/Training/WorkoutTemplates/WorkoutTemplatesController.cs` |
| T21 | `WorkoutsController` | `src/Features/Training/Workouts/WorkoutsController.cs` |
| T22 | `WeightTrackingController` | `src/Features/Training/WeightTracking/WeightTrackingController.cs` |
| T23 | `TrainingDashboardController` | `src/Features/Training/Dashboard/TrainingDashboardController.cs` |

**Depends on**: T7 (cada um independente dos demais — `[P]`)
**Reuses**: Policies de T7
**Requirement**: AUTHZ-08, AUTHZ-10

**Tools**: MCP: NONE / Skill: NONE

**Done when** (por controller):
- [ ] Nenhuma referência a `RequireScopesAttribute` restante
- [ ] Usuário acessa seus próprios dados de treino normalmente (AUTHZ-10 — dono dos próprios dados independe de credencial)
- [ ] Gate check passa: `dotnet test tests/IntegrationTests/IntegrationTests.csproj`
- [ ] Test count: mínimo 1 happy path + 1 deny por endpoint

**Tests**: integration
**Gate**: full

**Commit** (um por controller): `refactor(training): migrate {Controller} to capability policies`

---

### T24: Teste de integração relacionamento treinador-cliente (P2 Independent Test) [depende de T17-T23]

**What**: Escrever o teste de integração que o spec pede como Independent Test da P2 — treinador A com relacionamento ativo com cliente B, e cliente C sem relação; confirma que A acessa B mas recebe `403` para C
**Where**: `tests/IntegrationTests/Domains/Training/Endpoints/TrainerClientAuthorizationTests.cs`
**Depends on**: T17, T18, T19, T20, T21, T22, T23, T4 (repositório de Relationships)
**Reuses**: `IntegrationWebApplicationFactory`
**Requirement**: AUTHZ-09

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Trainer A acessa dados de B (relacionamento ativo); recebe `403` para C (sem relacionamento)
- [ ] Gate check passa: `dotnet test tests/IntegrationTests/IntegrationTests.csproj`
- [ ] Test count: mínimo 2 (allow com relacionamento ativo, deny sem relacionamento)

**Tests**: integration
**Gate**: full

**Commit**: `test(training): add trainer-client relationship authorization coverage`

---

### T25: Remover `Group/Scope/UserGroup/GroupScope` e `RequireScopesAttribute` [depende de T16, T24]

**What**: Migration EF que dropa as 4 tabelas legadas; remover `RequireScopesAttribute.cs`, `ScopesController.cs`, `GroupController.cs`, handlers de Groups/Scopes e suas referências; `AuthorizationMiddleware` deixa de popular `Scopes` no `UserContext`
**Where**: `src/Features/Authorization/Groups/**` (delete), `src/Features/Authorization/Scopes/**` (delete), `src/Features/Authorization/Infrastructure/Authorization/RequireScopesAttribute.cs` (delete), `src/Features/Authorization/Infrastructure/Authorization/AuthorizationMiddleware.cs` (modify), migration nova em `src/Features/Authorization/Shared/Data/Migrations/`
**Depends on**: T16, T24 (só remove depois que nada mais lê o legado)
**Reuses**: N/A — é remoção
**Requirement**: AUTHZ-11

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Zero referência a `Group`/`Scope`/`UserGroup`/`GroupScope`/`RequireScopesAttribute` em todo o `src/` (confirmar via grep antes de considerar concluído)
- [ ] Migration de remoção aplica sem erro
- [ ] Suite completa de testes (unit + integration) passa sem depender do legado
- [ ] Gate check passa: `dotnet test tests/UnitTests/UnitTests.csproj && dotnet test tests/IntegrationTests/IntegrationTests.csproj`

**Tests**: none (é remoção — a suíte existente já cobre a ausência de regressão)
**Gate**: full

**Commit**: `chore(authorization): remove legacy Group/Scope RBAC model`

---

### T26: Atualizar `Authorization/ARCHITECTURE.md` e `README.md` [depende de T25]

**What**: Reescrever a documentação da feature `Authorization` para descrever o modelo nativo (não mais como "camada sobre" o antigo, já que o antigo não existe mais) — mandatório pelo `src/AGENTS.md` ("todo domínio implementado deve ter ARCHITECTURE.md atualizado")
**Where**: `src/Features/Authorization/ARCHITECTURE.md`, `src/Features/Authorization/README.md`; criar `ARCHITECTURE.md` para `Credentials`, `Relationships`, `Memberships`, `Entitlements` (mesma exigência do AGENTS.md, um por domínio novo)
**Depends on**: T25
**Reuses**: Estilo ASCII diagram já usado em `Authorization/ARCHITECTURE.md` (exigência do AGENTS.md)
**Requirement**: N/A (qualidade/documentação, não requirement funcional)

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Todo domínio novo (`Credentials`, `Relationships`, `Memberships`, `Entitlements`) tem `ARCHITECTURE.md` com diagrama ASCII, conforme padrão do AGENTS.md
- [ ] `Authorization/README.md` não menciona mais Groups/Scopes como mecanismo atual
- [ ] Gate check passa: `dotnet build ShapeUp.slnx`

**Tests**: none (documentação)
**Gate**: build

**Commit**: `docs(authorization): rewrite architecture docs for native model`

---

### T27: Atualizar `docs/rfcs/rfc-001-authorization-model.md` e `CURRENT_STATE_ASSESSMENT.md` [depende de T26]

**What**: Marcar a RFC como implementada (não só decidida) e atualizar o assessment de estado atual para refletir que Authorization não é mais RBAC plano
**Where**: `docs/rfcs/rfc-001-authorization-model.md` (Outcome/Follow-up), `CURRENT_STATE_ASSESSMENT.md` (seção 2 e 3, feature inventory)
**Depends on**: T26
**Reuses**: N/A
**Requirement**: N/A (fechamento de processo)

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Checkbox de follow-up da RFC marcado
- [ ] `CURRENT_STATE_ASSESSMENT.md` reflete o novo status de Authorization (não é mais 🟣/RBAC plano)

**Tests**: none (documentação)
**Gate**: build

**Commit**: `docs: close RFC-001 follow-up and update state assessment`

---

## Parallel Execution Map

```
Phase 1 (Sequential):
  T1 → T2 ─┐
  T3 → T4 ─┼→ T7
  T5 ──────┤
  T6 ──────┘

Phase 2 (Parallel, após T7):
    ├── T8  [P]
    ├── T9  [P]
    ├── T10 [P]
    ├── T11 [P]
    ├── T12 [P]
    ├── T13 [P]
    ├── T14 [P]
    └── T15 [P]
         └──→ T16

Phase 3 (Parallel, após T7, independente da Phase 2):
    ├── T17 [P]
    ├── T18 [P]
    ├── T19 [P]
    ├── T20 [P]
    ├── T21 [P]
    ├── T22 [P]
    └── T23 [P]
         └──→ T24

Phase 4 (Sequential, após T16 e T24):
  T25 → T26 → T27
```

**Parallelismo real**: dentro da Phase 2, todos os controllers de GymManagement usam integration tests contra `SqlServerWriteCollection` (não parallel-safe por evidência da Parallelism Assessment) — portanto `[P]` aqui significa "sem dependência de código entre si", mas a execução dos gates de teste roda sequencialmente dentro da mesma coleção xUnit. O mesmo vale para Phase 3.

---

## Task Granularity Check

| Task | Scope | Status |
|---|---|---|
| T1 | 1 entidade + 1 DbContext + 1 migration | ✅ Granular |
| T7 | 3 arquivos pequenos (Requirement, Handler, Resolver) + registro DI — coeso, um conceito só (resolução) | ✅ Granular (2-3 arquivos relacionados, cohesivo) |
| T8–T15, T17–T23 | 1 controller por task | ✅ Granular |
| T25 | Remoção de 4 tabelas + 1 attribute + handlers relacionados — várias exclusões, mas é UMA operação lógica (descomissionar o legado) | ✅ Granular (ação única, mesmo tocando múltiplos arquivos de deleção) |

---

## Diagram-Definition Cross-Check

| Task | Depends On (task body) | Diagram Shows | Status |
|---|---|---|---|
| T7 | T2, T4, T5, T6 | T2,T4,T5,T6 → T7 | ✅ Match |
| T8–T15 | T7 | T7 → cada um | ✅ Match |
| T16 | T8-T15 | T8-T15 → T16 | ✅ Match |
| T17–T23 | T7 | T7 → cada um | ✅ Match |
| T24 | T17-T23, T4 | T17-T23 → T24 (T4 já é dependência transitiva via T4→T7... **nota**: T24 depende diretamente de T4 para o repositório de Relationships, adicionado explicitamente no corpo da task) | ✅ Match (dependência extra explícita, não é conflito) |
| T25 | T16, T24 | T16,T24 → T25 | ✅ Match |
| T26 | T25 | T25 → T26 | ✅ Match |
| T27 | T26 | T26 → T27 | ✅ Match |

---

## Test Co-location Validation

| Task | Code Layer Created/Modified | Matrix Requires | Task Says | Status |
|---|---|---|---|---|
| T1 | Entity/Config | none | none | ✅ OK |
| T2 | Domain (state machine + repo) | unit | unit | ✅ OK |
| T3 | Entity/Config | none | none | ✅ OK |
| T4 | Repository | unit + integration | unit + integration | ✅ OK |
| T5 | Repository (adapter) | unit | unit | ✅ OK |
| T6 | Repository (adapter) | unit | unit | ✅ OK |
| T7 | Domain (resolver) | unit | unit | ✅ OK |
| T8–T15 | Controller/endpoint | integration | integration | ✅ OK |
| T16 | Controller/endpoint (cross-cutting) | integration | integration | ✅ OK |
| T17–T23 | Controller/endpoint | integration | integration | ✅ OK |
| T24 | Controller/endpoint (cross-cutting) | integration | integration | ✅ OK |
| T25 | Entity/Config (migration de remoção) | none | none (suíte existente cobre regressão) | ✅ OK |
| T26, T27 | Documentação | none | none | ✅ OK |

Nenhuma violação — todas as tasks podem ser apresentadas.

---

## Tools & Skills — pergunta obrigatória antes de Execute

Nenhuma task acima precisa de MCP externo ou skill de domínio adicional (é C#/.NET puro, testes com o stack já presente no repo — xUnit/Moq/Testcontainers). Antes de iniciar Execute, confirmar com o usuário:

> Para cada task, algum MCP ou skill específico deveria ser usado (ex.: Context7 para validar API do ASP.NET Core `IAuthorizationHandler`), ou seguimos só com leitura/edição de código direta como listado (`NONE` em todas)?
