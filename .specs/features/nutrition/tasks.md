# Nutrição Tasks

## Execution Protocol (MANDATORY -- do not skip)

Implement these tasks with the `tlc-spec-driven` skill: **activate it by name and follow its Execute flow and Critical Rules.** Do not search for skill files by filesystem path. The skill is the source of truth for the full flow (per-task cycle, sub-agent delegation, adequacy review, Verifier, discrimination sensor).

**If the skill cannot be activated, STOP and tell the user — do not proceed without it.**

---

**Design**: `ShapeUpApi/.specs/features/nutrition/design.md`
**Status**: Draft

---

## Test Coverage Matrix

> Gerado a partir de `ShapeUpApi/src/AGENTS.md` (guideline de backend encontrada: xunit+Moq, keyset pagination mandatório, CQRS+FluentValidation+ResultPattern mandatórios) + amostragem de `tests/UnitTests/Domains/Gamification/*` e `tests/IntegrationTests/Domains/Gamification/*` (padrão de localização mais recente no repo). Frontend: nenhuma guideline/framework de teste existe hoje (`ShapeUp-Web` sem `*.test.*`, sem script `test`) — usuário confirmou introduzir Vitest + React Testing Library NESTA feature (T7), decisão registrada, não um default do repo.

| Code Layer | Required Test Type | Coverage Expectation | Location Pattern | Run Command |
|---|---|---|---|---|
| Backend domain/business logic (`TdeeCalculator`, similaridade de macro, `CapabilityAuthorizationHandler`-consumível `IFeatureFlagReader`, lógica de decisão de triagem, derivação de streak na leitura) | unit | Todos os branches; 1:1 com as ACs da spec; todo edge case listado tem teste | `ShapeUpApi/tests/UnitTests/Domains/{Nutrition,PlatformFeatureFlags,Gamification}/*.cs` | `dotnet test tests/UnitTests/UnitTests.csproj` |
| Backend handlers/controllers/consumers (CQRS handlers, `NutritionGoalEvaluationJobConsumer`, `GamificationNutritionGoalMetConsumer`, `ResendEmailNotificationSender` editado) | integration | Toda rota/consumer em escopo: happy path + edge case listado + erro/falha | `ShapeUpApi/tests/IntegrationTests/Domains/{Nutrition,PlatformFeatureFlags,Gamification}/*.cs` | `dotnet test tests/IntegrationTests/IntegrationTests.csproj` |
| Backend repositório (Mongo `Food`/`FoodOverride`/`FoodModerationRequest`/`MealPlan`, EF Core `NutritionDbContext`) | integration | Caminhos de query principais + tratamento de erro | mesmo padrão acima | `dotnet test tests/IntegrationTests/IntegrationTests.csproj` |
| Backend entidade/config/migração EF Core | none | — (só gate de build) | — | `dotnet build src/ShapeUp.csproj` |
| Frontend hook (`useNutritionApi.js`) | unit (Vitest) | Toda função exportada: caminho feliz + caminho de erro; funções que passam por `enqueueMutation` testam que enfileiram (não chamam `apiClient` direto) | `ShapeUp-Web/src/hooks/api/__tests__/useNutritionApi.test.js` | `npm run test` |
| Frontend componente/tela nova | unit (Vitest + RTL) | Render correto + interação principal (happy path) + estado vazio/erro (cobre os Loading/Empty/Error states do Definition of Done) | `ShapeUp-Web/src/**/__tests__/*.test.jsx` | `npm run test` |
| Frontend config/setup (Vitest config, script novo) | none | — (só gate de lint/build) | — | `npm run lint && npm run build` |

## Parallelism Assessment

> Gerado a partir do código + `ShapeUpApi/.specs/STATE.md` (Handoff de `gamification`: "Integração RabbitMQ+Mongo compartilhado não é parallel-safe").

| Test Type | Parallel-Safe? | Isolation Model | Evidence |
|---|---|---|---|
| Backend unit (xunit) | Sim | Dependências mockadas (`Moq`), sem banco/fila compartilhado — mesmo padrão de `AntiCheatClassifierTests` (função pura, sem I/O) | `tests/UnitTests/Domains/Gamification/AntiCheatClassifier*Tests.cs` |
| Backend integration (SQL Server + Mongo + RabbitMQ compartilhados) | Não | Banco/broker compartilhado entre testes, sem schema/namespace por-teste | `ShapeUpApi/.specs/STATE.md` Handoff da feature `gamification` (flake de teardown MassTransit já observado) |
| Frontend unit (Vitest, mocks de `apiClient`/`mutationQueue`) | Sim | Sem backing store real — tudo mockado por teste, isolado por arquivo | Decisão desta feature (T7) — primeira vez que Vitest entra no repo, desenhado já isolado desde o início |

## Gate Check Commands

| Gate Level | When to Use | Command |
|---|---|---|
| Quick (backend) | Após task com só unit tests | `dotnet test tests/UnitTests/UnitTests.csproj` |
| Full (backend) | Após task com integration tests | `dotnet test tests/UnitTests/UnitTests.csproj && dotnet test tests/IntegrationTests/IntegrationTests.csproj` |
| Build (backend) | Task só de entidade/config/migração | `dotnet build src/ShapeUp.csproj` |
| Quick (frontend) | Após task com componente/hook novo | `npm run test` |
| Full (frontend) | Fim de fase de frontend | `npm run lint && npm run build && npm run test` |

---

## Execution Plan

### Phase 0: Spike (Sequential — bloqueia tudo que usa Job Consumer)

```
T1
```

### Phase 1: Foundation (Sequential)

```
T1 → T2 → T3
       ↘ T4
       ↘ T5
       ↘ T6
T7 (independente, só frontend tooling)
```

### Phase 2: Catálogo de alimentos + triagem (Parallel OK após Foundation)

