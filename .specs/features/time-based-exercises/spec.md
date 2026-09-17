# Exercícios Baseados em Tempo — Specification

> **Cross-repo**: esta spec é canônica no repo da API (`ShapeUpApi`) porque `ExerciseType` e os campos de duração/distância vivem no catálogo/domínio de `workout-editor`/`workout-execution-validation`; a UI de `ShapeUp-Web` (`TrainingPlansClient.jsx`, editor de bloco) só troca os inputs condicionalmente conforme esse tipo, sem lógica de negócio própria. Cada AC é marcado **[Frontend]** ou **[Backend]**. Não existe spec irmã separada no frontend — o consumo/UI está descrito nesta mesma spec. Ordem: backend (`ExerciseType`, validação, PR de pace) primeiro; frontend consome o tipo já resolvido no snapshot da sessão.

## Problem Statement

Nem todo exercício é peso × reps. Corrida, exercícios de resistência/endurance e alongamento são medidos por duração (e, no caso de corrida, opcionalmente por distância/ritmo) — não por carga levantada. Hoje o modelo de execução do `workout-editor`/`workout-execution-validation` assume peso+reps para TODO set, sempre: `Exercise` (catálogo, `Features/Training/Shared/Entities/Exercise.cs`) não tem nenhum campo de tipo/medição, e o set (`PlannedSetDocumentValueObject` no planejamento, `WorkoutSetValueObject`/`ExecutedSetDocumentValueObject` na execução) tem `Load: decimal` (não nullable) e `Repetitions: int?` como única forma de descrever o que foi prescrito/feito. `WEV-01`/`WEV-02` (spec `workout-execution-validation`, já confirmada) tornam essa suposição uma regra hard-coded: um set só pode ser concluído com peso e reps válidos preenchidos — o que é fisicamente impossível de satisfazer para uma corrida ou um alongamento cronometrado. Sem esta feature, a Fase 2 (Core Fitness) do roadmap não está de fato completa — falta o segundo tipo de treino que o roadmap pede.

## Goals

- [ ] Um exercício no catálogo pode ser classificado como peso-baseado (`WeightBased`, comportamento atual, default) ou tempo-baseado (`TimeBased`)
- [ ] Um set de um exercício `TimeBased` é logado por duração (sempre obrigatória) e, opcionalmente, distância — nunca por peso/reps
- [ ] O gate de conclusão de set (`WEV-01`/`WEV-02`) passa a ser condicional ao tipo do exercício: `WeightBased` continua exigindo peso+reps exatamente como hoje; `TimeBased` exige duração (distância opcional) — os dois gates coexistem, nenhum enfraquece o outro
- [ ] RPE continua aplicável a ambos os tipos de exercício, sem nenhuma mudança de regra
- [ ] A tela de execução (`TrainingPlansClient.jsx`) troca condicionalmente os inputs de peso/reps por um input de duração (e distância, quando aplicável) conforme o tipo do exercício do set
- [ ] Exercícios `TimeBased` com distância registrada (ex.: corrida) têm um equivalente de PR baseado em ritmo (pace = duração/distância) — exercícios `TimeBased` sem distância (ex.: alongamento) não geram PR nenhum

## Out of Scope

| Item | Motivo |
|---|---|
| GPS tracking / sensores de mobile | Fase 6 (Mobile & Offline) não existe ainda — esta feature é só Web, entrada manual de duração/distância |
| Analytics de ritmo/pace além do PR (splits, gráfico de evolução de pace ao longo do tempo, pace médio por km) | Fora do pedido — esta feature calcula e persiste o PR de pace (ver TBE-06), mas não constrói dashboards/analytics de pace; isso é feature futura própria |
| Integração com wearables (relógio, cinta cardíaca, etc.) | Nenhuma integração de hardware/terceiro é construída aqui — entrada é sempre manual |
| Redesenho de `ShapeScore`/fórmulas de XP para tratar duração como métrica de primeira classe | `Gamification`/`ShapeScore` continuam como estão nesta feature — o cálculo de volume (`Load * Repetitions`) simplesmente não se aplica a sets `TimeBased` (ver Assumptions e Cross-Cutting Dependencies). Adaptar `ShapeScore`/anti-cheat/PRs para usar duração como métrica alternativa é trabalho de follow-up em `gamification`, não desta spec |
| Taxonomia completa de técnicas avançadas para exercícios tempo-baseados (novas técnicas tipo "tempo run", "fartlek", etc.) | Fora do pedido — esta feature só resolve QUAIS técnicas/blocos hoje existentes (`Technique`, `BlockType`) continuam fazendo sentido para `TimeBased` (ver Assumptions), não inventa técnicas novas |
| Migração/backfill de exercícios já cadastrados no catálogo para `TimeBased` | Todo exercício existente permanece `WeightBased` por default (migração é aditiva, não reclassifica dado histórico) — reclassificar exercícios específicos (ex.: "Corrida" já cadastrado hoje como genérico) é curadoria de conteúdo, não desta spec |
| Unidade imperial (milhas, pés) para distância | Ver Assumptions — decisão de manter metric-only, consistente com `nutrition` |

