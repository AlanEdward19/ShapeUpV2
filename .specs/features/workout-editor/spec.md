# Editor de Treino — Specification

## Problem Statement

O editor de treino atual (`PlanEditor`, `ShapeUp-Web/src/pages/Dashboard/ClientDetail.jsx:136`) já cobre sets/reps/carga/RPE/descanso/dropset/rest-pause ponta a ponta (DB → API → hook → UI). Ele modela cada set como pertencente a exatamente um exercício, em sequência linear. Isso não representa técnicas onde **múltiplos exercícios ou rounds se agrupam numa mesma unidade estrutural** — superset (2+ exercícios sem descanso entre eles), AMRAP (bloco com tempo-limite) e EMOM (bloco com intervalo fixo e rounds). RIR (Reps in Reserve) também não existe — só RPE. Sem isso, profissionais não conseguem prescrever treinos com essas técnicas, que são padrão em programação de força/hipertrofia.

## Goals

- [ ] Profissional consegue criar, editar e salvar um bloco Superset (2+ exercícios) num plano/template
- [ ] Profissional consegue criar, editar e salvar um bloco AMRAP (tempo-limite) num plano/template
- [ ] Profissional consegue criar, editar e salvar um bloco EMOM (intervalo + rounds) num plano/template
- [ ] Cada set expõe intensidade como RPE **ou** RIR (exclusivo), nunca os dois

## Out of Scope

Explicitamente excluído desta spec. Documentado pra prevenir scope creep.

| Feature | Reason |
|---|---|
| Execução do treino (timer de AMRAP/EMOM rodando, contagem de rounds em tempo real) | Fase 2 trata como feature separada — "Execução de treino". Esta spec cobre só o **editor** (o que fica salvo no plano), não o runtime de quem está treinando |
| Edição colaborativa em tempo real (2 profissionais no mesmo plano simultaneamente) | Não solicitado; comportamento atual (last-write-wins) mantido |
| Presets/templates rápidos de superset/AMRAP/EMOM pré-configurados | Nice-to-have, não bloqueia o core da feature — vira P3 |
| Limites numéricos novos (máx. exercícios por bloco, máx. blocos por plano) | Nenhum limite existe hoje no modelo atual; não introduzir sem necessidade real observada |
| Novas regras de autorização para editar planos | Reusa o modelo de autorização da Fase 1 (Identity+Credentials+Relationships+Membership+Entitlements) sem alteração |

---

## Assumptions & Open Questions

Toda ambiguidade foi resolvida ou registrada aqui.

| Assumption / decision | Chosen default | Rationale | Confirmed? |
|---|---|---|---|
| Modelo de agrupamento | Novo objeto `Block` (não um `groupId` solto) contendo `Type` (Straight\|Superset\|Amrap\|Emom) + lista ordenada de `BlockExercise`, cada um com sua lista de `Set` | Usuário pediu "melhor abordagem mesmo que afete a estrutura atual". `Block` explícito é o único jeito de carregar `TimeCapSeconds`/`IntervalSeconds`/`TotalRounds` sem overloading o set existente, e generaliza pra técnicas futuras sem novo redesenho | y (delegado ao agente) |
| RPE/RIR exclusivo | `Set.Intensity: { Type: Rpe\|Rir, Value: int }` substitui o campo `Rpe: int` atual | Usuário escolheu explicitamente "exclusivo" | y |
| Dado existente / compatibilidade | Nenhuma migração necessária — sem produção, sem dado a preservar. `PlannedExerciseDocumentValueObject`/`WorkoutExerciseDto` e o campo flat `Rpe` são **substituídos diretamente** pelo novo schema `Block`/`Intensity`, sem camada de compat | Confirmado pelo usuário: "não temos nada em produção, nem precisamos manter compatibilidade" | y |
| AMRAP: reps fixo ou só esforço máximo | `BlockExercise.Sets[0].Reps` opcional; null = esforço máximo (AMRAP puro), preenchido = "AMRAP com meta" (variante usada por alguns coaches) | Nenhuma das duas é universalmente "a" definição de AMRAP; opcional cobre ambas sem forçar escolha | n — assumption, log apenas |
| EMOM: 1 ou N exercícios por round | `Block.Exercises` (lista ordenada) define a rotação por round — 1 item = EMOM de exercício único, 2+ = rotação entre exercícios a cada minuto | Generaliza sem precisar de dois modelos diferentes pra EMOM simples vs. composto | n — assumption, log apenas |
| Descanso dentro de blocos agrupados | `Set.RestSeconds` só é significativo em blocos `Straight` (descanso entre séries do mesmo exercício). Em `Superset`, descanso entre exercícios do bloco é sempre 0 (é a definição de superset) — só existe `Block.RestAfterSeconds` (descanso após o bloco inteiro). Em `Amrap`/`Emom`, descanso é governado pelo `TimeCapSeconds`/`IntervalSeconds`, `Set.RestSeconds` não se aplica (deve ser rejeitado na validação) | Consistente com a definição de cada técnica; evita campo ambíguo/contraditório salvo no bloco errado | n — assumption, log apenas |
| Technique (dropset/rest-pause/cluster/muscle-round) dentro de blocos agrupados | Continua permitido em qualquer set, de qualquer tipo de bloco (é ortogonal ao agrupamento) | Nada nas técnicas existentes conflita com superset/AMRAP/EMOM; forçar exclusão seria restrição não pedida | y (default seguro) |
| Comportamento offline | Blocos passam pelo mesmo `enqueueMutation` que já cobre create/update de plano hoje, sem caminho especial | Fase 1 já fechou "todo write passa pela fila ou tem razão documentada" — payload maior não muda o mecanismo | y (reuso do padrão existente) |
| i18n dos novos rótulos (Superset/AMRAP/EMOM/RIR) | Chaves novas em `LanguageContext` (EN/PT-BR/ES), mesma paridade 100% já exigida pelas chaves existentes | Definition of Done da Fase 2 herda o padrão i18n fechado na Fase 1 | y |

