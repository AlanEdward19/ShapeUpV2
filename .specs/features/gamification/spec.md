# Gamification — Specification

## Problem Statement

A Fase 3 do roadmap pede recompensar consistência real de treino (XP, níveis, streak, ShapeCoins, ShapeScore, rankings) — mas o próprio roadmap marca anti-cheating como **pré-requisito, não trabalho paralelo**: nada pode conceder recompensa sem antes classificar a atividade que a gerou. Hoje não existe nenhum conceito de XP/nível/streak-persistido/ShapeCoins/ShapeScore no domínio (confirmado por scan de código) — é greenfield. A feature `event-bus` (já implementada, verificada, `Status: Implemented`) resolve o "como reagir a um treino terminado sem acoplar direto" — esta feature é o domínio de negócio que efetivamente reage a isso: substitui o consumidor de prova-de-conceito (`WorkoutFinishedConsumer`) por um real, que primeiro classifica a atividade (anti-cheat) e só credita recompensa quando ela é legítima.

## Goals

- [x] Todo treino concluído é classificado (`Verified`/`Likely Valid`/`Suspicious`/`Invalid`) ANTES de qualquer recompensa ser concedida — nunca depois, nunca sem checagem
- [x] Treinos `Verified`/`Likely Valid` concedem XP e ShapeCoins de forma consistente; `Suspicious`/`Invalid` não concedem nada
- [x] Streak (dias consecutivos com atividade legítima) e nível evoluem a partir do histórico real, não de auto-relato
- [x] ShapeScore (consistência + evolução + metas + atividades verificadas — nunca volume bruto) fica disponível por usuário, com ranking global
- [x] Usuário consegue ver seu próprio progresso (XP, nível, streak, ShapeCoins, ShapeScore) via API

## Out of Scope

| Item | Motivo |
|---|---|
| Desafios (challenges) como fonte de XP/ShapeCoins/ShapeScore | Depende da feature de Challenges (Fase 7/Social), que não existe — decisão já confirmada com o usuário antes desta spec |
| Ranking filtrado por grafo social (amigos/seguidos) | Não existe follow/friendship no domínio ainda (Fase 7) — v1 é só ranking global |
| Anti-cheat por GPS/localização | Não existe atividade outdoor/GPS-trackable hoje (só treino de academia) — decisão já confirmada com o usuário antes desta spec |
| Achievements/badges (sistema inteiro, não só o catálogo) | Usuário pediu explicitamente pra revisitar isso numa fase própria futura, com motor dinâmico (achievement é dado configurável, nenhum redeploy pra adicionar um novo) — construir uma versão mecânica pequena agora só pra depois jogar fora não serve. Esta feature entrega XP/nível/streak/ShapeCoins/ShapeScore/ranking, e garante (ver Assumptions) que os "momentos que importam" (XP creditado, subiu de nível, bateu marco de streak) ficam observáveis o suficiente pra essa fase futura plugar sem redesenhar o core |
| Animações/badges/ícones/UI "viciante" (frontend) | Pedido explícito do usuário, mas é decisão de produto/design de UI, não arquitetura de backend — fica para a spec de frontend desta feature (ou pra fase de Achievements) decidir a experiência visual; esta spec (backend) só garante que o DADO necessário pra essas celebrações existe e é consultável (ver Assumptions) |
| Loja/resgate de ShapeCoins | Sem catálogo de itens/recompensas resgatáveis definido — isso é Fase 4 (Monetização) território, não esta feature |
| Reversão/estorno de recompensa já concedida | Decisão já confirmada: anti-cheat SEGURA a recompensa até classificar, nunca credita e depois reverte — logo não existe fluxo de estorno pra construir |
| Revisão manual/admin de sessões `Suspicious`/`Invalid` | Sem ferramenta de admin definida; sessão fica classificada e sem recompensa, ponto final, nesta versão |
| Extrair dados de sessão além do que a análise de anti-cheat precisa (nenhum novo campo no evento `WorkoutFinished`) | Ver Assumptions — o consumidor busca detalhe direto no `IWorkoutSessionRepository` do Training (mesmo padrão já usado por `TrainerClientAdherenceCalculator` em GymManagement), evento continua minimalista |

---