```
T4,T5 ──┬→ T8 ──┬→ T9 ──→ T10 ──→ T12
        │       └→ T11 [P]
T6 ─────┴────────────────→ T13 ──→ T12
```

### Phase 3: Diário, perfil, cardápio (Parallel OK após Foundation)

```
T4 ──→ T14 ──┐
T4 ──→ T15 [P]┤
T4,T8 ──→ T16 ┼──→ T17 ──→ T18
```

### Phase 4: Gamificação (Sequential, depende de Phase 2+3)

```
T1,T14,T16 ──→ T19 ──→ T20
```

### Phase 5: Frontend (Sequential entry, Parallel depois)

```
T7 ──→ T21 ──┬→ T22 [P]
             ├→ T23 [P]
             ├→ T24 [P]
             ├→ T25 [P]
             ├→ T26 [P]
             └→ T27 [P]
```

### Phase 6: Gate full-stack (Sequential)

```
T22,T23,T24,T25,T26,T27 ──→ T28
```

---

## Task Breakdown

### T1: Spike — MassTransit recurring Job Consumer (registro + execução real)

**What**: Provar que `IJobConsumer<T>` + `AddOrUpdateRecurringJob` funciona no projeto (RabbitMQ, sem Quartz/Hangfire) ANTES de qualquer task depender disso — mesmo padrão do T1 de `event-bus`. Job trivial (log + no-op), cron a cada poucos minutos só pro spike, removido/ajustado depois em T19.
**Where**: `Features/Nutrition/GoalEvaluation/_Spike/` (descartável), `Configurations/MessagingExtensions.cs` (edição temporária)
**Depends on**: None
**Reuses**: `Configurations/MessagingExtensions.cs` (já existe, `event-bus`)
**Requirement**: NUT-08 (pré-requisito técnico)

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [x] Recurring job registrado via `AddOrUpdateRecurringJob` executa de fato (confirmado por log observado, não só compila)
- [x] Se a API esperada (nomes de método) divergir do documentado em design.md, decisão registrada inline (qual API real funcionou) — se nada funcionar, ESCALAR pro usuário antes de continuar (não substituir por `BackgroundService` silenciosamente)
- [x] Código de spike removido (2026-09-09, incidente): a wiring do spike (`AddJobSagaStateMachines`/`SetInMemorySagaRepositoryProvider`/`AddDelayedMessageScheduler`/consumer) tinha sido deixada ATIVA e INCONDICIONAL em `MessagingExtensions.cs`, rodando em todo `AddMassTransit` — inclusive nos ~50+ `IntegrationWebApplicationFactory` da suíte de integração, cada um pagando o custo de startup/teardown de job saga. Isso travou a suíte por 46+ minutos (reportado pelo usuário rodando via Cursor). Removida a wiring inteira + pasta `_Spike/`; achados da pesquisa preservados como comentário em `MessagingExtensions.cs` pra T18 reusar sem repetir a investigação. Confirmado: `dotnet build` limpo, subset de integration tests (Gamification, 6 testes) voltou a rodar em 19s

**Tests**: none (spike descartável)
**Gate**: build

---

### T2: Migrar `WeightTracking` de Training pra Nutrition (backend)

**What**: Mover `WeightTargetDocument`/`WeightRegisterDocument`, handlers (`UpsertTargetWeight`/`UpsertDailyWeightRegister`/`GetWeightRegisters`) e `WeightTrackingController` de `Features/Training/WeightTracking` pra `Features/Nutrition/WeightTracking`, rota de `/api/training/weight` pra `/api/nutrition/weight`, preservando dado existente (migração, não recriação).
**Where**: `Features/Nutrition/WeightTracking/*` (novo), `Features/Training/WeightTracking/*` (removido)
**Depends on**: None
**Reuses**: código integral já existente (mover, não reescrever)
**Requirement**: NUT-07

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [x] Grep por `training/weight` e pelos 3 nomes de handler confirma ZERO referência restante em `Features/Training`
- [x] Rota nova responde, rota antiga não existe mais (404)
- [x] Testes existentes de peso (se houver) migram junto e passam
- [x] Gate: `dotnet test tests/IntegrationTests/IntegrationTests.csproj`

**Tests**: integration (migração de handler já testado — testes existentes movem e devem continuar verdes)
**Gate**: full

---

### T3: Frontend — mover peso de `useTrainingApi` pra `useNutritionApi` [P]

**What**: Remover `upsertTargetWeight`/`upsertDailyWeightRegister`/`getWeightRegisters` de `useTrainingApi.js`, criar `useNutritionApi.js` (esqueleto inicial) com as 3 funções apontando pra `/api/nutrition/weight/*`, atualizar `ObjectivesClient.jsx` (único consumidor confirmado)
**Where**: `ShapeUp-Web/src/hooks/api/useTrainingApi.js` (edit), `ShapeUp-Web/src/hooks/api/useNutritionApi.js` (novo), `ShapeUp-Web/src/pages/Dashboard/ObjectivesClient.jsx` (edit)
**Depends on**: T2
**Reuses**: shape exato das 3 funções já existentes, só muda a rota/arquivo
**Requirement**: NUT-07

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [x] `ObjectivesClient.jsx` funciona idêntico ao antes (peso registra/lê corretamente)
- [x] `npm run lint` sem novo erro
- [x] Grep confirma zero outra tela importando peso de `useTrainingApi`

**Tests**: none (esqueleto do hook — teste real de `useNutritionApi` vem em T21, quando o hook estiver completo; ver "Resolving compilation dependencies" — merge forward)
**Gate**: build (frontend)

---

### T4: `NutritionDbContext` (SQL) — entidades + migração EF Core

