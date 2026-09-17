# Variações/Equivalências de Exercício — Specification

## Problem Statement

Hoje o catálogo de exercícios (`Features/Training/Exercises`, SQL — `Exercise`/`ExerciseMuscleProfile`/`ExerciseEquipment`) não modela nenhuma relação entre exercícios. A tela de biblioteca (`ExercisesPublicMarkup.tsx`, drawer de detalhe) já reserva um espaço visual pra isso — "Substituições Mecânicas Equivalentes" — mas é hoje um texto estático fixo (`drawerSubs: 'Consulte a biblioteca para selecionar uma substituição.'`), nunca uma lista real. O caso de uso concreto do usuário (PM) é de execução: durante um treino, o aparelho que o plano pede está ocupado por outra pessoa na academia, e o aluno precisa trocar pra um exercício equivalente sem perder o fluxo da sessão nem os sets já logados no exercício original. Hoje não existe like nenhum jeito — nem de registrar quais exercícios são equivalentes entre si (dado, não texto solto), nem de agir sobre isso na tela de execução (`TrainingPlansClient.jsx`).

**Dependência cross-repo**: esta é a spec canônica de DADO/API (backend, `ShapeUpApi`) — o consumo visual do resultado no drawer da biblioteca é a spec irmã `ShapeUp-Web/.specs/features/exercise-detail-drawer` (que já define um contrato de apresentação `ExerciseEquivalent` à espera deste backend). Ordem de implementação: esta spec (backend, contrato de API/relação simétrica) primeiro; `exercise-detail-drawer` reconcilia sua UI contra o formato real que este backend expuser — nenhuma seção desta spec depende de `exercise-detail-drawer` fechar antes.

## Goals

- [ ] Um exercício pode ter um conjunto de outros exercícios marcados como equivalentes (dado real, uma relação, não anotação em texto livre)
- [ ] O profissional/autor marca essas equivalências a partir do editor de treino (`workout-editor`), no mesmo ponto onde já insere/edita o exercício num bloco do plano
- [ ] O drawer de detalhe da Biblioteca de Exercícios (`ExercisesPublicMarkup.tsx`) exibe a lista real de equivalentes de um exercício, cada um navegável pro próprio detalhe
- [ ] Na tela de execução de treino (`TrainingPlansClient.jsx`), o aluno troca um exercício por um equivalente registrado, no meio da sessão, sem perder os sets já logados no exercício original nem quebrar a fila de mutação offline

## Out of Scope

Explicitamente excluído desta spec. Documentado pra prevenir scope creep.

| Item | Motivo |
|---|---|
| Sugestão automática/algorítmica de equivalentes (IA, similaridade de macro-padrão motor, ranking por %) | Pedido explícito do usuário: autor sempre marca manualmente. O mockup de referência (`Biblioteca de exercicios/code.html`) mostra um "% de similaridade" por sugestão — esse número é gerado por um algoritmo que não existe e não é construído aqui (ver Assumptions, "Conteúdo exibido por equivalente") |
| Mudança em como exercícios são buscados/filtrados na listagem da Biblioteca (`ExercisesShell.tsx`/`Exercises.jsx`, grid/lista principal) | Só o drawer de detalhe e o botão de troca na execução estão no escopo — a busca/filtro da lista principal não muda |
| Concernências do `workout-execution-validation` (gate de peso/reps, `RequireRpe`, i18n de rótulos) | Spec própria, já fechada em paralelo — esta feature só ADICIONA o botão de troca rápida, não toca nenhuma regra de validação de conclusão de set |
| Recalcular quantidade/carga automaticamente ao trocar de exercício (ex.: ajustar carga pra "igualar" volume) | Fora do pedido — a troca leva o exercício novo com os campos prescritos que ele já tinha no plano (ou em branco, se inserido puro na sessão); nenhuma equivalência matemática de carga é calculada |
| Tela dedicada de administração/curadoria de exercícios (CRUD completo do catálogo) | Não existe hoje no frontend (confirmado no scan — `Exercises.jsx`/`ExercisesShell.tsx` são só leitura + `SuggestExerciseModal`; criar/editar exercício só existe via API com capability `platform.exercises.manage`, sem UI). Construir essa tela do zero é fora do pedido; esta feature reusa o ponto de entrada que JÁ vai existir no `workout-editor` (`ExerciseRow`, ver Assumptions) em vez de inventar uma tela nova só pra isso |
| Limite numérico de quantos equivalentes um exercício pode ter | Nenhum limite existe hoje em relações parecidas do catálogo (ex.: músculos/equipamentos por exercício); não introduzir sem necessidade observada |
| Fila/aprovação de equivalência sugerida por aluno (mesmo padrão de moderação que `nutrition` usa pra edição de alimento) | Não pedido — só quem já tem a capability `platform.exercises.manage` marca equivalências, mesma autorização que já protege Create/Update/Delete de exercício hoje |
| Executar a troca retroativamente em sessões já finalizadas/histórico | O botão de troca só existe numa sessão de execução EM ANDAMENTO; sessões já finalizadas são histórico imutável, fora de alcance desta feature |

