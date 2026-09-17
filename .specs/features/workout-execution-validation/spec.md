# Validação de Execução de Treino — Specification

> **Cross-repo**: esta spec é canônica no repo da API (`ShapeUpApi`) porque toca o modelo de domínio do `workout-editor` (Fase 2, já implementado — `BlockExerciseDocumentValueObject`, `IntensityType`, `WorkoutSetValueObject`), mas boa parte dos ACs é só frontend (`ShapeUp-Web`). Cada AC abaixo é marcado explicitamente **[Backend]**, **[Frontend]** ou **[Backend+Frontend]**.

## Problem Statement

A tela de execução de treino (`ShapeUp-Web/src/pages/Dashboard/TrainingPlansClient.jsx`, função `toggleSetComplete`, linha 439) permite marcar um set como concluído (`set.completed = true`) sem checar se peso e reps foram preenchidos — o usuário pode "cravar" séries vazias. Isso é um problema de integridade de dado real, não cosmético: `ShapeScore`/gamificação (Fase 3) e o histórico de treino (`WorkoutFinished`, PRs) confiam no que foi logado como concluído. Ao mesmo tempo, o validador atual do backend (`UpdateWorkoutExecutionStateCommandValidator.cs`) já é mais rígido do que o produto pretende: `RuleFor(x => x.Intensity).NotNull()` torna RPE **obrigatório para todo set, sempre** — o oposto do comportamento desejado (RPE opcional por padrão, exigível apenas quando o profissional marcar explicitamente um exercício como "RPE obrigatório" no `workout-editor`). Esse campo de exigência por exercício (`RequireRpe`) não existe hoje em `BlockExerciseDocumentValueObject`. Por fim, dois rótulos da tela de execução ignoram o `LanguageContext`/`t()` já usado em 865/865 chaves (Fase 1): o card do plano exibe `{plan.phase}`/`{plan.difficulty}` cru (`TrainingPlansClient.jsx:756,758` — vem de `plan.phase = 'Hypertrophy'`/`'Strength'`/etc., setado em `trainingNormalization.js`/`TrainingPlansIndependent.jsx`/`TrainingPlansProfessional.jsx`) e o timer de descanso mostra a string fixa `"Rest"` (`TrainingPlansClient.jsx:866`, `<span className="su-timer-kicker">Rest</span>`), nunca traduzida, mesmo com o usuário em pt-BR/es.

## Goals

- [ ] Um set só pode ser marcado como concluído na tela de execução quando peso e reps têm valores válidos preenchidos (gate client-side, reforçado server-side)
- [ ] Profissional pode marcar, por exercício, que RPE é obrigatório para concluir os sets daquele exercício na execução (`RequireRpe`, default `false`)
- [ ] Profissional tem uma ação única para aplicar "RPE obrigatório" a todos os exercícios do treino de uma vez (bulk toggle), sem precisar repetir exercício a exercício
- [ ] Quando um exercício está marcado como RPE obrigatório, a execução bloqueia a conclusão dos sets daquele exercício sem RPE preenchido (mesmo padrão de gate do item anterior)
- [ ] RPE aceita só valores inteiros de 1 a 10, validado tanto na UI quanto no backend (hoje o backend torna RPE sempre obrigatório e sem validar faixa corretamente em todos os pontos — vira parte do fix)
- [ ] Rótulo do timer de descanso ("Rest") e o rótulo de fase/tipo de treino (ex.: "Hypertrophy") exibidos na tela de execução/listagem do cliente passam a usar `t()`/`LanguageContext`, corretos nas 3 línguas (pt-BR/en/es)

## Out of Scope