## Assumptions & Open Questions

| Assumption / decision | Chosen default | Rationale | Confirmed? |
|---|---|---|---|
| Timing de anti-cheat vs recompensa | Anti-cheat roda SÍNCRONO dentro do consumidor de `WorkoutFinished`, antes de qualquer crédito. `Suspicious`/`Invalid` nunca creditam (sem estorno) | Usuário confirmou explicitamente esta opção | y |
| Regra de "volume anormal" | Compara volume da sessão contra a média móvel das últimas N sessões (N=10, ou todas se houver menos) do MESMO usuário — vota graduado: `Likely Valid` se <3 sessões prévias (dado insuficiente), `Verified` se ≤3x a média, `Suspicious` se entre 3x e 6x, `Invalid` se >6x | Usuário confirmou baseline por usuário em vez de limite absoluto; graduação usa os 4 estados do roadmap em vez de binário (ver P1 "Anti-cheat... 4 níveis") | y |
| Storage | SQL Server (`GamificationDbContext`, novo, mesmo padrão dos outros domínios) | Usuário confirmou — dado é contador/ledger, relacional encaixa, maioria dos domínios já é SQL | y |
| Regra de "atividade impossível" | `r = DurationSeconds / (soma de RestSeconds de todos os sets + 10s de execução mínima por set)`. Vota graduado: `r<0.3`→`Invalid`, `0.3–0.5`→`Suspicious`, `0.5–0.8`→`Likely Valid`, `≥0.8`→`Verified` | Nenhuma definição de produto existia; regra é mecânica, calculável a partir de dados já persistidos em `WorkoutSessionDocument`; faixas são valores iniciais ajustáveis (não uma verdade absoluta sobre fisiologia humana), graduadas em vez de binárias pra usar os 4 estados do roadmap | n — assumption, log apenas |
| Regra de "duplicação" | Ao avaliar uma sessão, sistema busca outras sessões JÁ AVALIADAS do MESMO `ExecutedByUserId`, finalizadas dentro de 5 minutos. Match EXATO (100% dos sets idênticos)→`Invalid`; match parcial (≥80% dos pares exercício/carga/reps, não 100%)→`Suspicious`; nenhum match→`Verified`. A(s) sessão(ões) anterior(es) já avaliada(s) NUNCA são reclassificadas nem tem crédito revogado (consistente com "anti-cheat roda antes de creditar, nunca reverte depois" — como a avaliação é sempre síncrona e prévia ao crédito, a sessão que chega primeiro nunca encontra duplicata ainda, então é sempre a segunda/subsequente que recebe o voto desta regra) | Cobre o caso óbvio (replay do mesmo treino) sem exigir um novo dado (device fingerprint, etc.) que não existe hoje, sem contradizer "nunca reverte", e distinguindo replay comprovado (`Invalid`) de coincidência parcial ambígua (`Suspicious`) | n — assumption, log apenas |
| Combinação dos 3 votos numa classificação final | "Pior vence": `Invalid` > `Suspicious` > `Likely Valid` > `Verified` | Uma sessão só deveria ser tratada tão bem quanto a regra mais desconfiada permite — uma regra gritando `Invalid` não deveria ser diluída por outras duas dizendo `Verified` | y |
| Onde o consumidor busca detalhe da sessão (duração, sets, volume) que o evento `WorkoutFinished` não carrega | Consumidor injeta e chama `IWorkoutSessionRepository.GetByIdAsync` (Training) diretamente — mesmo padrão já usado hoje por `TrainerClientAdherenceCalculator` (GymManagement lendo `WorkoutSessionDocument` do Training) | Evita reabrir a feature `event-bus` (já implementada e verificada) só pra engordar o payload do evento; segue um padrão de leitura cross-domain que já existe no código, não é novidade arquitetural | y (baseado em precedente já existente no código) |
| XP por treino verificado | 50 XP flat (`Verified`/`Likely Valid`) | Sem fórmula de produto definida; valor simples e isolado numa única constante, fácil de retunar depois sem migração | n — assumption, log apenas |
| Fórmula de nível | `Level = floor(TotalXp / 500) + 1` (500 XP por nível, flat, sem curva) | Mais simples possível que ainda produz progressão; retunar a curva depois é mudar uma função, não um redesenho | n — assumption, log apenas |
| ShapeCoins por treino verificado | 10 moedas flat | Mesma razão do XP — valor isolado, fácil de ajustar | n — assumption, log apenas |
| Bônus de streak | A cada marco de 7 dias consecutivos, +50 ShapeCoins bônus (uma vez por marco, não repetido dentro do mesmo marco) | Padrão comum de gamificação (streak matters), sem inventar mecânica nova além do que o roadmap já pede | n — assumption, log apenas |
| Achievements/badges | Fora desta feature por completo (nem catálogo mínimo) — fica pra uma fase futura dedicada, com motor dinâmico | Usuário pediu explicitamente pra revisitar isso separadamente, num sistema mais robusto (dinâmico, sem redeploy pra adicionar achievement) — não faz sentido construir 5 hardcoded agora só pra substituir depois | y |
| Sinal observável pra celebração/achievement futuro | Toda mudança que "importa" pro usuário (XP creditado, nível subiu, marco de streak batido) fica registrada de forma consultável — não só o estado atual (XP total, nível atual), mas também O QUE mudou nesta última avaliação (ex.: `LeveledUpFrom`/`LeveledUpTo` quando aplicável, `StreakMilestoneHit: bool` na resposta do consumo do evento ou num endpoint de "últimas conquistas"). Sem isso, uma fase futura de achievements/animação teria que re-inferir "o que aconteceu de notável" varrendo histórico, ou esta feature teria que ser redesenhada pra expor isso depois Custo mínimo de acoplamento futuro: exige só que o dado já sendo calculado (delta de XP, nível antes/depois, streak antes/depois) seja PERSISTIDO/exposto em vez de descartado após uso interno — não é um motor de achievement, é só não jogar fora o "diff" que uma UI viciante ou um motor de regra vai precisar depois | y (compromisso mínimo, decidido pra viabilizar as fases futuras sem redesenho) |
| Fórmula de ShapeScore (v1) | Média simples de 4 sub-scores (0–1 cada, resultado 0–100): Consistência (dias com treino verificado / dias no período de 30 dias corridos), Evolução (1 se houve novo PR pessoal no período, senão 0), Metas (fração de semanas do período em que a meta semanal — `SessionsTargetPerWeek` já existente no dashboard — foi batida), Verificação (fração de sessões do período que são `Verified`/`Likely Valid`, não `Suspicious`/`Invalid`) | Nenhuma fórmula de produto existe; média simples é o ponto de partida mais defensável e mais fácil de explicar/ajustar depois — documentado explicitamente como v1/placeholder | n — assumption, log apenas |
| Ranking | Só global (todos os usuários, ordenado por ShapeScore desc), paginado | Confirmado com o usuário (sem grafo social ainda) | y |
| Sessão sem histórico suficiente (usuário novo) | Streak começa em 1 no primeiro treino verificado; ShapeScore usa os sub-scores calculáveis com o dado disponível (ex.: sem PR anterior pra comparar, Evolução = 0, não erro) | Evita necessidade de dado sintético/seed; sistema degrada graciosamente pra usuário novo | y (default seguro) |
| Coexistência do streak de Gamificação com o card de streak já existente no dashboard (`DashboardClient.jsx`, calculado direto do histórico bruto do Training, sem anti-cheat) | Os dois ficam visíveis, sem unificar — o card já existente NÃO muda (continua contando todo dia com treino, `Suspicious` incluso); o novo card de Gamificação mostra o streak que só conta dias `Verified`/`Likely Valid`. Podem divergir num dia com sessão suspeita, e isso é esperado, não um bug | Unificar os dois exigiria mudar o comportamento de uma tela já existente fora do escopo desta feature, ou esconder a diferença entre "treinou" e "treinou de forma verificada" — que é justamente a distinção que este anti-cheat existe pra fazer | y (default seguro, decisão de não tocar em UI pré-existente fora de escopo) |