---

## Assumptions & Open Questions

Toda ambiguidade foi resolvida ou registrada aqui — nenhuma decisão de produto ficou sem dono.

| Assumption / decision | Chosen default | Rationale | Confirmed? |
|---|---|---|---|
| Simetria da relação | **Simétrica**: se A marca B como equivalente, B passa a listar A automaticamente também — é uma única aresta não-direcionada por par de exercícios, não duas relações independentes | Casa com o modelo mental de "estes dois são intercambiáveis" (pedido do usuário) — nada no mockup de referência (`Biblioteca de exercicios/code.html`) sugere direcionalidade (a lista de "substituições" é sempre no sentido de "o que pode substituir ESTE exercício", nunca com uma distinção de "só nesse sentido"). Simétrica também evita o estado inconsistente de A listar B mas B não listar A | n — assumption, log apenas (decisão delegada ao agente, sem contradição encontrada no material de referência) |
| Onde a relação é persistida (schema) | Tabela de junção própria (self-referencing many-to-many) sobre `Exercise` (SQL, EF Core) — uma linha por PAR não-ordenado (`ExerciseId < EquivalentExerciseId` como invariante de armazenamento), nunca duas linhas pro mesmo par em ordens opostas | `Exercise` já é entidade SQL (`Features/Training/Shared/Entities/Exercise.cs`); tabela de junção é o padrão EF Core idiomático pra N:N, mesmo mecanismo que `ExerciseEquipment`/`ExerciseMuscleProfile` já usam pra outras relações do mesmo agregado. Canonicalizar a ordem do par evita duplicidade/2 fontes de verdade pro mesmo relacionamento simétrico | y (decorre diretamente da decisão de simetria acima — não há como ter simetria de verdade com 2 linhas independentes sem risco de divergência) |
| Elegibilidade de exercícios como equivalentes | **Sem restrição rígida** — qualquer exercício do catálogo pode ser marcado equivalente de qualquer outro (autor decide). UI mostra grupo muscular/equipamento de cada candidato pra ajudar o julgamento, e exibe um aviso (não bloqueia) quando o candidato não compartilha nenhum grupo muscular com o exercício original | Caso de uso real do usuário (aparelho ocupado) exige que um exercício de peso corporal e um de máquina possam ser equivalentes entre si mesmo sem overlap de equipamento — só grupo muscular tem alguma relevância biomecânica real, e mesmo assim vira aviso, não bloqueio (autor pode ter um motivo legítimo, ex. equivalência de "padrão de movimento" que a app não modela) | y (decorre diretamente do caso de uso descrito pelo usuário) |
| Ponto de entrada pra AUTORAR equivalências (onde o autor marca "isso é equivalente àquilo") | Reusa o botão "Substituir Exercício" (ícone `cached`, mockup `Criação de treinos/code.html:305-307`) já desenhado (mas ainda não implementado) no cabeçalho de `ExerciseRow.jsx` (`ShapeUp-Web/src/components/training/ExerciseRow.jsx`, componente do `workout-editor`, hoje sem esse botão) — clique abre um modal de picker (busca no catálogo + grupo muscular/equipamento visível) pra marcar/desmarcar equivalentes do exercício daquela linha, persistidos direto no catálogo (não no plano) | O usuário pediu "exercise CRUD/authoring flow" mas o scan confirmou que essa tela não existe como CRUD dedicado — o único lugar de fato onde o profissional já manipula "este exercício, nesta linha" é dentro do editor de plano (`workout-editor`, `BlockCard`→`ExerciseRow`), e o próprio mockup de referência do usuário desenha exatamente esse botão nesse exato lugar (não numa tela de administração separada). Reusar em vez de inventar uma tela nova de zero | n — assumption, log apenas, é a maior decisão de UX desta spec — revisitar se o usuário quiser uma tela de catálogo dedicada no futuro |
| Conteúdo exibido por equivalente (biblioteca + picker) | Nome, grupo(s) muscular(es) principal(is), equipamento — SEM "% de similaridade" (esse número no mockup vem de um algoritmo de IA que está fora de escopo, ver Out of Scope). Campo opcional de texto livre "motivo" (ex. "mesma cadeia posterior") fica de fora do MVP — nenhuma story pede curadoria de motivo, só a lista em si | Mockup mostra `match: '96% similaridade motora'` — reproduzir esse número sem o algoritmo por trás seria inventar dado falso. Cobrir só nome/músculo/equipamento entrega o que a story pede (ajudar o aluno a reconhecer visualmente o substituto) sem simular uma métrica que não existe | y (decorre diretamente do Out of Scope de sugestão algorítmica) |
| Semântica de troca na execução — sets já logados de A | **Mantidos, atribuídos a A** (histórico imutável) — a troca só afeta os sets registrados DAQUELE PONTO EM DIANTE na sessão, que passam a ser atribuídos a B. Ambos (A e B) coexistem na MESMA sessão de execução, como duas entradas independentes na lista já existente `WorkoutSessionDocument.Exercises` (`List<ExecutedExerciseDocumentValueObject>`) — não é criado nenhum conceito novo de "sessão dividida" | Confirmado no código: `WorkoutSessionDocument.Exercises` já é uma lista FLAT de exercícios (não uma árvore com 1 exercício fixo por posição) — `StartWorkoutExecutionHandler.cs:52-72` já achata `plan.Blocks.SelectMany(b => b.Exercises)` pra essa mesma lista. Adicionar um exercício B a essa lista já-flat, mantendo A como está, é diretamente representável pelo agregado atual, sem migração de schema nem redesenho — só uma nova operação que acrescenta uma entrada | y (verificado contra o código real do agregado de execução antes de assumir — `ExecutedExerciseDocumentValueObject` não tem nenhum campo que amarre "1 posição = 1 exercício fixo") |
| Sets ainda não logados de A no momento da troca (prescritos, mas não feitos) | Descartados da sessão corrente pro exercício A (não viram sets "pendentes" de B automaticamente — B começa sua participação na sessão do zero, sem sets pré-preenchidos, igual a um exercício normal ao iniciar). Os sets JÁ CONCLUÍDOS de A permanecem como estão (ver linha acima); só os não-concluídos de A somem da tela ativa depois da troca | Não existe prescrição salva pra B dentro DESSE plano especificamente (B não estava no plano) — inventar sets "herdados" de A pra B seria fabricar prescrição que o profissional nunca definiu. Aluno pode adicionar sets extras a B manualmente (mecanismo `isExtra` já existente e reusado, ver Design) | n — assumption, log apenas |
| Mutação offline da troca | Segue o MESMO mecanismo já fechado (`enqueueMutation`/`mutationQueue.js`) — a troca é uma operação nova enfileirada (`endpoint: /api/training/workouts/{sessionId}/swap-exercise` ou equivalente, método a definir em Design), com `dedupeKey` por sessão+exercício-original pra evitar duplicar a troca em retry, no mesmo espírito do `dedupeKey: workout-state-${workoutSessionId}` já usado pelo sync de estado | Pedido explícito do usuário: reconciliar com a fila offline (Fase 1), não inventar sincronização própria — mesmo padrão já usado por toda escrita de execução (`state`, `cancel`) hoje | y (pedido explícito do usuário) |
| Exercício indisponível/excluído aparecendo como equivalente | Se um exercício listado como equivalente foi excluído do catálogo (soft-delete futuro, ou hoje hard-delete via `DeleteExerciseHandler`), sistema SHALL simplesmente não exibi-lo na lista de equivalentes (filtra na leitura) — não precisa de tratamento especial de "indisponível" como o `workout-editor` já tem pra exercício referenciado num plano, porque a lista de equivalentes é só leitura informativa, nunca uma referência que precisa resolver em tempo de execução | Nenhum efeito colateral de "quebrar" nada — diferente de um `BlockExercise.ExerciseId` que precisa existir pra reconstituir o plano, a lista de equivalentes é só um atalho de navegação; se sumiu do catálogo, some da lista, sem erro | y (default seguro, consistente com o comportamento de leitura já existente) |
| Ownership do modelo de dados (feature própria vs. dentro de `workout-editor`) | **Sub-feature nova dentro do vertical-slice já existente `Features/Training/Exercises`** (ex. `Features/Training/Exercises/ExerciseEquivalents/`), não uma feature de topo nova, e não dentro de `workout-editor` | Segue o precedente do repo: `nutrition` MIGROU `WeightTracking` de dono quando o dado (antropometria) tinha mais coesão com o novo domínio do que com o antigo — aqui é o oposto, o dado (equivalência) já nasce coeso com quem HOJE é dono de `Exercise` (`Features/Training/Exercises`, não `workout-editor`, que só referencia `ExerciseId` de fora, dentro de `WorkoutPlanDocument.Blocks`). Não inventa feature de topo nova pra um dado que pertence a uma entidade já servida por uma feature vertical-slice existente — mesmo espírito de reuso já registrado no AD-002/AD-006 (reusar o que já existe em vez de duplicar) | y (decorre diretamente do padrão já estabelecido — Exercise já tem feature dona, equivalência é atributo de Exercise) |

