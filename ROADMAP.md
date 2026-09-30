# ROADMAP — ShapeUp Platform (Backend / ShapeUpApi)

> Fonte: `PRD — ShapeUp Platform.md` v1.1 (06/09/2026), seção 102 e correlatas. A ordem depois da Fase 3.5 segue a decisão de produto de 29 set 2026 ([ShapeUp — jornada, lançamento e preço](https://linear.app/arqontech/document/shapeup-jornada-lancamento-e-preco-c64703e5f9ba)).
> Sequência de dependências — fases não são paralelas: cada uma assume a anterior pronta. Fases 0 a 3.5 estão concluídas e não se reabrem.
> Este é o roadmap do **backend** (`ShapeUpApi`). O roadmap do frontend vive em `ShapeUp-Web/ROADMAP.md`. Fases são as mesmas nos dois arquivos (sequência de produto é única); os itens de cada fase é que foram filtrados por repo. Itens marcados **[Cross-repo]** aparecem nos dois arquivos porque exigem trabalho dos dois lados — a spec correspondente (quando existe) indica onde cada metade vive. Em item **[Cross-repo]**, o contrato da API vem primeiro; a UI consome o que já está estável.

**North Star:** WAPU (Weekly Active Progress Users) — usuário conta quando realiza ação relevante de progresso na semana, não quando só abre o app.
**Princípio estratégico:** Utility → Habit → Progress → Identity. O card da sessão se posta fora do app. Feed, seguidor e kudos ficam de fora. Chat e presença ao vivo vêm por último, antes de IA e ecossistema.

---

## Fase 0 — Current State Assessment (gate obrigatório)

**Concluída.** Histórico abaixo; esta fase não se reabre.

Nenhuma expansão funcional começa sem mapear o estado real da plataforma.

- Feature inventory por status: ✅ funcional / 🟡 parcial / 🔵 frontend-mock / 🟣 backend-sem-front / 🔴 quebrado / ⚫ legado / ⚪ ausente **[Cross-repo]**
- Auditoria backend (rotas, contratos, regras de negócio, auth, queries, índices, cache, filas, jobs, idempotência, observabilidade, testes)
- Auditoria de arquitetura (o que escala, o que não escala, o que custa caro, o que está acoplado demais) **[Cross-repo]**
- Dívida técnica, custos atuais, segurança **[Cross-repo]**

## Fase 1 — Foundation

**Concluída.** Histórico abaixo; esta fase não se reabre.

Base de identidade e autorização que toda feature seguinte assume.

- Identity model (`User` único, múltiplas capacidades sobre ele)
- Credential model (`ProfessionalCredential`, estados DRAFT→VERIFIED→EXPIRED/REVOKED)
- Autorização contextual (Identidade + Credenciais + Relacionamentos + Membership + Entitlements — não RBAC global) — **DONE**, ver [`docs/rfcs/rfc-001-authorization-model.md`](docs/rfcs/rfc-001-authorization-model.md) e `ShapeUpApi/Features/Authorization/ARCHITECTURE.md`
- Observabilidade day one **[Cross-repo]** (SLIs: availability, p95/p99, error rate, payment success, sync success, queue delay, crash-free sessions) — **fundação pronta, testada ponta a ponta**: backend (`ShapeUpApi`) exporta traces/metrics/logs via OpenTelemetry (OTLP/HTTP) pro Seq self-hosted (`docker-compose.yml`, sem conta SaaS), `/health/live` + `/health/ready` cobrem availability, p95/p99 e error rate vêm do histograma padrão do ASP.NET Core instrumentation. Frontend (`ShapeUp-Web`) tem `ErrorBoundary` + `telemetry.js` (crash-free sessions, local) e `mutationQueue.js` emite eventos de sync success/queue delay. Payment success: sem feature de billing ainda (monetização leve), nada pra instrumentar
- Analytics, CI/CD (lint→typecheck→unit→integration→build→security scan→preview→E2E→prod) **[Cross-repo]** — pipeline do backend própria; frontend tem a dela em `ShapeUp-Web/ROADMAP.md`
- Offline foundation — backend aceita id gerado no cliente como id real (`StartWorkoutExecutionCommand.Id`/`CreateWorkoutPlanCommand.Id`, formato Mongo ObjectId) para online/offline sem reconciliação — mecanismo consumido pelo motor de fila do frontend (ver `ShapeUp-Web/ROADMAP.md` para o resto do mecanismo — mutation queue, hooks, UI)

## Fase 2 — Core Fitness

**Concluída.** Histórico abaixo; esta fase não se reabre.

O motivo de abrir o app todo dia. Sem isso nada de profissional/social tem o que orbitar. **[Cross-repo]** — cada item tem contraparte de API + UI; a decomposição por repo acontece quando a feature entra em Specify (ver `ShapeUp-Web/ROADMAP.md` para a mesma lista do lado frontend).

- Editor de treino (séries, reps, carga, RPE/RIR, técnicas avançadas: superset, drop set, rest-pause, AMRAP, EMOM...) — spec: `.specs/features/workout-editor`
- Execução de treino (fluxo: selecionar→iniciar→registrar→descanso→finalizar→resumo→XP→ShapeCoins)
- Nutrição (alimentos, refeições, macros, micronutrientes) — spec: `.specs/features/nutrition`
- Métricas (básicas: séries/reps/peso/duração; intermediárias: volume/PRs; avançadas: densidade/RPE/RIR/aderência)

## Fase 3 — Gamification

**Concluída.** Histórico abaixo; esta fase não se reabre.

⚠️ Anti-cheating é pré-requisito, não segue-junto. **[Cross-repo]** — spec: `.specs/features/gamification`.

- XP, níveis, streak, achievements, badges
- ShapeCoins (moeda virtual — ganho por treino/meta/streak/desafio)
- ShapeScore & rankings (consistência + evolução + metas + atividades verificadas + desafios — não é volume bruto)
- **Anti-cheating obrigatório antes do lançamento**: detecção de atividade impossível, duplicação, GPS incompatível, volumes anormais (estados: Verified/Likely Valid/Suspicious/Invalid)

## Fase 3.5 — Polish pós-Gamificação (gate antes de Monetização)

**Concluída.** Histórico abaixo; esta fase não se reabre.

Achados de uso real da Fase 2/3 (execução de treino, dashboard, nutrição) levantados após `Gamification`/`nutrition` fecharem Verifier. Não é feature nova — é fechar lacunas de UX/consistência antes de abrir superfície de cobrança. Itens de UI pura (Privacy/Terms, forgot-password, nutrição visual, lazy loading, retema, drawer de exercício) estão só em `ShapeUp-Web/ROADMAP.md`.

- Execução de treino **[Cross-repo]**: permite check de série sem peso/reps preenchidos; RPE deveria ser opcional por padrão mas configurável como obrigatório na montagem do treino (por exercício + botão "aplicar a todos"); RPE aceita qualquer valor (deve limitar 1–10); tipo de descanso e tipo de treino não respeitam idioma selecionado (sempre em inglês) — spec: `.specs/features/workout-execution-validation`
- Ganho de XP **[Cross-repo]**: animação customizada de ganho de XP ao finalizar treino (popup, placeholder de imagem até mascote existir); dashboard: barra de progresso de XP sempre zerada após treino (XP não reflete no nível/ShapeScore exibido) — spec: `.specs/features/xp-feedback-loop`
- Dashboard **[Cross-repo]**: card "Exercícios Prescritos para Hoje" só deve aparecer quando o plano de treino tiver dias da semana específicos atribuídos (feature nova: atribuir dia da semana a um treino na montagem do plano, opcional); card de frequência deve refletir a quantidade real de treinos do plano do usuário, não um valor fixo — spec: `.specs/features/workout-schedule-dashboard`
- Exercícios com variações/equivalentes **[Cross-repo]**: ao cadastrar um exercício, marcar quais exercícios são equivalentes (API); aparece na biblioteca (detalhe do exercício) e na execução do treino (botão de troca rápida quando o aparelho está ocupado) — spec: `.specs/features/exercise-variations`
- Treino baseado em tempo **[Cross-repo]** (corrida, resistência, alongamento) além de carga/reps — hoje a execução assume peso+reps sempre; sem isso a Fase 2 (Core Fitness) fica incompleta. Inclui PR equivalente por ritmo/pace para exercícios com distância (corrida); alongamento e afins não geram PR — spec: `.specs/features/time-based-exercises`

## Fase 4 — Hábito (sessão de hoje)

O motivo de abrir o app no meio do treino. Público: quem já paga academia ou Wellhub/TotalPass no Brasil, treina força 3 a 5 vezes por semana e hoje anota no papel, no Hevy, no Strong ou no WhatsApp com o treinador. Comprador de RH, treino em casa e coach a US$ 199 ficam de fora. **[Cross-repo]** — o contrato da API vem primeiro; a tela (sessão já aberta, timer, share sheet) vive em `ShapeUp-Web/ROADMAP.md` e no app.

- Sessão de hoje já aberta: o treino do dia, exercícios na ordem, cada série com a última carga e as últimas repetições. Ele confirma ou corrige. A ficha não se reescreve no banco a cada série.
- A série traz o descanso prescrito. Ao marcar, o timer começa no cliente. A API entrega a duração; não desenha o timer.
- No fim da sessão, uma frase só: quantas séries foram cumpridas e se a carga subiu em relação à última vez naquele exercício. Junto, o payload do card da sessão. O card mostra o que subiu, no formato de story, para o share sheet do celular postar no Instagram e no resto. É motivação e ego — o mesmo motivo pelo qual a atividade compartilhável da Strava funcionou. O card faz parte do hábito. Feed, kudos, seguidor e chat ficam de fora do app. O card da sessão é grátis.
- Ao fechar a sessão de hoje, a de amanhã já existe. No grátis, repete a ficha com a última carga registrada. O motivo de abrir o app amanhã não depende de assinatura. Mudar a ficha porque hoje foi pesado demais, ou porque a nutrição pede outro dia, é o plano pago (Fase 8).
- Queda de sinal no meio da academia já está na fundação offline da Fase 1. Esta fase não reabre sync.

## Fase 5 — Catálogo que muda o pedido de amanhã

Depois do hábito, antes do progresso semanal e do lançamento público. Comida e treino seguem no mesmo app. **[Cross-repo]** — o contrato da API vem primeiro.

- Alimento e suplemento na mesma coleção: `category` `Food` ou `Supplement`. Sem categoria, o alimento nasce `Food`. O app só lista o que a API devolve (ARQ-233).
- `GET /health` com estado por funcionalidade (`healthy`, `unhealthy`, `disabled`), sem expor exceção, conexão ou host (ARQ-236). O app que esconde o que não está saudável consome este contrato. Fica antes do lançamento público.
- Ficha técnica do exercício servida pela API (descrição, passos, músculos) e convite de aluno pelo `POST` já existente, fora da fila offline — e-mail não é idempotente (ARQ-235). O convite não abre o plano do profissional: o aluno não paga o app por causa do treinador.
- O catálogo deixa a pergunta de amanhã possível. A progressão que altera a ficha, e a nutrição que muda o que ele come ou suplementa no dia seguinte, cobram na Fase 8.

## Fase 6 — Progresso semanal visível

Depois do catálogo, antes do lançamento público e antes de cobrar. **[Cross-repo]** — o contrato da API vem primeiro; a tela da leitura fica no web e no app.

- WAPU do próprio aluno: em quantos dias da semana ele registrou trabalho de verdade.
- Tendência de carga no histórico dele.
- A leitura fica visível nesta fase. Cobrar essa leitura, junto com a progressão que muda a ficha e a nutrição do dia seguinte, é a Fase 8.

## Fase 7 — Lançamento grátis do aluno

Cabeça de praia: um grupo pequeno de alunos de força que já treinam em academia no Brasil e aceitam trocar o papel ou o Hevy pelo logger. Um box ou uma academia basta. Sem calendário. Não é lançamento nacional e não é venda para RH.

- Beta fechado, por convite. A superfície pública da API é o logger: sessão de hoje, última carga, descanso, frase de fim, card da sessão, amanhã repetindo a última carga.
- Em seguida, o mesmo logger público e grátis, para quem já treina em academia. Ainda sem cobrança.
- Conta, treino e o que as fases 0–3.5 já entregaram continuam de pé. Execução, nutrição básica, XP, streak e ShapeScore permanecem no grátis.
- Fora deste contrato: chat, WebSocket de “treinando agora”, vídeo para o treinador revisar, marketplace, clube, academia, lado profissional, IA e ecossistema. Essas rotas não entram no lançamento.

## Fase 8 — Monetização leve

Só depois do logger público. O logger é o hábito, não a receita. A receita fica em progresso e nutrição, no mesmo app. Recomendação, não preço testado com usuário.

Grátis para sempre:

- Sessão de hoje, última carga, timer, histórico do próprio treino.
- Frase curta do fim da sessão e o card dessa sessão para postar.
- A sessão de amanhã repetindo a última carga.
- O que as fases 2 e 3 já entregaram de execução, nutrição básica e gamificação (XP, streak, ShapeScore) não volta para trás de paywall.

Um plano pago do aluno, nome de trabalho Progresso:

- R$ 27,90 por mês, ou R$ 149,90 por ano no Pix.
- O que é pago: a progressão que muda a ficha de amanhã (não só repetir a última carga), a leitura semanal de progresso do próprio aluno e a nutrição que muda o que ele vai comer ou suplementar no dia seguinte.

API: entitlement desse plano num lugar só, com Pix no anual. Sem marketplace e sem take rate. O teto de alunos do profissional fica na Fase 10. Cota de IA fica na Fase 13.

## Fase 9 — Clubes

Só quando o placar nasce do log. Feed, kudos e seguidor continuam de fora. **[Cross-repo]** — o contrato da API vem primeiro (ARQ-240); a aba consome o que o servidor calculou.

- Academia por perto, com distância calculada no servidor, check-in e lotação a partir do que foi registrado. Placar preenchido na mão não entra.
- Chat e “treinando agora” não entram nesta fase.

## Fase 10 — Profissional

Depois dos clubes. O personal da mesma academia. O aluno não paga o app por causa do treinador. Um professor grande, num preço fixo sem teto, cresce o custo sem crescer a receita. Recomendação, não preço testado com usuário.

- R$ 39,90 por mês inclui até 30 alunos ativos. Aluno ativo é quem registrou treino naquele mês. Acima de 30, R$ 1,90 por aluno ativo. Carteira parada não conta.
- Trinta alunos ficam em R$ 39,90. Cem ativos dão R$ 172,90 (39,90 + 70 × 1,90). Duzentos ativos dão R$ 362,90. Os 30 e o R$ 1,90 são teto de risco, não um custo medido por aluno. O R$ 39,90 acompanha a âncora da MFIT no plano cheio e serve para o personal pequeno, não como teto de qualquer carteira.
- API: contar aluno ativo pelo log de treino do mês e aplicar o teto no entitlement. Plano ilimitado não existe.
- Verificação de credencial (CREF, CRN — submissão, revisão, aprovação, badge) segue pré-requisito de quem atende aluno por aqui. Gestão (convidar, arquivar, acompanhar) e anamnese cabem debaixo desse teto. **[Cross-repo]** — o contrato da API vem primeiro; ver `ShapeUp-Web/ROADMAP.md`.
- Vídeo para o treinador revisar fica na Fase 12.

## Fase 11 — Academia

Depois do profissional. A academia é o lugar onde o aluno já paga a mensalidade, ou o Wellhub/TotalPass. **[Cross-repo]** — o contrato da API vem primeiro.

- Organização, equipe (Owner/Manager/Receptionist/Finance/Staff) e check-in lido do que foi registrado.
- Rede multi-academia no estilo Wellhub/Gympass (eligibility, payout, antifraude de repasse) fica fora desta sequência. O usuário desta jornada já paga academia ou Wellhub/TotalPass.

## Fase 12 — Chat e presença ao vivo

Por último antes de IA e ecossistema. O chat não ganha o hábito: o WhatsApp já ganhou a caixa de entrada. “Treinando agora” só se paga quando um treinador ou a academia está olhando a série, então vem junto com o chat, não antes do lançamento. **[Cross-repo]** — o contrato da API vem primeiro.

- WebSocket de “treinando agora”: quem está na série, para o treinador vinculado. Auth na primeira mensagem, nunca token na query (ARQ-234).
- Chat treinador-aluno, com histórico (ARQ-239), no mesmo canal.
- Vídeo de execução que o treinador revisa (ARQ-241). É armazenamento e revisão, não o card grátis da sessão.
- A explicação de aba vazia de clube e chat é tela (ARQ-237). A API não publica o contrato de chat nem de presença antes desta fase.

## Fase 13 — IA e ecossistema

Por último. IA assiste; não substitui julgamento profissional. A decisão final é humana. **[Cross-repo]** — o contrato da API vem primeiro, com cota. A cota não é o plano Progresso.

- Copiloto do aluno (explicar métricas, resumir evolução, apontar tendência) e do profissional (anamnese + histórico + métricas → resumo, pontos relevantes, perguntas sugeridas).
- Multimodal (fotos) exige consentimento, proteção de dados, acesso restrito, exclusão e retenção configurável. Não é o card da sessão.
- Ecossistema, no fim: APIs públicas, ingestão de wearable (HealthKit, Health Connect e, depois, Garmin, Fitbit, Polar, Whoop, Oura), parceiros, internacionalização plena, enterprise.

---

## Riscos transversais (mitigação por fase)

| Risco | Mitigação | Onde mitigar |
|---|---|---|
| Escopo excessivo | Construir jornadas completas por etapa, não fatias horizontais | Todas |
| Sistema de permissões complexo | Separar Identity / Credentials / Relationships / Memberships / Entitlements | Fase 1 |
| Profissionais falsos | Verificação obrigatória de credencial | Fase 10 |
| Plano profissional ilimitado | Teto de alunos que registraram treino no mês; excedente por aluno ativo | Fase 10 |
| Logger atrás de paywall | Sessão, card, amanhã repetindo a última carga, e o que as fases 2 e 3 já entregaram, ficam grátis | Fase 8 |
| Rede social dentro do app | Card da sessão sai pelo share sheet; feed, kudos e seguidor ficam de fora. Chat, quando vier, não expõe sinal sensível cru | Fase 4 / 12 |
| Gamificação manipulável | Anti-fraud antes do lançamento | Fase 3 |
| Custo de IA | Quotas e limites (`aiUsageQuota`) | Fase 13 |

## Definition of Done (aplicável a toda feature, todas as fases)

UX · Responsivo · Light/Dark · i18n · Autorização · Validação · API · Testes · Analytics · Logs · Error handling · Offline behavior · Loading/Empty/Error states · Acessibilidade (WCAG 2.2 AA) · Documentação

---

## Próximos passos sugeridos

1. Fases 0 a 3.5 ficam concluídas. O próximo contrato é o hábito (Fase 4): sessão de hoje, última carga, descanso, frase, card e amanhã repetindo a última carga.
2. Para cada fase, ao entrar em execução: usar `tlc-spec-driven` (via `agentic-delivery`) para especificar feature a feature — este roadmap é o mapa, não a spec.
3. Em item **[Cross-repo]**, fechar o contrato da API antes da UI em `ShapeUp-Web/ROADMAP.md` consumir.
4. Harness (`harness-engineering`) uma vez por repo, atualizado incrementalmente por fase.