---

## Assumptions & Open Questions

| Assumption / decision | Chosen default | Rationale | Confirmed? |
|---|---|---|---|
| Modelo de dados — onde vive o tipo do exercício | Um único enum `ExerciseType` (`WeightBased = 1` default, `TimeBased = 2`) em `Exercise` (catálogo, SQL/EF — `Features/Training/Shared/Entities/Exercise.cs`), não dois agregados de exercício paralelos. O SET (`PlannedSetDocumentValueObject`/`WorkoutSetValueObject`/`ExecutedSetDocumentValueObject`) ganha `DurationSeconds: int?` e `DistanceMeters: decimal?` (ambos nullable, mesmo padrão de `RestSeconds: int?` já existente) ao lado dos campos de peso/reps já existentes (`Load`, `Repetitions`) — `Load` deixa de ser `decimal` não-nullable e passa a `decimal?` (nullable), já que um set `TimeBased` não tem carga. Nenhum campo é removido; a validação (não o schema) é que passa a ramificar pelo `ExerciseType` do exercício pai do set | Um exercício não muda de tipo dinamicamente por set (não existe "metade do exercício é peso, metade é tempo") — o tipo pertence ao exercício do catálogo, não ao set. Um único VO de set com campos nullable-por-tipo é mais simples que dois VOs paralelos (`WeightSet`/`TimeSet`): evita duplicar toda a estrutura já existente (`SetType`, `Technique`, `Intensity`, `RestSeconds`, `IsExtra`) só para trocar 2 campos, e evita migração de schema em cascata (DTOs, mappers, handlers) que dois tipos de set exigiriam. Nenhum `ExerciseType`/`MeasurementType` existe hoje no código (grep confirmado) — não há green-field a preservar, mas também nenhum precedente a quebrar | y (decorre da leitura do modelo existente, decisão de design mais simples que atende o requisito) |
| Duração vs. distância — o que é obrigatório | Para um set `TimeBased`: `DurationSeconds` é SEMPRE obrigatório para concluir o set (é a única resposta universal a "quanto desse exercício foi feito" — alongamento não tem distância, corrida em esteira sem sensor também não). `DistanceMeters` é sempre OPCIONAL, independente do exercício (mesmo para corrida) | Pedido explícito do usuário: "duração é sempre a resposta universal"; esteira sem tracking de distância é comum — exigir distância bloquearia um caso de uso legítimo e frequente | y (pedido explícito do usuário) |
| Unidade de armazenamento — duração | Segundos (`int`) como unidade canônica única de armazenamento (`DurationSeconds`), exibida formatada (mm:ss ou hh:mm:ss) na UI — mesmo padrão já usado por `RestSeconds` no set existente | Reuso direto do padrão já estabelecido no mesmo VO (`RestSeconds: int?`) — nenhuma unidade nova de armazenamento a decidir, apenas estender o padrão já usado | y (decorre do padrão já existente no repo) |
| Unidade de armazenamento — distância | Metros (`decimal`) como unidade canônica, exibida formatada (m ou km conforme magnitude) na UI. Metric-only — sem suporte a milhas/pés nesta versão | `nutrition` (spec confirmada) já fixou "metric-only" como padrão do app inteiro para quantidade (100g/100ml, sem libras/onças) — mesma convenção aplicada aqui para consistência. Único precedente de unidade imperial no repo é `LoadUnit` (`Kg`/`Lbs`, exclusivo de peso, pré-existente ao produto decidir por metric-only em `nutrition`) — não é uma razão para introduzir imperial numa dimensão nova (distância) que nunca teve precedente | y (consistente com decisão já confirmada em `nutrition`; revisitar apenas se o produto decidir reabrir unidades imperiais de forma ampla, o que não foi pedido) |
| Gate de conclusão de set — reconciliação com `WEV-01`/`WEV-02` | `WEV-01` (`TrainingPlansClient.jsx`, `toggleSetComplete`) e `WEV-02` (`UpdateWorkoutExecutionStateCommandValidator`) continuam EXATAMENTE como especificados — peso `>= 0` E reps `>= 1` obrigatórios — mas essa regra passa a se aplicar apenas quando `set.Exercise.ExerciseType == WeightBased` (o caso de hoje, e o único caso que existia quando `WEV-01`/`WEV-02` foram escritos). Um NOVO gate paralelo (`TBE-01`/`TBE-02` abaixo) exige `DurationSeconds >= 1` quando `ExerciseType == TimeBased`, com `DistanceMeters` sempre opcional. Nenhuma linha de `WEV-01`/`WEV-02` é removida ou enfraquecida — o `RuleFor`/gate client-side que hoje roda incondicionalmente passa a rodar dentro de um `if (exerciseType == WeightBased)`, com um `else if (TimeBased)` novo ao lado | Requisito explícito do usuário: "não enfraquecer WEV-01/02 para exercícios de peso — esta feature ADICIONA um gate paralelo". A branch por tipo é a única forma de fazer isso sem duplicar toda a tela/validador em dois fluxos | y (requisito explícito do usuário, ver texto da tarefa) |
| RPE em exercícios `TimeBased` | RPE continua aplicável a ambos os tipos, sem nenhuma mudança de regra herdada de `workout-execution-validation` (`RequireRpe`, faixa 1-10, opcional por padrão) | Esforço percebido é um conceito de exercício, não de carga — correr uma sessão intensa tem RPE tanto quanto uma série de agachamento. Nenhum motivo de negócio para restringir RPE a `WeightBased` | y (RPE é ortogonal a carga, sem indicação em contrário) |
| Técnicas avançadas (`Technique`: DropSet/RestPause/ClusterSet/MuscleRound) e blocos (`BlockType`: Superset/Amrap/Emom) em exercícios `TimeBased` | `Technique` (DropSet/RestPause/ClusterSet/MuscleRound) permanece exclusivo de `WeightBased` — todas as 4 variantes dependem de reduzir/pausar carga entre esforços, conceito que não existe em duração. Editor SHALL restringir o seletor de `Technique` a `Straight` quando o exercício do bloco for `TimeBased` (nenhuma técnica nova inventada). `BlockType.Amrap`/`Emom` (bloco, não técnica) já são estruturas time-boxed por natureza e continuam permitidas com exercícios `TimeBased` dentro — ex.: um EMOM de corrida em esteira por intervalos é um caso de uso plausível e não exige mudança de schema (Block já é agnóstico ao tipo de exercício dentro dele, por `AD-007`) | Pedido explícito do usuário: manter o escopo estreito, não redesenhar toda a taxonomia de técnica. `Technique` é estruturalmente sobre carga (drop = reduzir peso; rest-pause = pausar sob a mesma carga; cluster = fracionar a mesma carga); nenhuma delas tem sentido sem peso. `BlockType` já é uma decisão de agrupamento independente do tipo de exercício (`AD-007`), então não precisa de restrição nova | y (pedido explícito do usuário: "log as an assumption, keep it narrow") |
| Editor (`workout-editor`) — onde o profissional define `ExerciseType` | `ExerciseType` é um atributo do CATÁLOGO (`Exercise`), definido no cadastro/edição do exercício (mesma tela onde `Name`/`MuscleProfiles`/`Equipments` já são definidos hoje), não escolhido por plano/bloco. Ao adicionar um exercício `TimeBased` a um bloco no editor, a tela de set já nasce mostrando os campos de duração/distância em vez de peso/reps, sem esse toggle aparecer de novo na tela de montagem do plano | O tipo é uma propriedade intrínseca do exercício ("Corrida" é sempre tempo-baseado, não varia por plano) — colocar o campo no catálogo evita redundância de precisar reclassificar o mesmo exercício em cada plano que o usa | y (decorre logicamente da natureza do dado — um exercício não muda de "o que ele mede" entre planos) |
| Exercícios `TimeBased` dentro de `Superset` (`BlockType.Superset`) | Permitido sem restrição adicional — um superset pode combinar exercícios `WeightBased` e `TimeBased` livremente (ex.: agachamento + prancha cronometrada). Cada set dentro do superset segue o gate do seu próprio `ExerciseType`, independente dos demais sets do mesmo bloco | Nenhuma indicação de que superset deva ser homogêneo por tipo; bloco já agrega exercícios heterogêneos por natureza (`AD-007` trata Block como estrutural, não como restrição de tipo de exercício) | n — assumption, log apenas |
| Frontend — onde o `ExerciseType` chega até a tela de execução | Igual ao padrão já usado por `RequireRpe` (`workout-execution-validation`, `WEV-05`): `ExerciseType` é persistido no catálogo, refletido no snapshot congelado quando a sessão de execução é iniciada (`StartWorkoutExecutionHandler`), e consumido pela tela de execução a partir do snapshot da sessão — nunca por uma segunda consulta ao catálogo em tempo de execução | Reuso direto do mecanismo de flatten já estabelecido — nenhum mecanismo novo de propagação de dado é necessário | y (reuso do padrão já confirmado em `WEV-05`) |
| Migração de exercícios já cadastrados no catálogo | Todo `Exercise` existente recebe `ExerciseType = WeightBased` (default), preservando o comportamento atual de 100% dos planos já criados sem nenhuma mudança visível | É a única migração aditiva possível sem quebrar dado existente — nenhum exercício hoje tem informação suficiente pra inferir automaticamente que "deveria" ser `TimeBased` (ex.: um exercício chamado "Corrida" hoje é só texto livre, sem estrutura pra inferência segura) | y (migração aditiva é o único caminho seguro, sem heurística de inferência automática pedida ou confiável) |

