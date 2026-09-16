# Nutrição — Specification

## Problem Statement

A Fase 2 do roadmap (Core Fitness) pede nutrição (alimentos, refeições, macros, micronutrientes) como um dos 4 pilares "motivo de abrir o app todo dia". Hoje o domínio não existe em nenhum dos dois repos (confirmado no Fase 0 assessment — `Nutrition: ⚪ ausente` em back e front) — é greenfield. Dois públicos precisam ser atendidos sem excluir um pelo outro: quem cadastra tudo manualmente (estilo MyFitnessPal, banco de alimentos colaborativo com auditoria/moderação) e quem segue um cardápio fixo (hoje autoral do próprio usuário; a Fase 5/Professionals vai querer o nutricionista prescrevendo, e o modelo de dados não pode exigir retrabalho estrutural quando isso chegar). A feature também precisa se conectar ao motor de gamificação já pronto (`Gamification`, Fase 3, `T1–T19` completo) — bater meta de macro é o "loop central" que dá ao usuário um motivo pra voltar todo dia, do mesmo jeito que treino verificado já credita XP/ShapeCoins hoje.

## Goals

- [ ] Usuário registra o que come ao longo do dia (refeições com alimentos e quantidades), vê macros/micros acumulados do dia
- [ ] Usuário cadastra alimento novo (com ou sem código de barras) e ele fica disponível pra todos imediatamente ("público" desde o primeiro cadastro)
- [ ] Usuário identifica alimento com dado errado, edita, e passa a ver sua própria versão até que a triagem de admin aprove (vira pública) ou recuse (override pessoal permanece, sem virar pública)
- [ ] Usuário monta um cardápio fixo próprio (refeições planejadas com alimentos e quantidades fixas) e consegue substituir um item pontualmente num dia sem alterar o plano
- [ ] Usuário tem meta diária de macros (calculada por TDEE ou definida manualmente) e recebe XP/ShapeCoins/streak nutricional ao bater a meta dentro da tolerância — mesmo mecanismo de recompensa que treino já usa
- [ ] Modelo de dados já reserva o vínculo "cardápio prescrito por profissional" (nullable, não usado até a Fase 5) sem exigir migração estrutural futura
- [ ] Envio de e-mail da plataforma (não só desta feature) pode ser desligado globalmente por um administrador, sem redeploy, via um mecanismo mínimo de feature flag reutilizável

## Out of Scope

| Item | Motivo |
|---|---|
| Nutricionista prescrevendo cardápio de fato (fluxo, UI, permissão) | Depende de `Professionals`/`Credentials`/`Relationships` da Fase 5, que ainda não existe como capability de negócio ativa — esta feature só reserva o campo no modelo de dados |
| Auditoria automática de nomes irregulares/impróprios cadastrados por usuário | Pedido explícito do usuário pra registrar como visão futura, não implementar agora — nenhuma ferramenta de moderação de texto (profanidade, spam) existe ou é construída aqui |
| Lookup automático em base externa de alimentos por código de barras (ex.: Open Food Facts) | Decisão de integração externa nova (rate limit, licença de dados, contrato de API de terceiro) fora do escopo desta spec — cadastro por código de barras nesta versão é sempre feito pelo próprio usuário (ver Assumptions) |
| Cálculo automático de equivalência nutricional na substituição de item (ajuste de quantidade pra bater exatamente os macros originais) | Usuário confirmou: sugestão de equivalente por similaridade de macro entra no MVP, mas como sugestão simples de outro alimento similar — recalcular quantidade pra igualar macro exato é complexidade adicional não pedida agora (ver P1 "Substituição") |
| Loja/resgate de ShapeCoins, catálogo de recompensas | Fase 4 (Monetização), não existe ainda — esta feature só credita, não gasta |
| Achievements/badges específicos de nutrição | `Gamification` já deferiu achievements/badges por completo pra fase própria futura (AD-011) — nutrição não abre exceção pra si mesma |
| App mobile / scanner de código de barras nativo (câmera dedicada, wearable) | Fase 6 (Mobile & Offline) ainda não existe — leitura de código de barras nesta feature é via input manual ou câmera do navegador (Web), não app nativo |
| Receitas compostas (alimento feito de múltiplos ingredientes, calculado automaticamente) | Não mencionado pelo usuário como necessidade do MVP — alimento é sempre uma unidade só (cru, industrializado, ou prato já pronto cadastrado como um item); combinar múltiplos itens acontece no nível da refeição, não de um "alimento composto" reutilizável |
| Revisão/apelação do usuário sobre uma edição recusada pela triagem | Usuário confirmou fluxo: recusa notifica e mantém override pessoal — não existe um segundo round de disputa/apelação nesta versão |
| Sistema de notificação em tempo real da plataforma (WebSocket, presença de usuário conectado, in-app + fallback e-mail) | Gap de plataforma inteira, não desta feature — ver `GAPS.md`. Será especificado em spec própria futura; nesta feature a notificação de recusa usa o canal já existente (e-mail) |
| Feature flags com regra de rollout (percentual, por usuário/segmento, A/B) | Usuário pediu só um liga/desliga global de e-mail — motor de flag genérico com regra de segmentação é escopo muito maior que o pedido; mecanismo entregue aqui é on/off por chave, extensível depois se precisar |
| Tela de administração completa de feature flags (histórico de mudança, múltiplas flags gerenciáveis via UI rica) | MVP entrega o mecanismo (tabela + endpoint + 1 flag real) e uma tela mínima de toggle — não uma central de configuração de plataforma |

---

