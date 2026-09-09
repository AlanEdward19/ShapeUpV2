# Event Bus (Outbox + Pluggable Broker) — Specification

## Problem Statement

Nenhum mecanismo de eventos/mensageria existe hoje no backend (`ShapeUpApi`) — confirmado por scan de código e já registrado como gap conhecido em `CURRENT_STATE_ASSESSMENT.md`. Toda escrita de domínio é síncrona e isolada: quem quiser reagir a "um treino terminou" hoje só pode chamar código de outro domínio direto de dentro do handler (acoplamento forte, sem retry, sem sobreviver a crash). A Fase 3 (Gamification) precisa reagir a eventos de domínio (treino concluído, meta batida) sem acoplar `Training` diretamente a `Gamification`, e sem perder eventos se o processo cair entre a escrita e a reação — daí a necessidade de um Event Bus durável, publicado antes de qualquer feature de domínio depender dele (mesmo padrão usado pela Fase 1 com Autorização).

## Goals

- [x] Um domínio publica um evento e sua própria escrita de negócio na mesma transação (garantia atômica — nunca "escrita foi, evento não" nem o contrário)
- [x] Eventos pendentes são entregues ao broker de forma confiável, sobrevivendo a crash/restart do processo
- [x] Consumidores processam eventos de forma idempotente (at-least-once — nunca exactly-once, isso é assumido, não perseguido)
- [x] O broker de mensageria é plugável por interface (RabbitMQ local hoje, trocável por Azure Service Bus ou outro em produção sem reescrever publishers/consumers)
- [x] Prova ponta a ponta: `Training` publica `WorkoutFinished` ao concluir uma sessão; um consumidor de exemplo recebe e processa (esse consumidor real vira a base da Fase 3/Gamification depois, mas aqui serve só de prova de que o pipe funciona)

## Out of Scope

| Item | Motivo |
|---|---|
| Exactly-once delivery | Padrão de indústria é at-least-once + consumidor idempotente; exactly-once distribuído é problema não resolvido em geral, não vamos fingir que resolvemos |
| Event sourcing / event store como fonte de verdade | Esta feature é mensageria entre domínios, não persistência de estado via eventos — os agregados continuam sendo a fonte de verdade em Mongo/SQL |
| Schema registry / versionamento formal de contrato de evento | Monólito único hoje, sem múltiplos serviços consumindo o mesmo tópico externamente; versionamento fica em convenção de nome (`WorkoutFinished`) até haver necessidade real |
| UI de monitoramento da fila/outbox | Observabilidade via Seq (já existe, OpenTelemetry) cobre isso por enquanto — nenhuma tela nova |
| Migrar TODOS os domínios existentes para publicar eventos | Esta feature entrega a infra + UM publisher/consumer real (`Training`→`WorkoutFinished`) como prova; outros domínios adotam publish conforme suas próprias features precisarem, não é retrofit em massa aqui |
| Broker gerenciado em produção (Azure Service Bus de fato provisionado) | Fora de escopo desta feature decidir/contratar o serviço de nuvem — a interface fica pronta para receber esse adapter quando alguém decidir qual usar; só o adapter RabbitMQ (local/self-hosted) é implementado agora |

---

## Assumptions & Open Questions