**Open questions:** nenhuma sem resposta — tudo acima está resolvido ou registrado como assumption.

---

## User Stories

### P1: Criar bloco Superset ⭐ MVP

**User Story**: Como profissional montando um plano, quero agrupar 2+ exercícios num superset (sem descanso entre eles) pra prescrever a técnica corretamente.

**Why P1**: É a técnica de agrupamento mais comum; sem ela a feature não resolve o problema central.

**Acceptance Criteria**:

1. WHEN profissional adiciona um bloco do tipo Superset com 2+ exercícios e salva o plano THEN sistema SHALL persistir um `Block{Type=Superset}` com a lista de `BlockExercise` na ordem escolhida
2. WHEN profissional tenta salvar um bloco Superset com menos de 2 exercícios THEN sistema SHALL rejeitar com mensagem explícita ("Superset precisa de pelo menos 2 exercícios") e não salvar
3. WHEN profissional reabre um plano com um bloco Superset já salvo THEN sistema SHALL exibir os exercícios agrupados visualmente distintos de um bloco Straight, na mesma ordem salva
4. WHEN profissional define `Set.RestSeconds` num set dentro de um bloco Superset (exceto o último set do bloco) THEN sistema SHALL rejeitar (rest entre exercícios do mesmo superset é sempre 0 por definição)

**Independent Test**: Criar plano com 1 bloco Superset de 2 exercícios, salvar, recarregar página, confirmar que os 2 exercícios aparecem agrupados na mesma ordem.

---

### P1: Criar bloco AMRAP ⭐ MVP

**User Story**: Como profissional montando um plano, quero definir um bloco com tempo-limite (AMRAP) pra prescrever a técnica de máximo esforço em janela fixa.

**Why P1**: Técnica padrão em condicionamento/hipertrofia; parte do gap identificado.

**Acceptance Criteria**:

1. WHEN profissional cria um bloco AMRAP com `TimeCapSeconds` > 0 e 1+ exercícios THEN sistema SHALL persistir `Block{Type=Amrap, TimeCapSeconds}` com os `BlockExercise` associados
2. WHEN profissional salva um bloco AMRAP sem `TimeCapSeconds` ou com valor ≤ 0 THEN sistema SHALL rejeitar com mensagem explícita
3. WHEN profissional deixa `Reps` de um set em branco dentro de um bloco AMRAP THEN sistema SHALL aceitar (representa esforço máximo, sem meta fixa)
4. WHEN profissional define `Set.RestSeconds` num set dentro de um bloco AMRAP THEN sistema SHALL rejeitar (descanso é governado pelo tempo-limite do bloco, não por set)

