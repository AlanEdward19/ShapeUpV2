# Native Authorization Model Specification

## Problem Statement

O backend do ShapeUp autoriza requisições com um RBAC plano (`Group/Scope/UserGroup/GroupScope`, string `domain:subdomain:action`), verificado uma vez por request via `RequireScopesAttribute` — sem contexto (não distingue "staff da academia 1" de "staff da academia 2"; hoje isso é remendado por checagem manual dentro de cada handler, ex. `IsOwnerOrReceptionistAsync` em `AddGymStaffHandler`). O PRD do produto exige permissão = Identidade + Credenciais + Relacionamentos + Membership + Entitlements + Contexto, e nenhuma das fases seguintes do roadmap (Professionals, Marketplace, Academias, Monetização) pode ser construída sobre RBAC plano sem repetir esse remendo em cada handler novo. `docs/rfcs/rfc-001-authorization-model.md` já decidiu (Opção 2): redesenho nativo, descartando o schema atual, sem camada de compatibilidade.

## Goals

- [ ] Autorização deixa de ser checagem plana de string + lógica ad-hoc por handler — vira resolução centralizada `(user, ação, contexto) → allow/deny`, com contexto de organização como cidadão de primeira classe (não bolt-on)
- [ ] GymManagement e Training passam a consumir o resolver novo, sem `RequireScopesAttribute`/`IsOwnerOrReceptionistAsync`-style checks espalhados
- [ ] Schema para Credentials/Relationships/Entitlements existe e participa da resolução, pronto para as features de Fase 3/5 do roadmap não precisarem de outra RFC de schema
- [ ] Zero downtime de autenticação — Firebase Auth (`firebase_uid`, login, MFA) não muda em nenhum ponto

## Out of Scope

| Feature | Reason |
|---|---|
| Fluxo de verificação de credencial profissional (upload de documento, fila de revisão manual/automática, badge público) | É a feature de produto "Professional Verification" (Fase 3 do `ROADMAP.md`) — esta spec só cria o schema/estado que o resolver consulta, não a UX de submissão/aprovação |
| Cálculo de Entitlement a partir de billing real (Stripe/gateway, cobrança recorrente) | Fase 5 (Monetização) do roadmap — esta spec só cria o ponto de extensão (`Entitlement` deriva de `PlatformTier` já existente), não o fluxo de pagamento |
| Autenticação (login, senha, MFA, social login, refresh de token Firebase) | Explicitamente fora de escopo pela RFC — Firebase Auth intocado |
| Rate limiting / API gateway | Gap conhecido do backend (visto no `CURRENT_STATE_ASSESSMENT.md`), mas não é escopo desta feature de autorização — tratado separadamente na Fase 1 (Foundation) |
| Migração de dados de usuários reais | RFC Assumption 1: zero consumidores em produção hoje — não há dado real de usuário para migrar, só schema |
| Frontend (`ShapeUp-Web`) consumindo o novo modelo granularmente | Fora de escopo desta spec de backend; hoje o frontend só usa um `role` único em `localStorage` — atualizar o frontend é uma spec própria depois que o backend estabilizar |

---

## Assumptions & Open Questions

| Assumption / decision | Chosen default | Rationale | Confirmed? |
|---|---|---|---|
| Onde o `Entitlement` busca a fonte de verdade | Deriva do `PlatformTier` já atribuído ao usuário/gym em GymManagement (`UserPlatformRole`/`PlatformTier` existentes) — sem tabela de billing nova | RFC não pede modelo de billing agora; reaproveita o que já existe em GymManagement em vez de inventar novo conceito | n |
| Remoção física das tabelas legadas (`Group`, `Scope`, `UserGroup`, `GroupScope`) | Migration EF que dropa as tabelas, após GymManagement e Training migrados e validados (não no mesmo commit que cria o schema novo) | RFC pede descarte, mas zero-downtime de leitura durante a janela de migração exige ordem: criar novo → migrar consumidores → só então dropar antigo | n |
| Enforcement de transição de estado do `ProfessionalCredential` nesta spec | Cria o enum e valida transições (`DRAFT→SUBMITTED→UNDER_REVIEW→VERIFIED→REJECTED`, `VERIFIED→EXPIRED/SUSPENDED/REVOKED`) via guard no domínio, mas **sem** endpoint de submissão/revisão (isso é Fase 3) — só a entidade e a leitura pelo resolver | Schema precisa existir e ser consistente desde já para o resolver funcionar, mesmo sem UX de verificação ainda | n |
| Local estrutural do código novo (namespace/pasta) | Novo `Features/Authorization` reestruturado internamente (`Identity`, `Credentials`, `Relationships`, `Memberships`, `Entitlements`, `Resolver` como sub-áreas), não uma feature separada por domínio | Decisão de estrutura de pastas é Design, não Specify — assumption provisória para permitir escrever ACs; Design pode revisar | n |
| Fail-safe do resolver quando uma fonte de dado falha (ex.: banco de credenciais indisponível) | Deny-by-default — se qualquer fonte necessária não responde, a ação é negada, nunca permitida por omissão | Segurança: autorização deve falhar fechado, não aberto | n |
| Rate limiting / auditoria de decisão de autorização | Toda decisão (allow/deny) do resolver é logada via o `AuditLogs` já existente (motivo da negação incluso), mas criar rate limiting em si fica fora de escopo (ver Out of Scope) | Reaproveita infraestrutura de observabilidade já existente, sem inventar uma nova | n |