| Item | Motivo |
|---|---|
| Mudança na escala de RPE em si (ex.: adicionar RIR, CR-10, decimais) | Fora do pedido — RPE continua inteiro 1-10 (Borg-derived), igual ao `workout-editor` já fechou para o par RPE/RIR na autoria. Esta spec não reabre esse par |
| Reconciliar a dualidade RPE/RIR da autoria (`Set.Intensity.Type`) com a execução | A tela de execução hoje só loga RPE (coluna única `client.session.table.rpe`, sem seletor RIR) independente de o set ter sido autorado como RPE ou RIR no `workout-editor` — esse gap já existe hoje e não é criado nem resolvido por esta spec. `RequireRpe` passa a exigir o preenchimento desse campo de RPE de execução já existente, não uma reconciliação com `IntensityType.Rir` |
| Mudança em quais exercícios podem ter técnicas avançadas (superset/dropset/AMRAP/EMOM/etc.) | Não relacionado a este conjunto de bugs — `workout-editor` já resolveu isso em spec própria |
| Backfill/limpeza de sets já concluídos no passado com peso/reps vazios ou RPE fora de 1-10 | Validação vale só daqui pra frente (novas escritas); dado histórico já persistido não é uma migração desta spec (mesmo espírito do `workout-editor`: sem necessidade real observada de tocar dado passado) |
| Nova UI de gestão de capabilities/roles para quem pode marcar `RequireRpe` | Reusa a mesma autorização que já protege edição de plano no `workout-editor` (autor do plano/profissional), sem capability nova |
| Notificação/alerta ao aluno quando um exercício passa a exigir RPE num plano já em execução | Ver Edge Cases — sessão em andamento usa o snapshot flatten no início (`StartWorkoutExecutionHandler`); mudança de `RequireRpe` no plano não afeta sessão já iniciada |

## Assumptions & Open Questions

| Assumption / decision | Chosen default | Rationale | Confirmed? |
|---|---|---|---|
| Limiar mínimo pra "peso+reps válidos" | `Weight >= 0` (exercícios com peso corporal usam 0, valor explícito e válido) e `Reps >= 1` (inteiro). Ambos precisam estar preenchidos (não vazio/não nulo) pra permitir concluir o set | Usuário não especificou o mínimo exato; 0 pro peso é o padrão universal pra bodyweight (flexão, prancha) — bloquear peso=0 quebraria esses exercícios legitimamente | n — assumption, log apenas |
| Escala de RPE | Inteiro 1–10 (escala de Borg adaptada, já usada em `client.session.table.rpe` e em `FinishWorkoutExecutionCommandValidator.PerceivedExertion`) — sem casas decimais | Usuário não especificou; a única faixa já referenciada no código (`InclusiveBetween(1, 10)` em `FinishWorkoutExecutionCommandValidator`) e no `workout-editor` (RPE/RIR como `int`) é essa | y (decorre do padrão já existente no repo) |
| Onde vive o novo campo de exigência de RPE | `RequireRpe: bool` (default `false`) em `BlockExerciseDocumentValueObject` (o exercício dentro de um `Block` do plano/template), refletido no DTO de execução (`WorkoutExerciseDto`/`ExecutedExerciseDocumentValueObject`) quando a sessão é iniciada (mesmo mecanismo de flatten do `StartWorkoutExecutionHandler`) | É o nível "por exercício dentro do plano" pedido explicitamente pelo usuário — nome exato de campo/migração fica pra fase de Design, esta spec só fixa a intenção e o local conceitual | y (pedido explícito do usuário; naming/migração deferido ao Design) |
| Escopo do toggle em massa ("aplicar a todos") | Aplica `RequireRpe = true` a todos os exercícios do plano/template **atualmente aberto no editor**, de uma vez, como uma ação de conveniência (preenche o estado local, autor ainda precisa salvar o plano) — não é uma configuração global/persistente que afeta planos futuros ou outros planos | Pedido do usuário é "um botão que evita fazer exercício por exercício" nesse plano — não foi pedida uma preferência de conta/organização que sobreviva a outros planos | y (leitura direta do pedido: "salvando o autor de fazer exercício por exercício" implica escopo do plano em edição) |
| `RequireRpe` alterado enquanto uma sessão já está em andamento | Sessão já iniciada usa o snapshot congelado no início (`StartWorkoutExecutionHandler.flatten`, AD-007 do `workout-editor` já estabelece que execução é congelada em relação ao plano); mudar `RequireRpe` no plano depois não reabre/retroage numa sessão ativa — só vale pra próximas sessões iniciadas a partir daquele plano | Consistente com o comportamento já documentado de "execução é decidida no start, independente de edição posterior do plano", evita estado inconsistente numa sessão em andamento | n — assumption, log apenas |
| Bug pré-existente do validador backend (`UpdateWorkoutExecutionStateCommandValidator`) | Hoje `RuleFor(x => x.Intensity).NotNull()` torna RPE obrigatório em **todo** set enviado, sempre — contradiz o "RPE opcional por padrão" que já é a intenção documentada no `workout-editor` ("campo opcional, como RPE é hoje"). Esta spec corrige essa regra pra: `Intensity` **opcional por padrão**; **obrigatório apenas** quando o `ExerciseId` daquele set corresponde a um exercício com `RequireRpe = true` no plano/sessão associado. `Intensity.Value` continua validado em `1..10` quando presente, em qualquer um dos dois casos | Achado ao ler o validador atual — é um bug de comportamento já em produção (torna RPE sempre obrigatório), não só uma lacuna nova; a correção é parte do mesmo fix pedido (item RPE opcional/condicional) | y (achado concreto no código, correção decorre diretamente do requisito pedido) |
| Peso/reps: dado já existe no payload hoje? | `WorkoutSetValueObject.Repetitions` (nullable `int?`) e `.Load` (`decimal`, não nullable) já trafegam em `UpdateWorkoutExecutionStateCommand`; o validador atual já exige `Repetitions.NotNull()` e `Load >= 0` pra QUALQUER set enviado (não só os concluídos) — o gate desta spec é sobre a AÇÃO de marcar como concluído na UI (client-side, `set.completed`), que hoje não existe como conceito no payload salvo no backend. Design decide se `completed`/similar passa a ser um campo explícito no DTO de execução ou se o gate continua sendo puramente um controle de fluxo client-side antes de permitir o envio | O conceito de "set concluído" hoje só existe no estado React (`set.completed`), não no schema salvo — reconciliar isso (adicionar campo explícito vs. inferir) é decisão de schema/Design, não de produto | n — assumption, log apenas, revisitar em Design |
| Set com técnica/tipo especial (dropset, extra set, falha) | O gate de peso+reps (e RPE quando exigido) vale igual pra qualquer `SetType`/`Technique` — nenhuma exceção por tipo de set. Falha (`set.failure`) já trava RPE=10 no client hoje (`TrainingPlansClient.jsx:1164`, `disabled={set.completed || set.failure}`), comportamento existente que não muda | Nenhuma indicação do usuário de que sets extras/dropset/falha devem ter regra diferente — regra uniforme é o default mais simples e mais seguro pra integridade de dado | y (default seguro, nenhum pedido em contrário) |
| Set já concluído e depois editado pra valor inválido | WHEN o usuário apaga peso ou reps de um set já marcado concluído THEN sistema SHALL desmarcar automaticamente a conclusão daquele set (não pode existir set "concluído" com dado ausente) — ver Edge Cases | Consequência direta da regra "só conclui com dado válido": se o dado deixa de ser válido, o estado de conclusão que dependia dele deixa de valer | y (decorre logicamente da regra principal, sem alternativa razoável) |