## Assumptions & Open Questions

| Assumption / decision | Chosen default | Rationale | Confirmed? |
|---|---|---|---|
| Escopo de nutriente obrigatório vs. opcional | Macros (kcal, proteína, carboidrato, gordura) são obrigatórios no cadastro de alimento; micronutrientes (vitaminas, minerais) são campos opcionais, om deles zerados/nulos por padrão | Usuário confirmou explicitamente | y |
| Regra de "bateu a meta diária" (dispara XP/streak) | Os 3 macronutrientes (proteína, carboidrato, gordura) são a meta — juntos determinam kcal, então kcal não é checado separadamente (seria redundante). "Bateu a meta" = os 3 macros, cada um individualmente, dentro de ±10% da meta daquele macro. kcal continua exibido no card do dia como informativo (soma dos 3), mas não entra na regra de gamificação | Usuário corrigiu explicitamente: macro nutrientes como um todo (não kcal isolado) são a meta — kcal é derivado, checá-lo separadamente seria contar a mesma coisa duas vezes. ±10% em cada macro reaproveita a mesma tolerância já validada com o usuário pra kcal | y (correção explícita do usuário) |
| Substituição de item no cardápio fixo | Sistema sugere alimentos equivalentes por similaridade de macro (ranking por menor distância euclidiana normalizada entre os 4 macros do item original e candidatos do banco), mas usuário pode ignorar a sugestão e escolher qualquer alimento livremente (mesmo que ultrapasse a meta do dia) — substituição vale só para aquele dia específico, não altera o cardápio fixo salvo | Usuário confirmou ambas as opções coexistindo | y |
| Origem da meta diária de macros | Sistema calcula por TDEE (Mifflin-St Jeor: peso, altura, idade, sexo biológico, nível de atividade) durante onboarding da feature; usuário pode pular o onboarding e definir a meta manualmente a qualquer momento (e redefinir depois mesmo tendo passado pelo TDEE) | Usuário confirmou ambas as opções | y |
| Dado antropométrico pro cálculo de TDEE (altura, idade, sexo biológico, nível de atividade, peso) | `WeightTracking` (hoje em `Features/Training/WeightTracking`, Mongo — `WeightTargetDocument`/`WeightRegisterDocument`) MIGRA para `Features/Nutrition` por completo (não é leitura cross-domain — é mudança de dono do módulo). Altura/idade/sexo/nível de atividade são novos, vivem junto no mesmo módulo `Nutrition`, formando um único `NutritionProfile` coeso com peso | Usuário confirmou explicitamente: peso/antropometria pertence mais a Nutrição (consome TDEE) do que a Training — mover o módulo em vez de só ler cross-domain evita a divisão artificial de ter "peso" em Training e "o resto do corpo" em Nutrition | y (correção explícita do usuário) |
| Persistência do banco de alimentos e da versão pessoal (override) | Um alimento tem exatamente UMA linha "pública" ativa por vez (nunca duas públicas divergentes) e N overrides pessoais (um por usuário que editou), cada override apontando pra qual alimento público ele se originou. Usuário nunca vê 2 versões simultâneas do mesmo alimento — vê a pública OU a sua própria, com flag indicando qual, e pode alternar | Só assim "unifica na pública" (quando aprovado) e "mantém override pessoal sem afetar a pública" (quando recusado) fazem sentido sem ambiguidade de qual é a fonte da verdade em cada momento | y (decorre diretamente da regra de negócio dada pelo usuário) |
| Concorrência: dois usuários editam o mesmo alimento público ao mesmo tempo | Cada edição gera seu PRÓPRIO override pessoal e sua PRÓPRIA entrada na fila de triagem — não há lock nem merge; overrides de usuários diferentes sobre o mesmo alimento público coexistem independentemente. Se dois overrides do mesmo alimento forem aprovados em momentos diferentes, o segundo aprovado substitui a versão pública deixada pelo primeiro (last-approved-wins), e o autor do primeiro (se ainda tiver override, o que não é o caso pós-aprovação — ele já unificou) não é afetado retroativamente | Evita bloqueio de escrita numa feature colaborativa de baixa criticidade (dado nutricional não é transação financeira) — mesmo espírito de "resolver na prática, não travar o usuário" já usado em outras partes do produto (offline-first) | n — assumption, log apenas |
| Cadastro por código de barras sem lookup externo | Código de barras é só um campo de busca/identificação (string única quando presente); ao ler um código nunca visto, sistema abre o formulário de cadastro vazio pro usuário preencher — não tenta resolver o código contra nenhuma base externa | Ver Out of Scope (lookup externo é decisão de integração separada) | y (decorre do Out of Scope confirmado) |
| Leitura de código de barras na Web | Usa a Barcode Detection API nativa do navegador quando disponível (Chrome/Edge), com fallback pra entrada manual do código quando o navegador não suporta (ex.: Safari/Firefox sem a API) — sem biblioteca JS de terceiro pra decodificação | Evita dependência nova pra uma capability que o próprio navegador já expõe nos browsers majoritários; fallback garante que a feature nunca fica bloqueada em navegador sem suporte | n — assumption, log apenas, revisitar em Design se a cobertura de browser for baixa demais |
| Unidade de quantidade no registro de refeição/diário | Todo alimento é cadastrado com valores nutricionais por 100g (sólido) ou 100ml (líquido) — obrigatório. Opcionalmente o cadastro pode incluir 1 "medida caseira" (ex.: "1 fatia = 30g", "1 copo = 200ml") como atalho de UI. Ao registrar no diário, a quantidade digitada é sempre convertida pra base 100g/100ml pro cálculo (a medida caseira é só conveniência de input, não uma segunda fonte de verdade) | Replica o padrão universal (MyFitnessPal, TACO, rótulos brasileiros) de "por 100g" como base única, evitando ambiguidade de cálculo quando duas medidas caseiras diferentes existissem pro mesmo alimento | n — assumption, log apenas |
| Capability de administrador pra triagem | Reaproveita `PlatformRoleType.Admin` já existente (AD-006, `Authorization`) — nova capability `platform.nutrition.moderate-food-edit`, mesmo padrão de prefixo `platform.*` que já desbloqueou T14/T15/T17/T18 | Reuso do mecanismo nativo de admin já pronto, sem inventar um papel novo | y (decorre do padrão já estabelecido no repo, AD-006) |
| Notificação de recusa de edição | Nesta feature, usa o mecanismo de `Notifications` já existente (email via Resend), sempre. O ideal (usuário conectado/ativo na plataforma recebe in-app via WebSocket, sem e-mail; offline cai pra e-mail) depende de um sistema de notificação em tempo real que NÃO existe hoje pra plataforma inteira (gerenciar usuários conectados, WebSocket, fallback) — registrado como GAP de plataforma (`GAPS.md`), fora do escopo desta feature, a ser especificado em spec própria futura. Quando esse sistema existir, a notificação de recusa passa a usá-lo sem redesenho desta feature (o "evento de recusa" já é um dado estruturado, só troca o canal de entrega) | Usuário confirmou a visão ideal, mas ela é uma capability de plataforma inteira (não só Nutrição) — construir WebSocket+presença só pra esta feature seria escopo errado; email já existente entrega o requisito funcional (usuário é notificado) enquanto o gap de plataforma não é resolvido | y (email agora é decisão confirmada; tempo-real é gap de plataforma, não desta feature) |
| Integração com Gamification | Nutrição PUBLICA um evento de domínio (`NutritionGoalMet`, mesmo padrão de `WorkoutFinished`/`IPublishEndpoint`/MassTransit já estabelecido, AD-009) quando a meta diária é batida pela primeira vez naquele dia; `Gamification` ganha um NOVO consumer (`GamificationNutritionGoalMetConsumer`) que credita XP/ShapeCoins e evolui um streak nutricional PRÓPRIO (separado do streak de treino) — sem reabrir/alterar o consumer de treino já existente | Segue o precedente arquitetural já verificado no repo (evento minimalista + consumer dedicado por domínio de origem); manter streaks separados evita a mesma armadilha já documentada em `Gamification` (AD do streak de dashboard vs. streak de gamificação divergindo, esperado) | y (padrão arquitetural, não preferência nova) |
| Persistência (SQL vs Mongo) por sub-domínio | Decisão técnica de armazenamento fica para a fase de Design (mistura já usada no repo: catálogo relacional + dado de alto-volume em Mongo) — não é uma decisão de produto, não bloqueia o fechamento desta spec | N/A — decisão técnica pura, não uma zona cinzenta de negócio | y (adiada corretamente para Design, não uma ambiguidade de requisito) |
| Registro de refeição (diário) offline (web hoje, mobile na Fase 6) | Segue o MESMO mecanismo já fechado na Fase 1 (`mutationQueue.js`): cliente gera o id da entrada (Mongo ObjectId-shaped, `utils/objectId.js`) e a `Date`/`MealSlot` explícitos no momento do registro (nunca "agora do servidor"); a escrita é enfileirada (`enqueueMutation`) e sincroniza quando a conexão volta — mesmo que isso aconteça num dia calendário diferente do dia em que a refeição foi de fato registrada. O servidor aceita o id e a data como vieram do cliente (mesmo padrão já usado por `StartWorkoutExecutionCommand`/`CreateWorkoutPlanCommand`), tornando o retry idempotente (mesmo id = mesmo efeito). Quando a Fase 6 (mobile) existir, o app nativo implementa o mesmo contrato de fila (id+data gerados no cliente) — sem exigir mudança no endpoint do backend | Usuário pediu explicitamente pra reaproveitar a fila offline já existente em vez de inventar mecanismo novo — mesma razão que motivou `StartWorkoutExecutionCommand` aceitar id do cliente na Fase 1 | y (pedido explícito do usuário) |
| Mecanismo de feature flag global (fora do domínio de Nutrição, mas construído nesta feature a pedido do usuário) | Tabela chave-valor nova (`PlatformFeatureFlag`: `Key` único, `Enabled`, `UpdatedAtUtc`, `UpdatedByUserId`) em `Features/PlatformFeatureFlags` (Feature própria, vertical-slice, SQL — mesmo padrão do resto do backend), lida por qualquer feature via `IFeatureFlagReader.IsEnabledAsync(key)`. Primeira flag real: `notifications.email-enabled` (seed = `true`). `Notifications` (`ResendEmailNotificationSender`) passa a checar essa flag antes de enviar — se desligada, suprime o envio e loga, sem lançar erro pro chamador. Gestão (listar/alternar) exige capability `platform.feature-flags.manage` (mesmo padrão `platform.*`/AD-006), com tela mínima de toggle no admin | Usuário pediu explicitamente essa capability nesta spec, mesmo reconhecendo que não é domínio de Nutrição — key-value é o design mais simples que não precisa ser redesenhado quando a 2ª flag aparecer (qualquer feature futura só chama `IsEnabledAsync` com sua própria chave, sem tocar neste mecanismo) | y (pedido explícito do usuário) |

