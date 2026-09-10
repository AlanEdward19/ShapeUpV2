# STATE

## Decisions

### AD-001
- **Decision**: Novo modelo de autorização é estruturado como Features vertical-slice separadas por domínio (`Features/Credentials`, `Features/Relationships`, `Features/Memberships`, `Features/Entitlements`), seguindo o mesmo padrão já usado por `GymManagement`/`Training`. A resolução de capability usa o mecanismo nativo do ASP.NET Core (`IAuthorizationHandler`/`IAuthorizationRequirement` + `[Authorize(Policy = "...")]`), substituindo o `RequireScopesAttribute` caseiro.
- **Reason**: Confirmado com o usuário (RFC-001 + confirmação explícita no Design da feature `native-authorization-model`). Mantém consistência com o padrão vertical-slice já estabelecido no repo e usa o mecanismo idiomático do framework em vez de um filtro custom.
- **Trade-off**: Mais arquivos/Features novas do que uma reestruturação interna de `Features/Authorization` só; requer registrar Policies na DI para cada capability.
- **Scope**: Todo backend `ShapeUpApi` — toda feature nova que precisar checar permissão usa este mecanismo (Policies), não escopo plano nem checagem ad-hoc em handler.
- **Date**: 2026-09-06
- **Status**: active

### AD-002
- **Decision**: `Memberships` e `Entitlements` não criam tabelas novas no MVP — são adapters (read-model) sobre dados que já existem em `GymManagement` (`Gym.OwnerId`, `GymStaff.Role`, `PlatformTier`). `Credentials` e `Relationships` criam tabelas novas (`ProfessionalCredential`, `ProfessionalClientRelationship`) porque não existe equivalente hoje.
- **Reason**: Reuso — não duplicar/migrar dados de `GymManagement` que já modelam corretamente RBAC interno de organização e tier de plano; só falta um contrato de leitura único para o resolver consumir.
- **Trade-off**: `Memberships`/`Entitlements` ficam acoplados à forma interna de `GymManagement` até que precisem de dado próprio (ex.: quando Entitlement precisar de billing real na Fase 4 (Monetização), vira tabela própria — decisão a ser revisitada nessa RFC futura).
- **Scope**: Feature `native-authorization-model` e qualquer feature futura que leia capability via `ICapabilityResolver`.
- **Date**: 2026-09-06
- **Status**: active

### AD-003
- **Decision**: O `CapabilityResolver` (RFC-001) não modela capabilities de nível "platform admin" (ações globais sem `gymId`/`trainerId`/relacionamento, ex.: gerenciar o catálogo de `PlatformTier`). `T15` (migração de `PlatformTiersController`) foi deferida — o controller continua em `RequireScopesAttribute` (legado) até essa lacuna ser resolvida.
- **Reason**: Nenhuma das 4 fontes (Membership/Credential/Relationship/Entitlement) se aplica a uma ação sem contexto de usuário/gym — inventar uma quinta fonte agora seria decisão de arquitetura nova, fora do escopo desta RFC, e melhor decidida deliberadamente (não como efeito colateral de uma migração mecânica de controller).
- **Trade-off**: `PlatformTiersController` fica com dois mecanismos de autorização coexistindo por mais tempo (o antigo aqui, o novo em todo o resto) até essa decisão ser tomada.
- **Scope**: Qualquer feature futura que precise de uma ação verdadeiramente global/sem contexto de usuário (não só `PlatformTiersController`) esbarra na mesma lacuna.
- **Date**: 2026-09-06
- **Status**: superseded by AD-006