**Open questions:** nenhuma sem resposta — tudo acima está resolvido ou registrado como assumption.

---

## User Stories

### P1: Bloqueio de conclusão de set sem peso e reps válidos (client-side) ⭐ MVP

**User Story**: Como aluno executando um treino, quero que um set só possa ser marcado como concluído depois que eu preencher peso e reps, pra que meu histórico e minha gamificação reflitam o que eu realmente fiz.

**Why P1**: É um bug de integridade de dado ativo hoje — permite "cravar" séries vazias, inflando `ShapeScore`/histórico sem esforço real. Afeta confiança em todo o produto.

**Acceptance Criteria**:

1. **[Frontend]** WHEN o usuário clica no botão de concluir um set (`su-check-circle`, `toggleSetComplete`) e o campo de peso (`set.log.weight`) OU o de reps (`set.log.reps`) está vazio/não numérico THEN sistema SHALL recusar a conclusão (não alterna `set.completed`), sem disparar o timer de descanso, e SHALL indicar visualmente qual campo falta (ex.: highlight/shake no input vazio)
2. **[Frontend]** WHEN peso preenchido é exatamente `0` (exercício com peso corporal) E reps é um inteiro `>= 1` THEN sistema SHALL permitir a conclusão normalmente (peso 0 é válido, não é "vazio")
3. **[Frontend]** WHEN reps preenchido é `0`, negativo, ou não numérico THEN sistema SHALL recusar a conclusão (reps mínimo válido é `1`)
4. **[Frontend]** WHEN peso preenchido é negativo ou não numérico THEN sistema SHALL recusar a conclusão
5. **[Frontend]** WHEN o usuário preenche peso e reps válidos e clica em concluir THEN sistema SHALL marcar o set como concluído normalmente (comportamento hoje já correto quando os dados existem — não regride)
6. **[Frontend]** WHEN um set já concluído tem seu peso ou reps apagado pelo usuário (edição posterior) THEN sistema SHALL desmarcar automaticamente a conclusão desse set (ver Assumptions)