**What**: Criar `NutritionProfile`, `WeightTarget`, `WeightRegister` (schema migrado de T2), `DiaryDay`, `DiaryEntry`, `NutritionGoalEvaluation` com Fluent config, gerar migração EF Core
**Where**: `Features/Nutrition/Shared/Entities/*.cs`, `Features/Nutrition/Infrastructure/Data/NutritionDbContext.cs`, migração em `Features/Nutrition/Infrastructure/Data/Migrations/`
**Depends on**: T2 (schema de peso já migrado)
**Reuses**: padrão Fluent config de `GymManagementDbContext`/`GamificationDbContext`
**Requirement**: NUT-05, NUT-06, NUT-07

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [x] Migração aplica limpo em banco de dev vazio
- [x] `dotnet build` sem erro

**Tests**: none (entidade/config — coberto indiretamente pelos testes de handler/repositório em tasks seguintes)
**Gate**: build

---

### T5: Repositórios Mongo — `Food`, `FoodOverride`, `FoodModerationRequest`, `MealPlan`

**What**: `FoodDocument`/`FoodOverrideDocument`/`FoodModerationRequestDocument`/`MealPlan`+`MealPlanItem`, registro de `Mongo__Nutrition__ConnectionString`, `IFoodRepository`/`IFoodOverrideRepository`/`IFoodModerationRepository`/`IMealPlanRepository` com métodos do design (`SearchAsync`, `GetByBarcodeAsync`, `SoftDeleteAsync`, etc.)
**Where**: `Features/Nutrition/Shared/Documents/*.cs`, `Features/Nutrition/Infrastructure/Mongo/*.cs`
**Depends on**: None
**Reuses**: padrão de registro Mongo já usado por `Features/Training/Infrastructure/Mongo/`
**Requirement**: NUT-01, NUT-02, NUT-09

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [x] Índice único em `Barcode` (quando não-nulo) criado
- [x] Repositórios compilam e conectam (smoke via integration test básico de `CreateAsync`+`GetByIdAsync`)
- [x] Gate: `dotnet test tests/IntegrationTests/IntegrationTests.csproj`

**Tests**: integration (caminhos de query principais: criar, buscar por nome, buscar por barcode, soft-delete)
**Gate**: full

---

### T6: Feature `PlatformFeatureFlags` (backend, isolada de Nutrition)

**What**: `PlatformFeatureFlag` (SQL, EF Core, `PlatformFeatureFlagsDbContext`), `IFeatureFlagReader.IsEnabledAsync` (fail-open), `PlatformFeatureFlagsController` (`GET`/`PUT`) atrás de `capability:platform.feature_flags.manage`, seed `notifications.email-enabled=true`
**Where**: `Features/PlatformFeatureFlags/*`
**Depends on**: None
**Reuses**: padrão `platform.*`/AD-006 (zero registro de policy novo — dinâmico)
**Requirement**: NUT-12

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [x] `IsEnabledAsync` retorna `true` pra chave inexistente (fail-open) — unit test cobre isso explicitamente
- [x] `GET`/`PUT` funcionam só com a capability certa (403 sem ela)
- [x] `PUT` idempotente (ligar 2x seguidas não causa erro)

**Tests**: unit (`IFeatureFlagReader` fail-open + toggle, 1:1 com NUT-12 ACs 1-4) + integration (endpoints, 403 sem capability, NUT-12 AC3)
**Gate**: full

---

### T7: Frontend — introduzir Vitest + React Testing Library

**What**: Instalar `vitest`, `@testing-library/react`, `@testing-library/jest-dom`, `jsdom`; criar `vitest.config.js`; adicionar script `"test": "vitest run"` ao `package.json`; 1 teste smoke (ex.: `Card.test.jsx` num componente já existente) provando que o setup funciona antes de qualquer teste real desta feature depender dele
**Where**: `ShapeUp-Web/package.json`, `ShapeUp-Web/vitest.config.js` (novo), `ShapeUp-Web/src/components/shared/__tests__/Card.test.jsx` (novo, smoke)
**Depends on**: None
**Reuses**: nada — primeira vez no repo
**Requirement**: N/A (infra de teste, decisão confirmada com o usuário)

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [x] `npm run test` executa e passa (o smoke test)
- [x] `npm run lint` e `npm run build` continuam passando

**Tests**: unit (o próprio smoke test é a prova)
**Gate**: quick (frontend)

---

### T8: `Food` — cadastro, busca, get-by-barcode

**What**: `CreateFoodCommand`/Handler/Validator, `SearchFoodsQuery`/Handler, `GetFoodByBarcodeQuery`/Handler, `FoodsController` (CQRS, FluentValidation, keyset pagination na busca)
**Where**: `Features/Nutrition/Foods/{CreateFood,SearchFoods,GetFoodByBarcode}/`, `Features/Nutrition/Foods/FoodsController.cs`
**Depends on**: T5
**Reuses**: padrão CQRS/ResultPattern/FluentValidation (`AGENTS.md`), keyset pagination já usado em outros domínios
**Requirement**: NUT-01, NUT-02

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [x] NUT-01 AC1-5 cobertas (macro obrigatório, micro opcional, rejeita campo faltante, barcode duplicado rejeitado, barcode novo aceito)
- [x] NUT-02 AC1-4 cobertas (busca por nome case-insensitive, busca por barcode existente/inexistente, resposta pra navegador sem suporte a leitura nativa é só "aceita string manual" — sem lógica de detecção no backend)

**Tests**: unit (validators, 1:1 AC) + integration (rotas, happy+edge: barcode duplicado, campo faltante, busca vazia)
**Gate**: full

---

### T9: `FoodOverride` — edição pessoal + resolução de versão ativa [P após T8]