**Open questions:** nenhuma sem resposta — tudo acima está resolvido ou registrado como assumption.

---

## User Stories

### P1: Consumidor real classifica e credita XP/ShapeCoins ⭐ MVP

**User Story**: Como sistema, quero que todo treino concluído seja classificado por anti-cheat e, se legítimo, credite XP e ShapeCoins ao usuário que o executou.

**Why P1**: É o loop central — sem isso, "gamification" não existe, é só um evento sendo ignorado.

**Acceptance Criteria**:

1. WHEN o evento `WorkoutFinished` é consumido THEN sistema SHALL buscar o detalhe da sessão (`IWorkoutSessionRepository.GetByIdAsync`) e classificá-la em `Verified`, `Likely Valid`, `Suspicious`, ou `Invalid` ANTES de qualquer crédito
2. WHEN a sessão classifica como `Verified` ou `Likely Valid` THEN sistema SHALL creditar 50 XP e 10 ShapeCoins ao `ExecutedByUserId`
3. WHEN a sessão classifica como `Suspicious` ou `Invalid` THEN sistema SHALL NÃO creditar XP nem ShapeCoins, e SHALL registrar a classificação de forma consultável (não descartar silenciosamente)
4. WHEN o mesmo `WorkoutFinished` é entregue mais de uma vez (redelivery do bus) THEN sistema SHALL processar o efeito uma única vez (idempotência já garantida pelo bus — `event-bus` AD-009 — este consumidor não duplica crédito)

