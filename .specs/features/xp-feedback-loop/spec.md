# XP Feedback Loop — Specification

> **Cross-repo**: esta spec é canônica no repo da API (`ShapeUpApi`) porque a causa raiz da barra de XP zerada e o crédito assíncrono de XP vivem em `Gamification` (backend); o popup de animação em si é implementado em `ShapeUp-Web`. Cada AC é marcado **[Frontend]** ou **[Backend+Frontend]**. Não existe spec irmã separada no frontend — o consumo/UI está descrito nesta mesma spec.

## Problem Statement

`Gamification` (Fase 3, `GAM-01`–`GAM-13`, `Coverage: 13 total, 13 Verified`) already credits XP/ShapeCoins/streak/ShapeScore correctly on the backend, and the dashboard (`GamificationProgressCard.jsx`) already reads and renders `totalXp`/`level`/`currentStreak`/`shapeCoins`/`shapeScore` via `useGamificationApi` (`GET /api/gamification/me`). The mechanism works — but the two moments where a user should actually **feel** it are broken or missing:

1. Finishing a workout today gives zero visual feedback about XP gained. `TrainingPlansClient.jsx` (`submitFeedback`, around line 525) enqueues the finish mutation (`enqueueMutation`, `endpoint: /api/training/workouts/{id}/finish`, `dedupeKey: workout-finish-{id}`, same offline-first mutation-queue mechanism from Fase 1, `mutationQueue.js`) and immediately opens the overview/summary modal (`setShowOverviewModal(true)`) built **entirely from local `exercises`/`workoutTime` state** — the code comment at line 527 says so explicitly: *"the session summary shown next is built entirely from local exercises/workoutTime, not from this response."* The actual XP credit happens later, asynchronously, when `Gamification`'s `WorkoutFinishedConsumer` (MassTransit) picks up the `WorkoutFinished` event published after the finish endpoint persists the session — there is no hook anywhere in the finish/overview flow that shows the user what they just earned.
2. The dashboard scoreboard (`GamificationProgressCard.jsx`) correctly shows updated `level` and `shapeScore` after a workout, but the in-level XP progress bar (`su-gamification-progress-track`/`su-gamification-progress-fill`, driven by `xpInCurrentLevel = totalXp % XP_PER_LEVEL` at line 31) renders empty/zero — a visible product break in the exact "core loop" screen the Gamification spec (`GAM-08`, `GAM-12`) was built to make usable.

Both are P1: gamification that isn't felt on the two screens users actually look at (workout finish, dashboard) doesn't retain anyone, regardless of how correct the backend ledger is.

## Goals

- [ ] Finishing a workout shows a custom popup/animation celebrating XP gained (e.g. "+120 XP"), reusing the existing offline-first/eventual-consistency posture instead of pretending the credit is synchronous
- [ ] The popup has an explicit pending/loading state for the window between "workout finished, popup shown" and "backend confirms XP credited" — it never fabricates a number before the real one is known
- [ ] The popup's image is a placeholder/component-prop slot only — no mascot artwork is designed or shipped by this spec
- [ ] The dashboard's in-level XP progress bar renders the user's real progress toward the next level, consistent with the same `totalXp`/`level` already shown correctly in the same card
- [ ] Root cause of the progress-bar bug is explicitly left for Design/Execute to investigate, not guessed at here

## Out of Scope