**Open questions:** nenhuma sem resposta — tudo acima está resolvido ou registrado como assumption.

---

## User Stories

### P1: Autor marca exercícios equivalentes a partir do editor de treino ⭐ MVP

**User Story**: Como profissional montando um plano no `workout-editor`, quero marcar quais outros exercícios do catálogo são equivalentes ao exercício que estou prescrevendo, pra que meu aluno tenha uma alternativa registrada (não uma trocada aleatória) quando precisar substituir esse exercício depois.

**Why P1**: É a base de tudo — sem a relação persistida, não existe o que exibir na biblioteca nem o que oferecer na troca de execução.

**Acceptance Criteria**:

1. WHEN o profissional clica no botão de substituir/equivalência (ícone, cabeçalho de `ExerciseRow`) de um exercício THEN sistema SHALL abrir um picker com busca no catálogo completo de exercícios (exceto o próprio exercício corrente), mostrando nome, grupo(s) muscular(es) principal(is) e equipamento de cada candidato
2. WHEN o profissional seleciona 1+ exercícios no picker e confirma THEN sistema SHALL persistir a relação de equivalência entre o exercício corrente e cada selecionado, IMEDIATAMENTE (não depende de salvar o plano — é uma escrita direta no catálogo, não no plano)
3. WHEN a relação é persistida entre A e B THEN sistema SHALL considerar A equivalente de B E B equivalente de A (simétrica — ver Assumptions), sem exigir uma segunda ação do autor no exercício B
4. WHEN o candidato selecionado não compartilha nenhum grupo muscular com o exercício corrente THEN sistema SHALL exibir um aviso não-bloqueante (o autor pode confirmar mesmo assim)
5. WHEN o profissional reabre o picker de um exercício que já tem equivalentes marcados THEN sistema SHALL exibir esses equivalentes pré-selecionados, permitindo desmarcar (remover a relação)
6. WHEN o profissional tenta marcar o próprio exercício como equivalente dele mesmo THEN sistema SHALL rejeitar (não aparece nem como opção de busca)