**Open questions:** nenhuma sem resposta — tudo acima está resolvido ou registrado como assumption.

---

## User Stories

### P1: Cadastro colaborativo de alimento ⭐ MVP

**User Story**: Como usuário, quero cadastrar um alimento que ainda não existe no banco (com nome, macros obrigatórios, micros opcionais, e opcionalmente um código de barras), pra poder registrá-lo nas minhas refeições.

**Why P1**: Sem alimento cadastrado não existe registro de refeição nem cardápio — é a base de tudo.

**Acceptance Criteria**:

1. WHEN o usuário cadastra um alimento com nome e os 4 macros (kcal, proteína, carboidrato, gordura) preenchidos THEN sistema SHALL salvar o alimento com status "público", visível imediatamente pra todos os usuários
2. WHEN o usuário cadastra um alimento sem preencher micronutrientes THEN sistema SHALL aceitar o cadastro normalmente (micros ficam nulos/zerados, nunca bloqueiam o cadastro)
3. WHEN o usuário tenta cadastrar um alimento sem nome ou com algum dos 4 macros ausente/negativo THEN sistema SHALL rejeitar o cadastro com mensagem indicando o campo faltante
4. WHEN o usuário informa um código de barras que já existe em outro alimento cadastrado THEN sistema SHALL rejeitar o cadastro (código de barras é único quando presente) com mensagem indicando que o alimento já existe
5. WHEN o usuário informa um código de barras nunca visto antes THEN sistema SHALL aceitar o cadastro normalmente (ver Assumptions — sem lookup externo)