**What**: `CreateFoodOverrideCommand`/Handler (cria override, enfileira triagem via `IFoodModerationRepository`), `SetActiveFoodVersionCommand`/Handler (alternar pública↔pessoal), lógica de resolução (busca/diário mostram override ativo do usuário quando existe)
**Where**: `Features/Nutrition/Foods/{CreateFoodOverride,SetActiveFoodVersion}/`
**Depends on**: T8
**Reuses**: `IFoodRepository`/`IFoodOverrideRepository` (T5)
**Requirement**: NUT-03

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [ ] NUT-03 AC1-3, AC7 cobertas (override não altera pública, flag de versão, alternância, múltiplos overrides independentes)

**Tests**: unit (lógica de resolução override-vs-pública, 1:1 AC) + integration (rotas)
**Gate**: full

---

### T10: Triagem administrativa (`FoodModerationRequest`)

**What**: `GetPendingModerationsQuery`/Handler (diff público-vs-proposto), `DecideModerationCommand`/Handler (aprovar unifica pública + limpa override do autor; recusar mantém override + notifica), `FoodModerationController` atrás de `capability:platform.nutrition_foods.moderate`
**Where**: `Features/Nutrition/Moderation/{GetPendingModerations,DecideModeration}/`, `Features/Nutrition/Moderation/FoodModerationController.cs`
**Depends on**: T9
**Reuses**: padrão `platform.*`
**Requirement**: NUT-04

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [ ] NUT-04 AC1-3 cobertas (lista com diff, 403 sem capability, decisão dupla rejeitada — idempotência)
- [ ] NUT-03 AC4-6 cobertas (aprovar unifica pública, recusar mantém override + notifica)

**Tests**: unit (regra de decisão idempotente, 1:1 AC) + integration (rotas, 403, decisão dupla)
**Gate**: full

---

### T11: Exclusão de alimento (soft-delete, admin) [P após T8]

**What**: `DeleteFoodCommand`/Handler (idempotente), endpoint em `FoodsController` atrás de `capability:platform.nutrition_foods.moderate`; `SearchAsync`/`GetByIdAsync` passam a excluir `IsDeleted=true` do resultado normal
**Where**: `Features/Nutrition/Foods/DeleteFood/`
**Depends on**: T8
**Reuses**: mesma capability de T10
**Requirement**: NUT-13

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [ ] NUT-13 AC1-3, AC5 cobertas (soft-delete, 403 sem capability, histórico preservado, no-op em delete duplicado)
- [ ] AC4 (cardápio ativo referenciando item excluído) coberta em T17/T18 (cross-referenciada aqui, não duplicada)

**Tests**: unit (idempotência do soft-delete) + integration (rota, 403, busca não retorna excluído)
**Gate**: full

---

### T12: Notificação de recusa (e-mail, respeitando a flag)

**What**: `DecideModerationCommand` (T10), quando `Rejected`, chama `SendEmailTemplateHandler` (Notifications, já existente) pro autor do override
**Where**: `Features/Nutrition/Moderation/DecideModeration/DecideModerationCommandHandler.cs` (edit)
**Depends on**: T10, T13
**Reuses**: `SendEmailTemplateHandler`/`IEmailNotificationSender` (já existentes, agora com guard de flag vindo de T13)
**Requirement**: NUT-04 AC (parte de "notifica o autor")

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [ ] Recusa dispara e-mail quando a flag está ligada
- [ ] Recusa NÃO dispara e-mail quando a flag está desligada (T13), e a recusa em si continua funcionando normalmente

**Tests**: integration (recusa com flag ligada/desligada, spec NUT-12 Independent Test)
**Gate**: full

---

### T13: `ResendEmailNotificationSender` — guard de feature flag ✅ Complete

**What**: Construtor ganha `IFeatureFlagReader`; método de envio checa `IsEnabledAsync("notifications.email-enabled")` antes de chamar Resend — se desligada, loga supressão e retorna sucesso-no-op sem lançar erro
**Where**: `Features/Notifications/Infrastructure/Resend/ResendEmailNotificationSender.cs` (edit)
**Depends on**: T6
**Reuses**: estrutura existente do sender
**Requirement**: NUT-12 AC1-2

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [x] NUT-12 AC1-2 cobertas (flag desligada suprime e-mail sem erro pro chamador)

**Tests**: unit (guard clause, 1:1 AC — flag ligada chama Resend, flag desligada não chama e não lança)
**Gate**: quick

---

### T14: `NutritionProfile` — onboarding TDEE + meta manual

**What**: `TdeeCalculator` (função pura, Mifflin-St Jeor), `CompleteOnboardingCommand`/Handler (calcula e salva meta), `SetManualGoalCommand`/Handler (define/substitui meta manual), `NutritionProfileController`
**Where**: `Features/Nutrition/Shared/TdeeCalculator.cs`, `Features/Nutrition/Profile/{CompleteOnboarding,SetManualGoal}/`, `Features/Nutrition/Profile/NutritionProfileController.cs`
**Depends on**: T4
**Reuses**: `IWeightTrackingRepository` (T2, peso atual)
**Requirement**: NUT-06

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [ ] NUT-06 AC1-4 cobertas (TDEE calcula, pular onboarding permite meta manual, redefinir mantém histórico de dias passados, diário funciona sem meta)
- [ ] Edge case (altura/idade fora de faixa plausível) rejeitado na validação

**Tests**: unit (`TdeeCalculator` pura, 1:1 AC + edge case de faixa inválida) + integration (endpoints)
**Gate**: full

---

### T15: Diário — `DiaryDay`/`DiaryEntry` CRUD [P após T4, T8]

**What**: `AddDiaryEntryCommand`/Handler (calcula macros via `IFoodRepository`/`IFoodOverrideRepository`, resolve `DiaryDay` pela `Date` do CLIENTE — nunca "agora do servidor", id vindo do cliente, upsert idempotente), `RemoveDiaryEntryCommand`/Handler, `GetDiaryDayQuery`/Handler, `DiaryController`
**Where**: `Features/Nutrition/Diary/{AddDiaryEntry,RemoveDiaryEntry,GetDiaryDay}/`, `Features/Nutrition/Diary/DiaryController.cs`
**Depends on**: T4, T8
**Reuses**: `IFoodRepository`/`IFoodOverrideRepository` (T5/T9)
**Requirement**: NUT-05

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [ ] NUT-05 AC1-5 cobertas (calcula macro por quantidade, agrupa por refeição, recalcula ao remover, usa override quando aplicável, dia vazio sem erro)
- [ ] Retry com mesmo `id` (fila offline) é idempotente — não duplica entrada