**Independent Test**: Criar plano com 1 bloco AMRAP de tempo-limite 600s e 2 exercícios sem reps fixos, salvar, recarregar, confirmar tempo-limite e exercícios persistidos.

---

### P1: Criar bloco EMOM ⭐ MVP

**User Story**: Como profissional montando um plano, quero definir um bloco de intervalo fixo com rounds (EMOM) pra prescrever a técnica de repetição a cada minuto.

**Why P1**: Técnica padrão em condicionamento; parte do gap identificado.

**Acceptance Criteria**:

1. WHEN profissional cria um bloco EMOM com `IntervalSeconds` > 0, `TotalRounds` > 0 e 1+ exercícios THEN sistema SHALL persistir `Block{Type=Emom, IntervalSeconds, TotalRounds}` com os `BlockExercise` na ordem de rotação
2. WHEN profissional salva um bloco EMOM sem `IntervalSeconds`/`TotalRounds` ou com algum valor ≤ 0 THEN sistema SHALL rejeitar com mensagem explícita
3. WHEN bloco EMOM tem 2+ exercícios THEN sistema SHALL tratar a ordem da lista como ordem de rotação por round (exibida como tal na UI)
4. WHEN profissional define `Set.RestSeconds` num set dentro de um bloco EMOM THEN sistema SHALL rejeitar (descanso é governado pelo intervalo)

**Independent Test**: Criar plano com 1 bloco EMOM de intervalo 60s, 10 rounds, 2 exercícios em rotação, salvar, recarregar, confirmar intervalo/rounds/ordem de rotação persistidos.

---

### P1: Intensidade exclusiva RPE ou RIR ⭐ MVP

**User Story**: Como profissional, quero registrar a intensidade de cada set como RPE **ou** RIR (nunca os dois), pra evitar dado duplicado/ambíguo.

**Why P1**: Requisito explícito do usuário; afeta todos os sets, de todos os tipos de bloco.

**Acceptance Criteria**:

1. WHEN profissional escolhe RPE e informa um valor pra um set THEN sistema SHALL persistir `Set.Intensity = {Type: Rpe, Value}` e SHALL não expor campo de RIR simultaneamente pro mesmo set
2. WHEN profissional escolhe RIR e informa um valor pra um set THEN sistema SHALL persistir `Set.Intensity = {Type: Rir, Value}` e SHALL não expor campo de RPE simultaneamente pro mesmo set
3. WHEN profissional troca o tipo de intensidade de um set já preenchido (RPE→RIR ou vice-versa) THEN sistema SHALL limpar o valor anterior (não converter automaticamente — escalas não são diretamente conversíveis 1:1)
4. WHEN nenhuma intensidade é informada THEN sistema SHALL aceitar `Intensity = null` (campo opcional, como o RPE é hoje)

**Independent Test**: Criar set com RPE=8, trocar pra RIR, confirmar que RPE some da UI e do payload salvo; salvar com RIR=2, recarregar, confirmar persistência.

---

### P1: Migração de planos/templates existentes ⭐ MVP

**User Story**: Como profissional com planos já criados, quero que eles continuem abrindo e editáveis sem perda de dado depois que o modelo de Block existir.

**Why P1**: Sem isso, "rever tudo" quebra todo o histórico já em produção — inaceitável pela Definition of Done (sem regressão).

**Acceptance Criteria**:

1. WHEN o backfill roda sobre um `WorkoutPlanDocument`/`WorkoutTemplateDocument` existente (schema pré-Block) THEN sistema SHALL converter cada `PlannedExercise` em um `Block{Type=Straight}` com exatamente 1 `BlockExercise`, preservando a ordem original
2. WHEN o backfill processa um set com `Rpe > 0` THEN sistema SHALL gravar `Intensity = {Type: Rpe, Value: Rpe}`; WHEN `Rpe == 0` ou ausente THEN sistema SHALL gravar `Intensity = null`
3. WHEN um plano já migrado é aberto no editor THEN sistema SHALL exibir os blocos Straight de forma indistinguível (visualmente) da lista linear de exercícios anterior
4. WHEN o backfill encontra um documento já no novo schema (rodado 2x) THEN sistema SHALL ser idempotente (não duplicar/corromper blocos já migrados)

**Independent Test**: Rodar backfill contra um snapshot de dados pré-Block, abrir um plano migrado no editor, confirmar que todos os exercícios/sets/RPEs aparecem intactos como blocos Straight.