**Independent Test**: Abrir um exercício no editor de plano, marcar 2 exercícios como equivalentes, fechar e reabrir o picker — os 2 continuam marcados; abrir o picker de um dos 2 exercícios marcados — o exercício original aparece na lista dele também (simetria).

---

### P1: Biblioteca de Exercícios exibe a lista real de equivalentes ⭐ MVP

**User Story**: Como qualquer usuário navegando a Biblioteca de Exercícios, quero ver quais exercícios são equivalentes ao que estou olhando, e poder pular direto pro detalhe de um deles, pra explorar alternativas sem sair da tela.

**Why P1**: É a segunda superfície explícita pedida pelo usuário — sem isso, a relação persistida (Story anterior) fica invisível pra quem consome a biblioteca.

**Acceptance Criteria**:

1. WHEN o usuário abre o drawer de detalhe de um exercício que tem 1+ equivalentes registrados THEN sistema SHALL substituir o texto estático atual (`drawerSubs`) por uma lista real, mostrando nome, grupo muscular principal e equipamento de cada equivalente
2. WHEN o usuário clica num item da lista de equivalentes THEN sistema SHALL abrir o drawer de detalhe DAQUELE exercício equivalente (mesma UX de navegação já usada pra abrir o drawer a partir da lista principal — `inspect`)
3. WHEN o exercício corrente não tem nenhum equivalente registrado THEN sistema SHALL exibir um estado vazio claro (nunca a string genérica atual de placeholder)
4. WHEN um exercício listado como equivalente foi removido do catálogo entre o cadastro da relação e a consulta THEN sistema SHALL simplesmente omiti-lo da lista (ver Assumptions — sem erro, sem "item indisponível")