**Tests**: unit (cálculo de macro por quantidade, 1:1 AC) + integration (rotas, idempotência de retry, dia vazio)
**Gate**: full

---

### T16: `MealPlan` — criar, ativar, aplicar ao diário

**What**: `CreateMealPlanCommand`/Handler, `ActivateMealPlanCommand`/Handler (preenche `DiaryDay` do dia com itens do plano — sinaliza item indisponível se `Food` referenciado está `IsDeleted`, ver NUT-13 AC4), `MealPlanController`
**Where**: `Features/Nutrition/MealPlans/{CreateMealPlan,ActivateMealPlan}/`, `Features/Nutrition/MealPlans/MealPlanController.cs`
**Depends on**: T5, T15
**Reuses**: `IMealPlanRepository` (T5), `AddDiaryEntryCommand` internamente (T15)
**Requirement**: NUT-09, NUT-13 (AC4)

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [ ] NUT-09 AC1-3 cobertas (cria com `PrescribedByRelationshipId` nulo, ativar preenche diário, editar direto no diário não altera o plano salvo)
- [ ] NUT-13 AC4 coberta (ativação sinaliza item excluído, não aplica silenciosamente)

**Tests**: unit (regra de "não altera plano salvo", 1:1 AC) + integration (rotas, ativação com item excluído)
**Gate**: full

---

### T17: Substituição de item (sugestão por macro + escolha livre)

**What**: `SuggestSubstituteQuery`/Handler (ranking por distância euclidiana normalizada dos 4 macros), `SubstituteDiaryItemCommand`/Handler (troca só no dia, plano salvo intocado, aceita item fora da sugestão mesmo estourando meta)
**Where**: `Features/Nutrition/Diary/{SuggestSubstitute,SubstituteDiaryItem}/`
**Depends on**: T8, T15, T16
**Reuses**: `IFoodRepository.SearchAsync` (T5)
**Requirement**: NUT-10

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [ ] NUT-10 AC1-4 cobertas (sugestão ranqueada, substituição só no dia, escolha livre aceita mesmo estourando meta, total recalcula)

**Tests**: unit (função de distância/ranking, 1:1 AC) + integration (rota, plano salvo intocado)
**Gate**: full

---

### T18: `NutritionGoalMet` (evento) + `NutritionGoalEvaluationJobConsumer`

**What**: `record NutritionGoalMet(int UserId, DateOnly Date)`; `NutritionGoalEvaluationJobConsumer : IJobConsumer<EvaluateNutritionGoals>` real (substitui o spike de T1) — varre `DiaryDay` de ontem sem `EvaluatedAtUtc`, checa os 3 macros ±10%, publica quando bate, sempre marca avaliado; registro do recurring job (cron diário) em `MessagingExtensions.cs`
**Where**: `Features/Nutrition/Shared/Events/NutritionGoalMet.cs`, `Features/Nutrition/GoalEvaluation/NutritionGoalEvaluationJobConsumer.cs`, `Configurations/MessagingExtensions.cs` (edit)
**Depends on**: T1, T14, T15
**Reuses**: `IPublishEndpoint` (MassTransit, já registrado), saga repository EF Core sobre `NutritionDbContext`
**Requirement**: NUT-08

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [ ] NUT-08 AC1, AC4 cobertas (publica só quando os 3 macros batem a tolerância; nunca publica "perdeu"; varredura restrita a `WHERE Date=ontem AND EvaluatedAtUtc IS NULL`, não toda a base)
- [ ] Rerun do job no mesmo dia não reavalia (`EvaluatedAtUtc` já setado)
- [ ] Código de spike (T1) removido/substituído por este

**Tests**: unit (regra de tolerância dos 3 macros, 1:1 AC) + integration (execução do job, idempotência de reavaliação)
**Gate**: full

---

### T19: `GamificationNutritionGoalMetConsumer` + streak nutricional

**What**: Colunas novas em `GamificationProfile` (`NutritionCurrentStreak`, `LastNutritionGoalMetDate`) + migração EF Core; `GamificationNutritionEvaluation` (idempotência); `GamificationNutritionGoalMetConsumer : IConsumer<NutritionGoalMet>` (credita XP/coins, incrementa streak — nunca decrementa); `GetGamificationProfileHandler` (já existente) ganha a derivação de leitura (streak exibido = 0 se `LastNutritionGoalMetDate` não é hoje/ontem)
**Where**: `Features/Gamification/NutritionGoalMet/GamificationNutritionGoalMetConsumer.cs`, `Features/Gamification/Shared/Entities/GamificationProfile.cs` (edit), migração, `Features/Gamification/GetGamificationProfile/GetGamificationProfileHandler.cs` (edit)
**Depends on**: T18
**Reuses**: `GamificationDbContext` (já existe), molde de `GamificationWorkoutFinishedConsumer`
**Requirement**: NUT-08 AC2-3

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [ ] NUT-08 AC2-3 cobertas (credita XP/coins uma vez, streak incrementa, redelivery não duplica)
- [ ] Streak exibido cai pra 0 na leitura quando `LastNutritionGoalMetDate` está velho — sem nenhum job/evento fazendo esse reset ativamente

**Tests**: unit (derivação de streak na leitura, 1:1 AC) + integration (consumer, idempotência)
**Gate**: full

---

### T20: `useNutritionApi.js` (hook completo)

