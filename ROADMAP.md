# ROADMAP — ShapeUp Platform (Backend / ShapeUpApi)

> Fonte: `PRD — ShapeUp Platform.md` v1.1 (06/09/2026), seção 102 e correlatas.
> Sequência de dependências — fases não são paralelas: cada uma assume a anterior pronta.
> Este é o roadmap do **backend** (`ShapeUpApi`). O roadmap do frontend vive em `ShapeUp-Web/ROADMAP.md`. Fases são as mesmas nos dois arquivos (sequência de produto é única); os itens de cada fase é que foram filtrados por repo. Itens marcados **[Cross-repo]** aparecem nos dois arquivos porque exigem trabalho dos dois lados — a spec correspondente (quando existe) indica onde cada metade vive.

**North Star:** WAPU (Weekly Active Progress Users) — usuário conta quando realiza ação relevante de progresso na semana.
**Princípio estratégico:** Utility → Habit → Progress → Identity → Community → Network.

---

## Fase 0 — Current State Assessment (gate obrigatório)

Nenhuma expansão funcional começa sem mapear o estado real da plataforma.

- Feature inventory por status: ✅ funcional / 🟡 parcial / 🔵 frontend-mock / 🟣 backend-sem-front / 🔴 quebrado / ⚫ legado / ⚪ ausente **[Cross-repo]**
- Auditoria backend (rotas, contratos, regras de negócio, auth, queries, índices, cache, filas, jobs, idempotência, observabilidade, testes)
- Auditoria de arquitetura (o que escala, o que não escala, o que custa caro, o que está acoplado demais) **[Cross-repo]**
- Dívida técnica, custos atuais, segurança **[Cross-repo]**

## Fase 1 — Foundation

Base de identidade e autorização que toda feature seguinte assume.

- Identity model (`User` único, múltiplas capacidades sobre ele)
- Credential model (`ProfessionalCredential`, estados DRAFT→VERIFIED→EXPIRED/REVOKED)
- Autorização contextual (Identidade + Credenciais + Relacionamentos + Membership + Entitlements — não RBAC global) — **DONE**, ver [`docs/rfcs/rfc-001-authorization-model.md`](docs/rfcs/rfc-001-authorization-model.md) e `ShapeUpApi/Features/Authorization/ARCHITECTURE.md`
- Observabilidade day one **[Cross-repo]** (SLIs: availability, p95/p99, error rate, payment success, sync success, queue delay, crash-free sessions) — **fundação pronta, testada ponta a ponta**: backend (`ShapeUpApi`) exporta traces/metrics/logs via OpenTelemetry (OTLP/HTTP) pro Seq self-hosted (`docker-compose.yml`, sem conta SaaS), `/health/live` + `/health/ready` cobrem availability, p95/p99 e error rate vêm do histograma padrão do ASP.NET Core instrumentation. Frontend (`ShapeUp-Web`) tem `ErrorBoundary` + `telemetry.js` (crash-free sessions, local) e `mutationQueue.js` emite eventos de sync success/queue delay. Payment success: sem feature de billing ainda (Fase 4), nada pra instrumentar
- Analytics, CI/CD (lint→typecheck→unit→integration→build→security scan→preview→E2E→prod) **[Cross-repo]** — pipeline do backend própria; frontend tem a dela em `ShapeUp-Web/ROADMAP.md`
- Offline foundation — backend aceita id gerado no cliente como id real (`StartWorkoutExecutionCommand.Id`/`CreateWorkoutPlanCommand.Id`, formato Mongo ObjectId) para online/offline sem reconciliação — mecanismo consumido pelo motor de fila do frontend (ver `ShapeUp-Web/ROADMAP.md` para o resto do mecanismo — mutation queue, hooks, UI)

## Fase 2 — Core Fitness

O motivo de abrir o app todo dia. Sem isso nada de profissional/social tem o que orbitar. **[Cross-repo]** — cada item tem contraparte de API + UI; a decomposição por repo acontece quando a feature entra em Specify (ver `ShapeUp-Web/ROADMAP.md` para a mesma lista do lado frontend).

