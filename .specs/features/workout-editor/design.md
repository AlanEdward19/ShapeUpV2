# Editor de Treino — Design

**Spec**: `ShapeUpApi/.specs/features/workout-editor/spec.md`
**Status**: Draft

---

## Architecture Overview

Sem produção pra preservar, a mudança é uma **substituição direta de schema**: o que hoje é `WorkoutPlanDocument.Exercises: List<PlannedExercise>` (lista linear, 1 exercício = 1 unidade) vira `WorkoutPlanDocument.Blocks: List<Block>`, onde `Block` é a unidade estrutural (`Straight` = hoje, `Superset`/`Amrap`/`Emom` = novo) contendo 1+ `BlockExercise`, cada um com sua lista de `Set`. RPE deixa de ser `int` fixo pra virar `Intensity` (objeto único `{Type: Rpe|Rir, Value}`, exclusivo por construção).

O mesmo shape de `Set` (`WorkoutExerciseDto`/`WorkoutSetValueObject`) é reusado hoje por Execução de treino (`FinishWorkoutExecutionCommand`/`UpdateWorkoutExecutionStateCommand`) de forma **achatada** (sem Block). Essa decisão se mantém: Block é conceito de planejamento (o que o profissional desenha), não de execução (o que foi de fato feito) — ver Tech Decisions.

```mermaid
graph TD
    UI[PlanEditor / BlockCard / ExerciseRow / SetRow] -->|POST/PUT blocks[]| API[WorkoutPlansController / WorkoutTemplatesController]
    API --> CMD[CreateWorkoutPlanCommand / UpdateWorkoutPlanCommand]
    CMD --> VAL[CommandValidator: min-2-exercicios Superset, campos obrigatorios Amrap/Emom, RestSeconds proibido fora de Straight]
    VAL --> HANDLER[Handler]
    HANDLER --> DOC[WorkoutPlanDocument.Blocks: List of BlockDocumentValueObject]
    DOC --> MONGO[(MongoDB)]

    EXEC[FinishWorkoutExecutionCommand / UpdateWorkoutExecutionStateCommand] -->|Exercises flat, sem Block| EXECDOC[WorkoutExecutionDocument]
```

---

## Code Reuse Analysis

### Existing Components to Leverage

| Component | Location | How to Use |
|---|---|---|
| `SetType`, `Technique` enums | `ShapeUpApi/src/Features/Training/Shared/Enums/{SetType,Technique}.cs` | Reusados sem alteração — ortogonais ao agrupamento em blocos |
| `LoadUnit`, `Difficulty` enums | `Shared/Enums/` | Reusados sem alteração |
| FluentValidation (`AbstractValidator`, `RuleForEach().ChildRules(...)`) | `CreateWorkoutPlanCommandValidator.cs:5-38` | Mesmo padrão de `ChildRules` aninhado, +1 nível (Blocks→Exercises→Sets) |
| `ExerciseLibraryModal` | `ShapeUp-Web/src/components/ExerciseLibraryModal.jsx` | Reusado sem alteração — seleção de exercício não muda |
| `useTrainingApi.js` hooks | `ShapeUp-Web/src/hooks/api/useTrainingApi.js:6-88` | Assinatura mantida, payload interno (`exercises`→`blocks`) muda |
| Mutation queue (`enqueueMutation`) | Fase 1 | Sem alteração — payload maior passa pelo mesmo mecanismo |
| `LanguageContext` / `t()` | Fase 1 | Novas chaves seguem o padrão de paridade EN/PT-BR/ES já fechado |

### Integration Points

| System | Integration Method |
|---|---|
| `WorkoutPlansController` / `WorkoutTemplatesController` | Mesmas rotas (`POST/PUT/GET /api/training/workout-plans[...]`), payload novo |
| `WorkoutPlanMappings.Clone` / `WorkoutTemplateMappings` (usado por Copy) | Reescrito pra percorrer `Blocks→BlockExercises→Sets` em vez de `Exercises→Sets` |
| `Workouts/Shared/Dtos/WorkoutExerciseDto` + `ValueObjects/WorkoutSetValueObject` (Execução) | Recebem só o rename `Rpe`→`Intensity` (mecânico, compila); **não** ganham wrapper de Block nesta feature |

---

## Components

### Backend — Enums novos