**Independent Test**: Cadastrar um alimento novo com nome+macros, sem código de barras — aparece na busca de alimentos como público, disponível pra qualquer outro usuário.

---

### P1: Busca de alimento (por nome ou código de barras) ⭐ MVP

**User Story**: Como usuário, quero buscar um alimento já cadastrado por nome ou lendo um código de barras, pra não precisar recadastrar algo que já existe.

**Why P1**: Sem busca, todo alimento vira recadastro duplicado — quebra o propósito de banco colaborativo.

**Acceptance Criteria**:

1. WHEN o usuário busca por texto THEN sistema SHALL retornar alimentos cujo nome contém o texto buscado (case-insensitive), priorizando a versão que o próprio usuário deve ver (sua própria override, se existir; senão a pública — ver P1 "Edição")
2. WHEN o usuário busca por código de barras (leitura via câmera do navegador ou digitação manual) e o código já existe THEN sistema SHALL retornar o alimento correspondente diretamente (sem precisar digitar nome)
3. WHEN o usuário busca por código de barras que não existe THEN sistema SHALL informar "não encontrado" e oferecer o formulário de cadastro pré-preenchido com o código lido
4. WHEN o navegador não suporta leitura nativa de código de barras THEN sistema SHALL permitir a digitação manual do código como alternativa, sem bloquear o fluxo

**Independent Test**: Buscar um alimento existente por parte do nome — aparece na lista; buscar um código de barras inexistente — sistema oferece cadastro com o código já preenchido.

---

### P1: Edição de alimento existente com override pessoal + triagem ⭐ MVP

**User Story**: Como usuário, quero corrigir um dado nutricional que percebo errado num alimento já cadastrado, sem esperar aprovação pra continuar usando o valor correto no meu próprio diário.

**Why P1**: É o mecanismo central de qualidade de dado num banco colaborativo — sem isso, dado errado nunca se corrige e ninguém confia no banco.

**Acceptance Criteria**:

1. WHEN o usuário edita qualquer campo nutricional (macro ou micro) de um alimento público THEN sistema SHALL criar um override pessoal daquele usuário sobre aquele alimento, SEM alterar a versão pública, e SEM afetar o que outros usuários veem
2. WHEN o usuário tem um override pessoal sobre um alimento THEN toda tela onde esse alimento aparece pra ESSE usuário (busca, diário, cardápio) SHALL exibir os valores do override, com uma flag visual indicando "sua versão" (distinta de "versão pública")
3. WHEN o usuário com um override pessoal ativo escolhe explicitamente voltar pra versão pública THEN sistema SHALL passar a exibir a versão pública pra ele novamente (o override continua existindo, só deixa de ser o ativo — pode reativar depois)
4. WHEN um override pessoal é criado THEN sistema SHALL enfileirar essa edição pra triagem administrativa, com status "pendente"
5. WHEN um administrador aprova uma edição pendente THEN sistema SHALL: (a) tornar os valores do override a nova versão pública do alimento; (b) remover a divergência do autor do override (ele passa a ver só a versão pública, que agora é a dele mesmo); (c) marcar a triagem como "aprovada"
6. WHEN um administrador recusa uma edição pendente THEN sistema SHALL: (a) manter o override pessoal do autor intacto e ainda ativo pra ele, se ele quiser continuar usando; (b) NÃO alterar a versão pública; (c) marcar a triagem como "recusada"; (d) notificar o autor da recusa
7. WHEN existem múltiplos overrides pessoais diferentes sobre o MESMO alimento público (usuários diferentes editaram valores diferentes) THEN sistema SHALL tratar cada um independentemente — aprovar ou recusar um não afeta o outro

**Independent Test**: Editar um valor de um alimento público — passa a ver "sua versão" com flag; recusar a edição via triagem — usuário ainda vê sua versão (flag mantida), versão pública inalterada, notificação de recusa recebida. Em outro caso, aprovar a edição — usuário passa a ver só "versão pública" (a nova), sem flag de divergência.

---

### P1: Triagem administrativa de edições de alimento ⭐ MVP