**Independent Test**: Abrir uma sessão de execução, tentar concluir um set sem preencher peso/reps — botão de concluir não marca o set, indicação visual aparece; preencher peso=0 e reps=12 (exercício bodyweight) — conclui normalmente; apagar o reps de um set já concluído — set volta a "não concluído".

---

### P1: Reforço server-side do gate de conclusão (defense in depth) ⭐ MVP

**User Story**: Como sistema, quero que a mesma regra de "peso e reps válidos" seja garantida no backend, já que o comando de salvar execução (`UpdateWorkoutExecutionStateCommand`) já é aceito hoje sem revalidar isso — um cliente adulterado ou um bug futuro no front não pode contornar a regra.

**Why P1**: Client-side sozinho nunca é suficiente pra integridade de dado que alimenta gamificação/histórico — mesmo padrão de "defense in depth" já esperado no restante do backend (ex.: faixa de RPE já validada em `FinishWorkoutExecutionCommandValidator`).

**Acceptance Criteria**:

1. **[Backend]** WHEN `UpdateWorkoutExecutionStateCommand` chega com um set cujo `Repetitions` é nulo, `<= 0`, ou cujo `Load` é negativo THEN sistema SHALL rejeitar a requisição com 400 e mensagem indicando o set/campo inválido (reforça a regra já parcialmente presente no validador atual — `Repetitions.NotNull()`/`Load >= 0` já existem; este AC garante que continuam corretos após o fix de `Intensity` abaixo)
2. **[Backend]** WHEN o mesmo comando chega com `Load == 0` (peso corporal) e `Repetitions >= 1` THEN sistema SHALL aceitar normalmente (0 é válido, não é ausência de dado)
3. **[Backend]** WHEN a validação falha THEN sistema SHALL retornar 400 sem persistir nenhuma alteração da sessão (mesma sessão continua com o último estado válido salvo)

**Independent Test**: Enviar `UpdateWorkoutExecutionStateCommand` via teste de integração com um set `Repetitions=null` — API retorna 400; enviar com `Load=0, Repetitions=12` — API aceita e persiste.

---

### P1: Correção do rótulo "Rest" hardcoded no timer de descanso ⭐ MVP

**User Story**: Como aluno usando o app em pt-BR ou es, quero ver o rótulo do timer de descanso no meu idioma, não sempre em inglês.

**Why P1**: Bug de i18n visível em produto que já entrega paridade 100% de chaves nas 3 línguas (Fase 1) — quebra a experiência já prometida, não é um detalhe cosmético menor.

**Acceptance Criteria**:

1. **[Frontend]** WHEN a tela de execução exibe o timer de descanso ativo THEN sistema SHALL renderizar o rótulo via `t()` (nova chave, ex. `client.session.timer.rest_label`) em vez da string fixa `"Rest"` (`TrainingPlansClient.jsx:866`)
2. **[Frontend]** WHEN o idioma selecionado é pt-BR THEN rótulo SHALL exibir a tradução em português (ex. "Descanso"); WHEN é es THEN SHALL exibir em espanhol (ex. "Descanso"); WHEN é en THEN SHALL exibir em inglês ("Rest")
3. **[Frontend]** WHEN o usuário troca o idioma do app (via `LanguageContext`) durante uma sessão de execução ativa THEN rótulo do timer SHALL atualizar imediatamente pro novo idioma, sem precisar recarregar a página (mesmo comportamento reativo já existente em outras chaves do app)

**Independent Test**: Abrir sessão de execução, concluir um set com descanso configurado, trocar idioma pra pt-BR nas configurações — rótulo do timer muda de "Rest" pra "Descanso" sem reload.

---

### P1: Correção do rótulo de fase/tipo de treino hardcoded ⭐ MVP

**User Story**: Como aluno usando o app em pt-BR ou es, quero ver o tipo/fase do treino (ex. "Hipertrofia", "Força") no meu idioma na tela de listagem/execução, não sempre em inglês.

**Why P1**: Mesmo bug de i18n do item anterior, em outro ponto da tela — já existe a chave de tradução (`pro.builder.phase.hypertrophy` etc., usada hoje só na autoria em `ClientDetail.jsx`), só falta reusá-la na exibição pro aluno.