**What**: Completar o hook (esqueleto de T3) com todas as funções: foods (search/create/edit/delete), diário (add/remove/get, via `enqueueMutation`), cardápio (create/activate/substitute), perfil (onboarding/meta manual), triagem (admin)
**Where**: `ShapeUp-Web/src/hooks/api/useNutritionApi.js` (edit)
**Depends on**: T7, T8, T9, T10, T11, T14, T15, T16, T17
**Reuses**: shape de `useTrainingApi.js`/`useGamificationApi.js`; `enqueueMutation`/`utils/objectId.js` (Fase 1) pras funções de escrita do diário
**Requirement**: NUT-11

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [ ] Toda função do hook testada (happy + erro), incluindo a prova de que `addDiaryEntry` enfileira (não chama `apiClient` direto)
- [ ] `npm run lint` sem novo erro

**Tests**: unit (Vitest, 1 arquivo cobrindo todas as funções exportadas — matriz "toda função: happy+erro")
**Gate**: quick (frontend)

---

### T21: Telas — cadastro/busca/edição de alimento (com código de barras) [P]

**What**: Tela de busca (texto + leitura de código de barras via Barcode Detection API com fallback manual), formulário de cadastro/edição, flag visual "sua versão"/"versão pública" com toggle
**Where**: `ShapeUp-Web/src/pages/Dashboard/Nutrition/FoodSearch.jsx`, `FoodForm.jsx` (novos)
**Depends on**: T20
**Reuses**: `Card`, tokens de design-system
**Requirement**: NUT-01, NUT-02, NUT-03, NUT-11

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [ ] Fluxo de busca→cadastro (barcode não encontrado)→edição→flag de versão funciona
- [ ] Fallback de digitação manual funciona quando `'BarcodeDetector' in window` é falso

**Tests**: unit (Vitest+RTL: render, fluxo de busca, fallback sem Barcode API, estado vazio)
**Gate**: quick (frontend)

---

### T22: Tela de diário (dia, macro progress, celebração) [P]

**What**: Visão do dia (refeições+itens), anel/barra de progresso de macro vs. meta, celebração visual ao bater meta
**Where**: `ShapeUp-Web/src/pages/Dashboard/Nutrition/DiaryDay.jsx` (novo)
**Depends on**: T20
**Reuses**: `Card`, `GamificationProgressCard` como referência visual
**Requirement**: NUT-05, NUT-11

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [ ] Progresso visual bate com totais reais; dia vazio mostra estado vazio claro, não quebrado

**Tests**: unit (Vitest+RTL: render com dados, estado vazio)
**Gate**: quick (frontend)

---

### T23: Telas de cardápio fixo + substituição [P]

**What**: Gestão de cardápio (criar/ativar), atalho de substituição de item a partir do diário
**Where**: `ShapeUp-Web/src/pages/Dashboard/Nutrition/MealPlanManager.jsx`, `SubstituteItemModal.jsx` (novos)
**Depends on**: T20
**Reuses**: `Card`
**Requirement**: NUT-09, NUT-10, NUT-11

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [ ] Criar/ativar cardápio preenche o diário; substituir (sugestão ou livre) reflete no dia sem alterar o plano salvo

**Tests**: unit (Vitest+RTL: fluxo de ativação, fluxo de substituição)
**Gate**: quick (frontend)

---

### T24: Tela de onboarding TDEE / meta manual [P]

**What**: Formulário de onboarding (altura/idade/sexo/atividade) com opção de pular pra meta manual
**Where**: `ShapeUp-Web/src/pages/Dashboard/Nutrition/GoalOnboarding.jsx` (novo)
**Depends on**: T20
**Reuses**: `Card`, padrão de formulário já usado no app
**Requirement**: NUT-06, NUT-11

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [ ] Completar onboarding calcula meta plausível; pular oferece meta manual

**Tests**: unit (Vitest+RTL: os 2 fluxos)
**Gate**: quick (frontend)

---

### T25: Admin — triagem de alimentos + feature flags [P]

**What**: Tela de fila de triagem (diff público-vs-proposto, aprovar/recusar), tela mínima de toggle de feature flags
**Where**: `ShapeUp-Web/src/pages/Admin/FoodModerationQueue.jsx`, `FeatureFlagsPanel.jsx` (novos)
**Depends on**: T20
**Reuses**: `Card`, `platform.*` capability já resolvida no backend
**Requirement**: NUT-04, NUT-12

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [ ] Aprovar/recusar reflete na fila; toggle de flag liga/desliga e persiste

**Tests**: unit (Vitest+RTL: aprovar, recusar, toggle)
**Gate**: quick (frontend)

---

### T26: Streak nutricional no card de progresso [P]

**What**: Estender `GamificationProgressCard` (ou componente irmão) com o streak nutricional (`NutritionCurrentStreak` derivado)
**Where**: `ShapeUp-Web/src/components/gamification/GamificationProgressCard.jsx` (edit) ou novo componente irmão
**Depends on**: T20, T19
**Reuses**: `GamificationProgressCard` existente, `useGamificationApi`
**Requirement**: NUT-08, NUT-11

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [ ] Streak nutricional visível no dashboard, distinto visualmente do streak de treino (ícone diferente, mesmo espírito do AD de Gamification)

**Tests**: unit (Vitest+RTL: render com streak > 0 e streak = 0)
**Gate**: quick (frontend)

---

### T27: Gate full-stack + documentação