**Independent Test**: Finalizar um treino simples e plausível, confirmar que XP/ShapeCoins do usuário aumentam exatamente uma vez.

---

### P1: Anti-cheat classifica atividade impossível, duplicada e volume anormal — nos 4 níveis do roadmap

**User Story**: Como sistema, quero detectar as 3 formas de fraude em escopo (atividade impossível, duplicação, volume anormal) antes de conceder qualquer recompensa, usando os 4 estados que o roadmap define (`Verified`/`Likely Valid`/`Suspicious`/`Invalid`) — não só um binário aprovado/reprovado.

**Why P1**: É o pré-requisito explícito do roadmap — sem isso, a história acima concede recompensa sem checagem nenhuma. Usar os 4 estados de verdade (em vez de só 2) aproveita melhor o sinal que já existe nos 3 dados em escopo (duração, replay exato, desvio de volume) sem precisar de nenhum dado novo.

**Acceptance Criteria** — cada uma das 3 regras produz um veredito graduado (não binário); a classificação final da sessão é o PIOR veredito entre as 3 (`Invalid` > `Suspicious` > `Likely Valid` > `Verified`, nessa ordem de severidade):

1. **Atividade impossível** — seja `r = DurationSeconds / (soma de RestSeconds de cada set + 10s de execução mínima por set)`:
   - WHEN `r < 0.3` THEN a regra SHALL votar `Invalid` (fisicamente implausível sob qualquer leitura)
   - WHEN `0.3 ≤ r < 0.5` THEN a regra SHALL votar `Suspicious`
   - WHEN `0.5 ≤ r < 0.8` THEN a regra SHALL votar `Likely Valid`
   - WHEN `r ≥ 0.8` THEN a regra SHALL votar `Verified`
2. **Duplicação** — comparando contra sessões JÁ AVALIADAS do MESMO `ExecutedByUserId` finalizadas dentro de 5 minutos:
   - WHEN existe uma sessão anterior com conjunto EXATAMENTE idêntico de `(ExerciseId, Sets.Count, cada Load, cada Repetitions)` THEN a regra SHALL votar `Invalid` (replay comprovado, sem ambiguidade)
   - WHEN existe uma sessão anterior com pelo menos 80% dos mesmos pares `(ExerciseId, Load, Repetitions)` mas não 100% idêntica THEN a regra SHALL votar `Suspicious` (quase-duplicata, ambíguo demais pra `Invalid`)
   - WHEN nenhuma sessão anterior parecida é encontrada THEN a regra SHALL votar `Verified`
   - Em qualquer caso, a sessão anterior já avaliada NUNCA é reclassificada nem perde crédito já concedido (só a sessão sendo avaliada agora recebe o voto desta regra)
