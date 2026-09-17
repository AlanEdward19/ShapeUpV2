# Agenda Semanal do Treino no Dashboard — Specification

## Problem Statement

O editor de treino (`workout-editor`, `WorkoutPlanDocument` em `ShapeUpApi/src/Features/Training/Shared/Documents/WorkoutPlanDocument.cs`) hoje não tem noção de dia da semana: um `WorkoutPlanDocument` é uma unidade de treino (nome, fase, dificuldade, `Blocks: List<BlockDocumentValueObject>`) sem nenhum campo de agendamento, e um usuário pode ter N `WorkoutPlanDocument`s (`getWorkoutPlansByUser`, paginado). O dashboard (`ShapeUp-Web/src/pages/Dashboard/OperationalDashboardsShell.tsx`) já assume implicitamente dois conceitos que o modelo de dados não sustenta:

1. O card "Exercícios Prescritos para Hoje" (`AthleteDashboardMarkup.tsx:211-258`) sempre renderiza e sempre busca dados, mesmo que não exista NENHUM conceito de "o treino de hoje" — hoje ele só mostra `plans[0]` (o primeiro item retornado por `getWorkoutPlansByUser`, sem nenhum critério de "qual é o treino de hoje"), o que é enganoso: o card promete algo ("prescrito para hoje") que o backend não calcula.
2. O widget de frequência semanal (card "Frequência", `AthleteDashboardMarkup.tsx:134-148`) mostra `sessionsCompletedThisWeek / sessionsTargetPerWeek`, mas o denominador vem de `getDashboardMe(5)` — **`5` é um literal fixo no chamador** (`OperationalDashboardsShell.tsx:179`), não um dado real do plano do usuário. Um usuário com um plano de 2 treinos/semana vê "1/5", o mesmo que um usuário com 7 treinos/semana veria — o número não reflete o plano real de ninguém.

Esta spec resolve os dois problemas introduzindo uma capability nova no editor de treino (atribuição opcional de dia(s) da semana a um treino) que os dois bugs de dashboard passam a consumir. É um pacote único porque os dois bugs de dashboard não têm correção real sem a capability primeiro existir — não são três specs independentes, são uma spec (fundação) com dois consumidores diretos.

## Goals

- [ ] Autor do plano (profissional ou usuário independente) pode, ao criar/editar um treino no `workout-editor`, atribuir opcionalmente um ou mais dias da semana (Seg–Dom) a esse treino
- [ ] Um plano pode existir sem nenhuma atribuição de dia (comportamento atual, preservado) ou com todos os treinos atribuídos
- [ ] O card "Exercícios Prescritos para Hoje" só renderiza (e só dispara a chamada de API que o alimenta) quando o plano ativo do usuário tem pelo menos um treino atribuído ao dia da semana corrente
- [ ] O widget de frequência mostra um denominador real — dinâmico a partir do plano do usuário — no lugar do literal `5` hoje hardcoded em `OperationalDashboardsShell.tsx:179`

## Out of Scope

| Item | Motivo |
|---|---|
| Tela/visualização de calendário além do seletor de dia já existente no editor de treino | Não pedido — o seletor de dia da semana é um campo de formulário no `workout-editor`, não uma nova UI de calendário/agenda |
| Notificações/lembretes vinculados ao dia agendado (push, e-mail, in-app) | Não pedido nesta spec; se a plataforma ganhar um sistema de notificação (gap já registrado em `nutrition/spec.md`), pode consumir o dado de agendamento depois, sem redesenho aqui |
| Ciclos multi-semana / alternância de semanas (ex.: "semana A" ≠ "semana B" com treinos diferentes por semana) | Não pedido — assume-se recorrência semanal simples: um dia da semana atribuído vale igual toda semana, sem variação de ciclo (ver Assumptions) |
| Mudar o conceito de "plano ativo" além do que o dashboard já usa hoje (`plans[0]`) | Fora de escopo desta spec — está registrado como Assumption porque afeta os 3 itens, mas redesenhar "qual plano é o ativo" é problema pré-existente, não introduzido aqui |
| Edição em lote de dias de vários treinos ao mesmo tempo | Não pedido — atribuição é por treino, um de cada vez, no fluxo de edição já existente |