**Acceptance Criteria**:

1. **[Frontend]** WHEN a tela de listagem de planos do aluno exibe a tag de fase do treino (`su-tag`, `TrainingPlansClient.jsx:756`) THEN sistema SHALL renderizar via `t('pro.builder.phase.' + plan.phase.toLowerCase())` (reuso da MESMA chave já usada na autoria, `pro.builder.phase.hypertrophy`/`.strength`/`.endurance`/`.deload`) em vez do valor cru `{plan.phase}`
2. **[Frontend]** WHEN o valor de `plan.phase` não corresponde a nenhuma chave conhecida (dado legado/inesperado) THEN sistema SHALL cair de volta pro valor cru como hoje (nunca quebrar a tela nem mostrar a chave literal tipo `pro.builder.phase.undefined`)
3. **[Frontend]** WHEN a tela exibe a dificuldade do plano (`client.training.card.difficulty`, `TrainingPlansClient.jsx:758`, valor cru `{plan.difficulty}`) THEN sistema SHALL, da mesma forma, passar a traduzir os valores conhecidos (`Beginner`/`Intermediate`/`Advanced`) via novas chaves (`client.training.difficulty.beginner` etc.), com o mesmo fallback do item 2
4. **[Frontend]** WHEN o idioma é pt-BR/en/es THEN as 3 línguas SHALL ter paridade 100% pras chaves novas de fase e dificuldade (mesmo padrão de Definition of Done já fechado na Fase 1)

**Independent Test**: Com idioma pt-BR, abrir a listagem de planos do aluno — tag de fase mostra "Hipertrofia"/"Força"/etc. em vez de "Hypertrophy"/"Strength"; trocar pra es — mostra em espanhol.

---

### P1: Campo `RequireRpe` por exercício no editor de treino ⭐ MVP

**User Story**: Como profissional montando um plano no `workout-editor`, quero marcar que RPE é obrigatório pra um exercício específico, pra garantir que meu aluno registre a intensidade percebida naquele movimento em particular.

**Why P1**: É a capability nova explicitamente pedida pelo usuário — sem o campo no modelo de autoria, não existe o que a execução (próxima story) precisaria bloquear.

**Acceptance Criteria**:

1. **[Backend]** WHEN o modelo de exercício dentro de um bloco do plano (`BlockExerciseDocumentValueObject`) é estendido THEN sistema SHALL adicionar um campo booleano de exigência de RPE, default `false` pra todo exercício já existente e todo exercício novo (exata nomenclatura/migração fica pra fase de Design — ver Assumptions)
2. **[Backend]** WHEN um plano/template é salvo com esse campo marcado `true` pra um exercício THEN sistema SHALL persistir esse valor e refletir no snapshot criado quando uma sessão de execução é iniciada a partir desse plano (`StartWorkoutExecutionHandler`)
3. **[Frontend]** WHEN o profissional edita um exercício dentro de um bloco no `workout-editor` THEN interface SHALL exibir um toggle "RPE obrigatório" por exercício, refletindo e persistindo o estado atual
4. **[Frontend]** WHEN o profissional reabre um plano já salvo com exercícios marcados `RequireRpe=true` THEN interface SHALL exibir o toggle já ativado pra esses exercícios (estado persistido corretamente)

**Independent Test**: Marcar "RPE obrigatório" num exercício do plano, salvar, recarregar o editor — toggle continua marcado pra aquele exercício e desmarcado pros demais.

---

### P1: Toggle em massa — aplicar "RPE obrigatório" a todos os exercícios do plano ⭐ MVP

**User Story**: Como profissional, quero um único botão que marque "RPE obrigatório" em todos os exercícios do plano que estou editando, sem precisar repetir a ação exercício por exercício.

**Why P1**: Pedido explícito do usuário — parte do mesmo requisito de capability, não uma conveniência à parte; sem isso, planos com muitos exercícios tornam a marcação individual impraticável.

**Acceptance Criteria**:

1. **[Frontend]** WHEN o profissional aciona a ação "Exigir RPE em todos os exercícios" no editor do plano THEN interface SHALL marcar `RequireRpe = true` em TODOS os exercícios de TODOS os blocos do plano atualmente aberto (estado local, ainda não persistido)
2. **[Backend]** WHEN o plano é salvo após essa ação em massa THEN sistema SHALL persistir `RequireRpe = true` pra todos os exercícios afetados, no mesmo request/fluxo de salvar plano já existente (sem endpoint novo dedicado a bulk)
3. **[Frontend]** WHEN o profissional aciona a ação em massa e DEPOIS desmarca manualmente o toggle de um exercício específico THEN sistema SHALL respeitar a última ação do usuário pra aquele exercício (toggle individual sempre vence sobre a ação em massa anterior — não é um "modo travado")
4. **[Frontend]** WHEN o plano não tem nenhum exercício ainda (bloco vazio) THEN sistema SHALL desabilitar ou tornar a ação em massa um no-op (nada pra marcar)

**Independent Test**: Criar plano com 5 exercícios em 2 blocos, acionar "Exigir RPE em todos", salvar, recarregar — os 5 exercícios aparecem com o toggle marcado; desmarcar 1 manualmente e salvar de novo — só esse 1 volta a `false`, os outros 4 continuam `true`.

---

### P1: Execução bloqueia conclusão de set sem RPE quando o exercício exige ⭐ MVP

**User Story**: Como aluno executando um treino cujo exercício foi marcado pelo profissional como "RPE obrigatório", quero ser impedido de concluir um set daquele exercício sem informar o RPE.

**Why P1**: É o que torna a capability de autoria (stories anteriores) efetiva na prática — sem o bloqueio na execução, marcar "RPE obrigatório" no editor não teria efeito nenhum sobre o aluno.

**Acceptance Criteria**:

1. **[Frontend]** WHEN o exercício da sessão de execução tem `RequireRpe = true` (vindo do snapshot da sessão) E o usuário tenta concluir um set sem preencher `set.log.rpe` THEN sistema SHALL recusar a conclusão, com o mesmo padrão de indicação visual do gate de peso/reps (Story 1)
2. **[Frontend]** WHEN o exercício NÃO tem `RequireRpe` marcado (default) THEN sistema SHALL permitir concluir o set com ou sem RPE preenchido — comportamento atual mantido (RPE opcional é o padrão)
3. **[Backend]** WHEN `UpdateWorkoutExecutionStateCommand` chega com um set de um exercício `RequireRpe=true` e `Intensity == null` THEN sistema SHALL rejeitar com 400 (reforço server-side, corrige o bug do validador atual — ver Assumptions: `Intensity` deixa de ser sempre obrigatório e passa a ser condicional a `RequireRpe`)
4. **[Backend]** WHEN o set pertence a um exercício sem `RequireRpe` (default) THEN sistema SHALL aceitar `Intensity == null` normalmente (fix do bug atual, que hoje rejeita `Intensity` nulo sempre)

**Independent Test**: Marcar um exercício como "RPE obrigatório" no editor, iniciar sessão, tentar concluir um set daquele exercício sem RPE — bloqueado; preencher RPE — conclui; num exercício sem a marcação, concluir sem RPE — permitido normalmente (hoje isso já falharia no backend por causa do bug do `Intensity.NotNull()`).

---

### P2: Validação de faixa 1–10 do RPE

**User Story**: Como aluno, quero ser impedido de digitar um RPE fora da escala 1–10, pra que o dado registrado sempre faça sentido dentro do padrão usado no app.

**Why P2**: É um bug de validação de dado real (faixa hoje não é garantida em todos os pontos de entrada), mas não bloqueia o loop central de concluir um set — é menor em escopo que a capability nova de `RequireRpe` (Stories anteriores), por isso desmembrado como P2.

**Acceptance Criteria**:

1. **[Frontend]** WHEN o usuário digita um valor de RPE menor que `1` ou maior que `10` no campo de log (`set.log.rpe`) THEN interface SHALL rejeitar ou fazer clamp do valor pro limite mais próximo (1 ou 10), nunca aceitar o valor fora da faixa no estado
2. **[Frontend]** WHEN o usuário digita um valor não inteiro (ex. `8.5`) THEN interface SHALL arredondar ou rejeitar, mantendo o valor final sempre um inteiro entre 1 e 10
3. **[Backend]** WHEN `UpdateWorkoutExecutionStateCommand` chega com `Intensity.Value` fora de `1..10` THEN sistema SHALL rejeitar com 400 (a regra `InclusiveBetween(1, 10)` já existe no validador atual — este AC garante que ela continua sendo aplicada corretamente após o fix de "opcional por padrão" da Story anterior, e cobre também o caminho de `FinishWorkoutExecutionCommand` se sets forem validados lá no futuro)
4. **[Backend]** WHEN o marcador de falha (`set.failure`) trava o RPE em 10 automaticamente (comportamento já existente no client, `TrainingPlansClient.jsx:1164`) THEN sistema SHALL aceitar esse valor normalmente (10 está dentro da faixa válida, nenhuma mudança necessária aqui)