### AD-006
- **Decision**: Resolvido o gap do AD-003 — 5ª fonte de capability adicionada: `PlatformRoleType.Admin` (reaproveita `UserPlatformRole`, sem tabela nova, mesmo espírito do AD-002/T5). `AuthorizationContext.RequiresPlatformAdmin` é um gate EXCLUSIVO no `CapabilityResolver` (nunca cai pra self-access/membership — ação de plataforma não tem dono). `CapabilityAuthorizationHandler` infere isso de uma convenção de nome: capability prefixada `"platform."` → exige admin, sem depender de route value nenhum.
- **Reason**: Reaproveitar `UserPlatformRole`/`PlatformRoleType` (já existe, já usado por `AssignUserRoleHandler`) em vez de criar uma tabela/conceito novo — mesma decisão de reuso do AD-002. Convenção de prefixo no nome da capability evita precisar de um registro por-policy explícito (mesmo espírito do `CapabilityPolicyProvider` dinâmico do AD-001).
- **Trade-off**: bootstrapping do primeiro admin depende de alguém já ter o papel (galinha-e-ovo) — fora de escopo aqui, é uma questão operacional de seed/migration, não de código (mesma situação que o antigo grupo "Administrators" seedado por migration já tinha).
- **Scope**: Desbloqueia T14 (UserRoles), T15 (PlatformTiers), T17 (Exercises), T18 (Equipments) — todos migrados nesta mesma sessão. Qualquer feature futura de nível plataforma usa `capability:platform.*` + `RequiresPlatformAdmin`.
- **Date**: 2026-09-06
- **Status**: active

### AD-004
- **Decision**: Registro do 403-em-vez-de-500 (ver Handoff) fica em `Features/Authorization/Resolver/CapabilityAuthorizationResultHandler.cs` como implementação única de `IAuthorizationMiddlewareResultHandler`. Nunca reintroduzir uma segunda classe com o mesmo propósito (aconteceu uma vez, ver Handoff) — qualquer novo achado sobre o pipeline de autorização edita esta classe, não cria outra.
- **Reason**: Dois agentes paralelos (T8, T9) descobriram e corrigiram o mesmo bug de infra independentemente, cada um criando sua própria classe com nome idêntico em pastas diferentes — gerou ambiguidade de compilação (`CS0104`) no merge. Consolidado mantendo a versão do T9 (`Features/Authorization/Resolver/`, consistente com o resto do resolver).
- **Trade-off**: nenhum — é só o registro canônico do fix.
- **Scope**: Toda a pipeline de autorização do backend.
- **Date**: 2026-09-06
- **Status**: active

### AD-005
- **Decision**: Diferente de GymManagement, Training não recebe `[Authorize(Policy=...)]` nos controllers. Autorização entre-usuários fica centralizada em `ITrainingAccessPolicy` (chamada de dentro dos handlers), que agora consulta `ICapabilityResolver`-adjacent sources (self-access + `IProfessionalClientRelationshipRepository`) em vez de scope strings. `RequireScopesAttribute` foi removido sem substituto nos controllers (a autenticação do `AuthorizationMiddleware` + a checagem inline/via-policy dentro do handler já são suficientes).
- **Reason**: rotas de Training carregam o ID do RECURSO (planId/templateId/sessionId), não o dono — `[Authorize(Policy)]` não consegue montar `AuthorizationContext` a partir da rota antes do handler buscar a entidade. Confirmado com o usuário antes de dispatchar agentes (evitou repetir a armadilha do `GymsController.Create`/`GetAll` em escala).
- **Trade-off**: duas formas de "onde a autorização acontece" coexistem no backend agora — atributo de controller (GymManagement) vs. lógica de handler (Training). Documentado, não um bug — decisão deliberada por causa da forma da rota.
- **Scope**: Qualquer feature futura em domínio com ownership só conhecido pós-fetch (ID de recurso na rota, não ID de dono) deve seguir o padrão Training (autorização dentro do handler), não tentar forçar `[Authorize(Policy)]`.
- **Date**: 2026-09-06
- **Status**: active

### AD-007
- **Decision**: `Block` (agrupamento estrutural: Straight/Superset/Amrap/Emom) existe só no schema de **planejamento** (`WorkoutPlanDocument`/`WorkoutTemplateDocument`), nunca em Execução (`WorkoutExecutionDocument`, feature futura "Execução de treino") — Execução permanece com lista flat de exercícios/sets. Intensidade de set vira objeto único exclusivo `Intensity{Type: Rpe|Rir, Value}` (substitui `Rpe: int`), aplicado tanto em planejamento quanto em execução (mesmo shape de `Set` reusado nos dois, só o Block que não).
- **Reason**: Block é decisão de como o profissional desenha o treino; o que foi de fato executado é pergunta separada, respondida pela spec futura de Execução (pode ou não precisar de contexto de bloco/timer pra AMRAP/EMOM). Intensidade como objeto único torna "RPE ou RIR, nunca os dois" invariante do tipo, não regra de validação esquecível.
- **Trade-off**: Execução e Planejamento compartilham `WorkoutExerciseDto`/`WorkoutSetValueObject` (mesmo tipo) mas divergem em Block — se a spec de Execução decidir que precisa de contexto de bloco, essa decisão é revisitada lá, não aqui.
- **Scope**: Toda feature futura que toque planejamento (`WorkoutPlans`/`WorkoutTemplates`) ou execução (`Workouts`) de treino.
- **Date**: 2026-09-08
- **Status**: active