- **Purpose**: Tipar bloco e tipo de intensidade
- **Location**: `ShapeUpApi/src/Features/Training/Shared/Enums/BlockType.cs`, `.../IntensityType.cs`
- **Interfaces**: `enum BlockType { Straight, Superset, Amrap, Emom }`; `enum IntensityType { Rpe, Rir }`
- **Dependencies**: nenhuma
- **Reuses**: mesmo padrão de `SetType`/`Technique` (enum simples, `[BsonRepresentation(BsonType.String)]` no document)

### Backend — `BlockDocumentValueObject` / `IntensityDocumentValueObject`

- **Purpose**: Persistir a unidade estrutural do plano/template
- **Location**: `Shared/Documents/ValueObjects/BlockDocumentValueObject.cs` (substitui `PlannedExerciseDocumentValueObject` como item de topo)
- **Interfaces**:
  - `BlockDocumentValueObject { BlockType Type; List<BlockExerciseDocumentValueObject> Exercises; int? TimeCapSeconds; int? IntervalSeconds; int? TotalRounds; int? RestAfterSeconds }`
  - `BlockExerciseDocumentValueObject` — mesmo shape do atual `PlannedExerciseDocumentValueObject` (`ExerciseId, ExerciseName, StrengthGainPercentage?, List<PlannedSetDocumentValueObject> Sets`), só renomeado pra deixar claro que vive dentro de um Block
  - `PlannedSetDocumentValueObject.Rpe: int` → `PlannedSetDocumentValueObject.Intensity: IntensityDocumentValueObject?`
  - `IntensityDocumentValueObject { IntensityType Type; int Value }`
- **Dependencies**: `BlockType`, `IntensityType`
- **Reuses**: `PlannedSetDocumentValueObject` mantém `Repetitions, Load, LoadUnit, SetType, Technique, RestSeconds` inalterados

### Backend — DTOs de comando (API contract)

- **Purpose**: Contrato de entrada de Create/Update de plano e template
- **Location**: `Workouts/Shared/Dtos/` (`BlockDto.cs` novo), `Workouts/Shared/ValueObjects/WorkoutSetValueObject.cs` (editado)
- **Interfaces**:
  - `BlockDto(BlockType Type, WorkoutExerciseDto[] Exercises, int? TimeCapSeconds, int? IntervalSeconds, int? TotalRounds, int? RestAfterSeconds)`
  - `WorkoutExerciseDto` — inalterado (`ExerciseId, Sets, StrengthGainPercentage?`), reusado dentro de `BlockDto.Exercises` E ainda usado flat pela Execução
  - `WorkoutSetValueObject(int? Repetitions, decimal Load, LoadUnit LoadUnit, SetType SetType, Technique Technique, IntensityDto? Intensity, int? RestSeconds, bool IsExtra = false)` — `Repetitions` vira nullable (AMRAP sem meta fixa), `RestSeconds` vira nullable (N/A fora de Straight)
  - `IntensityDto(IntensityType Type, int Value)`
- **Dependencies**: `BlockType`, `IntensityType`
- **Reuses**: assinatura de `CreateWorkoutPlanCommand`/`UpdateWorkoutPlanCommand`/template equivalentes só troca `WorkoutExerciseDto[] Exercises` por `BlockDto[] Blocks`

### Backend — Validators (extends existentes)

- **Purpose**: Aplicar WOED-02/03/05/07 (min. exercícios Superset, campos obrigatórios Amrap/Emom, RestSeconds proibido fora de Straight)
- **Location**: `CreateWorkoutPlanCommandValidator.cs`, `UpdateWorkoutPlanCommandValidator.cs`, equivalentes de Template
- **Interfaces** (regras a adicionar, mesmo padrão `RuleForEach().ChildRules`):
  - `RuleFor(b => b.Exercises).Must(ex => ex.Length >= 2).When(b => b.Type == BlockType.Superset)`
  - `RuleFor(b => b.TimeCapSeconds).GreaterThan(0).When(b => b.Type == BlockType.Amrap)`
  - `RuleFor(b => b.IntervalSeconds).GreaterThan(0).When(b => b.Type == BlockType.Emom)`; idem `TotalRounds`
  - `RuleForEach(b => b.Exercises).ChildRules(ex => ex.RuleForEach(e => e.Sets).ChildRules(s => s.RuleFor(x => x.RestSeconds).Empty().When(x => parentBlockType != Straight)))` — checagem cross-nível via `RuleFor(b => b).Must(...)` no nível do Block (FluentValidation não dá acesso direto ao pai dentro de `ChildRules` aninhado 2x; regra final decidida em Tasks, ver Risks)
  - Substitui `set.RuleFor(x => x.Rpe).InclusiveBetween(1, 10)` (hoje obrigatório) por `set.RuleFor(x => x.Intensity!.Value).InclusiveBetween(1, 10).When(x => x.Intensity != null)` — intensidade vira opcional (mudança de comportamento deliberada, spec WOED-08 AC4)