| Assumption / decision | Chosen default | Rationale | Confirmed? |
|---|---|---|---|
| Nível de durabilidade | Outbox transacional (não fila em memória, não best-effort dual-write) | Usuário pediu explicitamente "o mais durável e correto para longo prazo, mesmo que seja mais complexo" | y |
| Broker local vs produção | Interface `IEventBus`/publisher-consumer plugável; adapter RabbitMQ implementado agora (self-hosted, mesmo espírito do Seq — sem SaaS); adapter de produção (Azure Service Bus etc.) fica como ponto de extensão, não implementado nesta feature | Usuário pediu explicitamente essa abstração | y |
| MongoDB precisa suportar transação multi-documento (para o outbox do domínio `Training`, que grava em Mongo) | Reconfigurar o Mongo do `docker-compose` como replica set single-node (permite `session.WithTransactionAsync`) | Mongo standalone (config atual) não suporta transação multi-documento; usuário escolheu resolver isso corretamente em vez de aceitar dual-write sem garantia | y |
| Onde vive a tabela/coleção de Outbox | Por domínio, na mesma base de dados do agregado que publica (ex.: `Training` → coleção Mongo `outbox_events` na mesma base de `workout_sessions`; um domínio SQL futuro → tabela `OutboxEvents` no seu próprio `DbContext`) — nunca uma tabela/coleção compartilhada entre domínios | Só assim a escrita do evento entra na MESMA transação do agregado; uma tabela compartilhada cruzando bases quebraria a garantia atômica que é o motivo de existir o outbox | n — assumption, log apenas |
| Quem lê o outbox e publica no broker (o "relay") | Um `BackgroundService` (.NET `IHostedService`) por domínio publisher, rodando dentro do próprio `ShapeUp` processo (sem novo processo/deployment separado por enquanto) — faz polling do outbox daquele domínio, publica no broker, marca como `Published` | Simplicidade operacional (sem novo artefato de deploy); revisitar se volume/latência exigir um worker dedicado fora do processo web | n — assumption, log apenas |
| Ordem de entrega | Garantida só por agregado (mesma `AggregateId`/routing key vai pro mesmo consumidor em ordem), nunca globalmente entre agregados diferentes | Padrão realista para brokers com partição/routing key (RabbitMQ direct/topic exchange); exigir ordem global seria over-engineering sem caso de uso que precise disso hoje | y (default seguro) |
| Retry/dead-letter | Consumidor com falha reprocessa com backoff exponencial (N tentativas configurável); esgotadas as tentativas, evento vai pra fila/tabela de dead-letter, não é descartado silenciosamente | Perder evento silenciosamente é o problema que esta feature existe pra evitar | y |
| Idempotência do consumidor | Responsabilidade do CONSUMIDOR (guardar `EventId` processado, ignorar duplicata), não do bus tentar deduplicar | At-least-once é a garantia do bus; exactly-once ficaria caro/impossível de garantir de forma geral — está fora de escopo (ver Out of Scope) | y |
| Formato/serialização do evento | JSON, envelope com `EventId (Guid)`, `EventType (string)`, `OccurredAtUtc`, `Payload (JSON)` | Simplicidade, já é o padrão usado no resto da API (System.Text.Json); nenhuma necessidade de binário/protobuf identificada | y (default seguro) |

**Open questions:** nenhuma sem resposta — tudo acima está resolvido ou registrado como assumption.

---

## User Stories

### P1: Publisher grava evento atomicamente com sua escrita de domínio ⭐ MVP

**User Story**: Como desenvolvedor de um domínio (ex.: `Training`), quero publicar um evento de domínio na MESMA transação da minha escrita de negócio, para nunca ter "escrita foi, evento não" (ou vice-versa).

**Why P1**: É a garantia central que todo o resto da feature depende — sem isso, "durável" não significa nada.

**Acceptance Criteria**:

1. WHEN um handler grava um agregado E chama `Publish(evento)` dentro da mesma transação/sessão de banco THEN sistema SHALL persistir o evento numa linha de outbox como parte da MESMA transação (commit atômico com o agregado)
2. WHEN a transação falha/dá rollback (por qualquer motivo, incluindo falha ao gravar o evento) THEN sistema SHALL reverter TANTO o agregado quanto o evento — nunca um sem o outro
3. WHEN o domínio publisher grava em MongoDB (ex.: `Training`) THEN sistema SHALL usar uma sessão/transação Mongo real (`IClientSessionHandle.WithTransactionAsync`) cobrindo agregado + linha de outbox
4. WHEN o domínio publisher grava em SQL Server (via EF Core) THEN sistema SHALL usar a transação implícita do `DbContext.SaveChangesAsync` cobrindo agregado + linha de outbox (mesmo `DbContext`)

**Independent Test**: Simular falha forçada após gravar o agregado mas antes de persistir o evento (dentro da mesma transação) — nem agregado nem evento devem existir após o rollback.

---

### P1: Relay entrega eventos pendentes ao broker de forma confiável

**User Story**: Como sistema, quero que todo evento gravado no outbox eventualmente chegue ao broker, mesmo que o processo caia entre a gravação e a publicação.