3. **Volume anormal** — seja `m` a média de volume das últimas 10 sessões do usuário (ou todas, se menos de 10):
   - WHEN o usuário tem menos de 3 sessões anteriores THEN a regra SHALL votar `Likely Valid` (dado insuficiente pra confirmar `Verified` com confiança, mas nenhuma evidência contra também — nunca vota `Suspicious`/`Invalid` só por falta de histórico)
   - WHEN o volume desta sessão é `> 6x m` THEN a regra SHALL votar `Invalid`
   - WHEN o volume desta sessão está entre `3x m` (exclusive) e `6x m` (inclusive) THEN a regra SHALL votar `Suspicious`
   - WHEN o volume desta sessão é `≤ 3x m` THEN a regra SHALL votar `Verified`
4. WHEN as 3 regras votam THEN sistema SHALL classificar a sessão com o PIOR veredito entre elas (`Invalid` vence sobre `Suspicious`, que vence sobre `Likely Valid`, que vence sobre `Verified`)

**Independent Test**: Forçar cada regra isoladamente em cada uma das suas faixas (ex.: `r=0.2`→`Invalid`, `r=0.4`→`Suspicious`, `r=0.6`→`Likely Valid`, `r=0.9`→`Verified` só pra atividade impossível) e confirmar o veredito de cada regra; depois combinar 2 regras com veredito diferente (ex.: duplicação exata=`Invalid` + volume normal=`Verified`) e confirmar que a classificação final é `Invalid` (o pior vence).

---

### P1: Streak evolui a partir do histórico real de treinos legítimos

**User Story**: Como usuário, quero que meu streak reflita dias consecutivos reais de treino verificado, não auto-relato.

**Why P1**: É um dos 3 pilares centrais do roadmap (XP/streak/badges), e alimenta o ShapeScore.

**Acceptance Criteria**:

1. WHEN o usuário completa um treino `Verified`/`Likely Valid` no dia seguinte ao seu último dia com treino legítimo (UTC) THEN sistema SHALL incrementar o streak em 1
2. WHEN o usuário completa um treino `Verified`/`Likely Valid` no MESMO dia (UTC) do seu último treino legítimo já contado THEN sistema SHALL manter o streak (não incrementa duas vezes no mesmo dia)
3. WHEN o usuário completa um treino `Verified`/`Likely Valid` com um gap de mais de 1 dia desde o último THEN sistema SHALL resetar o streak para 1
4. WHEN o treino classifica `Suspicious`/`Invalid` THEN sistema SHALL NÃO alterar o streak de forma alguma

**Independent Test**: Simular treinos legítimos em dias consecutivos (streak sobe), depois um gap de 2 dias (streak reseta pra 1), depois uma sessão `Suspicious` no meio (streak não muda).

---

### P1: Nível evolui a partir do XP acumulado

**User Story**: Como usuário, quero ver meu nível subir conforme acumulo XP.

**Why P1**: Parte do pilar "XP, níveis" do roadmap.

**Acceptance Criteria**:

1. WHEN o XP total do usuário muda THEN sistema SHALL recalcular o nível como `floor(TotalXp / 500) + 1`

**Independent Test**: Creditar XP suficiente pra cruzar um limite de nível, confirmar que o nível reportado muda.

---

### P1: Usuário consegue ler (e VER) seu próprio progresso

**User Story**: Como usuário, quero ver meu XP, nível, streak, ShapeCoins e ShapeScore atuais — na tela, não só numa resposta de API que ninguém olha.

**Why P1**: Sem uma forma de LER o que foi construído acima, a feature não é usável — é a metade "read" do vertical slice. E sem uma TELA mostrando isso, o backend não entrega valor de produto nenhum (gamificação que ninguém vê não gamifica ninguém).

**Acceptance Criteria**:

1. WHEN o usuário autenticado consulta seu próprio perfil de gamificação THEN sistema SHALL retornar XP total, nível, streak atual, ShapeCoins, e ShapeScore (mesmo que ainda não existam — retorna zerado pra usuário sem histórico, nunca erro)
2. WHEN o usuário abre seu dashboard (`ShapeUp-Web`) THEN a interface SHALL exibir XP/progresso-até-o-próximo-nível, nível atual, streak (o de Gamificação, não o card de streak já existente no dashboard que é calculado direto do histórico bruto do Training — ver Assumptions), ShapeCoins, e ShapeScore, sem precisar de outra tela

