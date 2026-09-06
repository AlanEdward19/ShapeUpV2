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
- **Phase / Task**: Tasks concluído (27 tasks, 4 fases) — aguardando confirmação de tools/skills e início de Execute
- **Completed**: spec.md, design.md, tasks.md
- **In-progress**: nenhum
- **Next step**: perguntar ao usuário sobre MCPs/skills por task (fim de tasks.md) e então iniciar Execute pela Phase 1 (T1)
- **Blockers**: none
- **Uncommitted files**: `ShapeUpApi/.specs/**` (novos, ainda não commitados — commit só sob aprovação explícita do usuário)
- **Branch**: (não verificado — confirmar branch atual antes de commitar)