| PR equivalente para exercícios `TimeBased` — o que conta como "recorde" | PR de **pace** (`DurationSeconds / DistanceMeters`, menor valor = melhor, exibido formatado como min/km): calculado e persistido SOMENTE quando o set tem `DistanceMeters` preenchido e `> 0`. Quando `DistanceMeters` é nulo/ausente (ex.: alongamento, prancha cronometrada, qualquer `TimeBased` sem distância) NENHUM PR é calculado ou tentado — não é um erro, é simplesmente um exercício sem essa dimensão de recorde, mesmo espírito de "opcional nunca bloqueia" já usado pro campo em si. Reaproveita a mesma estrutura de detecção de PR que já existe para `max_volume` (`FinishWorkoutExecutionHandler.cs`/`CompleteWorkoutSessionHandler.cs`), adicionando um tipo novo de PR (`"best_pace"`) ao lado do já existente, em vez de um mecanismo paralelo | Pedido explícito do usuário: corrida tem pace como métrica natural de progresso; alongamento não tem (nem precisa) de equivalente de PR — a mesma nulidade de `DistanceMeters` que já distingue "corrida" de "alongamento" no modelo de dados serve exatamente para decidir quem ganha PR, sem precisar de um segundo flag/campo novo | y (pedido explícito do usuário) |
| PR de duração pura (sem distância) — ex.: prancha, alongamento, corrida sem GPS | NÃO é criado nenhum PR de "maior duração" nesta versão — usuário confirmou explicitamente que alongamento (e por extensão, qualquer `TimeBased` sem distância) não precisa de equivalente de PR. Se no futuro fizer sentido um PR de "aguentou mais tempo" para prancha, é decisão de produto separada, não assumida aqui | Pedido explícito do usuário: "alongamentos não necessitam de equivalente" — generalizado para "sem distância, sem PR", não só para alongamento nominalmente | y (pedido explícito do usuário) |

