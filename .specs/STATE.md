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

## Handoff

- **Feature**: native-authorization-model (`ShapeUpApi/.specs/features/native-authorization-model/`)
- **Phase / Task**: Phase 1 (T1-T7) e Phase 2 (T8-T13) completas e mergeadas em `feature/native-authorization-model`. T14/T15 deferidos (AD-003). Falta: Phase 3 (T17-T24, Training) e Phase 4 (T25-T27, descomissionamento do legado).
- **Completed**:
  - Phase 1: T1-T7 (schema Credentials/Relationships, adapters Memberships/Entitlements, CapabilityResolver + handler + policy provider nativos).
  - Phase 2: T8 (Gyms — GetById/Update/Delete; GetAll/Create ficaram no legado, sem gymId), T9 (GymStaff), T10 (GymPlans), T11 (GymClients), T12 (TrainerPlans — só self-access), T13 (TrainerClients — só self-access, AcceptInvite não migrado), T16 (satisfeito via cobertura distribuída nos testes de T8-T11).
  - **Bug crítico achado e corrigido durante a Fase 2**: sem `AddAuthentication` registrado, toda negação de `[Authorize(Policy=...)]` virava `500` (Challenge) em vez de `403` (Forbid) — corrigido por `CapabilityAuthorizationResultHandler` (ver AD-004). Descoberto de forma independente por 3 agentes (T11, T12, T13) rodando em paralelo; a versão do T9 foi a que ficou.
  - Suite final: 328 unit tests (330 − 2 removidos intencionalmente no T10, checagem que virou responsabilidade da policy) + 197 integration tests, 0 falhas.
- **In-progress**: nenhum
- **Next step**: Phase 3 (T17-T23, migrar controllers de Training — mesmo padrão mecânico da Phase 2, o fix de infra já está pronto) seguido de T24 (teste de relacionamento treinador-cliente). Depois, Phase 4 (T25 remove `Group/Scope/UserGroup/GroupScope` — só depois que Phase 3 também estiver completa e validada).
- **Blockers**: T14 (UserRoles) e T15 (PlatformTiers) seguem bloqueados por AD-003 (falta conceito de "platform admin" no resolver) — não fazem parte do caminho crítico para Phase 3/4.
- **Uncommitted files**: none (working tree limpo, todos os merges commitados)
- **Branch**: `feature/native-authorization-model` — histórico com merges (`--no-ff`) de 6 branches de task (T8-T13), cada uma via worktree isolada (já removida). Nenhuma pushed para `origin`.
- **Lição de processo (para não repetir)**: ao mover o ponteiro de uma branch de worktree para um commit mais novo com trabalho não commitado no meio, usar `git stash` + `git rebase` (ou simplesmente `git checkout <branch-alvo> -- <arquivos-que-a-task-não-tocou>`) — nunca `git reset --soft` sozinho, porque ele não atualiza a working tree, deixando arquivos que a task não tocou desatualizados silenciosamente (isso quase re-quebrou T10-T13 com o bug de 500 já corrigido, exigindo recuperação manual).