- **Dependencies**: FluentValidation
- **Reuses**: estrutura existente, só ganha 1 nível de aninhamento (Block) e novas `RuleFor`

### Backend — Mappings (Clone/ToResponse)

- **Purpose**: Traduzir documento↔DTO nas duas direções, e no Copy (`Clone`)
- **Location**: `WorkoutPlans/Shared/WorkoutPlanMappings.cs`, `WorkoutTemplates/Shared/WorkoutTemplateMappings.cs`
- **Interfaces**: mesmas assinaturas (`Clone`, `ToResponse`), corpo reescrito pra `Blocks.Select(b => new BlockDocumentValueObject { ..., Exercises = b.Exercises.Select(...).ToList() })`
- **Dependencies**: `BlockDocumentValueObject`, `BlockDto`
- **Reuses**: mesmo padrão de `Select().ToList()`/`ToArray()` já usado

### Frontend — `BlockCard` (novo)

- **Purpose**: Renderizar 1 bloco — seletor de tipo + campos específicos (TimeCap pra Amrap; Interval+Rounds pra Emom) + lista de `ExerciseRow`
- **Location**: `ShapeUp-Web/src/components/training/BlockCard.jsx` (novo arquivo — extraído do `PlanEditor` monolítico)
- **Interfaces**: `<BlockCard block={block} onChange={...} onRemove={...} onAddExercise={...} />`
- **Dependencies**: `ExerciseRow`
- **Reuses**: JSX/estilos `su-exercise-builder-card` já existentes, adaptado pra envelope de bloco

### Frontend — `ExerciseRow` (extraído)

- **Purpose**: Nome/tags/notes do exercício + lista de `SetRow` — reusado dentro de qualquer tipo de bloco
- **Location**: `ShapeUp-Web/src/components/training/ExerciseRow.jsx` (novo, extrai lógica hoje inline em `ClientDetail.jsx:322-351`)
- **Interfaces**: `<ExerciseRow exercise={ex} onChange={...} onRemove={...} />`
- **Dependencies**: `SetRow`
- **Reuses**: JSX existente, sem mudança de comportamento

### Frontend — `SetRow` (extraído + intensidade exclusiva)

- **Purpose**: Colunas type/technique/reps/load/rest + controle de intensidade (toggle RPE/RIR + 1 input)
- **Location**: `ShapeUp-Web/src/components/training/SetRow.jsx` (novo, extrai `ClientDetail.jsx:365-391`)
- **Interfaces**: `<SetRow set={s} blockType={block.type} onChange={...} onRemove={...} />` — `rest` fica `disabled`/oculto quando `blockType !== 'straight'` (WOED-03/05/07 AC4)
- **Dependencies**: nenhuma nova
- **Reuses**: `SET_TYPES`/`TECHNIQUES` constants (`ClientDetail.jsx:130,134`), inalterados

### Frontend — `PlanEditor` (editado)

- **Purpose**: Orquestrar meta do plano + stack de blocos + sidebar de resumo (perde a lógica de exercício/set, que migra pros componentes acima)
- **Location**: `ShapeUp-Web/src/pages/Dashboard/ClientDetail.jsx:136-...`
- **Interfaces**: mesma assinatura (`{ plan, onSave, onCancel, onAssign, isIndependent }`), estado interno `currentExercises`→`currentBlocks`
- **Dependencies**: `BlockCard`
- **Reuses**: form de meta do plano, sidebar de resumo (summary), `ExerciseLibraryModal` — tudo inalterado; `avgRpe`/`intensityDist` do resumo passam a considerar `Intensity.Type === 'rpe'` apenas (RIR não é a mesma escala, não entra na mesma média — ver Risks)

---

## Data Models