**User Story**: Como administrador ShapeUp, quero revisar edições de alimentos propostas por usuários e aprovar ou recusar cada uma, pra manter a qualidade do banco público.

**Why P1**: É o outro lado da story anterior — sem uma tela/endpoint de triagem, edições ficam pendentes pra sempre.

**Acceptance Criteria**:

1. WHEN um administrador consulta a fila de triagem THEN sistema SHALL listar edições com status "pendente", mostrando o valor público atual lado a lado com o valor proposto
2. WHEN um usuário sem a capability de administrador tenta acessar a fila de triagem ou aprovar/recusar uma edição THEN sistema SHALL retornar 403
3. WHEN uma edição já foi aprovada ou recusada THEN sistema SHALL impedir uma segunda decisão sobre a mesma edição (idempotente — reenvio da mesma ação não duplica efeito, tentativa de mudar uma decisão já tomada é rejeitada)

**Independent Test**: Logar como administrador, ver uma edição pendente com diff público-vs-proposto, aprovar — status muda pra "aprovada" e não aparece mais na fila; tentar aprovar de novo — sistema rejeita (já decidida).

---

### P1: Diário alimentar — registrar refeições do dia ⭐ MVP

**User Story**: Como usuário, quero registrar o que comi em cada refeição do dia (café da manhã, almoço, jantar, lanches), com quantidade de cada alimento, e ver o total de macros/micros acumulado.

**Why P1**: É o loop de uso diário — o "abrir o app todo dia" que a Fase 2 pede.

**Acceptance Criteria**:

1. WHEN o usuário adiciona um alimento a uma refeição do dia com uma quantidade (em gramas/ml, ou por medida caseira quando o alimento tiver uma cadastrada) THEN sistema SHALL calcular e persistir os macros/micros daquele item convertidos pra base 100g/100ml × quantidade
2. WHEN o usuário consulta seu diário de um dia THEN sistema SHALL retornar os itens agrupados por refeição e o total acumulado de macros/micros do dia inteiro
3. WHEN o usuário remove um item já registrado THEN sistema SHALL recalcular o total do dia sem esse item
4. WHEN o usuário registra um item que usa a VERSÃO PESSOAL (override) de um alimento THEN sistema SHALL usar os valores do override no cálculo (consistente com a story de Edição)
5. WHEN o usuário consulta um dia sem nenhum registro THEN sistema SHALL retornar o dia vazio (zero em todos os totais), nunca erro
6. WHEN o usuário registra uma refeição offline (sem conexão) THEN cliente SHALL enfileirar o registro (mesmo mecanismo de `mutationQueue.js` já usado por outras escritas do app, id+data gerados no cliente) e sincronizar quando a conexão voltar, mesmo que isso ocorra num dia calendário diferente — sistema SHALL persistir a entrada na `Date` informada pelo cliente (a refeição que ela representa), nunca na data do momento da sincronização

**Independent Test**: Registrar 3 alimentos em 2 refeições diferentes num dia — total do dia bate com a soma manual; remover um item — total recalcula corretamente. Em outro caso, simular offline, registrar uma refeição de "ontem", reconectar hoje — a entrada aparece no diário de ontem, não no de hoje.

---

### P1: Meta diária de macros (TDEE ou manual) ⭐ MVP

**User Story**: Como usuário, quero ter uma meta diária de kcal/proteína/carboidrato/gordura, calculada automaticamente a partir dos meus dados (peso, altura, idade, sexo, atividade) ou definida por mim manualmente, pra saber se estou no caminho certo.

**Why P1**: Sem meta não existe "bater a meta" — pré-requisito direto da gamificação desta feature.

**Acceptance Criteria**:

1. WHEN o usuário completa o onboarding nutricional (informa altura, idade, sexo biológico, nível de atividade — peso é lido do `WeightTracking` já existente) THEN sistema SHALL calcular a meta diária de macros via fórmula Mifflin-St Jeor + fator de atividade, e salvá-la como meta ativa
2. WHEN o usuário opta por pular o onboarding THEN sistema SHALL permitir que ele defina a meta diária manualmente (kcal/proteína/carboidrato/gordura), sem exigir nenhum dado antropométrico
3. WHEN o usuário já tem uma meta (calculada ou manual) e define uma nova meta manualmente THEN sistema SHALL substituir a meta ativa pela nova, mantendo o histórico de dias passados inalterado (dias já fechados usam a meta que estava ativa no dia, não a meta atual)
4. WHEN o usuário tenta registrar refeição sem ter nenhuma meta definida ainda THEN sistema SHALL permitir o registro normalmente (meta é opcional pro diário funcionar, só é necessária pra gamificação avaliar "bateu a meta")

**Independent Test**: Completar onboarding com dados plausíveis — meta calculada aparece coerente (kcal na faixa esperada pro perfil); pular onboarding e definir meta manual — meta salva exatamente como digitada.

---

### P1: Migração do módulo de peso (`WeightTracking`) de `Training` para `Nutrition` ⭐ MVP

**User Story**: Como sistema, quero que peso corporal e o resto dos dados antropométricos (altura, idade, sexo, atividade) vivam no mesmo módulo (`Nutrition`), já que TDEE precisa dos dois juntos.

**Why P1**: Pré-requisito técnico da story de Meta diária (TDEE) — usuário confirmou explicitamente que peso pertence mais a Nutrição do que a Training; sem mover o módulo agora, a feature nasceria com o dado antropométrico dividido em dois domínios.

**Acceptance Criteria**:

1. WHEN a migração é concluída THEN `WeightTargetDocument`/`WeightRegisterDocument` (e seus handlers `UpsertTargetWeight`/`UpsertDailyWeightRegister`/`GetWeightRegisters`) SHALL viver em `Features/Nutrition` (não mais em `Features/Training`), unidos no mesmo `NutritionProfile` que carrega altura/idade/sexo/nível de atividade
2. WHEN dados de peso já existentes (produção/dev atual) THEN sistema SHALL preservá-los na migração (migração de dado, não recriação vazia da coleção)
3. WHEN o frontend chama peso THEN sistema SHALL expor as rotas sob `/api/nutrition/weight/*` (não mais `/api/training/weight/*`), com `useTrainingApi.js` perdendo as 3 funções de peso (`upsertTargetWeight`, `upsertDailyWeightRegister`, `getWeightRegisters`) e `useNutritionApi.js` (hook novo desta feature) ganhando-as, mesma assinatura
4. WHEN qualquer tela hoje consumindo peso via `useTrainingApi` (dashboard, gráfico de evolução de peso) THEN sistema SHALL atualizar o import pro novo hook, sem quebrar a tela

**Independent Test**: Após a migração, registrar um peso via `useNutritionApi` — aparece corretamente no gráfico/tela que antes lia de `useTrainingApi`; rota antiga (`/api/training/weight/*`) deixa de existir.

---

### P1: Gamificação — meta batida credita XP/ShapeCoins e evolui streak nutricional ⭐ MVP

**User Story**: Como usuário, quero ganhar XP/ShapeCoins e ver meu streak nutricional evoluir quando bato minha meta diária de macros, do mesmo jeito que já ganho por treino.

**Why P1**: É o gancho de retenção que a Fase 2 exige explicitamente ("design tem que ser viciante") — sem isso a meta é só um número, sem motivo pra voltar.

**Acceptance Criteria**:

1. WHEN o dia do usuário fecha (ver Edge Cases — definição de "fechamento" do dia) e os 3 macros (proteína, carboidrato, gordura) registrados estão, cada um, dentro de ±10% da meta daquele macro (ver Assumptions) THEN sistema SHALL publicar um evento `NutritionGoalMet` (mesmo padrão `IPublishEndpoint`/MassTransit de `WorkoutFinished`)
2. WHEN `Gamification` consome `NutritionGoalMet` pela primeira vez para aquele dia/usuário THEN sistema SHALL creditar XP e ShapeCoins (valores flat, mesmo espírito do AD de `Gamification` — reajustáveis sem migração) e incrementar um streak nutricional distinto do streak de treino
3. WHEN o mesmo `NutritionGoalMet` é entregue mais de uma vez (redelivery do bus) THEN sistema SHALL processar o crédito uma única vez (mesma garantia de idempotência já usada em `Gamification`/`WorkoutFinished`)
4. WHEN o usuário NÃO bate a meta num dia (ou não registra nada) THEN sistema SHALL NÃO publicar evento nenhum pra esse dia (o job de avaliação só publica quando bate, nunca "publica uma perda") — o streak exibido é DERIVADO na leitura (`GET /me`/ranking): se a última meta batida (`LastNutritionGoalMetDate`) não é ontem nem hoje, o streak mostrado é 0, mesmo que a coluna armazenada ainda tenha o último valor positivo. Nenhum job/evento/checagem extra é necessário só pra "detectar" a perda — ela nunca precisa ser detectada ativamente, só deixa de ser renovada

**Independent Test**: Registrar refeições que fecham dentro da tolerância da meta num dia — XP/ShapeCoins aumentam exatamente uma vez, streak nutricional sobe; um dia sem bater a meta — streak nutricional volta a 0.

---

### P1: Cardápio fixo — criar e seguir um plano de refeições ⭐ MVP

**User Story**: Como usuário que prefere não ficar cadastrando tudo todo dia, quero montar um cardápio fixo (refeições com alimentos e quantidades definidas) e usá-lo como base do meu diário diário.

**Why P1**: Atende o segundo público explicitamente pedido pelo usuário (PM) — sem isso, só o público "MyFitnessPal" está coberto.

**Acceptance Criteria**:

1. WHEN o usuário cria um cardápio fixo com uma ou mais refeições, cada uma com alimentos e quantidades THEN sistema SHALL salvar o cardápio associado a ele, com um campo `PrescribedByRelationshipId` nulo (reservado, não usado nesta feature — ver Assumptions/Fase 5)
2. WHEN o usuário ativa um cardápio fixo pro dia de hoje THEN sistema SHALL preencher o diário do dia automaticamente com os itens do cardápio (mesmo efeito de macro/micro que um registro manual produziria)
3. WHEN o usuário tem um cardápio ativo e edita um item DIRETO no diário do dia (sem passar pela tela de "substituição", story seguinte) THEN sistema SHALL tratar como um registro normal do diário daquele dia — o cardápio fixo salvo não é alterado

**Independent Test**: Criar um cardápio com 3 refeições, ativar pro dia — diário do dia aparece automaticamente preenchido com os mesmos totais do cardápio.

---

### P1: Substituição de item do cardápio fixo num dia específico ⭐ MVP

**User Story**: Como usuário com cardápio fixo, quero trocar um item por outro equivalente num dia que estou fora de casa, sem precisar editar meu cardápio salvo.

**Why P1**: Pedido explícito do usuário (PM) — é o que torna o cardápio fixo utilizável na vida real, não só numa condição ideal.

**Acceptance Criteria**:

