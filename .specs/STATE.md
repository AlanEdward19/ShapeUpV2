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

## Handoff

- **Feature**: native-authorization-model (`ShapeUpApi/.specs/features/native-authorization-model/`)
- **Phase / Task**: **Todas as 4 fases completas (T1-T27).** Feature encerrada.
- **Completed**:
  - Phase 1: T1-T7 (schema Credentials/Relationships, adapters Memberships/Entitlements, CapabilityResolver + handler + policy provider nativos).
  - Phase 2: T8 (Gyms — GetById/Update/Delete; GetAll/Create ficaram no legado, sem gymId), T9 (GymStaff), T10 (GymPlans), T11 (GymClients), T12 (TrainerPlans — só self-access), T13 (TrainerClients — só self-access, AcceptInvite não migrado), T16 (satisfeito via cobertura distribuída).
  - Phase 3 (redesenhada, ver AD-005): `TrainingAccessPolicy` modernizada (self-access + Relationships + checks nativos do GymManagement mantidos incondicionais). `RequireScopesAttribute` removido de WorkoutPlans/WorkoutTemplates/Workouts/WeightTracking/Dashboard (5 controllers). T24 satisfeito via cobertura distribuída.
  - **AD-003 resolvido (AD-006)**: 5ª fonte de capability — `PlatformRoleType.Admin` (reaproveita `UserPlatformRole`, sem tabela nova). `T14` (UserRoles: `GetUserRolesById`+`Assign`), `T15` (PlatformTiers: Create/Update/Delete), `T17` (Exercises: Create/Update/Delete; Read/Suggest ficaram abertos a qualquer autenticado), `T18` (Equipments: idem Exercises) — todos migrados para `capability:platform.*`.
  - **Bug crítico achado e corrigido durante a Fase 2**: sem `AddAuthentication` registrado, toda negação de `[Authorize(Policy=...)]` virava `500` em vez de `403` — corrigido por `CapabilityAuthorizationResultHandler` (AD-004).
  - **Gap achado durante a Fase 3**: `IntegrationWebApplicationFactory.cs` nunca registrava `RelationshipsDbContext` pro container de teste — ficou latente até os primeiros testes cross-user reais de Training. Corrigido (2 agentes acharam independentemente, merge trivial).
  - **Gap achado ao resolver AD-003**: 47 testes de integração (4 arquivos) usavam Exercises/Equipments Create via HTTP como fixture de setup, assumindo que Scope bastava — todos quebraram ao virar `platform.*`. Corrigido com `TestDataSeeder.GrantPlatformAdminAsync`, aplicado CONDICIONALMENTE (só quando o teste realmente precisa de acesso de escrita ao catálogo) pra não mascarar o teste que prova que non-admin é negado.
  - **Phase 4 (T25-T27)**: antes de T25, migrados os 5 endpoints restantes ainda em `RequireScopesAttribute` (`NotificationsController` send_html/send_template → `platform.notifications.send`; `GetAuditLogsController` → `platform.audit_logs.read`; `GymsController.GetAll`/`Create` deixados sem policy, de propósito, por não terem `gymId` na rota). T25: `Group/Scope/UserGroup/GroupScope`, `RequireScopesAttribute.cs`, todos os handlers/testes associados removidos por completo; migration `RemoveLegacyGroupScopeRbac` dropa as 5 tabelas legadas; `AuthorizationDbContext` reduzido a só `Users`; `UserContext` perdeu o campo `Scopes`. T26: `Authorization/{ARCHITECTURE.md,README.md,EXAMPLES.http}` reescritos para o modelo nativo; `Notifications/ARCHITECTURE.md` e `AuditLogs/ARCHITECTURE.md` tiveram as menções residuais a `RequireScopesAttribute` corrigidas; `ARCHITECTURE.md` novo criado para `Credentials`, `Relationships`, `Memberships`, `Entitlements` (exigência do `AGENTS.md`). T27: RFC-001 (`Outcome`/`Follow-up`/nova seção `Implementation Status`), `CURRENT_STATE_ASSESSMENT.md` (nota de atualização no topo, corpo preservado como registro histórico) e `ROADMAP.md` (Fase 1 marcada DONE) atualizados com link pra RFC.
  - Suite final: 203 unit tests + 194 integration tests, 0 falhas (queda esperada vs. 339/233 — feature Group/Scope inteira deletada, incluindo seus próprios testes).
- **In-progress**: nenhum
- **Next step**: nenhum — feature `native-authorization-model` está encerrada. Próximo trabalho de autorização (se houver) é uma feature nova (ex: modelar o compound staff/gym-client check dentro do `CapabilityResolver` em vez de deixá-lo incondicional em `ITrainingAccessPolicy` — ver Future Enhancements em `Authorization/README.md`).
- **Blockers**: nenhum.
- **Uncommitted files**: none (working tree limpo, todos os merges commitados)
- **Branch**: `feature/native-authorization-model` — histórico com merges (`--no-ff`) de 8 branches de task (T8-T13 + 2 de Phase 3) + commits diretos (T17-T23 base, AD-003/AD-006). Nenhuma pushed para `origin`.
- **Lição de processo (para não repetir)**: ao mover o ponteiro de uma branch de worktree para um commit mais novo com trabalho não commitado no meio, usar `git stash` + `git rebase` (ou `git checkout <branch-alvo> -- <arquivos-que-a-task-não-tocou>`) — nunca `git reset --soft` sozinho (não atualiza a working tree). Também: antes de assumir que um domínio segue o mesmo padrão de migração de outro, confirmar se o ID na rota é o do RECURSO ou o do DONO — Training quebrou essa suposição e exigiu redesenho a meio do caminho.