| Item | Motivo |
|---|---|
| Mascot artwork / final animation asset | Pedido explícito do usuário: usar imagem em branco/placeholder agora; a arte entra numa spec/asset futuro — esta spec só reserva o slot (prop de imagem no componente do popup) |
| Qualquer mecânica de gamificação nova (XP, nível, streak, ShapeScore, regras de crédito) | `Gamification` (GAM-01–GAM-13) já está `Verified` e não é reaberta aqui — esta feature só visualiza/anima o que já existe, não calcula nada novo |
| Achievements/badges (popup de conquista, catálogo, motor dinâmico) | `Gamification` já deferiu isso por completo pra fase própria futura (Out of Scope / AD daquela spec) — não pedido aqui, não reaberto aqui |
| Root-cause fix do progress bar em si | Investigar SQL/API/frontend e corrigir é trabalho de Design + Execute (`tlc-spec-driven`) — esta spec define o comportamento CORRETO esperado, não o diagnóstico |
| Sistema de notificação em tempo real da plataforma (WebSocket, presença) | Gap de plataforma inteira, já registrado (ver `nutrition/spec.md`, mesma referência a `GAPS.md`) — fora do escopo desta feature; a solução de timing do popup aqui usa um mecanismo mais leve (poll pontual pós-finish), não o WebSocket que ainda não existe |
| Progresso de XP nutricional dentro do mesmo popup de treino | O popup desta spec cobre o evento `WorkoutFinished` → crédito de XP de treino; o streak/XP nutricional (`NutritionGoalMet`, feature `nutrition`, ainda `Pending`) tem seu próprio ciclo de fechamento de dia (meia-noite UTC) — não é um "finish" pontual como o de treino, não faz sentido no mesmo popup |
| Mobile / app nativo | Fase 6 (Mobile & Offline) ainda não existe — esta spec cobre `ShapeUp-Web` |

---

## Assumptions & Open Questions