**Open questions:** none — todas as ambiguidades levantadas na varredura de dimensões (abaixo) estão resolvidas ou registradas acima.

---

## User Stories

### P1: Núcleo do modelo nativo + GymManagement migrado ⭐ MVP

**User Story**: Como desenvolvedor da ShapeUp, quero que toda checagem de autorização de GymManagement passe por um único serviço nativo de capabilities (não mais scope plano + checagem ad-hoc por handler), para que academias, staff e planos sejam autorizados de forma consistente e auditável.

**Why P1**: GymManagement é o maior consumidor atual (43 endpoints, `IsOwnerOrReceptionistAsync` e equivalentes espalhados por handler) — é onde o remendo hoje é mais visível e onde a inconsistência (checagem de scope + checagem de contexto separadas) mais arrisca um bug de autorização.

**Acceptance Criteria**:

1. WHEN um handler de GymManagement precisa autorizar uma ação THEN o sistema SHALL resolver a decisão através de um único serviço de resolução de capability (não mais `RequireScopesAttribute` + método de repositório ad-hoc combinados)
2. WHEN um usuário é `Owner` ou `Manager` de uma academia (`OrganizationMembership`) THEN o sistema SHALL permitir as ações de gestão de staff/planos daquela academia especificamente — não de outras academias onde o usuário não tem membership
3. WHEN um usuário tem `OrganizationMembership` do tipo `Staff`/`Receptionist`/`Finance` em uma academia THEN o sistema SHALL permitir apenas as ações que esse papel concede (equivalente ao RBAC interno hoje existente — seção 8 do PRD, `docs/rfcs/rfc-001-authorization-model.md` critério 4)
4. WHEN um usuário não tem nenhum `OrganizationMembership` na academia alvo da requisição THEN o sistema SHALL negar a ação com `403 Forbidden`, independente de qualquer outro escopo que o usuário possua
5. WHEN a resolução de capability falha por erro de infraestrutura (ex.: falha ao consultar membership) THEN o sistema SHALL negar a ação (deny-by-default) e retornar erro `500`, nunca permitir por omissão
6. WHEN uma decisão de autorização é tomada (allow ou deny) em endpoint de GymManagement THEN o sistema SHALL registrar a decisão no `AuditLogs` existente, incluindo o motivo quando negada
7. WHEN o usuário está autenticado via Firebase (token válido) THEN o sistema SHALL continuar identificando-o por `firebase_uid` sem nenhuma mudança no fluxo de login/token

**Independent Test**: Criar duas academias (A e B) com dois usuários (staff de A, staff de B); confirmar que staff de A não consegue gerenciar staff/planos de B (`403`), e que o Owner de A gerencia sua própria academia normalmente. Confirmar no `AuditLogs` que ambas as tentativas (permitida e negada) aparecem.

---

### P2: Training migrado para o mesmo resolver

**User Story**: Como desenvolvedor da ShapeUp, quero que Training (Exercises, WorkoutPlans, WorkoutTemplates, Workouts) autorize através do mesmo serviço nativo usado por GymManagement, para que não existam dois mecanismos de autorização paralelos no backend.

**Why P2**: Menor superfície que GymManagement, mas precisa estar no mesmo modelo antes de a Opção 2 poder considerar o legado removível — é o segundo maior consumidor de `RequireScopesAttribute` (5 controllers).

**Acceptance Criteria**:

1. WHEN um endpoint de Training (Exercises/WorkoutPlans/WorkoutTemplates/Workouts/Equipments/Dashboard) é chamado THEN o sistema SHALL autorizar via o mesmo serviço de resolução de capability usado por GymManagement, não mais `RequireScopesAttribute`
2. WHEN um treinador (via `ProfessionalClientRelationship` ativo, papel Trainer) acessa dados de treino de um cliente que não é seu THEN o sistema SHALL negar com `403`
3. WHEN um usuário comum (sem nenhuma credencial profissional) acessa seus próprios dados de treino THEN o sistema SHALL permitir normalmente (capability de dono dos próprios dados independe de credencial)

**Independent Test**: Criar um relacionamento treinador-cliente ativo entre A (trainer) e B (client); criar um cliente C sem relação com A. Confirmar que A acessa dados de treino de B, mas recebe `403` ao tentar acessar dados de C.

---

### P3: Descomissionamento do legado + schema pronto para Fase 3/5

**User Story**: Como desenvolvedor da ShapeUp, quero remover `Group/Scope/UserGroup/GroupScope` depois que GymManagement e Training estiverem migrados, e ter `ProfessionalCredential`/`ProfessionalClientRelationship`/`Entitlement` como schema estável, para que as próximas fases do roadmap (Professionals, Monetização) só precisem construir a UX/workflow em cima, sem outra RFC de modelagem.

