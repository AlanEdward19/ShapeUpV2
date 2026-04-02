# Cálculo de Adesão do Cliente - Documentação

## Visão Geral

O sistema de cálculo de adesão agora é mais sofisticado e preciso. Ele considera não apenas se o cliente completou sessões, mas também **quantas séries executou em relação ao que foi prescrito**.

## Fórmula de Cálculo

```
Adesão (%) = (Total de Séries Executadas / Total de Séries Prescritas) × 100
```

### Componentes

1. **Total de Séries Executadas**: Contagem de todas as séries que o cliente realizou em sessões completadas, excluindo séries "extras"
2. **Total de Séries Prescritas**: Contagem de todas as séries que foram planejadas no programa de treinamento

### Exemplos Práticos

#### Exemplo 1: Adesão Completa
- **Prescrição**: 3 exercícios × 4 séries = 12 séries
- **Execução**: Cliente realizou todas as 12 séries
- **Adesão**: (12 / 12) × 100 = **100%**

#### Exemplo 2: Adesão Parcial
- **Prescrição**: 3 exercícios × 4 séries = 12 séries
- **Execução**: Cliente completou apenas 2 exercícios (8 séries)
- **Adesão**: (8 / 12) × 100 = **66.67%**

#### Exemplo 3: Adesão com Múltiplas Sessões
- **Sessão 1 Prescrição**: 12 séries
- **Sessão 1 Execução**: 10 séries
- **Sessão 2 Prescrição**: 12 séries  
- **Sessão 2 Execução**: 12 séries
- **Adesão Total**: (22 / 24) × 100 = **91.67%**

## Implementação

### Classes Envolvidas

#### 1. `TrainerClientAdherenceCalculator.cs`
Fornece métodos estáticos para cálculo de adesão em diferentes níveis:

- **`CalculateAdherenceFromSessions()`**: Calcula adesão geral baseada em um conjunto de sessões completadas
- **`CalculateAdherencePerSession()`**: Retorna um dicionário com adesão por sessão
- **`CalculateExerciseAdherence()`**: Calcula adesão específica de um exercício em uma sessão
- **`CalculateAdherenceFromCompletedCount()`**: Versão simplificada (compatibilidade)

#### 2. `GetTrainerClientsHandler.cs`
Orquestra o cálculo de adesão ao listar clientes de um trainer:

1. Recupera sessões completadas do cliente desde `EnrolledAt`
2. Carrega os planos de treino associados às sessões (do MongoDB)
3. Conta séries executadas vs. prescritas
4. Calcula percentual final

### Fluxo de Dados

```
GET /api/gym-management/trainers/{trainerId}/clients
    ↓
Para cada cliente:
    ↓
1. Recuperar sessões completadas (MongoDB - IWorkoutSessionRepository)
    ├─ Filtra por: targetUserId + data >= enrolledAt + data <= hoje
    └─ Retorna: WorkoutSessionDocument[]
    
2. Para cada sessão completada:
    ├─ Contar séries executadas: Exercises → Sets → Contagem
    │  (Exclui sets "extras")
    │
    ├─ Se sesssão tem WorkoutPlanId:
    │  └─ Carregar plano (MongoDB - IWorkoutPlanRepository)
    │     └─ Contar séries prescritas: Exercises → Sets → Contagem
    │
    └─ Se não tem plano:
       └─ Assumir que o executado era o esperado
    
3. Somar totais:
    ├─ totalSetsExecuted = SUM(executedSets por sessão)
    └─ totalSetsPrescribed = SUM(prescribedSets por sessão)
    
4. Calcular percentual:
    └─ adherencePercentage = (totalSetsExecuted / totalSetsPrescribed) × 100
       ├─ Limitado a: 0% mínimo, 100% máximo
       └─ Fallback: 100% se completou sessões mas sem prescrição
```

## Comportamento em Casos Especiais

### Sem Sessões Completadas
- **Adesão = 0%**
- Indica que o cliente nunca completou um treino

### Sessão Sem Plano Associado
- **Comportamento**: Usa o número de séries executadas como referência
- **Racional**: O cliente completou algo, então conta como "prescrito e executado"

### Cliente Inativo
- **Cálculo continua**: A adesão é calculada baseada no histórico
- **Status**: Campo `Status` indica se ativo ou inativo
- Os campos são separados e independentes

## Séries "Extras"

Séries marcadas como `IsExtra = true` **NÃO** contam para o cálculo de adesão:
- O cliente pode ter executado séries adicionais
- Essas são positivas, mas não afetam o percentual de adesão ao plano
- Adesão mede se seguiu o **prescrito**, não o extra

## Performance e Otimizações

### Carregamento de Planos
- Planos são carregados uma única vez (em batch)
- Usa `Dictionary<planId, WorkoutPlanDocument>` para acesso O(1)
- Erros ao carregar um plano não afetam outros clientes

### Paginação
- Cálculo é feito por cliente (em conjunto com paginação keyset)
- Não há query N+1: planos são carregados em um único lote

### Tratamento de Exceções
- Se falhar ao carregar um plano, continua sem ele
- Cliente pode ter adesão calculada mesmo com planos indisponíveis

## Resposta da API

```json
{
  "items": [
    {
      "id": 1,
      "trainerId": 100,
      "clientId": 200,
      "clientName": "João Silva",
      "planName": "Programação Avançada",
      "hasActivePlan": true,
      "adherencePercentage": 87.5,
      "status": 1,
      "enrolledAt": "2024-01-15T10:30:00Z"
    }
  ],
  "nextCursor": "eyJpZCI6MjB9"
}
```

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `adherencePercentage` | decimal | 87.5 = 87.5% (séries executadas / prescritas) |
| `hasActivePlan` | bool | true = tem plano ativo atribuído |
| `status` | int | 1 = Active, 2 = Inactive, etc. |

## Refinamentos Futuros

1. **Adesão por Exercício**: Calcular adesão individual por exercício
   - Alguns clientes completam bem alguns exercícios, mal outros
   
2. **Adesão Ponderada**: Dar pesos diferentes para exercícios
   - Ex: exercícios principais (80% peso) vs. secundários (20%)
   
3. **Histórico de Adesão**: Gráficos de adesão ao longo do tempo
   - Adesão última semana, mês, trimestre, geral
   
4. **Adesão Técnica**: Validar qualidade (RPE, técnica)
   - Não é apenas quantidade, mas também qualidade

5. **Alertas de Adesão**: Notificar trainer se cliente cair abaixo de X%
   - Automação para engajamento

## Testes

### Unidade
Teste `TrainerClientAdherenceCalculator` com casos:
- Cliente com 0 sessões → 0%
- Cliente com sessões completas → 100%
- Cliente com sessões parciais → X%
- Cliente com múltiplas sessões → média agregada

### Integração
Teste `GetTrainerClientsHandler` com:
- Múltiplos clientes com diferentes adesões
- Paginação funcionando corretamente
- Carregamento correto de planos

## Referências

- `WorkoutSessionDocument`: Documento MongoDB com exercícios/séries executadas
- `WorkoutPlanDocument`: Documento MongoDB com exercícios/séries prescritos
- `ExecutedExerciseDocumentValueObject`: Exercício executado com seus sets
- `PlannedExerciseDocumentValueObject`: Exercício prescrito com seus sets