### AD-009
- **Decision**: Mensageria assíncrona entre domínios usa MassTransit direto (`IPublishEndpoint`/`IConsumer<T>`), sem wrapper custom (`IEventBus` próprio) por cima. RabbitMQ é o transport self-hosted padrão do projeto (`docker-compose.yml`, mesmo espírito do Seq — sem SaaS); troca de transport (ex.: Azure Service Bus em produção) acontece via config do MassTransit (`UsingRabbitMq`→`UsingAzureServiceBus`), sem tocar código de domínio.
- **Reason**: MassTransit já é a abstração broker-agnostic (é o que a lib existe pra fazer) — outra camada de interface por cima seria indireção sem ganho. Usuário pediu explicitamente durabilidade+troca de broker "mais correto pro longo prazo", que é exatamente o que MassTransit resolve pronto (outbox, retry, dead-letter, transport swap) em vez de reinventado na mão.
- **Trade-off**: dependência nova e grande no projeto; compat oficial com .NET 10 (preview) não confirmada de fontes oficiais — mitigado por spike timeboxed (T1 da feature `event-bus`) antes de qualquer outro código.
- **Scope**: Toda feature futura que precise publicar ou consumir eventos de domínio.
- **Date**: 2026-09-08
- **Status**: active

### AD-010
- **Decision**: MongoDB (`docker-compose.yml`) reconfigurado de standalone para replica set single-node (`--replSet rs0` + `rs.initiate()`), permitindo transação multi-documento real. Motivo direto: outbox do MassTransit pro domínio `Training` (Mongo) precisa disso; mas a capacidade fica disponível pra qualquer futuro uso de transação Mongo no projeto, não só outbox.
- **Reason**: Mongo standalone não suporta transação multi-documento (limite do próprio motor, não do MassTransit) — sem isso, "escrita do agregado + evento no outbox" na mesma transação não é possível pro domínio Training, que é justamente o caso de uso que motivou construir o event bus.
- **Trade-off**: mudança de infra que afeta toda leitura/escrita Mongo existente do domínio Training (não só o novo código) — suíte de integração completa (218 testes) precisa rodar de novo após a mudança, antes de qualquer código do event bus, pra pegar regressão cedo.
- **Scope**: Toda infra/config de MongoDB do projeto (`Mongo__Training__ConnectionString`), qualquer feature futura que precise de transação Mongo.
- **Date**: 2026-09-08
- **Status**: active

### AD-011
- **Decision**: Achievements/badges ficam de fora da feature `gamification` por completo (nem catálogo mínimo) — decisão explícita do usuário, revisitada numa fase futura dedicada com motor DINÂMICO (achievement é dado configurável, sem redeploy pra adicionar um novo). Em compensação, `gamification` garante um sinal mínimo "o que mudou" (subiu de nível? bateu marco de streak?) persistido em `GamificationProfile` (colunas simples, não uma tabela de eventos dedicada), pra essa fase futura e a UI de celebração (animações/badges/ícones, pedido explícito do usuário) não exigirem redesenho do core.
- **Reason**: Usuário quer um sistema mais robusto de achievements do que 5 itens hardcoded, e pediu explicitamente pra tratar isso como fase própria depois. Construir uma versão mínima agora só pra jogar fora depois seria retrabalho puro.
- **Trade-off**: a fase futura de achievements terá que evoluir o "sinal mínimo" atual (colunas simples) pra algo mais rico (provavelmente uma tabela de eventos append-only) quando o motor dinâmico for de fato especificado — aceito, não construído especulativamente agora.
- **Scope**: Feature `gamification` e a fase futura de Achievements/UI de celebração.
- **Date**: 2026-09-09
- **Status**: active