---

## Assumptions & Open Questions

| Assumption / decision | Chosen default | Rationale | Confirmed? |
|---|---|---|---|
| Um treino pode ser atribuído a mais de um dia da semana | Sim — `AssignedWeekdays` é uma lista (0 a 7 dias, sem duplicata), não um único dia obrigatório | Um mapeamento 1:1 fixo seria restritivo demais pra um caso comum (ex.: treino "Upper Body" repetido na Segunda e na Quinta) — usuário não especificou explicitamente, mas o caso de uso óbvio exige cardinalidade N | n — assumption, log apenas |
| Granularidade de "workout" pra fins de agendamento | O campo de agendamento vive em `WorkoutPlanDocument` (cada documento retornado por `getWorkoutPlansByUser` já é a unidade "um treino" — ex.: um `WorkoutPlanDocument` = "Treino A", outro = "Treino B" do mesmo usuário), não dentro de `BlockDocumentValueObject`. Um `Block` é uma técnica de execução (Straight/Superset/AMRAP/EMOM) dentro de UM treino, não uma unidade agendável independente | O vocabulário do PM ("atribuir dia a um workout dentro de um plano") mapeia mais perto de "plano de usuário = conjunto de WorkoutPlanDocuments, cada um um treino agendável" do que de Block, que é puramente estrutural/técnico (AD-007 do `workout-editor`) — atribuir dia a um Block quebraria a separação já estabelecida entre planejamento e técnica de execução | n — assumption, log apenas, revisitar em Design se o termo "plano" no vocabulário de produto divergir disso |
| "Plano ativo" do usuário (usado pelos itens 2 e 3) | Mesma noção que o dashboard já usa hoje: o conjunto de `WorkoutPlanDocument`s retornados por `getWorkoutPlansByUser(targetUserId)` pertence ao usuário — não existe hoje um campo `IsActive`/status no `WorkoutPlanDocument`. Esta spec NÃO introduz esse conceito; ela consome o que já existe (todos os treinos do usuário, paginados) | Redesenhar "qual plano/treino conta como ativo" é problema pré-existente e mais amplo que os 3 itens pedidos aqui — resolver isso agora expandiria o escopo além do solicitado (ver Out of Scope) | n — assumption, log apenas, sinalizar em Design como possível dívida pré-existente |
| Denominador do widget de frequência quando HÁ atribuição de dia | Contagem de dias da semana DISTINTOS atribuídos entre todos os treinos do usuário naquela semana (ex.: 2 treinos, um em Seg+Qui e outro em Ter → denominador 3, não 2) | Mantém o denominador coerente com o mesmo conceito do card "hoje" (que também olha dia-a-dia) — contar "treinos" em vez de "dias" sub-representaria a frequência real esperada quando o mesmo treino se repete em mais de um dia | y (decisão explícita do usuário/PM no pedido) |
| Denominador do widget de frequência quando NÃO HÁ nenhuma atribuição de dia em nenhum treino do plano | Contagem total de treinos (`WorkoutPlanDocument`s) do usuário — fallback dinâmico no lugar do `5` fixo hoje hardcoded | Decisão explícita do usuário/PM: preserva o comportamento "sempre mostra algum denominador plausível" sem exigir que o usuário adote agendamento por dia pra corrigir o bug do hardcode | y (decisão explícita do usuário/PM no pedido) |
| Janela de contagem do numerador (`sessionsCompletedThisWeek`) | Mantida como está hoje — `GetTrainingDashboardHandler.cs` já conta sessões completadas entre `StartOfWeekUtc` (segunda-feira UTC) e +7 dias (`workoutSessionRepository.GetCompletedByUserInRangeAsync`). Esta spec não altera essa janela, só o denominador que ela compara | Numerador já existe e não foi reportado como quebrado — mudar sua janela de contagem é redesenho não pedido. Verificar em Design se a janela (segunda a segunda, UTC) é realmente a intenção correta para o novo denominador dinâmico coexistir sem inconsistência de fuso | n — assumption, log apenas, revisitar em Design |
| Formato de "dia da semana" no modelo de dados | Enum simples de 7 valores (Monday–Sunday, mesmo padrão .NET `DayOfWeek` no backend), sem suporte a "toda semana par/ímpar" ou qualquer variação cíclica | Consistente com "Out of Scope: ciclos multi-semana" — recorrência semanal simples é o único caso pedido | n — assumption, log apenas |
| Contrato do endpoint de dashboard (`GET /api/training/dashboard/me`) | O parâmetro `sessionsTargetPerWeek` continua existindo na query string (compatibilidade com o contrato atual, `TrainingDashboardController.cs:14-15` e `GetTrainingDashboardQuery`) — quem muda é o CHAMADOR (frontend), que passa a calcular o valor dinamicamente a partir do plano do usuário em vez de enviar o literal `5`. O backend não precisa saber calcular esse número sozinho porque ele nunca teve acesso ao plano do usuário nesse endpoint hoje | Menor diff possível: o endpoint já aceita o número certo desde que quem chama mande o número certo — não há necessidade de o backend re-buscar o plano dentro do handler de dashboard quando o frontend já busca `getWorkoutPlansByUser` na mesma tela (`OperationalDashboardsShell.tsx:150`) | n — assumption, log apenas, revisitar em Design se o cálculo precisar ser feito também em superfícies que não tem o plano carregado em memória (ex.: uma futura tela mobile) |