**Open questions:** nenhuma sem resposta — tudo acima está resolvido ou registrado como assumption.

---

## Cross-Cutting Dependencies (Out-of-Scope-but-dependent-elsewhere)

Esta feature introduz sets sem `Load`/`Repetitions` preenchidos. Três lugares hoje assumem `Volume = Load * Repetitions` como métrica universal de treino — nenhum é redesenhado aqui, mas todos vão precisar de uma emenda de follow-up quando (e apenas quando) o usuário efetivamente registrar sessões com exercícios `TimeBased`:

| Onde | O que assume hoje | Por que quebra/degrada com `TimeBased` | Ação recomendada (fora desta spec) |
|---|---|---|---|
| `ExecutedSetDocumentValueObject.cs:21` (`public decimal Volume => Load * Repetitions;`) | Todo set executado tem `Volume` calculável | Um set `TimeBased` terá `Load = null`/`Repetitions = null` — `Volume` deve ser tratado como `0` (não erro), mas passa a subestimar sistematicamente o esforço real de uma sessão mista | Follow-up em `Training`/`Workouts`: decidir se `Volume` vira `0` silenciosamente (mínimo seguro) ou se uma métrica paralela (ex.: `DurationVolume`) é somada em relatórios futuros |
| `AntiCheatClassifier.cs:125-165` (`Gamification`, regra "volume anormal", `GetSessionVolume`) | Compara `Volume` da sessão contra média móvel das últimas N sessões do usuário para detectar fraude | Uma sessão só de exercícios `TimeBased` (ex.: dia de corrida) terá `Volume = 0`, sendo classificada como anomalia de baixo volume (falso positivo) ou distorcendo a baseline de sessões futuras mistas | Follow-up em `.specs/features/gamification/spec.md` (regra "Volume anormal", já `Verified`): decidir uma regra de volume separada por tipo de sessão, ou excluir sessões 100% `TimeBased` da checagem de volume (usar só duração-anomalia, não construída aqui) |
| `FinishWorkoutExecutionHandler.cs:104-107` / `CompleteWorkoutSessionHandler.cs:51-54` (detecção de PR `"max_volume"`) | PR de volume máximo por exercício assume `Volume` como métrica de progresso | Exercício `TimeBased` nunca vai gerar um PR de `max_volume` (sempre 0) | **Resolvido nesta spec (não é mais follow-up)** — ver TBE-06: um tipo de PR novo (`"best_pace"`) é adicionado ao lado de `"max_volume"` para sets `TimeBased` com `DistanceMeters` preenchido. Sets `TimeBased` sem distância (alongamento etc.) continuam sem gerar PR nenhum, por decisão explícita do produto (ver Assumptions), não por lacuna |
| `GetTrainingDashboardHandler.cs` (`WeeklyVolume`/`WeeklyVolumeProgressPercent`, dashboard) | Card de volume semanal soma `Volume` de todos os sets da semana | Sessões `TimeBased` não contribuem pro card, subestimando a atividade real do usuário que treina corrida/endurance | Follow-up em `.specs/features/workout-schedule-dashboard/spec.md` ou dashboard geral: decidir se o card de volume ganha uma segunda métrica (tempo total) ou se é escopado explicitamente como "volume de carga", com um card de duração ao lado |