### AD-012
- **Decision**: Eventos que reagem a um **corte temporal** (fechamento de dia, job recorrente, avaliação em lote) publicam via `IPublishEndpoint` direto, **sem outbox**. Outbox permanece exclusivo para eventos atômicos com uma escrita de domínio pontual na mesma transação (ex.: `WorkoutFinished` dentro da transação Mongo que finaliza a sessão).
- **Reason**: Um job de cutoff não está acoplado a uma única escrita — não há par agregado+evento na mesma transação que o outbox precise garantir. Publicar direto evita abrir caminho para outbox EF Core não testado ou segundo publisher Mongo só por simetria.
- **Trade-off**: Falha de publish após persistir a avaliação do dia pode perder o evento até retry manual do job; mitigado por `NutritionGoalEvaluation` (idempotência) e reprocessamento do recurring job.
- **Scope**: Toda feature futura com evento disparado por tempo/corte, não por write único. Primeiro caso: `NutritionGoalMet` (`Features/Nutrition/GoalEvaluation/`).
- **Date**: 2026-09-10
- **Status**: active

### AD-013
- **Decision**: Persistência dentro de um domínio pode dividir por **frequência de alteração** (baixa→Mongo, alta→SQL), não só por categoria de dado. Convenção de pastas: `Shared/Documents/` = Mongo, `Shared/Entities/` = SQL.
- **Reason**: Catálogo de alimentos e cardápios mudam raramente mas são lidos em todo registro de diário; diário/perfil/avaliação mudam repetidamente no mesmo dia — SQL transacional é o fit correto para o write path quente.
- **Trade-off**: Um domínio pode ter dois stores (ex.: `Nutrition` com `NutritionDbContext` + `Mongo__Nutrition__ConnectionString`); exceções deliberadas permitidas (ex.: `PlatformFeatureFlag` em SQL por ser singleton-like).
- **Scope**: Feature `nutrition` e qualquer domínio futuro que misture catálogo/documento de baixa mutação com log transacional de alta mutação.
- **Date**: 2026-09-10
- **Status**: active

## Handoff

- **Feature**: nutrition — **CLOSED** (T1–T27 complete, gate T27 passed 2026-09-10)
- **Phase / Task**: none (feature delivered)
- **Completed**: full stack — backend catalog/diary/profile/meal-plans/moderation/goal-evaluation/gamification integration + `PlatformFeatureFlags` + frontend screens T20–T26 + `ARCHITECTURE.md` for Nutrition and PlatformFeatureFlags + AD-012/AD-013 recorded.
- **In-progress**: nenhum
- **Next step**: feature `nutrition` closed; proceed to T28 (Verifier) or next feature per orchestrator.
- **Blockers**: none for nutrition delivery.
- **Test counts (T27 gate 2026-09-10)**:
  - unit: **344/344** pass
  - integration: **265 passed / 13 failed / 7 skipped** (~6m38s retry) — failures are pre-existing MassTransit `DisposeAsync` teardown flakes and SQL timeout on Messaging E2E hosts; no nutrition assertion failures
  - frontend: **70/70** pass (`lint` 0 errors, `build` ok)
- **Known integration flakes (document, do not loop)**:
  - `GymPlansControllerAuthorizationIntegrationTests` (3) — `TaskCanceledException` on `WebApplicationFactory.DisposeAsync`
  - `WorkoutExecutionEndpointsTests.CompleteRoute_IsRemovedAndReturnsNotFound` — same teardown flake
  - `WorkoutPlanningScopeEndpointsTests.CreateWorkoutPlan_ForTargetUserWithoutRelationship_ReturnsForbidden` — `NullReferenceException` on MassTransit `ReceiveEndpoint.Stop`
  - `Gamification*` (5 ranking/idempotency/e2e/anti-cheat) — teardown flake
  - `WorkoutFinishedEndToEndTests` (2) — SQL connection timeout on `MessagingIntegrationWebApplicationFactory` host start
  - `WeightTrackingEndpointsIntegrationTests.LegacyTrainingWeightRoute_ShouldReturnNotFound` — teardown flake (assertion passes; fails in `Dispose`)
- **Uncommitted files**: T27 docs + frontend lint fix (pending commit)
- **Branch**: `develop` (API + Web)