- Editor de treino (séries, reps, carga, RPE/RIR, técnicas avançadas: superset, drop set, rest-pause, AMRAP, EMOM...) — spec: `.specs/features/workout-editor`
- Execução de treino (fluxo: selecionar→iniciar→registrar→descanso→finalizar→resumo→XP→ShapeCoins)
- Nutrição (alimentos, refeições, macros, micronutrientes) — spec: `.specs/features/nutrition`
- Métricas (básicas: séries/reps/peso/duração; intermediárias: volume/PRs; avançadas: densidade/RPE/RIR/aderência)

## Fase 3 — Gamification

⚠️ Anti-cheating é pré-requisito, não segue-junto. **[Cross-repo]** — spec: `.specs/features/gamification`.

- XP, níveis, streak, achievements, badges
- ShapeCoins (moeda virtual — ganho por treino/meta/streak/desafio)
- ShapeScore & rankings (consistência + evolução + metas + atividades verificadas + desafios — não é volume bruto)
- **Anti-cheating obrigatório antes do lançamento**: detecção de atividade impossível, duplicação, GPS incompatível, volumes anormais (estados: Verified/Likely Valid/Suspicious/Invalid)

## Fase 3.5 — Polish pós-Gamificação (gate antes de Monetização)

Achados de uso real da Fase 2/3 (execução de treino, dashboard, nutrição) levantados após `Gamification`/`nutrition` fecharem Verifier. Não é feature nova — é fechar lacunas de UX/consistência antes de abrir superfície de cobrança. Itens de UI pura (Privacy/Terms, forgot-password, nutrição visual, lazy loading, retema, drawer de exercício) estão só em `ShapeUp-Web/ROADMAP.md`.

- Execução de treino **[Cross-repo]**: permite check de série sem peso/reps preenchidos; RPE deveria ser opcional por padrão mas configurável como obrigatório na montagem do treino (por exercício + botão "aplicar a todos"); RPE aceita qualquer valor (deve limitar 1–10); tipo de descanso e tipo de treino não respeitam idioma selecionado (sempre em inglês) — spec: `.specs/features/workout-execution-validation`
- Ganho de XP **[Cross-repo]**: animação customizada de ganho de XP ao finalizar treino (popup, placeholder de imagem até mascote existir); dashboard: barra de progresso de XP sempre zerada após treino (XP não reflete no nível/ShapeScore exibido) — spec: `.specs/features/xp-feedback-loop`
- Dashboard **[Cross-repo]**: card "Exercícios Prescritos para Hoje" só deve aparecer quando o plano de treino tiver dias da semana específicos atribuídos (feature nova: atribuir dia da semana a um treino na montagem do plano, opcional); card de frequência deve refletir a quantidade real de treinos do plano do usuário, não um valor fixo — spec: `.specs/features/workout-schedule-dashboard`
- Exercícios com variações/equivalentes **[Cross-repo]**: ao cadastrar um exercício, marcar quais exercícios são equivalentes (API); aparece na biblioteca (detalhe do exercício) e na execução do treino (botão de troca rápida quando o aparelho está ocupado) — spec: `.specs/features/exercise-variations`
- Treino baseado em tempo **[Cross-repo]** (corrida, resistência, alongamento) além de carga/reps — hoje a execução assume peso+reps sempre; sem isso a Fase 2 (Core Fitness) fica incompleta. Inclui PR equivalente por ritmo/pace para exercícios com distância (corrida); alongamento e afins não geram PR — spec: `.specs/features/time-based-exercises`

## Fase 4 — Monetização

Só depois que há uso real para cobrar.

- Tiers e entitlements (`maxWorkouts`, `maxClients`, `aiUsageQuota`, etc. — regras comerciais centralizadas, não espalhadas)
- Subscriptions, Payments (Charge/Payment/Refund/Invoice/Commission/Payout)
- Marketplace + take rate (Pagamento − Gateway − ShapeUp Take Rate = Repasse, variável por tier)

## Fase 5 — Professionals

Lado 1 do marketplace. Depende de verificação de credencial funcionando. **[Cross-repo]** — ver também `ShapeUp-Web/ROADMAP.md`.

- Verificação profissional (CREF, CRN — fluxo submissão→revisão→aprovação, badge de verificado)
- Gestão de alunos (convidar/adicionar/arquivar/acompanhar)
- Anamnese (formulários customizáveis)
- Dashboard profissional (alunos ativos, em risco, aderência, check-ins)
- Reputação (avaliações verificadas — só quem teve relacionamento válido avalia)
- Descoberta de profissionais (busca local, filtros de distância/especialidade/preço)