```typescript
// Backend (conceitual — C# real usa record/class, ver Components acima)
type BlockType = 'Straight' | 'Superset' | 'Amrap' | 'Emom'
type IntensityType = 'Rpe' | 'Rir'

interface Intensity {
  type: IntensityType
  value: number // 1-10
}

interface Set {
  repetitions?: number       // opcional (AMRAP sem meta)
  load: number
  loadUnit: 'Kg' | 'Lb'
  setType: 'Warmup' | 'Feeder' | 'Working' | 'Topset' | 'Dropset' | 'Backoff'
  technique: 'Straight' | 'DropSet' | 'RestPause' | 'ClusterSet' | 'MuscleRound'
  intensity?: Intensity
  restSeconds?: number       // só válido quando o Block pai é Straight
}

interface BlockExercise {
  exerciseId: number
  exerciseName: string
  strengthGainPercentage?: number
  sets: Set[]
}

interface Block {
  type: BlockType
  exercises: BlockExercise[]   // Superset exige >= 2
  timeCapSeconds?: number      // obrigatório quando type === 'Amrap'
  intervalSeconds?: number     // obrigatório quando type === 'Emom'
  totalRounds?: number         // obrigatório quando type === 'Emom'
  restAfterSeconds?: number    // descanso após o bloco inteiro (qualquer tipo)
}

interface WorkoutPlan {
  // ...campos inalterados (name, notes, durationInWeeks, phase, difficulty)
  blocks: Block[] // substitui `exercises: PlannedExercise[]`
}
```

**Relationships**: `WorkoutPlanDocument`/`WorkoutTemplateDocument` têm `Blocks: List<Block>` no lugar de `Exercises`. `WorkoutExecutionDocument` (Execução de treino, fora de escopo) mantém sua lista flat de exercícios/sets — só ganha o rename `Intensity` no set, sem Block.

---

## Error Handling Strategy

| Error Scenario | Handling | User Impact |
|---|---|---|
| Superset com < 2 exercícios | `400` via FluentValidation, mensagem "Superset precisa de pelo menos 2 exercícios" | Toast/alert no editor, plano não salva |
| Amrap sem `TimeCapSeconds` (ou ≤ 0) | `400` via FluentValidation | Idem |
| Emom sem `IntervalSeconds`/`TotalRounds` (ou ≤ 0) | `400` via FluentValidation | Idem |
| `RestSeconds` informado fora de um bloco Straight | `400` via FluentValidation | Idem — UI já desabilita o campo (defesa em profundidade) |
| Troca de `Type` de bloco já preenchido com dados incompatíveis (ex.: Straight→Superset com 1 exercício) | Validação client-side bloqueia a troca antes de submeter (spec Edge Case) | Botão de trocar tipo fica desabilitado/com tooltip até satisfazer o mínimo |
| `ExerciseId` referenciando exercício excluído do catálogo | Mesmo comportamento já existente hoje pra blocos Straight (reusado, não é novo) | Exercício exibido como "indisponível" |

---

## Risks & Concerns

| Concern | Location | Impact | Mitigation |
|---|---|---|---|
| Execução de treino (`FinishWorkoutExecutionCommand`/`UpdateWorkoutExecutionStateCommand`) reusa `WorkoutExerciseDto`/`WorkoutSetValueObject` hoje — o rename `Rpe`→`Intensity` ripple mecanicamente pra lá | `Workouts/FinishWorkoutExecution/FinishWorkoutExecutionCommand.cs:5`, `Workouts/UpdateWorkoutExecutionState/UpdateWorkoutExecutionStateCommand.cs:5` | Sem o rename lá, o projeto não compila (tipo compartilhado) | Aplicar só o rename mecânico nesses 2 comandos + seus handlers/testes, sem introduzir Block ali — feature "Execução de treino" (spec futura) decide se o runtime precisa de contexto de bloco (timer de AMRAP/EMOM) |
| Validação cross-nível "RestSeconds proibido fora de Straight" exige o validator de Set conhecer o `Type` do Block pai, mas `RuleForEach().ChildRules()` aninhado (Block→Exercise→Set) não expõe o avô diretamente no FluentValidation padrão | `CreateWorkoutPlanCommandValidator.cs` (novo nível) | Regra pode ficar mais verbosa/custom (`RuleFor(x => x).Custom(...)` no nível do Block, iterando Exercises/Sets manualmente) do que um simples `ChildRules` | Resolvido na fase Tasks com um `Custom` validator no nível do `BlockDto` que itera `Exercises→Sets` sabendo o `Type` do bloco corrente — mecanismo já usado no ecossistema FluentValidation, sem lib nova |
| `PlanEditor` (`ClientDetail.jsx:136-...`) é um componente único de 500+ linhas fazendo meta-form + stack de exercícios + sidebar de analytics | `ClientDetail.jsx:136` | Difícil de estender com blocos sem piorar ainda mais o tamanho | Extração de `BlockCard`/`ExerciseRow`/`SetRow` (already planejada acima) resolve isso como efeito colateral do trabalho já necessário — não é tarefa extra |
| Validador atual (`CreateWorkoutPlanCommandValidator.cs:33`) trata `Rpe` como obrigatório (`InclusiveBetween(1,10)` sem `When`) | `CreateWorkoutPlanCommandValidator.cs:33` e equivalentes de Update/Template | Se a regra for só copiada, `Intensity` continuaria obrigatório, contradizendo spec WOED-08 AC4 (opcional) | Regra nova usa `.When(x => x.Intensity != null)` — mudança de comportamento deliberada, documentada aqui pra não ser revertida por engano |
| Nenhum teste de frontend cobre `PlanEditor` hoje (achado já registrado no scan da Fase 1) | `ShapeUp-Web/src` (ausência) | Refactor de extração + novo comportamento sem rede de segurança automatizada no frontend | Gap pré-existente, não introduzido por esta feature — fora do escopo fechar aqui; backend ganha cobertura via unit/integration tests nas Tasks |
| Sidebar de resumo (`avgRpe`, `intensityDist`) hoje assume que todo set tem RPE numérico | `ClientDetail.jsx:240-263` | Sets com RIR (não RPE) quebrariam a média se tratados como RPE | Cálculo passa a filtrar só `Intensity.Type === 'Rpe'` pra essas duas métricas — sets em RIR simplesmente não entram na média de RPE (não inventamos conversão RIR→RPE, que não é 1:1) |