**What**: Rodar toda a suíte (backend unit+integration, frontend lint+build+test), corrigir o que quebrar; criar `Features/Nutrition/ARCHITECTURE.md` e `Features/PlatformFeatureFlags/ARCHITECTURE.md` (mandatório por `AGENTS.md`); atualizar `ShapeUpApi/.specs/STATE.md` (novo `AD-NNN` pras 2 decisões de projeto listadas em design.md, Handoff da feature)
**Where**: repo inteiro (gate), `Features/Nutrition/ARCHITECTURE.md`, `Features/PlatformFeatureFlags/ARCHITECTURE.md`, `ShapeUpApi/.specs/STATE.md`
**Depends on**: T21, T22, T23, T24, T25, T26
**Reuses**: formato de `ARCHITECTURE.md` já usado por `Authorization`/`Notifications`
**Requirement**: todas (gate final)

**Tools**: MCP: NONE · Skill: NONE

**Done when**:
- [ ] `dotnet test tests/UnitTests/UnitTests.csproj` — todos passam, contagem registrada
- [ ] `dotnet test tests/IntegrationTests/IntegrationTests.csproj` — todos passam, contagem registrada
- [ ] `npm run lint && npm run build && npm run test` — todos passam
- [ ] `ARCHITECTURE.md` das 2 features novas escritos com diagrama ASCII
- [ ] `STATE.md` ganha os 2 `AD-NNN` novos + Handoff da feature `nutrition` fechado

**Tests**: none (gate de integração final, não produz código novo)
**Gate**: full (backend) + full (frontend)

---

## Parallel Execution Map

```
Phase 0 (Sequential):
  T1

Phase 1 (Foundation, mistura sequencial/paralelo):
  T1 ──→ T2 ──→ T3
  T2 ──→ T4
  T5 (independente)
  T6 (independente)
  T7 (independente)

Phase 2 (Catálogo — parcialmente paralelo):
  T4,T5 ──→ T8 ──┬→ T9 ──→ T10 ─┐
                 └→ T11 [P]      ├→ T12
  T6 ──────────────→ T13 ───────┘

Phase 3 (Diário/Perfil/Cardápio):
  T4 ──→ T14
  T4,T8 ──→ T15 ──→ T16 ──→ T17

Phase 4 (Gamificação, sequencial):
  T1,T14,T15 ──→ T18 ──→ T19

Phase 5 (Frontend):
  T7 ──→ T20 ──┬→ T21 [P]
               ├→ T22 [P]
               ├→ T23 [P]
               ├→ T24 [P]
               ├→ T25 [P]
               └→ T26 [P] (depende também de T19)

Phase 6 (Gate):
  T21,T22,T23,T24,T25,T26 ──→ T27
```

**Parallelismo real**: dentro de Phase 1, T4/T5/T6/T7 não dependem uns dos outros (só T2→T3 e T2→T4 são sequenciais) — mas testes de integration backend (T4/T5/T6, se rodarem integration tests) NÃO são parallel-safe entre si (banco compartilhado), então mesmo marcados `[P]` de dependência, DEVEM rodar sequencialmente na hora de EXECUTAR os testes (ver Parallelism Assessment). `[P]` aqui é sobre ordem de implementação, não sobre execução simultânea da suíte de teste.

---

## Task Granularity Check

| Task | Scope | Status |
|---|---|---|
| T1: Spike Job Consumer | 1 spike isolado | ✅ Granular |
| T2: Migrar WeightTracking (backend) | 1 módulo inteiro movido (mecânico, não redesenho) | ⚠️ OK — mover código existente 1:1, não criação nova |
| T3: Mover peso no frontend | 2 arquivos (hook + 1 consumidor) | ✅ Granular |
| T4: NutritionDbContext | 1 DbContext + entidades relacionadas | ✅ Granular (1 conceito: schema SQL da feature) |
| T5: Repositórios Mongo | 4 documentos + 4 repositórios relacionados | ⚠️ OK — mesmo conceito (catálogo Mongo), cada um pequeno |
| T6: PlatformFeatureFlags | 1 feature isolada e pequena (1 entidade, 1 leitor, 1 controller) | ✅ Granular |
| T7: Vitest setup | 1 configuração + 1 smoke test | ✅ Granular |
| T8: Food create/search/barcode | 3 endpoints do mesmo agregado | ⚠️ OK — mesmo recurso (`Food`), cada handler pequeno |
| T9: FoodOverride | 2 endpoints do mesmo agregado | ✅ Granular |
| T10: Triagem admin | 2 endpoints do mesmo agregado (moderação) | ✅ Granular |
| T11: Delete de alimento | 1 endpoint | ✅ Granular |
| T12: Notificação de recusa | 1 edição pontual num handler existente | ✅ Granular |
| T13: Guard de flag no sender | 1 edição pontual | ✅ Granular |
| T14: TDEE + meta | 1 cálculo + 2 endpoints do mesmo agregado (perfil) | ⚠️ OK — mesmo recurso (`NutritionProfile`) |
| T15: Diário CRUD | 3 endpoints do mesmo agregado | ⚠️ OK — mesmo recurso (`DiaryDay`/`DiaryEntry`) |
| T16: MealPlan criar/ativar | 2 endpoints do mesmo agregado | ✅ Granular |
| T17: Substituição | 2 endpoints do mesmo agregado (sugestão + aplicar) | ✅ Granular |
| T18: Job de avaliação | 1 evento + 1 job consumer | ✅ Granular |
| T19: Consumer de Gamificação | 1 consumer + 1 migração + 1 edição de handler existente | ⚠️ OK — 1 conceito (reação ao evento), 3 arquivos pequenos |
| T20: Hook completo | 1 arquivo (hook) | ✅ Granular |
| T21-T26: Telas | 1-2 componentes por task, cada uma 1 fluxo de UI | ✅ Granular |
| T27: Gate + docs | Gate final, sem código de produto novo | ✅ Granular (é integração/documentação, não implementação) |

**Legenda**: ✅ Granular · ⚠️ OK (múltiplos arquivos pequenos do MESMO agregado/conceito, cobeso — não split forçado) · ❌ split necessário (nenhum caso aqui)

---

## Diagram-Definition Cross-Check