## Fase 6 — Mobile & Offline

Mobile-first e offline-first viram app de fato. Deliberadamente depois de Monetização/Gamification: prioridade é deixar o web quase completo antes de investir em mobile.

- Sync (mutation queue com `operation_id` para idempotência — crítico em treino, ShapeCoins, pagamentos, check-ins) — contraparte backend do mecanismo; apps móveis em si estão em `ShapeUp-Web/ROADMAP.md` (ou repo mobile futuro)
- Wearable foundation (HealthKit, Health Connect — Garmin/Fitbit/Whoop/Oura ficam para depois)

## Fase 7 — Social

⚠️ Social safety é pré-requisito, não segue-junto. **[Cross-repo]** — ver também `ShapeUp-Web/ROADMAP.md`.

- Social graph (Follow/Friendship/Block/Mute)
- Feed (treino concluído, PR, streak, desafio, conquista)
- Grupos (amigos competem/batalham entre si — treino, dieta e metas — ranking por quem evolui mais ou está mais focado)
- People You May Know (amigos de amigos, academia em comum, profissional em comum, interesses — nunca expor sinal sensível cru: "pessoas da sua região", nunca "mora a 240m de você")
- **Social safety antes de lançar**: block, mute, report, moderação, privacy settings

## Fase 8 — Academies

Lado 2 do marketplace — terceiro vértice do triângulo usuário↔profissional↔academia. **[Cross-repo]**

- Organizations + equipe (RBAC interno: Owner/Manager/Receptionist/Finance/Staff)
- CRM (pipeline: lead→contato→visita→trial→negociação→matriculado)
- Mapa de academias (filtros distância/preço/avaliação/modalidade)
- Planos & check-in (MVP: QR code / código temporário / confirmação manual; futuro: catraca/NFC)

## Fase 9 — ShapeUp Pass

Rede multi-academia estilo Wellhub/Gympass. Exige eligibility + payout + fraud prevention ponta a ponta antes de escalar.

- Rede de academias participantes
- Eligibility e validação de check-in
- Payouts (repasse às academias)
- Fraud prevention

## Fase 10 — AI Expansion

IA como assistente — nunca substitui julgamento profissional; decisão final é sempre humana. **[Cross-repo]**

- Copiloto para usuário (explicar métricas, resumir evolução, identificar tendências)
- Copiloto para profissional (anamnese + histórico + métricas → resumo + pontos relevantes + perguntas sugeridas)
- Multimodal (fotos — exige consentimento, proteção de dados, acesso restrito, exclusão, retenção configurável)

## Fase 11 — Ecosystem

Só depois que fases 0–10 seguram peso de produção.

- APIs públicas
- Wearables adicionais (Garmin, Fitbit, Polar, Whoop, Oura)
- Parceiros
- Internacionalização plena, enterprise

---

## Riscos transversais (mitigação por fase)

| Risco | Mitigação | Onde mitigar |
|---|---|---|
| Escopo excessivo | Construir jornadas completas por etapa, não fatias horizontais | Todas |
| Sistema de permissões complexo | Separar Identity / Credentials / Relationships / Memberships / Entitlements | Fase 1 |
| Profissionais falsos | Verificação obrigatória de credencial | Fase 5 |
| Reviews fraudulentas | Avaliações só de relacionamento verificado | Fase 5 |
| Recomendações sociais invasivas | Privacy controls, nunca expor sinal sensível bruto | Fase 7 |
| Gamificação manipulável | Anti-fraud antes do lançamento | Fase 3 |
| Custo de IA | Quotas e limites (`aiUsageQuota`) | Fase 4 / 10 |

## Definition of Done (aplicável a toda feature, todas as fases)

UX · Responsivo · Light/Dark · i18n · Autorização · Validação · API · Testes · Analytics · Logs · Error handling · Offline behavior · Loading/Empty/Error states · Acessibilidade (WCAG 2.2 AA) · Documentação

---

## Próximos passos sugeridos

1. Rodar Fase 0 (assessment) antes de qualquer commitment de fase seguinte.
2. Para cada fase, ao entrar em execução: usar `tlc-spec-driven` (via `agentic-delivery`) para especificar feature a feature — este roadmap é o mapa, não a spec.
3. Harness (`harness-engineering`) uma vez por repo, atualizado incrementalmente por fase.