1. WHEN o usuário pede pra substituir um item do dia (originado do cardápio ativo) THEN sistema SHALL sugerir alimentos do banco ranqueados por similaridade de macro (menor distância entre os 4 macros do item original e do candidato), sem alterar automaticamente nada até o usuário confirmar
2. WHEN o usuário escolhe uma das sugestões THEN sistema SHALL substituir o item apenas no diário DAQUELE dia (o cardápio fixo salvo permanece intacto)
3. WHEN o usuário ignora as sugestões e busca/escolhe qualquer outro alimento livremente (mesmo que ultrapasse a meta do dia) THEN sistema SHALL aceitar a substituição normalmente, sem bloquear por causa da meta
4. WHEN a substituição é confirmada THEN sistema SHALL recalcular o total do dia com o novo item no lugar do original

**Independent Test**: Ativar um cardápio, substituir um item por uma sugestão do sistema — total do dia reflete o novo item, cardápio salvo inalterado; substituir por um item escolhido livremente que estoura a meta — sistema aceita sem bloquear.

---

### P1: Frontend — telas de diário, cadastro/edição de alimento, cardápio e progresso de meta ⭐ MVP

**User Story**: Como usuário, quero fazer tudo isso (registrar refeição, cadastrar/editar alimento, ler código de barras, montar cardápio, ver minha meta e progresso) direto no app, sem depender de nenhuma chamada de API "invisível".

**Why P1**: Mesma razão já registrada em `Gamification` — backend sem tela não entrega valor de produto.

**Acceptance Criteria**:

1. WHEN o usuário abre a tela de diário do dia THEN interface SHALL mostrar as refeições do dia, os itens de cada uma, e um resumo visual de progresso de macro (barra/anel) comparando consumido vs. meta — usando os componentes visuais já existentes no design system (`Card`, tokens de cor, sem lib nova)
2. WHEN o usuário busca ou lê um código de barras THEN interface SHALL usar a Barcode Detection API do navegador quando disponível, com fallback de digitação manual (ver Assumptions)
3. WHEN o usuário está vendo um alimento com override pessoal ativo THEN interface SHALL exibir a flag "sua versão" com opção de alternar pra versão pública
4. WHEN o usuário bate a meta do dia THEN interface SHALL mostrar uma celebração visual simples (reforço do momento, mesmo espírito "viciante" pedido — sem exigir motor de achievement novo, é feedback imediato de UI) e refletir XP/ShapeCoins/streak nutricional atualizados
5. WHEN o usuário monta ou ativa um cardápio fixo THEN interface SHALL oferecer a tela de gestão do cardápio e o atalho de substituição de item a partir do diário do dia

**Independent Test**: Abrir a tela de diário com registros do dia — progresso visual bate com os totais reais; ler um código de barras existente via câmera (browser compatível) — retorna o alimento correto sem digitação.

---

### P2: Feature flag global — desligar envio de e-mail da plataforma

**User Story**: Como administrador ShapeUp, quero desligar o envio de e-mail da plataforma inteira (não só de Nutrição) sem precisar de um redeploy, pra ter um kill-switch em caso de incidente (ex.: provedor de e-mail com problema, envio em loop, custo disparando).

**Why P2**: Não bloqueia o loop central de Nutrição (registrar refeição, bater meta) — é uma capability operacional de plataforma que o usuário pediu pra entregar dentro desta spec por conveniência, não porque é dependência de outra story P1 daqui.

**Acceptance Criteria**:

1. WHEN um administrador desliga a flag `notifications.email-enabled` THEN sistema SHALL parar de enviar qualquer e-mail via `Notifications` (incluindo a notificação de recusa de edição desta feature) até a flag ser reativada
2. WHEN a flag está desligada e algum fluxo tentaria enviar e-mail THEN sistema SHALL suprimir o envio silenciosamente do ponto de vista do chamador (registra em log que foi suprimido, mas NÃO lança erro nem interrompe o fluxo que pediu o envio — ex.: recusa de edição continua sendo processada normalmente, só sem o e-mail)
3. WHEN um usuário sem a capability `platform.feature-flags.manage` tenta consultar ou alterar qualquer flag THEN sistema SHALL retornar 403
4. WHEN a plataforma consulta uma flag que nunca foi configurada (linha não existe na tabela) THEN sistema SHALL tratar como HABILITADA por padrão (fail-open — ausência de configuração nunca deve silenciosamente desligar um canal que ninguém desligou de propósito)
5. WHEN um administrador acessa a tela mínima de feature flags THEN interface SHALL listar as flags existentes (nome + estado atual) com um toggle por flag

**Independent Test**: Desligar `notifications.email-enabled`, recusar uma edição de alimento (story de Triagem) — edição é recusada normalmente, mas nenhum e-mail é enviado (log confirma supressão); religar a flag — próxima recusa envia e-mail normalmente.

---

### P2: Exclusão de alimento por administrador

**User Story**: Como administrador ShapeUp, quero excluir um alimento do catálogo público (ex.: nome impróprio, duplicata clara, spam), pra manter a qualidade do banco colaborativo.

**Why P2**: Não bloqueia o loop central (cadastrar/registrar/bater meta) — é uma ferramenta de curadoria complementar à triagem de edição, mesmo público administrador.

**Acceptance Criteria**:

1. WHEN um administrador com a capability de moderação exclui um alimento THEN sistema SHALL marcá-lo como excluído (soft-delete — nunca aparece mais em busca nem pode ser usado em NOVO registro de diário/cardápio), SEM apagar a linha fisicamente
2. WHEN um usuário sem a capability tenta excluir um alimento THEN sistema SHALL retornar 403 (mesma capability de triagem — `platform.nutrition_foods.moderate` — administradores ShapeUp são o público esperado)
3. WHEN um alimento excluído já foi usado em registros PASSADOS do diário THEN sistema SHALL preservar esses registros normalmente (macros já foram calculados e gravados no momento do registro — exclusão do alimento não afeta histórico já persistido)
4. WHEN um cardápio fixo ATIVO referencia um alimento que foi excluído THEN sistema SHALL, ao tentar ativar/aplicar esse cardápio, sinalizar o item como indisponível e oferecer a mesma tela de substituição já existente (P1 "Substituição de item"), nunca aplicar silenciosamente um alimento que não existe mais
5. WHEN um administrador tenta excluir um alimento já excluído THEN sistema SHALL tratar como no-op idempotente (não erro)

**Independent Test**: Excluir um alimento sem uso — some da busca; excluir um alimento já registrado num diário passado — o registro antigo continua mostrando os macros normalmente; excluir um alimento usado num cardápio fixo ativo — próxima ativação do cardápio sinaliza o item e oferece substituição.

---

## Edge Cases

- WHEN o "fechamento do dia" pra fins de avaliação de meta é definido THEN sistema SHALL considerar o dia fechado à meia-noite UTC do dia seguinte (mesmo referencial de fuso já usado pelo streak de `Gamification`, AD do domínio) — nenhuma avaliação de meta acontece em tempo real durante o dia, só no fechamento
- WHEN o usuário edita o total de um dia JÁ FECHADO (ex.: esqueceu de registrar algo de ontem) THEN sistema SHALL permitir o registro retroativo, mas SHALL NÃO reavaliar/reemitir `NutritionGoalMet` retroativamente pro streak (streak só reage ao fechamento do dia corrente, nunca corrige o passado)
- WHEN um alimento tem override pessoal aprovado (virou público) e o MESMO usuário edita de novo depois THEN sistema SHALL tratar como uma nova edição/override do zero (novo ciclo de triagem), não uma continuação da anterior
- WHEN um alimento é a única fonte de um item usado em cardápios fixos de outros usuários e sofre uma edição aprovada (nova versão pública) THEN sistema SHALL refletir o novo valor público em qualquer cardápio/diário futuro que referencie esse alimento (a referência é ao alimento, não a um snapshot do valor no momento do cadastro do cardápio) — dias JÁ FECHADOS no diário mantêm o valor histórico calculado na época (ver Assumptions da meta — consistente com "dias fechados usam a meta que estava ativa")
- WHEN o usuário não tem nenhum override e nenhuma triagem pendente, e apenas consulta um alimento público comum THEN sistema SHALL retornar normalmente sem nenhuma flag de versão (comportamento default, sem indicação nenhuma)
- WHEN o cálculo de TDEE recebe dado antropométrico fora de faixa plausível (ex.: altura 0, idade negativa) THEN sistema SHALL rejeitar o onboarding com mensagem de validação, nunca calcular uma meta absurda silenciosamente

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
|---|---|---|---|
| NUT-01 | P1: Cadastro colaborativo de alimento | Design | Pending |
| NUT-02 | P1: Busca de alimento (nome/código de barras) | Design | Pending |
| NUT-03 | P1: Edição com override pessoal + triagem | Design | Pending |
| NUT-04 | P1: Triagem administrativa | Design | Pending |
| NUT-05 | P1: Diário alimentar — registrar refeições | Design | Pending |
| NUT-06 | P1: Meta diária de macros (TDEE ou manual) | Design | Pending |
| NUT-07 | P1: Migração `WeightTracking` (Training → Nutrition) | Design | Pending |
| NUT-08 | P1: Gamificação — meta batida credita XP/ShapeCoins/streak | Design | Pending |
| NUT-09 | P1: Cardápio fixo — criar e seguir | Design | Pending |
| NUT-10 | P1: Substituição de item do cardápio num dia | Design | Pending |
| NUT-11 | P1: Frontend — diário, cadastro/edição, cardápio, progresso | Design | Pending |
| NUT-12 | P2: Feature flag global — desligar e-mail | Design | Pending |
| NUT-13 | P2: Exclusão de alimento por administrador (soft-delete) | Design | Pending |

**ID format:** `NUT-NN`

**Status values:** Pending → In Design → In Tasks → Implementing → Verified

**Coverage:** 13 total, 0 mapped to tasks, 13 unmapped ⚠️ (Tasks phase ainda não rodou)

---

## Success Criteria

- [ ] Usuário cadastra um alimento novo (com ou sem código de barras) e ele aparece imediatamente como público pra outros usuários
- [ ] Usuário edita um alimento, vê sua própria versão com flag, e a triagem de admin aprova/recusa corretamente (aprovado unifica na pública; recusado mantém override + notifica)
- [ ] Usuário registra refeições do dia e vê o total de macros/micros corretamente calculado e recalculado a cada mudança
- [ ] Usuário com cardápio fixo ativa o plano, vê o diário preenchido automaticamente, e substitui um item pontualmente sem alterar o cardápio salvo
- [ ] Usuário bate a meta diária (dentro da tolerância) e vê XP/ShapeCoins/streak nutricional refletidos, coerente com o mecanismo já existente de `Gamification`
- [ ] Nenhuma tela nova roda sobre dado mockado (mesmo padrão de rigor já cobrado no Fase 0 assessment)
- [ ] Administrador desliga a flag de e-mail e nenhum e-mail sai da plataforma até religar, sem quebrar os fluxos que tentam enviar