| **[Achado no gate check de T3/T4, 2026-09-08]** `WorkoutPlanDocument.Exercises`/`WorkoutTemplateDocument.Exercises` são lidos fora de `Features/Training` — passou batido no levantamento original (só varreu dentro de `Features/Training`) | `Features/GymManagement/Shared/TrainerClientAdherenceCalculator.cs` (4 pontos), `Features/GymManagement/TrainerClients/GetTrainerClients/GetTrainerClientsHandler.cs` (1 ponto), `Features/Training/Workouts/StartWorkoutExecution/StartWorkoutExecutionHandler.cs` (1 ponto — este também invalida a suposição de que Execution só toca `WorkoutExerciseDto`/`WorkoutSetValueObject`, não `WorkoutPlanDocument.Exercises` direto) | Sem correção, o build inteiro quebra (confirmado via `dotnet build` completo) | Task nova `T7b` (tasks.md) — mesmo achatamento `Blocks.SelectMany(b => b.Exercises)` usado no restante do Execution (T13/T14) |

> Fora esse achado, nenhum risco de segurança/autorização novo — mesma política de acesso (Fase 1, `ITrainingAccessPolicy`) já cobre as rotas de plano/template, sem mudança de superfície de autorização.

---

## Tech Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Onde o Block existe | Só em `WorkoutPlanDocument`/`WorkoutTemplateDocument` (planejamento). Execução (`WorkoutExecutionDocument`, fora de escopo) permanece flat | Block é decisão de como o profissional *desenha* o treino; o que foi de fato executado é outra pergunta, respondida pela spec futura "Execução de treino" |
| Intensidade como objeto único (`{Type, Value}`) em vez de dois campos nullable (`Rpe?`, `Rir?`) | Objeto único | Torna "exclusivo" uma invariante do próprio tipo (impossível setar os dois), não uma regra de validação que pode ser esquecida em algum call site novo |
| `RestSeconds` continua no shape do `Set` (não vira 4 shapes de Set diferentes por tipo de bloco) | Campo único, validado por regra condicional no nível do Block | Mantém `Set` uniforme — código de mapeamento (Clone/ToResponse) não precisa de switch por tipo; inválido é rejeitado na borda (validator), não estruturalmente impossível |
| `WorkoutExerciseDto`/`BlockExercise` seguem sendo o mesmo tipo, reusado tanto dentro de `BlockDto.Exercises` quanto flat na Execução | 1 tipo só | Shape idêntico hoje (`ExerciseId, Sets, StrengthGainPercentage?`); criar 2 tipos agora seria abstração prematura — separar só se divergirem de fato quando a spec de Execução for escrita |

> **Project-level**: as duas primeiras linhas acima (Block só em planejamento; Intensidade como objeto exclusivo) valem pra qualquer feature futura que toque planejamento ou execução de treino — registradas como `AD-007` em `ShapeUpApi/.specs/STATE.md`.

---

## Tips (não editar — referência do processo)

- Confirmar este design antes de ir pra Tasks.
</content>