| Task | Depends On (corpo da task) | Diagrama mostra | Status |
|---|---|---|---|
| T1 | None | Nenhuma seta de entrada | ✅ |
| T2 | None | Nenhuma seta de entrada (Phase 1) | ✅ |
| T3 | T2 | T2→T3 | ✅ |
| T4 | T2 | T2→T4 | ✅ |
| T5 | None | Nenhuma seta de entrada | ✅ |
| T6 | None | Nenhuma seta de entrada | ✅ |
| T7 | None | Nenhuma seta de entrada | ✅ |
| T8 | T5 | T4,T5→T8 (T4 citado a mais no diagrama por proximidade de fase — T8 só usa T5 de fato) | ⚠️ corrigido abaixo |
| T9 | T8 | T8→T9 | ✅ |
| T10 | T9 | T9→T10 | ✅ |
| T11 | T8 | T8→T11 [P] | ✅ |
| T12 | T10, T13 | T10→T12, T13→T12 | ✅ |
| T13 | T6 | T6→T13 | ✅ |
| T14 | T4 | T4→T14 | ✅ |
| T15 | T4, T8 | T4,T8→T15 | ✅ |
| T16 | T5, T15 | T5,T15→T16 (diagrama simplificado mostra T15→T16, T5 implícito via T16 usar `IMealPlanRepository`) | ⚠️ corrigido abaixo |
| T17 | T8, T15, T16 | T16→T17 (T8/T15 implícitos, já resolvidos antes de T16) | ⚠️ corrigido abaixo |
| T18 | T1, T14, T15 | T1,T14,T15→T18 | ✅ |
| T19 | T18 | T18→T19 | ✅ |
| T20 | T7, T8, T9, T10, T11, T14, T15, T16, T17 | T7→T20 (demais dependências de backend implícitas — todas as rotas que o hook consome já existem antes desta fase) | ⚠️ corrigido abaixo |
| T21-T26 | T20 (+T19 pra T26) | T20→cada uma [P], T19→T26 | ✅ |
| T27 | T21-T26 | todas→T27 | ✅ |

**Correção aplicada**: os 4 itens marcados ⚠️ acima são casos onde o diagrama-resumo (Parallel Execution Map, simplificado pra legibilidade) omite uma dependência de fase-anterior já satisfeita por ordem (ex.: T8 tecnicamente só precisa de T5, mas T4 já terminou na mesma fase antes dele rodar de qualquer forma). Nenhum caso é uma dependência FALTANDO no corpo da task (todas as dependências reais estão listadas em "Depends on" de cada task) — é o diagrama simplificado que agrupa por fase em vez de desenhar toda aresta transitiva. Nenhuma correção de conteúdo necessária, só uma ressalva de leitura.

---

## Test Co-location Validation

| Task | Code Layer Criado/Modificado | Matriz Exige | Task Diz | Status |
|---|---|---|---|---|
| T1 | Spike descartável | none (spike) | none | ✅ OK |
| T2 | Handler/controller (migrado) | integration | integration | ✅ OK |
| T3 | Hook frontend (esqueleto) | unit (mas função ainda incompleta) | none (merge forward pra T20) | ✅ OK — ver "Resolving compilation dependencies" |
| T4 | Entidade/config/migração | none | none | ✅ OK |
| T5 | Repositório | integration | integration | ✅ OK |
| T6 | Domain logic (fail-open) + integration (endpoints) | unit + integration | unit + integration | ✅ OK |
| T7 | Config/setup frontend | none | none (smoke test não é da matriz de cobertura, é a prova do setup) | ✅ OK |
| T8 | Handler/controller | unit + integration | unit + integration | ✅ OK |
| T9 | Handler/controller | unit + integration | unit + integration | ✅ OK |
| T10 | Handler/controller | unit + integration | unit + integration | ✅ OK |
| T11 | Handler/controller | unit + integration | unit + integration | ✅ OK |
| T12 | Handler (edição pontual, integration) | integration | integration | ✅ OK |
| T13 | Domain logic (guard) | unit | unit | ✅ OK |
| T14 | Domain logic (TDEE) + handler/controller | unit + integration | unit + integration | ✅ OK |
| T15 | Domain logic (cálculo) + handler/controller | unit + integration | unit + integration | ✅ OK |
| T16 | Handler/controller | unit + integration | unit + integration | ✅ OK |
| T17 | Domain logic (ranking) + handler/controller | unit + integration | unit + integration | ✅ OK |
| T18 | Domain logic (tolerância) + consumer/job | unit + integration | unit + integration | ✅ OK |
| T19 | Domain logic (derivação) + consumer | unit + integration | unit + integration | ✅ OK |
| T20 | Hook frontend | unit (Vitest) | unit | ✅ OK |
| T21-T26 | Componente frontend | unit (Vitest+RTL) | unit | ✅ OK |
| T27 | Gate/docs, sem código de produto | none (gate final) | none | ✅ OK |

Nenhuma violação — nenhum `Tests: none` aparece onde a matriz exige teste.

---

## Perguntas antes de Execute (MCPs/Skills)

Nenhum MCP específico foi solicitado além dos já disponíveis nesta sessão. Todas as tasks usam ferramentas padrão (Read/Edit/Write/Bash) e a skill `tlc-spec-driven` pro fluxo de Execute — nenhuma outra skill do pacote (ex.: `security-review`, `pr-review`) é necessária dentro das tasks em si (ficam pra depois do Execute, se o usuário quiser rodar `pr-review` no PR final).

Esta feature tem 6 fases (Phase 0-6) — acima do limiar de 3 que aciona a oferta de sub-agent por fase. **Pergunta pro usuário, antes de Execute começar**: quer que eu ofereça 1 sub-agent por fase (execução sequencial, cada um roda todas as tasks da sua fase e reporta um resumo compacto), ou prefere que eu execute tudo inline nesta janela?
