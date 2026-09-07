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
- **Trade-off**: `Memberships`/`Entitlements` ficam acoplados à forma interna de `GymManagement` até que precisem de dado próprio (ex.: quando Entitlement precisar de billing real na Fase 5, vira tabela própria — decisão a ser revisitada nessa RFC futura).
- **Scope**: Feature `native-authorization-model` e qualquer feature futura que leia capability via `ICapabilityResolver`.
- **Date**: 2026-09-06
- **Status**: active

### AD-003
- **Decision**: O `CapabilityResolver` (RFC-001) não modela capabilities de nível "platform admin" (ações globais sem `gymId`/`trainerId`/relacionamento, ex.: gerenciar o catálogo de `PlatformTier`). `T15` (migração de `PlatformTiersController`) foi deferida — o controller continua em `RequireScopesAttribute` (legado) até essa lacuna ser resolvida.
- **Reason**: Nenhuma das 4 fontes (Membership/Credential/Relationship/Entitlement) se aplica a uma ação sem contexto de usuário/gym — inventar uma quinta fonte agora seria decisão de arquitetura nova, fora do escopo desta RFC, e melhor decidida deliberadamente (não como efeito colateral de uma migração mecânica de controller).
- **Trade-off**: `PlatformTiersController` fica com dois mecanismos de autorização coexistindo por mais tempo (o antigo aqui, o novo em todo o resto) até essa decisão ser tomada.
- **Scope**: Qualquer feature futura que precise de uma ação verdadeiramente global/sem contexto de usuário (não só `PlatformTiersController`) esbarra na mesma lacuna.
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

## Handoff

- **Feature**: native-authorization-model (`ShapeUpApi/.specs/features/native-authorization-model/`)
- **Phase / Task**: Phase 1 (T1-T7), Phase 2 (T8-T13, T16) e Phase 3 (T19-T24, redesenhada — ver AD-005) completas e mergeadas em `feature/native-authorization-model`. T14/T15/T17/T18 deferidos (AD-003). Falta: Phase 4 (T25-T27, descomissionamento do legado `Group/Scope`).
- **Completed**:
  - Phase 1: T1-T7 (schema Credentials/Relationships, adapters Memberships/Entitlements, CapabilityResolver + handler + policy provider nativos).
  - Phase 2: T8 (Gyms — GetById/Update/Delete; GetAll/Create ficaram no legado, sem gymId), T9 (GymStaff), T10 (GymPlans), T11 (GymClients), T12 (TrainerPlans — só self-access), T13 (TrainerClients — só self-access, AcceptInvite não migrado), T16 (satisfeito via cobertura distribuída).
  - Phase 3 (redesenhada, ver AD-005): `TrainingAccessPolicy` modernizada (self-access + Relationships + checks nativos do GymManagement mantidos incondicionais). `RequireScopesAttribute` removido de WorkoutPlans/WorkoutTemplates/Workouts/WeightTracking/Dashboard (5 controllers). Exercises/Equipments deferidos (catálogo admin-curado, mesmo gap AD-003). T24 satisfeito via cobertura distribuída.
  - **Bug crítico achado e corrigido durante a Fase 2**: sem `AddAuthentication` registrado, toda negação de `[Authorize(Policy=...)]` virava `500` em vez de `403` — corrigido por `CapabilityAuthorizationResultHandler` (AD-004).
  - **Gap achado durante a Fase 3**: `IntegrationWebApplicationFactory.cs` nunca registrava `RelationshipsDbContext` pro container de teste — ficou latente até os primeiros testes cross-user reais de Training. Corrigido (2 agentes acharam independentemente, merge trivial).
  - Suite final: 334 unit tests + 230 integration tests, 0 falhas.
- **In-progress**: nenhum
- **Next step**: Phase 4 — T25 (remover `Group/Scope/UserGroup/GroupScope` + `RequireScopesAttribute.cs`, migration de remoção; depende de T16 e T24, ambos satisfeitos), T26 (atualizar `ARCHITECTURE.md`/`README.md` de Authorization e criar os dos domínios novos), T27 (fechar RFC-001 e `CURRENT_STATE_ASSESSMENT.md`). **Atenção antes de rodar T25**: `Exercises`/`Equipments`/`PlatformTiers`/parte de `UserRoles` ainda usam `RequireScopesAttribute` (deferidos por AD-003) — T25 não pode remover o mecanismo enquanto esses controllers dependerem dele. Ou resolve AD-003 antes, ou T25 precisa migrar esses 4 controllers pra alguma coisa (mesmo que rudimentar) antes de deletar o legado.
- **Blockers**: T14 (UserRoles), T15 (PlatformTiers), T17 (Exercises), T18 (Equipments) seguem bloqueados por AD-003 (falta conceito de "platform admin" no resolver) — **isso agora bloqueia T25 diretamente**, não é mais só um item cosmético fora do caminho crítico.
- **Uncommitted files**: none (working tree limpo, todos os merges commitados)
- **Branch**: `feature/native-authorization-model` — histórico com merges (`--no-ff`) de 8 branches de task (T8-T13 + 2 de Phase 3), cada uma via worktree isolada (já removida). Nenhuma pushed para `origin`.
- **Lição de processo (para não repetir)**: ao mover o ponteiro de uma branch de worktree para um commit mais novo com trabalho não commitado no meio, usar `git stash` + `git rebase` (ou `git checkout <branch-alvo> -- <arquivos-que-a-task-não-tocou>`) — nunca `git reset --soft` sozinho (não atualiza a working tree). Também: antes de assumir que um domínio segue o mesmo padrão de migração de outro, confirmar se o ID na rota é o do RECURSO ou o do DONO — Training quebrou essa suposição e exigiu redesenho a meio do caminho.