**Open questions:** nenhuma sem resposta — tudo acima está resolvido ou registrado como assumption.

---

## User Stories

### P1: Atribuir dia(s) da semana a um treino no editor ⭐ MVP — **Backend + Frontend**

**User Story**: Como autor de um plano de treino, quero atribuir opcionalmente um ou mais dias da semana a cada treino, pra que o sistema (e o dashboard do usuário) saiba em que dia esse treino é esperado.

**Why P1**: É a fundação dos outros dois itens — sem o dado de agendamento persistido, o card "hoje" e o denominador de frequência não têm nenhuma informação real da qual derivar.

**Acceptance Criteria**:

1. [Backend] WHEN o autor salva um treino sem informar nenhum dia da semana THEN sistema SHALL persistir `WorkoutPlanDocument.AssignedWeekdays` como lista vazia (comportamento atual preservado — agendamento é sempre opcional)
2. [Backend] WHEN o autor salva um treino com um ou mais dias da semana selecionados THEN sistema SHALL persistir `AssignedWeekdays` como a lista desses dias, sem exigir um dia único (mesmo treino pode valer pra Segunda E Quinta, por exemplo)
3. [Backend] WHEN o autor tenta salvar o mesmo dia da semana duplicado na seleção (ex.: Segunda duas vezes) THEN sistema SHALL deduplicar antes de persistir, nunca rejeitar por duplicidade
4. [Frontend] WHEN o autor está no editor de treino (`PlanEditor`, `ClientDetail.jsx:136`) THEN interface SHALL oferecer um seletor de dia(s) da semana (multi-seleção) por treino, refletindo o estado salvo ao reabrir
5. [Frontend] WHEN o autor remove todos os dias previamente atribuídos e salva THEN interface SHALL persistir a lista vazia (voltar ao estado "sem agendamento" é uma operação válida, não um erro)

**Independent Test**: Criar um treino, atribuir Segunda e Quinta, salvar, recarregar o editor — os dois dias aparecem selecionados; remover ambos e salvar — treino volta a não ter nenhum dia atribuído, sem erro.

---

### P1: Card "Exercícios Prescritos para Hoje" só aparece quando há treino de hoje ⭐ MVP — **Frontend (consome dado do item anterior)**

**User Story**: Como usuário do dashboard, quero que o card "Exercícios Prescritos para Hoje" só apareça quando existe de fato um treino programado pra hoje, pra não ver um card com a promessa de "hoje" quando meu plano não tem nenhum agendamento.