**Why P3**: É o fechamento da RFC — só é seguro depois que P1 e P2 estiverem validados (nada mais lê o schema antigo).

**Acceptance Criteria**:

1. WHEN P1 e P2 estão validados (todos os testes passam sem depender de `Group/Scope/UserGroup/GroupScope`) THEN o sistema SHALL remover essas tabelas via migration EF
2. WHEN uma entidade `ProfessionalCredential` é criada em estado `DRAFT` THEN o sistema SHALL permitir apenas transições válidas na máquina de estado (`DRAFT→SUBMITTED→UNDER_REVIEW→VERIFIED→REJECTED`, `VERIFIED→EXPIRED/SUSPENDED/REVOKED`) e rejeitar qualquer transição fora dessa lista
3. WHEN uma credencial está em qualquer estado diferente de `VERIFIED` THEN o resolver SHALL tratá-la como inexistente para fins de capability (ex.: não concede `can_prescribe_training`) — mesmo sem o fluxo de submissão (Fase 3) ainda existir
4. WHEN um `Entitlement` é consultado para um usuário sem `PlatformTier` atribuído THEN o resolver SHALL aplicar o tier padrão (Free) e conceder apenas as capabilities desse tier

**Independent Test**: Rodar a suíte de testes completa de GymManagement e Training após a migration de remoção — zero referência a `Group/Scope` deve compilar ou passar; criar uma `ProfessionalCredential` em `DRAFT` e confirmar que o resolver não concede capability de prescrição até o estado virar `VERIFIED` manualmente via seed/fixture de teste.

---

## Edge Cases

- WHEN um usuário tem `OrganizationMembership` em múltiplas academias com papéis diferentes (ex.: Staff em A, Owner em B) THEN o sistema SHALL avaliar a capability no contexto da academia específica da requisição, nunca misturar papéis entre academias
- WHEN um `ProfessionalCredential` expira (`expires_at` no passado) THEN o resolver SHALL tratá-lo como não-verificado a partir desse instante, mesmo que o campo `status` armazenado ainda diga `VERIFIED` (checagem de expiração é sempre em tempo de resolução, não depende de job assíncrono para estar correta)
- WHEN um `ProfessionalClientRelationship` é encerrado (`ended_at` preenchido) THEN o sistema SHALL negar imediatamente qualquer capability derivada dele, mesmo que o registro continue no banco para histórico
- WHEN a requisição não tem contexto de organização (ex.: endpoint que não é `/gyms/{gymId}/...`) THEN o sistema SHALL resolver capability sem exigir `OrganizationMembership` — nem toda ação depende de organização
- WHEN dois requests concorrentes tentam criar `OrganizationMembership` duplicada para o mesmo (user, gym) THEN o sistema SHALL rejeitar a segunda com conflito (`409`), nunca duas linhas ativas para o mesmo par

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
|---|---|---|---|
| AUTHZ-01 | P1: resolver central substitui scope+ad-hoc | Design | Pending |
| AUTHZ-02 | P1: OrganizationMembership escopado por gym | Design | Pending |
| AUTHZ-03 | P1: papéis internos (Owner/Manager/Staff/Receptionist/Finance) | Design | Pending |
| AUTHZ-04 | P1: deny sem membership | Design | Pending |
| AUTHZ-05 | P1: deny-by-default em falha de infra | Design | Pending |
| AUTHZ-06 | P1: decisão auditada | Design | Pending |
| AUTHZ-07 | P1: Firebase Auth intocado | Design | Pending |
| AUTHZ-08 | P2: Training no mesmo resolver | Design | Pending |
| AUTHZ-09 | P2: relacionamento treinador-cliente | Design | Pending |
| AUTHZ-10 | P2: dono dos próprios dados | Design | Pending |
| AUTHZ-11 | P3: remoção do legado | Design | Pending |
| AUTHZ-12 | P3: máquina de estado de credencial | Design | Pending |
| AUTHZ-13 | P3: credencial não-verificada = capability negada | Design | Pending |
| AUTHZ-14 | P3: entitlement default (Free tier) | Design | Pending |

**ID format:** `AUTHZ-NN`

**Status values:** Pending → In Design → In Tasks → Implementing → Verified

**Coverage:** 14 total, 0 mapeados a tasks ainda, 14 não mapeados ⚠️ (mapeamento acontece na fase Tasks)

---

## Success Criteria

- [ ] Nenhum controller de GymManagement ou Training referencia `RequireScopesAttribute` ou faz checagem manual de membership dentro do handler (ex.: `IsOwnerOrReceptionistAsync`) — tudo passa pelo resolver único
- [ ] Staff de uma academia não consegue, em nenhum teste de integração, agir sobre outra academia sem membership lá
- [ ] `Group/Scope/UserGroup/GroupScope` removidos do schema sem quebrar nenhum teste de GymManagement/Training
- [ ] `ProfessionalCredential`, `ProfessionalClientRelationship`, `Entitlement` existem como schema consultável pelo resolver, mesmo sem UX de Fase 3/5 construída
- [ ] Login/token/MFA via Firebase seguem funcionando sem nenhuma alteração de comportamento observável