Nenhum desses 4 pontos é alterado por esta spec — `Volume = 0` para sets `TimeBased` é o comportamento correto e seguro que resulta naturalmente de `Load`/`Repetitions` nulos, sem exigir nenhuma mudança de código nestes arquivos. A ação recomendada é presentational/analítica, não uma correção obrigatória.

---

## User Stories

### P1: Cadastro de exercício com tipo (peso ou tempo) ⭐ MVP

**User Story**: Como profissional cadastrando um exercício no catálogo, quero marcar se ele é medido por peso+reps ou por duração(+distância), pra que os planos que o usam já saibam qual tipo de dado pedir na execução.

**Why P1**: É a base estrutural de tudo — sem o tipo no catálogo, nenhum set sabe qual gate aplicar.

**Acceptance Criteria**:

1. WHEN um exercício é cadastrado ou editado no catálogo THEN sistema SHALL permitir marcar `ExerciseType` como `WeightBased` (default) ou `TimeBased`
2. WHEN um exercício já existente no catálogo não tem `ExerciseType` definido (dado pré-migração) THEN sistema SHALL tratá-lo como `WeightBased` (comportamento atual preservado, sem exceção)
3. WHEN um exercício é salvo como `TimeBased` THEN sistema SHALL persistir esse valor e refletir no snapshot de qualquer plano/sessão que o referencie a partir desse momento (planos/sessões já congeladas antes da mudança não são retroativamente afetadas — mesmo princípio já usado por `RequireRpe`/`WEV-05`)