**Why P1**: É um bug de comportamento hoje — o card sempre renderiza mostrando `plans[0]` (`OperationalDashboardsShell.tsx:202-203`) mesmo sem nenhum critério real de "hoje". Corrigido por consumir `AssignedWeekdays` do item anterior.

**Acceptance Criteria**:

1. [Frontend] WHEN o plano do usuário tem pelo menos um treino com `AssignedWeekdays` contendo o dia da semana corrente THEN dashboard SHALL renderizar o card "Exercícios Prescritos para Hoje" com os exercícios desse(s) treino(s)
2. [Frontend] WHEN nenhum treino do usuário tem o dia da semana corrente em `AssignedWeekdays` (incluindo o caso de nenhum treino ter QUALQUER dia atribuído) THEN dashboard SHALL NÃO renderizar o card "Exercícios Prescritos para Hoje"
3. [Frontend] WHEN a condição do critério 2 se aplica THEN dashboard SHALL NÃO disparar a chamada de API que busca os dados desse card (não basta esconder o card visualmente — a busca de dados não deve nem ser feita; requisito explícito do usuário: "não carregue essa parte")
4. [Frontend] WHEN mais de um treino do usuário está atribuído ao dia corrente THEN dashboard SHALL exibir os exercícios de todos esses treinos no card (agregados), não só do primeiro encontrado

**Independent Test**: Usuário com um treino atribuído a "hoje" — card aparece com os exercícios certos e a chamada de rede correspondente acontece (visível no Network tab); usuário sem nenhum treino atribuído a "hoje" (ou sem nenhuma atribuição no plano inteiro) — card não aparece e a chamada de rede não é disparada.

---

### P1: Widget de frequência reflete o tamanho real do plano ⭐ MVP — **Frontend (consumo) + Backend (contrato já suporta, sem mudança de endpoint)**

**User Story**: Como usuário do dashboard, quero que o alvo de frequência semanal (ex.: "3/3", "5/5") reflita meu plano de treino real, não um número fixo que nunca muda.

**Why P1**: Bug hoje — `OperationalDashboardsShell.tsx:179` chama `getDashboardMe(5)` com o literal `5` sempre, independente de quantos treinos o usuário realmente tem.

**Acceptance Criteria**:

1. [Frontend] WHEN o plano do usuário tem pelo menos um treino com `AssignedWeekdays` não-vazio THEN dashboard SHALL calcular o denominador de frequência como a contagem de dias da semana DISTINTOS atribuídos somando todos os treinos do plano (ex.: treino A em Seg+Qui, treino B em Ter → denominador 3), e SHALL passar esse valor pra `getDashboardMe(...)` no lugar do literal `5`
2. [Frontend] WHEN NENHUM treino do plano do usuário tem `AssignedWeekdays` preenchido THEN dashboard SHALL calcular o denominador como a contagem total de treinos (`WorkoutPlanDocument`s) do usuário, e SHALL passar esse valor pra `getDashboardMe(...)` (fallback dinâmico, substitui o `5` fixo)
3. [Backend] WHEN o frontend chama `GET /api/training/dashboard/me?sessionsTargetPerWeek=N` com um `N` calculado dinamicamente THEN sistema SHALL continuar aceitando e usando esse valor exatamente como hoje (`GetTrainingDashboardHandler.cs` já valida `N > 0` e já usa o valor recebido pro cálculo de `completionRate` — nenhuma mudança de contrato é necessária neste endpoint)
4. [Frontend] WHEN o usuário não tem NENHUM treino cadastrado (plano vazio) THEN dashboard SHALL evitar chamar `getDashboardMe` com denominador zero (`N` deve ser ≥ 1) — sistema SHALL tratar esse caso como "sem dado de frequência disponível" (mesmo tratamento que erro de rede já recebe hoje, card mostra "—")

**Independent Test**: Usuário com plano de 2 treinos sem nenhum dia atribuído — widget mostra "X/2"; usuário com 2 treinos, um atribuído a 2 dias e outro a 1 dia distinto — widget mostra "X/3"; usuário sem nenhum treino — widget mostra "—" sem erro de console.