**Independent Test**: Consultar o perfil de um usuário recém-criado (sem treinos) — retorna tudo zerado, sem erro, e o dashboard mostra o estado zerado sem quebrar; após um treino verificado, os valores refletem o crédito tanto na API quanto na tela.

---

### P1: Frontend — hook de API + card de progresso no dashboard

**User Story**: Como usuário, quero ver meu progresso de gamificação no MESMO lugar onde já vejo meu progresso de treino (dashboard), sem navegar pra uma tela separada.

**Why P1**: É a metade "visível" da story anterior — sem isso, "usuário consegue ler" vira só um contrato de API sem consumidor real.

**Acceptance Criteria**:

1. WHEN o hook de API de gamificação (`useGamificationApi`) chama o perfil THEN sistema SHALL expor os dados no mesmo padrão dos hooks já existentes (`useTrainingApi`, etc.) — sem reinventar o mecanismo de chamada
2. WHEN o card de progresso é renderizado no dashboard do cliente/independente THEN sistema SHALL mostrar os 5 valores (XP/progresso de nível, nível, streak, ShapeCoins, ShapeScore) usando os componentes visuais já existentes (`Card`, ícones `lucide-react` já em uso — `Flame` já é usado pro streak de Training, mas o de Gamificação usa outro ícone pra não confundir os dois conceitos) — sem introduzir uma biblioteca de UI nova
3. WHEN o usuário ainda não tem nenhum treino verificado THEN o card SHALL mostrar o estado zerado de forma clara (ex.: "Complete seu primeiro treino pra começar", não um card vazio/quebrado)

**Independent Test**: Abrir o dashboard de um usuário com pelo menos 1 treino verificado — confirma os 5 valores visíveis e corretos, sem chamada de API extra além da já existente pro resto do dashboard.

---

### P2: Frontend — tela/seção de ranking global

**User Story**: Como usuário, quero ver o ranking global de ShapeScore numa tela do app, não só via API.

**Why P2**: Depende do endpoint de ranking (P2 backend) já existir; é a metade visível daquela story.

**Acceptance Criteria**:

1. WHEN o usuário acessa a seção/tela de ranking THEN sistema SHALL listar usuários ordenados por ShapeScore desc, paginado (mesmo padrão de paginação por cursor já usado em outras listas do app), destacando a posição do próprio usuário se ele aparecer na página atual

**Independent Test**: Abrir a tela de ranking com 3+ usuários seedados com ShapeScores diferentes — confirma ordem correta e navegação de página funcionando.

---

### P2: Bônus de marco de streak

**User Story**: Como usuário, quero ganhar um bônus ao atingir marcos de streak (7, 14, 21 dias...).

**Why P2**: Reforça o loop de retenção, mas o P1 já funciona (credita XP/coins, rastreia streak/nível) sem isso.

**Acceptance Criteria**:

1. WHEN o streak do usuário atinge um múltiplo de 7 (7, 14, 21...) pela primeira vez naquele valor THEN sistema SHALL creditar 50 ShapeCoins bônus (uma vez por marco)

**Independent Test**: Atingir streak 7 — confirma bônus creditado uma vez; atingir 8, 9... não credita de novo até o próximo múltiplo de 7.

---

### P2: ShapeScore calculado e ranking global

**User Story**: Como usuário, quero ver meu ShapeScore e minha posição num ranking global.

**Why P2**: Depende do histórico (P1) já existir de forma confiável antes de fazer sentido expor um número agregado/comparativo.

**Acceptance Criteria**:

1. WHEN o ShapeScore de um usuário é calculado THEN sistema SHALL usar a média dos 4 sub-scores (Consistência, Evolução, Metas, Verificação) sobre uma janela de 30 dias corridos, resultando em 0–100
2. WHEN o usuário consulta o ranking global THEN sistema SHALL retornar usuários ordenados por ShapeScore desc, paginado

**Independent Test**: Dois usuários com históricos diferentes — confirmar ShapeScore maior pro mais consistente, e que ele aparece antes no ranking.

---

### P2: Sinal de "o que mudou" fica exposto (base pra achievements/animação futuros)

**User Story**: Como uma fase futura (achievements dinâmicos, UI de celebração), quero conseguir saber O QUE mudou de notável na última avaliação de um usuário (subiu de nível? bateu marco de streak?) sem ter que reprocessar todo o histórico.