---

### ~~P1: Migração de planos/templates existentes~~ — removida

Sem produção, sem dado a preservar. Substituição direta de schema, sem backfill/compat layer. Ver assumption "Dado existente / compatibilidade" acima.

---

### P2: Reordenar blocos no editor

**User Story**: Como profissional, quero reordenar blocos (Straight/Superset/AMRAP/EMOM) dentro do plano por drag-and-drop.

**Why P2**: Importante pra usabilidade, mas o plano funciona (na ordem de criação) sem isso.

**Acceptance Criteria**:

1. WHEN profissional arrasta um bloco pra nova posição THEN sistema SHALL persistir a nova ordem ao salvar
2. WHEN a lista tem 1 único bloco THEN sistema SHALL desabilitar a interação de reordenar (nada pra reordenar)

**Independent Test**: Criar plano com 3 blocos, reordenar via drag-and-drop, salvar, recarregar, confirmar nova ordem.

---

### P2: Duplicar bloco

**User Story**: Como profissional, quero duplicar um bloco já configurado (ex.: mesmo superset com carga diferente) sem recriar do zero.

**Why P2**: Acelera o fluxo de edição; não bloqueia o MVP.

**Acceptance Criteria**:

1. WHEN profissional duplica um bloco THEN sistema SHALL criar uma cópia idêntica logo após o original, com novo id interno

**Independent Test**: Duplicar um bloco Superset de 2 exercícios, confirmar cópia idêntica na posição seguinte.

---

### P3: Presets rápidos de Superset/AMRAP/EMOM

**User Story**: Como profissional, quero inserir um bloco pré-configurado comum (ex.: "AMRAP 20min") com um clique.

**Why P3**: Conveniência, não essencial pro MVP.

**Acceptance Criteria**:

1. WHEN profissional escolhe um preset THEN sistema SHALL inserir um bloco pré-preenchido editável

---

## Edge Cases

- WHEN um bloco Superset/AMRAP/EMOM referencia um `ExerciseId` que foi excluído do catálogo depois de salvo THEN sistema SHALL exibir o exercício com indicação de "indisponível" (mesmo comportamento que blocos Straight já têm hoje, se houver — reusar, não inventar)
- WHEN `TimeCapSeconds` (AMRAP) ou `IntervalSeconds`/`TotalRounds` (EMOM) recebem valor não-numérico ou negativo THEN sistema SHALL rejeitar na validação (client e server)
- WHEN profissional troca o `Type` de um bloco já preenchido (ex.: Straight→Superset) com dados incompatíveis (só 1 exercício virando Superset) THEN sistema SHALL bloquear a troca até os dados mínimos do novo tipo serem satisfeitos

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
|---|---|---|---|
| WOED-01 | P1: Superset | Design | Pending |
| WOED-02 | P1: Superset (validação min. 2 exercícios) | Design | Pending |
| WOED-03 | P1: Superset (rest=0 entre exercícios) | Design | Pending |
| WOED-04 | P1: AMRAP | Design | Pending |
| WOED-05 | P1: AMRAP (validação TimeCap) | Design | Pending |
| WOED-06 | P1: EMOM | Design | Pending |
| WOED-07 | P1: EMOM (validação Interval/Rounds) | Design | Pending |
| WOED-08 | P1: Intensidade RPE/RIR exclusiva | Design | Pending |
| WOED-10 | P2: Reordenar blocos | - | Pending |
| WOED-11 | P2: Duplicar bloco | - | Pending |
| WOED-12 | P3: Presets rápidos | - | Pending |

**ID format:** `WOED-NN` (WOED-09 removida — migração fora de escopo, sem produção)

**Status values:** Pending → In Design → In Tasks → Implementing → Verified

**Coverage:** 11 total, 0 mapped to tasks, 11 unmapped ⚠️ (aguardando fase Design)

---

## Success Criteria

- [ ] Profissional cria um plano contendo Superset + AMRAP + EMOM + blocos Straight misturados, salva e reabre sem perda de dado
- [ ] Zero set salvo com RPE e RIR simultâneos (violação de invariante = bug)
- [ ] Paridade de chaves i18n (EN/PT-BR/ES) 100% pros novos rótulos, igual ao padrão já fechado na Fase 1
</content>