**Independent Test**: Tentar digitar RPE=15 no campo de log — UI corrige pra 10 (ou recusa o dígito); tentar salvar via API um payload com `Intensity.Value=0` — API retorna 400.

---

## Edge Cases

- WHEN um set é do tipo extra (`set.isExtra`, adicionado manualmente durante a execução) THEN o gate de peso+reps (e RPE quando exigido) se aplica igual a qualquer outro set — nenhuma exceção por ser "extra"
- WHEN o usuário marca `failure=true` num set (trava RPE=10) e o exercício exige RPE THEN sistema SHALL considerar RPE=10 como preenchido/válido pro gate de conclusão (não bloqueia por engano um set que já tem RPE definido automaticamente)
- WHEN uma sessão de execução já foi iniciada ANTES de um exercício ser marcado `RequireRpe=true` no plano THEN sistema SHALL usar o snapshot congelado no início da sessão (sem o requisito) — mudança no plano só vale pra sessões futuras iniciadas depois da edição (ver Assumptions)
- WHEN o toggle em massa é acionado num plano que já tem alguns exercícios com `RequireRpe=true` e outros `false` THEN sistema SHALL sobrescrever TODOS pra `true` (a ação é "marcar todos", não "alternar" ou "preservar estado misto")
- WHEN `plan.phase`/`plan.difficulty` vem de um plano legado com valor fora do enum esperado (dado antigo/inconsistente) THEN sistema SHALL exibir o valor cru como fallback, nunca quebrar a renderização nem mostrar a chave de tradução literal
- WHEN o usuário está no meio de um set (peso preenchido, reps vazio) e navega pra outro exercício sem concluir THEN sistema SHALL preservar o que já foi digitado (comportamento atual mantido) — o gate só impede a AÇÃO de concluir, nunca impede digitar/salvar rascunho

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
|---|---|---|---|
| WEV-01 | P1: Bloqueio de conclusão sem peso/reps (client-side) | Design | Pending |
| WEV-02 | P1: Reforço server-side do gate de conclusão | Design | Pending |
| WEV-03 | P1: Correção i18n — rótulo "Rest" do timer | Design | Pending |
| WEV-04 | P1: Correção i18n — fase/tipo e dificuldade do treino | Design | Pending |
| WEV-05 | P1: Campo `RequireRpe` por exercício (workout-editor) | Design | Pending |
| WEV-06 | P1: Toggle em massa "RPE obrigatório em todos" | Design | Pending |
| WEV-07 | P1: Execução bloqueia conclusão sem RPE quando exigido | Design | Pending |
| WEV-08 | P2: Validação de faixa 1–10 do RPE | Design | Pending |

**ID format:** `WEV-NN`

**Status values:** Pending → In Design → In Tasks → Implementing → Verified

**Coverage:** 8 total, 0 mapped to tasks, 8 unmapped ⚠️ (Tasks phase ainda não rodou)

---

## Success Criteria

- [ ] Nenhum set é salvo como concluído sem peso e reps válidos (client-side bloqueia; backend rejeita se contornado)
- [ ] Profissional marca `RequireRpe` por exercício e via bulk toggle no `workout-editor`, persistido corretamente
- [ ] Aluno é bloqueado de concluir um set de exercício `RequireRpe=true` sem RPE preenchido, tanto na UI quanto se tentar contornar via API
- [ ] Backend não torna mais RPE obrigatório por padrão pra todo set (bug corrigido) — só exige quando `RequireRpe=true`
- [ ] RPE fora da faixa 1–10 é rejeitado/corrigido tanto na UI quanto na API
- [ ] Rótulo do timer de descanso e o rótulo de fase/dificuldade do treino aparecem corretamente traduzidos nas 3 línguas (pt-BR/en/es), sem nenhuma string fixa em inglês remanescente nesses dois pontos