**Why P2**: Não é a fase de achievements em si (fora de escopo, ver Out of Scope) — é só garantir que o "diff" que essa fase futura vai precisar não é jogado fora agora, o que evitaria redesenho depois.

**Acceptance Criteria**:

1. WHEN uma avaliação de gamificação processa um `WorkoutFinished` THEN sistema SHALL registrar, de forma consultável, se houve mudança de nível (`LeveledUp: bool`, com nível anterior e novo) e se um marco de streak foi atingido (`StreakMilestoneHit: bool`, com o valor do marco)
2. WHEN o usuário consulta seu perfil (story P1 acima) THEN sistema SHALL incluir os últimos eventos notáveis (nível subiu, marco de streak) de forma que uma UI futura consiga exibi-los sem consultar outra fonte

**Independent Test**: Creditar XP suficiente pra subir de nível numa única avaliação — o registro consultável mostra `LeveledUp: true` com nível anterior e novo, não só o nível atual.

---

## Edge Cases

- WHEN duas sessões idênticas (regra de duplicação) são avaliadas fora de ordem de criação (ex.: a que foi FINALIZADA por último é avaliada pelo consumidor antes da outra, por qualquer razão de timing do bus) THEN sistema SHALL votar `Invalid`/`Suspicious` (conforme o grau de match) na que for avaliada EM SEGUNDO LUGAR (não importa qual foi finalizada primeiro no Training — importa qual foi AVALIADA primeiro aqui) — nunca reclassifica ou revoga a que já foi avaliada e creditada
- WHEN o usuário nunca treinou antes (streak/XP/nível zerados) THEN sistema SHALL tratar como estado inicial válido, nunca erro (ver Assumptions)
- WHEN a sessão não tem nenhum set registrado (`Exercises` vazio, denominador da regra de atividade impossível é zero) THEN a regra de atividade impossível SHALL votar `Invalid` diretamente (degenerado — não há base nenhuma pra qualquer leitura mais branda; ausência de dado não é o mesmo que dado válido)
- WHEN o cálculo de ShapeScore roda para um usuário sem NENHUMA sessão nos últimos 30 dias THEN sistema SHALL retornar ShapeScore 0 (todos os 4 sub-scores são 0 por falta de dado), não erro

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
|---|---|---|---|
| GAM-01 | P1: Consumidor real classifica e credita | Execute | Verified |
| GAM-02 | P1: Consumidor — idempotência de crédito | Execute | Verified |
| GAM-03 | P1: Anti-cheat — atividade impossível | Execute | Verified |
| GAM-04 | P1: Anti-cheat — duplicação | Execute | Verified |
| GAM-05 | P1: Anti-cheat — volume anormal (com baseline mínima) | Execute | Verified |
| GAM-06 | P1: Streak incrementa/mantém/reseta | Execute | Verified |
| GAM-07 | P1: Nível a partir do XP | Execute | Verified |
| GAM-08 | P1: Leitura do próprio perfil | Execute | Verified |
| GAM-09 | P2: Bônus de marco de streak | Execute | Verified |
| GAM-10 | P2: ShapeScore + ranking global | Execute | Verified |
| GAM-11 | P2: Sinal de "o que mudou" exposto (base pra achievements/animação futuros) | Execute | Verified |
| GAM-12 | P1: Frontend — hook de API + card de progresso no dashboard | Execute | Verified |
| GAM-13 | P2: Frontend — tela/seção de ranking global | Execute | Verified |

**ID format:** `GAM-NN`

**Status values:** Pending → In Design → In Tasks → Implementing → Verified

**Coverage:** 13 total, 13 Verified ✅

---

## Success Criteria

- [x] Um treino plausível credita XP+ShapeCoins e avança streak/nível corretamente
- [x] Um treino relâmpago (duração impossível), um duplicado, e um com volume 4x a média do usuário são todos classificados `Suspicious` e NÃO creditam nada
- [x] Redelivery do mesmo evento não duplica crédito
- [x] Perfil de gamificação é consultável (mesmo zerado) sem erro
- [x] ShapeScore + ranking global refletem consistência real, não volume bruto
</content>