| Assumption / decision | Chosen default | Rationale | Confirmed? |
|---|---|---|---|
| Timing do popup de XP (crédito é assíncrono — evento `WorkoutFinished` publicado após o `finish` persistir, consumido depois por `Gamification`) | Popup abre IMEDIATAMENTE ao fechar o feedback flow (mesmo momento em que `showOverviewModal` abre hoje), num estado "pendente" (spinner/skeleton, sem número ainda). O frontend faz um poll curto e limitado (ex.: `GET /api/gamification/me` a cada ~2s, timeout em ~15s) comparando o `totalXp` retornado contra o `totalXp` capturado ANTES do finish (snapshot já disponível — o dashboard já chama esse mesmo endpoint); quando o valor sobe, o popup resolve pro delta real (`+120 XP`) e fecha o estado pendente. Se o timeout estourar sem mudança (ex.: sessão classificada `Suspicious`/`Invalid` pelo anti-cheat, ou consumer represado), popup mostra um estado neutro final ("XP em processamento" ou equivalente, nunca um "+0 XP" que pareça bug) e não trava a navegação do usuário | Arquitetura já estabelecida (Fase 1: mutation queue, consumers assíncronos via MassTransit) não tem hoje nenhum mecanismo de push/real-time (WebSocket) pro frontend saber o momento exato do crédito — construir isso seria um gap de plataforma inteira (ver `GAPS.md`, mesmo gap já registrado em `nutrition/spec.md` pra notificação de recusa). Poll curto e limitado é o "mecanismo mais leve" mencionado explicitamente como aceitável enquanto esse gap de plataforma não é resolvido; reaproveita o endpoint `GET /api/gamification/me` que já existe, sem endpoint novo | n — assumption, log apenas; revisitar se/quando um mecanismo de notificação em tempo real de plataforma for especificado (a troca de "poll" por "push" não deveria exigir redesenho do popup, só do canal, mesmo princípio já usado em `nutrition/spec.md` pra notificação de recusa) |
| Snapshot de XP "antes" do treino, pra calcular o delta exibido no popup | Frontend já tem o `totalXp` do dashboard carregado antes do usuário entrar na tela de execução do treino (a navegação normal passa pelo dashboard). Esse valor é passado/mantido como snapshot local (ex.: `sessionStorage` ou estado de navegação) pro fluxo de treino, sem chamada de API nova só pra isso. Se o usuário abrir o treino direto (deep link, sem passar pelo dashboard na sessão atual) e não houver snapshot disponível, o frontend faz UMA chamada extra a `GET /api/gamification/me` no início do `finishSession` (antes do popup abrir) só pra capturar o "antes" | Evita popup mostrar um delta errado (ex.: comparando contra um `totalXp` desatualizado de horas atrás) sem exigir um endpoint de "delta" novo no backend — a UI já tem ou pode buscar barato o dado que precisa | n — assumption, log apenas |
| Placeholder de imagem no popup | Componente aceita uma prop de imagem (ex.: `mascotImageUrl` ou `iconSlot`) com um valor placeholder neutro (ex.: ícone genérico do design system já em uso, `lucide-react`, mesmo padrão já usado em `GamificationProgressCard`) até a arte real ser definida em spec/asset futuro | Pedido explícito do usuário — "deixar slot pra imagem futura" sem travar esta spec numa decisão de arte que não é dela | y (pedido explícito do usuário) |
| Root cause do progress bar zerado (display bug no frontend vs. backend não retornando breakdown por nível vs. cache desatualizado) | DESCONHECIDO — não é assumido nesta spec. `GamificationProgressCard.jsx` linha 31 já calcula `xpInCurrentLevel = totalXp % XP_PER_LEVEL` localmente a partir do `totalXp` retornado por `GET /api/gamification/me`; se esse `totalXp` chega correto e ainda assim a barra renderiza zero, a causa mais provável é TIMING (dashboard busca o perfil antes do consumer assíncrono creditar o XP daquele treino — mesma janela de corrida do Goal 1 desta spec) e não um bug de fórmula — mas isso é hipótese, não fato verificado. Design DEVE investigar antes de propor a correção | Instrução explícita do usuário: root cause é trabalho de Design/Execute, não deve ser assumido no Specify | y (decisão de processo, não de produto) |
| Escopo de "próximo nível" na barra | A barra existente já assume nível seguinte = nível atual + 1, XP por nível = 500 flat (`XP_PER_LEVEL`, hardcoded no frontend, espelhando a fórmula já `Verified` em `Gamification`: `Level = floor(TotalXp / 500) + 1`). Esta spec não muda a fórmula, só exige que a barra reflita corretamente o mesmo dado que já determina o nível exibido ao lado dela | Reaproveita a fórmula já `Verified`, evita duplicar/reinventar a curva de nível numa segunda spec | y (decorre da fórmula já fechada em `Gamification`) |
| Sessão classificada `Suspicious`/`Invalid` pelo anti-cheat (nenhum XP creditado) | Popup, após o timeout de poll, mostra um estado final neutro (ex.: "Sessão em análise" ou equivalente — nunca "+0 XP" como se fosse uma confirmação de zero ganho, o que pareceria bug) — sem revelar ao usuário o veredito interno do anti-cheat (isso é decisão de UX pra Design detalhar, não escopo de produto novo) | `Gamification` já decidiu (Out of Scope daquela spec) que sessões suspeitas/inválidas não creditam nada e não há reversão — o popup só precisa não mentir sobre um crédito que não aconteceu | n — assumption, log apenas |

**Open questions:** nenhuma sem resposta — tudo acima está resolvido ou registrado como assumption.

---

## User Stories

### P1: Popup de celebração de XP ao finalizar treino (Frontend) ⭐ MVP

**User Story**: Como usuário, quero ver uma celebração visual (ex.: "+120 XP") ao terminar um treino, pra sentir que o app reconheceu meu esforço na hora, mesmo sabendo que o crédito real pode levar alguns segundos pra confirmar no backend.

**Why P1**: É o motivo explícito desta spec — hoje não existe NENHUM feedback visual de XP no fluxo de finalização, apesar do backend já creditar corretamente (`Gamification`, `Verified`); é o break mais visível do loop "treino → recompensa" que a Fase 3 pretendia tornar viciante.

**Camada**: Frontend (`ShapeUp-Web`)

**Acceptance Criteria**:

1. WHEN o usuário conclui o feedback de fim de treino (mesmo momento em que `submitFeedback` hoje abre `showOverviewModal`, `TrainingPlansClient.jsx`) THEN interface SHALL exibir um popup de celebração de XP em estado PENDENTE (spinner/skeleton, sem número), sem bloquear a navegação do usuário pro resumo do treino
2. WHEN o popup está pendente THEN interface SHALL fazer polling de `GET /api/gamification/me` em intervalo curto (ex.: a cada ~2s) até detectar que o `totalXp` retornado é maior que o snapshot capturado antes do finish, ou até um timeout máximo (ex.: ~15s)
3. WHEN o polling detecta `totalXp` maior que o snapshot anterior THEN interface SHALL resolver o popup pro delta real (`totalXp` novo − snapshot anterior), exibindo o valor exato (ex.: "+120 XP") e encerrar o polling
4. WHEN o timeout do polling é atingido sem nenhuma mudança de `totalXp` (sessão ainda não avaliada, ou avaliada como `Suspicious`/`Invalid` sem crédito) THEN interface SHALL substituir o estado pendente por um estado final neutro (nunca "+0 XP"), sem travar o usuário nem repetir o polling indefinidamente
5. WHEN o componente de popup é renderizado THEN interface SHALL aceitar uma prop de imagem/ícone (placeholder neutro por padrão, do design system já existente) no lugar reservado pra uma futura arte de mascote — nenhuma arte nova é produzida nesta feature
6. WHEN o usuário fecha o popup manualmente (antes do polling resolver) THEN interface SHALL cancelar o polling em andamento sem gerar erro nem popup fantasma reaberto depois

**Independent Test**: Finalizar um treino plausível — popup abre pendente, e dentro de alguns segundos resolve pro valor real de XP creditado (comparável ao delta visto no dashboard depois); finalizar um treino que o anti-cheat classificaria como suspeito (ex.: duração implausível) — popup abre pendente e, após o timeout, mostra o estado neutro final, nunca "+0 XP".

---

### P1: Barra de progresso de XP no dashboard reflete o XP real do usuário (bug) (Backend + Frontend)

**User Story**: Como usuário, quero que a barra de progresso de XP no meu dashboard mostre meu avanço real dentro do nível atual, do mesmo jeito que o nível e o ShapeScore ao lado dela já mostram corretamente.

**Why P1**: É um break visível de produto num componente já `Verified` (`GAM-08`/`GAM-12`) — usuário vê nível e ShapeScore corretos, mas a barra ao lado sempre aparece vazia, quebrando a confiança no card inteiro.

**Camada**: Backend + Frontend — a causa raiz (dado errado vindo da API vs. cálculo errado no componente vs. cache desatualizado) é DESCONHECIDA e cabe a Design investigar (ver Assumptions); os critérios abaixo descrevem o comportamento CORRETO esperado, independente de onde a correção acabe sendo aplicada.

**Acceptance Criteria**:

1. WHEN o XP total (`totalXp`) do usuário muda (crédito de um treino avaliado) THEN o `GET /api/gamification/me` SHALL retornar o `totalXp` atualizado de forma consistente com o `level` retornado na MESMA resposta (o mesmo total que determina o nível exibido é o total usado pra calcular o progresso dentro do nível)
2. WHEN o dashboard renderiza `GamificationProgressCard` com um `totalXp`/`level` não-zerados THEN a barra de progresso SHALL exibir `xpInCurrentLevel / XP_PER_LEVEL` (hoje `totalXp % 500`) como uma fração visualmente preenchida proporcional ao valor real — NUNCA vazia quando `xpInCurrentLevel > 0`
3. WHEN o usuário está exatamente no início de um nível (`totalXp % 500 === 0`, incluindo `totalXp === 0`) THEN a barra SHALL exibir 0% preenchido — esse é o ÚNICO caso em que vazio é o valor correto, distinto do bug (barra vazia mesmo com XP real acumulado no nível atual)
4. WHEN o dashboard é aberto/atualizado logo após um treino cujo `WorkoutFinished` ainda não foi consumido pelo `Gamification` (janela de corrida assíncrona, mesma do Goal 1 desta spec) THEN sistema SHALL, no mínimo, não exibir um estado inconsistente permanente — uma atualização subsequente do dashboard (nova consulta ao perfil) SHALL refletir o XP correto assim que o crédito for processado
5. WHEN a causa raiz for corrigida (Design/Execute) THEN a correção SHALL ser coberta por um teste que reproduza o estado hoje quebrado (usuário com `totalXp > 0` dentro do nível atual e barra renderizando zero) antes da correção, e passe depois