**Independent Test**: Abrir o drawer de um exercício com 2 equivalentes cadastrados — lista aparece com nome/músculo/equipamento de cada um; clicar num deles — drawer troca pro detalhe daquele exercício, e ele mostra o original de volta na sua própria lista (simetria visível na navegação).

---

### P1: Troca rápida de exercício durante a execução do treino ⭐ MVP

**User Story**: Como aluno executando um treino, quero trocar um exercício por um equivalente registrado no meio da sessão (ex.: aparelho ocupado), sem perder os sets que já registrei nesse exercício nem interromper meu fluxo de treino.

**Why P1**: É o caso de uso real que motivou toda a feature — sem o botão na execução, a relação persistida (Stories anteriores) não resolve o problema do usuário na prática.

**Acceptance Criteria**:

1. WHEN o exercício da sessão ativa tem 1+ equivalentes registrados THEN interface SHALL exibir um botão de "trocar/variações" próximo ao cabeçalho do exercício na tela de execução (mesma área do botão de detalhes já existente, `client.session.card.details`)
2. WHEN o aluno aciona o botão e o exercício NÃO tem nenhum equivalente registrado THEN sistema SHALL desabilitar o botão ou exibi-lo com indicação de que não há alternativa cadastrada (nunca abrir um picker vazio sem explicação)
3. WHEN o aluno aciona o botão e escolhe um dos equivalentes exibidos THEN sistema SHALL: (a) manter os sets JÁ CONCLUÍDOS do exercício original atribuídos a ele, intactos; (b) remover da tela ativa os sets NÃO concluídos do exercício original (ver Assumptions); (c) inserir o exercício escolhido na sessão como uma entrada nova, pronta pra registrar sets a partir daquele momento
4. WHEN o exercício escolhido já estava presente na mesma sessão (ex.: já era outro item do treino) THEN sistema SHALL rejeitar a troca por esse exercício (não permite duplicar o mesmo exercício 2x na mesma sessão via troca) e sinalizar isso pro aluno
5. WHEN a troca é confirmada THEN sistema SHALL enfileirar a mutação via o mesmo mecanismo offline (`enqueueMutation`) já usado pelo restante da execução, sincronizando quando a conexão permitir
6. WHEN a sessão é finalizada (`FinishWorkoutExecution`) após uma troca THEN sistema SHALL persistir ambos os exercícios (original com seus sets concluídos até a troca, substituto com os sets registrados depois) como entradas independentes do histórico daquela sessão — nenhum dos dois é descartado