**Why P1**: É o que torna o outbox útil — sem relay confiável, o evento fica preso no banco pra sempre.

**Acceptance Criteria**:

1. WHEN existe uma linha de outbox com status `Pending` THEN o relay daquele domínio SHALL publicá-la no broker dentro de um intervalo de polling configurável
2. WHEN a publicação no broker é confirmada (ack) THEN sistema SHALL marcar a linha como `Published` (nunca reprocessada de novo pelo relay)
3. WHEN o broker está indisponível (conexão falha) THEN sistema SHALL manter a linha como `Pending` e tentar de novo no próximo ciclo de polling (backoff, não busy-loop)
4. WHEN o processo reinicia com linhas `Pending` no outbox (de antes do restart) THEN o relay SHALL retomar a publicação delas normalmente ao subir de novo

**Independent Test**: Gravar uma linha de outbox diretamente (bypass do publisher), derrubar o broker, confirmar que a linha some (é publicada) assim que o broker volta.

---

### P1: Consumidor processa evento de forma idempotente

**User Story**: Como consumidor de um evento, quero processá-lo exatamente uma vez do ponto de vista de efeito, mesmo que o bus me entregue a mesma mensagem mais de uma vez (at-least-once).

**Why P1**: At-least-once é a garantia real do bus — sem idempotência no consumidor, uma redelivery duplica efeito (ex.: XP contado duas vezes).

**Acceptance Criteria**:

1. WHEN um consumidor recebe um evento com `EventId` já processado com sucesso anteriormente THEN sistema SHALL ignorar o processamento (no-op) e confirmar a mensagem (ack) sem reaplicar o efeito
2. WHEN um consumidor recebe um evento com `EventId` nunca visto THEN sistema SHALL processá-lo e registrar `EventId` como processado antes (ou atomicamente com) confirmar a mensagem
3. WHEN o processamento do consumidor lança exceção THEN sistema SHALL NÃO marcar o `EventId` como processado, e SHALL fazer retry com backoff exponencial até um limite configurável de tentativas
4. WHEN o limite de tentativas se esgota THEN sistema SHALL mover a mensagem para dead-letter (nunca descartar silenciosamente) e registrar isso de forma observável (log/trace)

**Independent Test**: Publicar o mesmo `EventId` duas vezes manualmente, confirmar que o efeito de negócio do consumidor acontece só uma vez.

---

### P1: Broker plugável por interface (RabbitMQ agora, trocável depois)

**User Story**: Como time, quero trocar de broker (RabbitMQ local → Azure Service Bus em produção, ou qualquer outro) sem reescrever código de publisher/consumer.

**Why P1**: Requisito explícito do usuário — é o que evita lock-in numa tecnologia de mensageria específica.

**Acceptance Criteria**:

1. WHEN um domínio publica um evento THEN sistema SHALL chamar apenas a interface `IEventBus`/`IEventPublisher` (nome final decidido em Design), nunca uma classe concreta de um broker específico
2. WHEN um domínio consome um evento THEN sistema SHALL implementar apenas a interface de consumidor abstrata, nunca depender de tipos do SDK do RabbitMQ diretamente no código de domínio
3. WHEN a implementação registrada em DI é o adapter RabbitMQ THEN sistema SHALL publicar/consumir via RabbitMQ sem nenhuma mudança de código de domínio
4. WHEN um segundo adapter (ex.: um fake/in-memory usado em testes) é registrado no lugar do RabbitMQ THEN sistema SHALL funcionar igual do ponto de vista do domínio (prova de que a abstração não vaza detalhe de broker)

**Independent Test**: Trocar o adapter registrado em DI de RabbitMQ para um fake in-memory nos testes de integração, sem alterar nenhum código de `Training` ou do consumidor de exemplo.

---

### P1: Prova ponta a ponta — `Training` publica `WorkoutFinished`

**User Story**: Como prova de que o pipe completo funciona, quero que terminar um treino publique um evento real, consumido por um handler de exemplo.

**Why P1**: Sem uma prova end-to-end real, a infra fica "confiança de papel" — este é o vertical slice que valida tudo acima junto.

**Acceptance Criteria**:

1. WHEN `FinishWorkoutExecutionHandler` completa uma sessão de treino com sucesso THEN sistema SHALL publicar um evento `WorkoutFinished` (contendo no mínimo: `SessionId`, `TargetUserId`, `ExecutedByUserId`, `EndedAtUtc`) na mesma transação Mongo da atualização da sessão
2. WHEN o evento `WorkoutFinished` chega ao consumidor de exemplo THEN sistema SHALL processá-lo (o comportamento do consumidor de exemplo é mínimo — ex.: logar/gravar um registro simples — o comportamento REAL de Gamification vem na feature seguinte, que substitui esse consumidor de exemplo)

**Independent Test**: Finalizar um treino via endpoint real, confirmar (via log/tabela de teste) que o consumidor de exemplo recebeu o evento correspondente.

---

### P2: Dead-letter observável

**User Story**: Como operador, quero ver o que caiu em dead-letter e por quê, para poder investigar/reprocessar manualmente.

**Why P2**: Importante para operação, mas o pipe funciona (via retry) sem isso no dia 1.

**Acceptance Criteria**:

1. WHEN uma mensagem vai para dead-letter THEN sistema SHALL registrar motivo, `EventId`, e stack trace/erro via o pipeline de observabilidade já existente (OpenTelemetry/Seq)

---

## Edge Cases

- WHEN duas instâncias do relay do mesmo domínio rodam ao mesmo tempo (ex.: 2 réplicas do processo web) THEN sistema SHALL evitar publicar a mesma linha de outbox duas vezes em paralelo (lock otimista/claim por linha — detalhe de implementação decidido em Design)
- WHEN o payload do evento excede um tamanho razoável (ex.: alguém tenta serializar uma lista grande demais) THEN sistema SHALL rejeitar a publicação na borda (validação), não deixar estourar no broker
- WHEN o consumidor de exemplo está fora do ar (deploy, restart) THEN sistema SHALL manter a mensagem na fila até o consumidor voltar (garantia nativa de fila do broker), não perder a mensagem
- WHEN o Mongo reconfigurado como replica set single-node fica indisponível momentaneamente THEN sistema SHALL falhar a transação de forma limpa (erro tratado), nunca gravar agregado sem evento ou vice-versa

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
|---|---|---|---|
| EVTB-01 | P1: Publisher grava evento atomicamente | Execute | Verified |
| EVTB-02 | P1: Publisher — Mongo usa transação real | Execute | Verified |
| EVTB-03 | P1: Publisher — SQL usa transação do DbContext | Execute | N/A (documented) — nenhum domínio SQL publica evento no escopo construído (só `Training`/Mongo); padrão EF Core outbox documentado em design.md para a próxima feature |
| EVTB-04 | P1: Relay entrega eventos pendentes | Execute | Verified |
| EVTB-05 | P1: Relay — retry/backoff quando broker indisponível | Execute | Verified |
| EVTB-06 | P1: Relay — retoma após restart | Execute | Verified |
| EVTB-07 | P1: Consumidor idempotente (dedupe por EventId) | Execute | Verified |
| EVTB-08 | P1: Consumidor — retry com backoff + dead-letter | Execute | Verified |
| EVTB-09 | P1: Broker plugável (interface, sem vazar SDK concreto) | Execute | Verified |
| EVTB-10 | P1: Prova E2E — `WorkoutFinished` publicado e consumido | Execute | Verified |
| EVTB-11 | P2: Dead-letter observável | Execute | Verified |

**ID format:** `EVTB-NN`

**Status values:** Pending → In Design → In Tasks → Implementing → Verified

**Coverage:** 11 total, 10 Verified, 1 N/A (documented)

---

## Success Criteria

- [x] Derrubar o broker (RabbitMQ) no meio de um teste, subir de novo, confirmar que eventos pendentes são entregues sem perda
- [x] Matar o processo da API entre gravar o agregado e o relay publicar — ao subir de novo, o evento pendente ainda está lá e é publicado
- [x] Reenviar manualmente o mesmo evento duas vezes — efeito do consumidor acontece uma vez só
- [x] Trocar o adapter de broker em DI (RabbitMQ → fake) sem tocar em código de `Training` ou do consumidor
- [x] `dotnet test` (unit + integration) verde cobrindo os cenários acima
</content>