---

## Edge Cases

- WHEN um treino tem `AssignedWeekdays` atribuído mas é excluído/removido do plano THEN sistema SHALL simplesmente deixar de contá-lo tanto no card "hoje" quanto no denominador de frequência (nenhuma limpeza especial necessária — a contagem é sempre derivada do estado atual do plano, nunca de um valor congelado)
- WHEN o mesmo treino aparece atribuído a um dia mas o usuário edita e remove esse dia no meio da semana THEN sistema SHALL refletir a mudança na PRÓXIMA vez que o dashboard carregar (sem necessidade de recalcular retroativamente nada já exibido antes da edição — é uma leitura sempre-atual, não um snapshot)
- WHEN dois treinos diferentes do mesmo usuário são atribuídos ao MESMO dia da semana THEN o card "hoje" SHALL mostrar os exercícios de ambos (ver AC4 da story 2), e esse dia conta UMA vez só no denominador de frequência (dias distintos, não pares treino-dia)
- WHEN o cálculo do dia da semana corrente ocorre no cliente (frontend) THEN sistema SHALL usar o fuso horário local do navegador do usuário pra decidir "qual dia é hoje" (mesmo padrão já usado pelo resto do dashboard pra exibir a data corrente, ex.: `AthleteDashboardMarkup.tsx:101` já usa `new Date()` sem conversão UTC explícita) — não introduzir um segundo padrão de fuso só pra esta feature
- WHEN o usuário tem múltiplos planos retornados por `getWorkoutPlansByUser` (não só `plans[0]`) THEN os itens 2 e 3 desta spec SHALL considerar TODOS os treinos retornados pro usuário (não só o primeiro), já que "plano ativo" nesta spec é assumido como o conjunto inteiro (ver Assumptions) — isto é uma correção de escopo em relação ao código atual, que só olhava `plans[0]` pra exercícios

---

## Requirement Traceability

| Requirement ID | Story | Camada | Phase | Status |
|---|---|---|---|---|
| WSD-01 | P1: Atribuir dia(s) da semana a um treino — persistência | Backend | Design | Pending |
| WSD-02 | P1: Atribuir dia(s) da semana a um treino — seletor no editor | Frontend | Design | Pending |
| WSD-03 | P1: Card "hoje" — renderizar só quando há treino atribuído ao dia corrente | Frontend | Design | Pending |
| WSD-04 | P1: Card "hoje" — não disparar fetch quando não há treino atribuído ao dia corrente | Frontend | Design | Pending |
| WSD-05 | P1: Widget de frequência — denominador por dias distintos atribuídos | Frontend | Design | Pending |
| WSD-06 | P1: Widget de frequência — fallback pra contagem de treinos sem agendamento | Frontend | Design | Pending |
| WSD-07 | P1: Widget de frequência — contrato do endpoint de dashboard inalterado | Backend | Design | Pending |

**ID format:** `WSD-NN`

**Status values:** Pending → In Design → In Tasks → Implementing → Verified

**Coverage:** 7 total, 0 mapped to tasks, 7 unmapped ⚠️ (Design/Tasks ainda não rodaram)

---

## Success Criteria

- [ ] Autor consegue atribuir 0, 1 ou N dias da semana a um treino no editor, e o estado persiste corretamente ao reabrir
- [ ] Card "Exercícios Prescritos para Hoje" nunca aparece (nem dispara fetch) quando nenhum treino do usuário está atribuído ao dia corrente
- [ ] Card "Exercícios Prescritos para Hoje" aparece corretamente e agrega todos os treinos atribuídos ao dia corrente quando há mais de um
- [ ] Widget de frequência nunca mais mostra um denominador fixo (`5`) — reflete dias distintos atribuídos (quando há agendamento) ou contagem total de treinos (quando não há)
- [ ] Nenhuma regressão no numerador (`sessionsCompletedThisWeek`) do widget de frequência — continua contando sessões completadas na semana corrente exatamente como hoje