**Independent Test**: Iniciar uma sessão, concluir 2 sets do exercício X (que tem um equivalente Y registrado), acionar a troca pra Y, registrar mais sets em Y, finalizar o treino — histórico da sessão mostra X com os 2 sets originais e Y com os sets registrados depois, nenhum dado perdido.

---

### P2: Remover equivalência já registrada

**User Story**: Como profissional, quero desfazer uma equivalência que marquei por engano ou que não faz mais sentido, sem precisar recriar o exercício.

**Why P2**: Complementa a Story 1 (picker já permite desmarcar reabrindo), mas como ação isolada/explícita não bloqueia o MVP — reabrir o picker já cobre o caso comum.

**Acceptance Criteria**:

1. WHEN o profissional desmarca um exercício já equivalente no picker (Story 1, AC5) THEN sistema SHALL remover a relação nos dois sentidos (A deixa de listar B e B deixa de listar A)
2. WHEN a relação removida não existia mais (ex.: dupla remoção) THEN sistema SHALL tratar como no-op idempotente, não erro

**Independent Test**: Marcar A↔B equivalentes, desmarcar no picker de A, reabrir o picker de B — B não lista mais A.

---

## Edge Cases

- WHEN o mesmo par de exercícios é marcado como equivalente 2x (idempotência de escrita) THEN sistema SHALL tratar como no-op (não duplica a relação, não erro)
- WHEN um exercício tem dezenas de equivalentes registrados THEN a lista no drawer e no botão de troca SHALL permitir rolagem/paginação simples (sem limite artificial de exibição, mas sem quebrar o layout — decisão de UI, não de produto)
- WHEN o usuário sem a capability `platform.exercises.manage` tenta marcar/remover uma equivalência (via API direta) THEN sistema SHALL retornar 403 (mesma capability que já protege Create/Update/Delete de exercício)
- WHEN a troca de exercício na execução é acionada offline (sem conexão) THEN sistema SHALL permitir a troca normalmente no estado local e enfileirar a sincronização, mesmo comportamento já garantido pro resto da execução (Fase 1)
- WHEN dois exercícios equivalentes um do outro são ambos excluídos do catálogo depois de registrados THEN a relação em si permanece órfã no banco (nenhuma limpeza automática é feita nesta feature — não é diferente de qualquer outra referência a um `ExerciseId` excluído já existente no sistema)

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
|---|---|---|---|
| EXVAR-01 | P1: Autor marca equivalentes no editor de treino | Design | Pending |
| EXVAR-02 | P1: Simetria da relação | Design | Pending |
| EXVAR-03 | P1: Aviso não-bloqueante de grupo muscular divergente | Design | Pending |
| EXVAR-04 | P1: Biblioteca exibe lista real de equivalentes | Design | Pending |
| EXVAR-05 | P1: Navegação entre detalhes via equivalente | Design | Pending |
| EXVAR-06 | P1: Botão de troca rápida na execução | Design | Pending |
| EXVAR-07 | P1: Preservação de sets já logados na troca | Design | Pending |
| EXVAR-08 | P1: Troca offline via mutation queue | Design | Pending |
| EXVAR-09 | P2: Remover equivalência | Design | Pending |

**ID format:** `EXVAR-NN`

**Status values:** Pending → In Design → In Tasks → Implementing → Verified

**Coverage:** 9 total, 0 mapped to tasks, 9 unmapped ⚠️ (Tasks phase ainda não roda nesta spec — fora do escopo deste pedido)

---

## Success Criteria

- [ ] Profissional marca 2+ exercícios como equivalentes a partir do editor de treino, e a relação aparece corretamente nos dois sentidos (simetria)
- [ ] Biblioteca de Exercícios mostra a lista real de equivalentes no drawer de detalhe, navegável, sem nenhum texto estático remanescente
- [ ] Aluno troca um exercício por um equivalente no meio da execução sem perder nenhum set já concluído do exercício original
- [ ] Troca de exercício na execução funciona offline e sincroniza pela fila de mutação já existente, sem endpoint/mecanismo de sync paralelo inventado
- [ ] Nenhuma equivalência exibida depende de um algoritmo de similaridade que não existe (sem "% de match" fabricado)