**Independent Test**: Cadastrar um exercício novo marcado `TimeBased`, salvar, recarregar o catálogo — tipo persistido corretamente; um exercício antigo sem o campo aparece como `WeightBased`.

---

### P1: Montagem de plano com exercício tempo-baseado (workout-editor) ⭐ MVP

**User Story**: Como profissional montando um plano, quero adicionar um exercício `TimeBased` a um bloco e configurar seus sets por duração (e opcionalmente distância), em vez de peso/reps.

**Why P1**: Sem isso, o tipo de exercício existe no catálogo mas não pode ser efetivamente usado num plano.

**Acceptance Criteria**:

1. WHEN o profissional adiciona ao bloco um exercício cujo `ExerciseType == TimeBased` THEN editor SHALL exibir, para cada set daquele exercício, campos de `DurationSeconds` (obrigatório) e `DistanceMeters` (opcional) no lugar dos campos de peso/reps
2. WHEN o profissional tenta salvar um set de um exercício `TimeBased` sem duração preenchida THEN sistema SHALL rejeitar o salvamento do plano com mensagem indicando o campo faltante (mesmo padrão de validação já usado para peso/reps em exercícios `WeightBased`)
3. WHEN o profissional seleciona `Technique` para um set de exercício `TimeBased` THEN editor SHALL restringir as opções a `Straight` (ver Assumptions — demais técnicas não se aplicam)
4. WHEN o profissional agrupa um exercício `TimeBased` dentro de um `Superset`/`Amrap`/`Emom` junto com exercícios `WeightBased` THEN sistema SHALL aceitar normalmente, sem restrição de homogeneidade de tipo dentro do bloco

**Independent Test**: Criar um plano com um bloco contendo um exercício `TimeBased` — tela de set mostra duração/distância, não peso/reps; salvar sem duração — sistema rejeita; preencher duração — salva normalmente.

---

### P1: Execução — gate de conclusão condicional ao tipo do exercício ⭐ MVP

**User Story**: Como aluno executando um treino com exercício de corrida/alongamento, quero marcar o set como concluído preenchendo a duração (e distância, se quiser), sem ser bloqueado por exigir peso e reps que não fazem sentido pra esse exercício.

**Why P1**: É o requisito central da feature — sem isso, `WEV-01`/`WEV-02` tornam literalmente impossível concluir um set de corrida hoje.

**Acceptance Criteria**:

1. **[Frontend]** WHEN o set pertence a um exercício `ExerciseType == WeightBased` THEN sistema SHALL aplicar exatamente o gate já especificado em `WEV-01` (peso `>= 0` E reps `>= 1`) — nenhuma mudança de comportamento
2. **[Frontend]** WHEN o set pertence a um exercício `ExerciseType == TimeBased` E `DurationSeconds` está vazio, zero, negativo ou não numérico THEN sistema SHALL recusar a conclusão do set (mesmo padrão de indicação visual do gate de peso/reps), independente do valor de distância
3. **[Frontend]** WHEN o set pertence a um exercício `TimeBased`, `DurationSeconds >= 1` E `DistanceMeters` está vazio/não preenchido THEN sistema SHALL permitir a conclusão normalmente (distância nunca bloqueia)
4. **[Backend]** WHEN `UpdateWorkoutExecutionStateCommand` chega com um set de exercício `TimeBased` cujo `DurationSeconds` é nulo, zero ou negativo THEN sistema SHALL rejeitar com 400 (reforço server-side, mesmo espírito de `WEV-02`)
5. **[Backend]** WHEN o mesmo comando chega com um set de exercício `WeightBased` THEN sistema SHALL continuar aplicando exatamente a validação já especificada em `WEV-02` (peso/reps), sem nenhuma alteração
6. WHEN um set `TimeBased` já concluído tem sua duração apagada pelo usuário (edição posterior) THEN sistema SHALL desmarcar automaticamente a conclusão desse set (mesmo comportamento já especificado em `WEV-01` AC6, aplicado à duração em vez de peso/reps)

**Independent Test**: Numa sessão com um exercício de corrida, tentar concluir um set sem duração — recusado; preencher duração sem distância — conclui normalmente; num exercício de peso na mesma sessão, tentar concluir sem peso/reps — recusado exatamente como hoje (comportamento `WEV-01` inalterado).

---

