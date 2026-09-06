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

## Handoff

- **Feature**: native-authorization-model (`ShapeUpApi/.specs/features/native-authorization-model/`)
- **Phase / Task**: Phase 1 (Foundation, T1-T7) completa e commitada. Próxima: Phase 2 (T8-T16, GymManagement) e Phase 3 (T17-T24, Training) — podem rodar em paralelo, cada uma sequencial internamente.
- **Completed**: T1 (Credentials entity/DbContext), T2 (Credentials repo+state guard), T3 (Relationships entity/DbContext), T4 (Relationships repo), T5 (Memberships adapter — estendeu `GymStaffRole` com Manager/Finance/Staff, decisão do usuário), T6 (Entitlements adapter), T7 (CapabilityResolver + CapabilityAuthorizationHandler + policies nativas). 327 unit tests + 138 integration tests passando, 0 falhas.
- **In-progress**: nenhum
- **Next step**: iniciar T8 (migrar `GymsController` para `[Authorize(Policy=...)]`) ou T17 (Training) — ambas dependem só de T7, que está pronto. Sem sub-agents ainda (usuário pediu execução direta na Fase 1; perguntar de novo antes de decidir sobre Fase 2/3, que têm 8 e 7 tasks paralelas respectivamente).
- **Blockers**: none
- **Uncommitted files**: none (working tree limpo após cada commit)
- **Branch**: `feature/native-authorization-model` (criado a partir de `main`, 7 commits de código + 1 de docs à frente)