**Independent Test**: Usuário com XP creditado que NÃO é múltiplo exato de 500 — a barra de progresso exibe um preenchimento visível (não vazio) proporcional ao `totalXp % 500`, e esse preenchimento muda visivelmente após o próximo treino creditado.

---

## Edge Cases

- WHEN o usuário finaliza um treino e navega pra fora da tela de resumo (ex.: fecha o app/aba) antes do polling do popup resolver THEN sistema SHALL simplesmente parar o polling (nenhum estado de popup pendente "vaza" pra outra tela nem reaparece depois fora de contexto)
- WHEN o usuário finaliza DOIS treinos em sequência rápida (segundo treino iniciado antes do popup do primeiro resolver) THEN cada finalização SHALL ter seu próprio snapshot/polling independente — o delta exibido nunca mistura XP de duas sessões
- WHEN o `GET /api/gamification/me` falha (erro de rede) durante o polling do popup THEN interface SHALL tratar como tentativa falha dentro do mesmo orçamento de timeout (não reinicia o timeout, não trava em erro) — comportamento igual ao de nenhuma mudança detectada
- WHEN o usuário está offline ao finalizar o treino (mutation enfileirada localmente, ainda não sincronizada) THEN o popup de XP SHALL entrar em estado pendente normalmente, mas o polling só pode confirmar o crédito depois que a mutation sincronizar E o consumer processar — nesses casos o timeout do polling é esperado ser atingido com frequência maior; sistema SHALL cair no estado neutro final sem erro (mesmo comportamento do Edge Case de timeout já especificado)
- WHEN o dado por trás da barra de progresso (Story 2) é investigado em Design e a causa raiz for "cache do frontend" (ex.: estado do perfil não invalidado após o treino) THEN a correção NÃO exige mudança de contrato de API — só do momento em que o frontend busca/invalida o dado já existente

---

## Requirement Traceability

| Requirement ID | Story | Layer | Phase | Status |
|---|---|---|---|---|
| XPF-01 | P1: Popup de celebração de XP — estado pendente + resolução | Frontend | Design | Pending |
| XPF-02 | P1: Popup de celebração de XP — timeout/estado neutro | Frontend | Design | Pending |
| XPF-03 | P1: Popup de celebração de XP — slot de imagem placeholder | Frontend | Design | Pending |
| XPF-04 | P1: Barra de progresso — consistência `totalXp`/`level` na API | Backend | Design | Pending |
| XPF-05 | P1: Barra de progresso — renderização correta do preenchimento | Frontend | Design | Pending |
| XPF-06 | P1: Barra de progresso — investigação de causa raiz + teste de regressão | Backend + Frontend | Design | Pending |

**ID format:** `XPF-NN`

**Status values:** Pending → In Design → In Tasks → Implementing → Verified

**Coverage:** 6 total, 0 mapped to tasks, 6 unmapped ⚠️ (Tasks phase ainda não rodou)

---

## Success Criteria

- [ ] Usuário finaliza um treino e vê um popup de XP que abre pendente e resolve pro valor real creditado (ou cai num estado neutro claro se o crédito não vier a tempo/não acontecer)
- [ ] Popup de XP tem um slot de imagem reservado, usando placeholder neutro — nenhuma arte de mascote produzida nesta feature
- [ ] Barra de progresso de XP no dashboard nunca aparece vazia quando o usuário tem XP real acumulado no nível atual
- [ ] Causa raiz da barra zerada é identificada em Design (não assumida aqui) e corrigida com teste de regressão cobrindo o estado hoje quebrado
- [ ] Nenhuma mecânica de gamificação nova (XP/nível/streak/ShapeScore) é introduzida — apenas visualização/feedback do que `Gamification` já credita corretamente
</content>