### P1: Execução — RPE aplicável a exercícios tempo-baseados ⭐ MVP

**User Story**: Como aluno correndo uma sessão intensa, quero poder registrar meu RPE daquele set, do mesmo jeito que já registro RPE numa série de peso.

**Why P1**: Confirma explicitamente que a regra de `RequireRpe`/faixa 1-10 (`workout-execution-validation`) não é afetada por esta feature — sem esta story, ficaria ambíguo se RPE segue existindo para `TimeBased`.

**Acceptance Criteria**:

1. WHEN um set de exercício `TimeBased` é exibido na tela de execução THEN interface SHALL mostrar o campo de RPE exatamente como já mostra hoje para `WeightBased` (mesmo componente, mesma faixa 1-10, mesma regra de opcional-por-padrão/obrigatório-quando-`RequireRpe`)
2. WHEN um exercício `TimeBased` está marcado `RequireRpe = true` THEN sistema SHALL bloquear a conclusão do set sem RPE preenchido, com o mesmo mecanismo já especificado em `WEV-07` para `WeightBased`

**Independent Test**: Marcar um exercício de corrida como `RequireRpe = true` no editor, iniciar sessão, tentar concluir um set com duração preenchida mas sem RPE — bloqueado; preencher RPE — conclui.

---

### P1: Frontend — tela de execução exibe duração/distância no lugar de peso/reps ⭐ MVP

**User Story**: Como aluno, quero ver campos de duração e distância (não peso/reps) quando o exercício da minha sessão é de corrida/endurance/alongamento.

**Why P1**: É a contraparte visual do gate — sem a troca de UI, o aluno veria campos de peso/reps sem sentido nenhum para o exercício.

**Acceptance Criteria**:

1. WHEN a tela de execução (`TrainingPlansClient.jsx`) renderiza um set cujo exercício é `TimeBased` THEN interface SHALL substituir os inputs de peso (`su-exec-input`, linha ~1138) e reps (linha ~1148) por um input de duração (mm:ss, convertido internamente para `DurationSeconds`) e um input opcional de distância (metros/km)
2. WHEN o mesmo set é `WeightBased` THEN interface SHALL continuar exibindo peso/reps exatamente como hoje, sem nenhuma mudança visual
3. WHEN o histórico/resumo pós-treino (linha ~1274, tabela de resumo) exibe um set `TimeBased` THEN interface SHALL mostrar duração(+distância) no lugar de "reps/peso", mesma lógica condicional
4. WHEN o campo de RPE é renderizado (linha ~1158) THEN interface SHALL exibi-lo igualmente para ambos os tipos de exercício (ver story anterior — RPE não é condicional ao tipo)

**Independent Test**: Abrir uma sessão com um exercício de corrida — linha do set mostra duração/distância, não peso/reps; um exercício de peso na mesma sessão continua mostrando peso/reps normalmente.

---

### P1: PR de pace para exercícios tempo-baseados com distância ⭐ MVP

**User Story**: Como aluno correndo, quero que meu melhor ritmo (pace) fique registrado como recorde pessoal, do mesmo jeito que já acontece com o volume máximo de peso.

**Why P1**: Sem isso, exercícios `TimeBased` com distância (corrida) nunca geram nenhum tipo de recorde pessoal — regressão percebida em relação ao que exercícios `WeightBased` já têm hoje.

**Acceptance Criteria**:

1. WHEN um set de exercício `TimeBased` é concluído com `DurationSeconds >= 1` E `DistanceMeters > 0` THEN sistema SHALL calcular o pace do set (`DurationSeconds / DistanceMeters`) e comparar contra o melhor pace já registrado pelo usuário para aquele exercício
2. WHEN o pace calculado é menor (melhor) que o recorde anterior daquele exercício (ou não existe recorde anterior) THEN sistema SHALL registrar um PR do tipo `"best_pace"`, mesmo mecanismo de persistência/notificação já usado para `"max_volume"`
3. WHEN um set de exercício `TimeBased` é concluído SEM `DistanceMeters` preenchido (ex.: alongamento, prancha cronometrada) THEN sistema SHALL NÃO calcular nem tentar registrar PR nenhum para esse set — não é um erro, é ausência esperada de PR para esse tipo de exercício
4. WHEN um set de exercício `WeightBased` é concluído THEN sistema SHALL continuar avaliando apenas PR de `"max_volume"` exatamente como hoje — a lógica de `"best_pace"` nunca roda para sets `WeightBased`

**Independent Test**: Concluir um set de corrida com duração+distância melhor que o recorde anterior — PR `"best_pace"` registrado; concluir um set de alongamento (sem distância) — nenhum PR tentado; concluir um set de agachamento — continua gerando só PR de volume, como hoje.

---

## Edge Cases

- WHEN um exercício é reclassificado de `WeightBased` para `TimeBased` (ou vice-versa) no catálogo DEPOIS de já usado em planos existentes THEN sistema SHALL refletir o novo tipo em qualquer sessão futura iniciada a partir desses planos (snapshot no start, mesmo princípio de `RequireRpe`), mas SHALL preservar sessões já concluídas no passado com os dados que foram de fato logados na época (nunca reinterpretar dado histórico)
- WHEN um set `TimeBased` é do tipo extra (`set.isExtra`, adicionado manualmente durante a execução) THEN o gate de duração se aplica igual a qualquer outro set — nenhuma exceção por ser "extra" (mesmo princípio já estabelecido em `WEV` para sets extras)
- WHEN o usuário digita uma duração com formato inválido (ex.: texto não numérico no campo mm:ss) THEN sistema SHALL recusar a conversão para `DurationSeconds` e tratar como campo vazio para fins do gate de conclusão (nunca aceitar um valor ambíguo)
- WHEN `DistanceMeters` é preenchido com valor negativo THEN sistema SHALL rejeitar o valor (mesma lógica de "não numérico/negativo" já usada para peso em `WEV-01` AC4), mesmo sendo um campo opcional — opcional significa "pode ficar vazio", nunca "aceita qualquer valor quando preenchido"
- WHEN um exercício `TimeBased` está dentro de um bloco `Amrap`/`Emom` (já time-boxed por natureza do bloco) THEN a duração logada no set é do ESFORÇO individual daquele set dentro da janela do bloco, não a duração do bloco inteiro — sistema SHALL NÃO tentar derivar ou validar consistência entre a duração do set e o timer do bloco (são conceitos independentes, nenhuma reconciliação automática nesta feature)

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
|---|---|---|---|
| TBE-01 | P1: Cadastro de exercício com `ExerciseType` | Design | Pending |
| TBE-02 | P1: Montagem de plano com exercício tempo-baseado (workout-editor) | Design | Pending |
| TBE-03 | P1: Execução — gate de conclusão condicional ao tipo (client-side + server-side) | Design | Pending |
| TBE-04 | P1: Execução — RPE aplicável a exercícios tempo-baseados | Design | Pending |
| TBE-05 | P1: Frontend — tela de execução exibe duração/distância condicionalmente | Design | Pending |
| TBE-06 | P1: PR de pace para exercícios tempo-baseados com distância | Design | Pending |

**ID format:** `TBE-NN`

**Status values:** Pending → In Design → In Tasks → Implementing → Verified

**Coverage:** 6 total, 0 mapped to tasks, 6 unmapped ⚠️ (Tasks phase ainda não rodou)

---

## Success Criteria

- [ ] Um exercício cadastrado como `TimeBased` pode ser adicionado a um plano com sets de duração(+distância), sem exigir peso/reps em nenhum ponto do fluxo
- [ ] Um exercício `WeightBased` continua se comportando EXATAMENTE como hoje — `WEV-01`/`WEV-02` intactos, nenhuma regressão
- [ ] Um set `TimeBased` só é concluído com duração válida preenchida (client-side bloqueia; backend rejeita se contornado); distância nunca bloqueia
- [ ] RPE funciona identicamente para os dois tipos de exercício, sem exceção
- [ ] A tela de execução mostra o input certo (peso/reps OU duração/distância) conforme o tipo do exercício, sem nenhum campo sem sentido exibido
- [ ] Exercícios `TimeBased` com distância geram PR de pace (`"best_pace"`); exercícios `TimeBased` sem distância (alongamento) não geram PR nenhum, por decisão de produto — não por lacuna
- [ ] As 3 dependências cross-cutting restantes (`Volume`, anti-cheat, dashboard de volume semanal) estão documentadas como follow-up explícito, não escondidas nem esquecidas — a de PR (`max_volume`/`best_pace`) já está resolvida nesta spec
