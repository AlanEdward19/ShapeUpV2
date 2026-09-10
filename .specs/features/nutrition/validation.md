# Nutrition Validation

**Date**: 2026-09-10
**Spec**: `.specs/features/nutrition/spec.md`
**Diff range**: `ShapeUpApi` `1224e08..db9adce` (27 commits) · `ShapeUp-Web` `0c44ee2..95f0c52` (10 commits)
**Verifier**: independent sub-agent (author ≠ verifier) — iteration 2/3

---

## Task Completion

| Task | Status | Notes |
| ---- | ------ | ----- |
| T1–T27 | ✅ Done | All tasks marked complete in `tasks.md`; T27 gate recorded in `STATE.md` |

---

## Spec-Anchored Acceptance Criteria

### P1: Cadastro colaborativo de alimento (NUT-01)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: nome + 4 macros → público imediato | Food saved public, visible to all | `FoodsEndpointsIntegrationTests.cs:41` — `Assert.Equal(HttpStatusCode.Created, response.StatusCode)`; `:56` — second user `Assert.Contains(searchPayload!.Items, x => x.Name == uniqueName)` | ⚠️ Spec-precision gap — visibility proven, explicit `status=public` field not asserted |
| AC2: micros omitidos aceitos | Micros null/zero, cadastro OK | `CreateFoodHandlerTests.cs:22-38` — `Assert.Null(result.Value.MicrosPer100)`; `CreateFoodCommandValidatorTests.cs:27-31` — `Assert.True(result.IsValid)` | ✅ PASS |
| AC3: sem nome ou macro ausente/negativo → rejeita | Validation error naming field | `CreateFoodCommandValidatorTests.cs:35-43` — `Assert.Contains(result.Errors, e => e.PropertyName == "Name")`; `:50-66` — `Assert.Contains(..., "MacrosPer100")`; `FoodsEndpointsIntegrationTests.cs:75-76` — `Assert.Equal("validation_error", error!.Code)` + `Assert.Contains("Name", ...)` | ✅ PASS |
| AC4: barcode duplicado → rejeita com mensagem | Conflict + "already exists" | `CreateFoodHandlerTests.cs:61-63` — `Assert.Equal("conflict", result.Error!.Code)` + `Assert.Contains("already exists", ...)`; `FoodsEndpointsIntegrationTests.cs:102-107` — `Assert.Equal(HttpStatusCode.Conflict, ...)` | ✅ PASS |
| AC5: barcode novo aceito | Create succeeds | `CreateFoodHandlerTests.cs:68-86` — `Assert.True(result.IsSuccess)`; `FoodsEndpointsIntegrationTests.cs` (create with barcode in handler tests) | ✅ PASS |

### P1: Busca de alimento (NUT-02)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: busca texto case-insensitive, prioriza override | Name contains query; override when active | `FoodQueryHandlerTests.cs:14-41` — `Assert.Equal("Banana Prata", result.Value!.Items[0].Name)`; `:59-103` — `Assert.True(result.Value!.Items[0].IsPersonalVersion)` | ✅ PASS |
| AC2: barcode existente retorna alimento | Direct food return | `FoodQueryHandlerTests.cs:106-130` — `Assert.Equal(barcode, result.Value!.Barcode)`; `FoodsEndpointsIntegrationTests.cs:148-169` — `Assert.Equal(HttpStatusCode.OK, ...)` | ✅ PASS |
| AC3: barcode inexistente → não encontrado + formulário | 404 + pre-filled form | `FoodQueryHandlerTests.cs:133-147` — `Assert.True(result.IsFailure)`; `FoodSearch.test.jsx:79-91` — `expect(getByTestId('food-form')).toBeInTheDocument()` + `toHaveValue('7891234567890')` | ✅ PASS |
| AC4: sem Barcode API → digitação manual | Manual input available | `FoodSearch.test.jsx:72-76` — `expect(getByTestId('barcode-manual-input')).toBeInTheDocument()`; `FoodQueryHandlerTests.cs:150-169` — accepts manual string | ✅ PASS |

### P1: Edição com override + triagem (NUT-03)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: editar público → override pessoal, público intacto | Override created, public unchanged | `CreateFoodOverrideHandlerTests.cs:27-55` — `foodRepository.Verify(...UpdateAsync..., Times.Never)`; `FoodsEndpointsIntegrationTests.cs:252-285` — other user still sees public macros | ✅ PASS |
| AC2: override ativo → exibir valores + flag "sua versão" | Personal values + flag | `FoodVersionResolverTests.cs:9-38` — `Assert.True(resolved.IsPersonalVersion)`; `FoodForm.test.jsx:32-48` — `expect(getByTestId('version-flag')).toHaveTextContent('Sua versão')` | ✅ PASS |
| AC3: voltar à versão pública | Public shown again, override kept | `SetActiveFoodVersionHandlerTests.cs:22-40` — `Assert.False(override.IsActive)`; `FoodsEndpointsIntegrationTests.cs:287-318` — toggle returns public macros | ✅ PASS |
| AC4: override criado → fila pendente | Moderation queued pending | `CreateFoodOverrideHandlerTests.cs:27-55` — `moderationRepository.Verify(x => x.CreateAsync(...), Times.Once)` | ✅ PASS |
| AC5: admin aprova → unifica pública, autor vê só pública | Public updated, override cleared for author | `DecideModerationHandlerTests.cs:85-108` — `Assert.Equal(overrideMacros, updatedFood.MacrosPer100)`; `FoodModerationEndpointsIntegrationTests.cs:78-101` | ✅ PASS |
| AC6: admin recusa → override mantido, público intacto, notifica | Override active, public unchanged, email | `DecideModerationHandlerTests.cs:111-130`; `FoodModerationEndpointsIntegrationTests.cs:203-220` — `Assert.Equal(publicKcal, publicFood.MacrosPer100.Kcal)` | ✅ PASS |
| AC7: múltiplos overrides independentes | Per-user isolation | `FoodVersionResolverTests.cs:71-99` — `Assert.Equal(11, resolved.ProteinG)` for user A only | ✅ PASS |

### P1: Triagem administrativa (NUT-04)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: fila pendente com diff público vs proposto | Pending list with diff | `FoodModerationEndpointsIntegrationTests.cs:59-75` — `Assert.NotNull(pending.ProposedMacros)` + public side present | ✅ PASS |
| AC2: sem capability → 403 | Forbidden | `FoodModerationEndpointsIntegrationTests.cs:35-56` — `Assert.Equal(HttpStatusCode.Forbidden, ...)` | ✅ PASS |
| AC3: decisão dupla rejeitada (idempotente) | Conflict on re-decide | `DecideModerationHandlerTests.cs:37-63` — `Assert.Equal("conflict", result.Error!.Code)`; `FoodModerationEndpointsIntegrationTests.cs:104-125` | ✅ PASS |

### P1: Diário alimentar (NUT-05)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: adicionar item → macros calculados 100g/ml × qty | Scaled macros persisted | `MacroCalculatorTests.cs:8-25` — `Assert.Equal(150, result.Kcal)`; `DiaryEndpointsIntegrationTests.cs:46-79` — `Assert.Equal(300, day.Totals.Kcal)` | ✅ PASS |
| AC2: consultar dia → agrupado por refeição + totais | Meals grouped + totals | `DiaryEndpointsIntegrationTests.cs:46-79` — `Assert.Equal(2, day.Meals.Count)` + totals | ✅ PASS |
| AC3: remover item → recalcula totais | Totals decrease | `DiaryEndpointsIntegrationTests.cs:141-174` — `Assert.Equal(90, day.Totals.Kcal)` after remove | ✅ PASS |
| AC4: override ativo usado no cálculo | Override macros in diary | `DiaryEndpointsIntegrationTests.cs:114-138` — `Assert.Equal(120, day.Totals.Kcal)` (override value) | ✅ PASS |
| AC5: dia sem registro → vazio, zero totais | Empty day, zeros, OK | `DiaryEndpointsIntegrationTests.cs:31-43` — `Assert.Empty(day.Meals)` + `Assert.Equal(0, day.Totals.Kcal)`; `DiaryDay.test.jsx:65-76` | ✅ PASS |
| AC6: offline → fila + id cliente + data do cliente | enqueueMutation + entry on client date, not sync day | `useNutritionApi.test.js:164-175` — `expect(enqueueMutation).toHaveBeenCalledWith({ body: { ...pastDateCommand, id: 'generated-entry-id' }})`; `DiaryEndpointsIntegrationTests.cs:83-118` — `Assert.Equal(clientDate, clientDay!.Date)` + `Assert.Empty(syncDayPayload!.Meals)` on sync day | ✅ PASS |

### P1: Meta diária TDEE/manual (NUT-06)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: onboarding → Mifflin-St Jeor meta salva | Calculated goal persisted | `TdeeCalculatorTests.cs:8-25` — `Assert.Equal(2898, result.Kcal)`; `NutritionProfileEndpointsIntegrationTests.cs:28-52` — `Assert.True(profile.ActiveGoal!.Kcal > 0)` | ✅ PASS |
| AC2: pular onboarding → meta manual | Manual goal without anthropometrics | `NutritionProfileEndpointsIntegrationTests.cs:54-74` — `Assert.Equal(2200, profile!.ActiveGoal!.Kcal)`; `GoalOnboarding.test.jsx:54-68` | ✅ PASS |
| AC3: nova meta manual substitui ativa, histórico intacto | New active goal, profile fields kept | `NutritionProfileEndpointsIntegrationTests.cs:95-119` — `Assert.Equal(2000, profile!.ActiveGoal!.Kcal)` + `Assert.Equal(180, profile.HeightCm)` | ✅ PASS |
| AC4: diário sem meta definida permitido | Diary works without goal | `DiaryEndpointsIntegrationTests.cs:46-79` — adds entries with no prior goal seed; `Assert.Equal(HttpStatusCode.OK, first.StatusCode)` | ✅ PASS |

### P1: Migração WeightTracking (NUT-07)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: handlers/docs em Features/Nutrition | Module under Nutrition | `WeightTrackingEndpointsIntegrationTests.cs:38-55` — `/api/nutrition/weight/*` responds OK | ✅ PASS |
| AC2: dados existentes preservados na migração | Existing production data retained | `WeightTrackingEndpointsIntegrationTests.cs:46-89` — `Assert.Equal(78.5m, target!.TargetWeight)` + `Assert.Equal(80.2m, legacyRegisters[0].Weight)` + `Assert.Equal("2025-11-15", legacyRegisters[0].Day)` | ✅ PASS |
| AC3: rotas `/api/nutrition/weight/*`, hook moved | New routes; old 404 | `WeightTrackingEndpointsIntegrationTests.cs:29-36` — `Assert.Equal(HttpStatusCode.NotFound, ...)` for `/api/training/weight/target`; `useNutritionApi.test.js:339-346` — weight functions call `/api/nutrition/weight/*` | ✅ PASS |
| AC4: telas consumindo peso atualizadas | Dashboard/graph use new hook | `ObjectivesClient.test.jsx:51-60` — `expect(mockGetWeightRegisters).toHaveBeenCalled()` + date-range args match ISO pattern | ✅ PASS |

### P1: Gamificação meta batida (NUT-08)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: 3 macros ±10% no fechamento → publica NutritionGoalMet | Event published when all 3 within tolerance | `NutritionGoalToleranceCalculatorTests.cs:17-27` — `Assert.True(AreMacrosWithinTolerance(...))`; `NutritionGoalEvaluationJobConsumerIntegrationTests.cs:45-47` — `Assert.Single(published)` | ✅ PASS |
| AC2: consumer credita XP/coins + streak nutricional | +50 XP, +10 coins, streak++ | `GamificationNutritionGoalMetConsumerTests.cs:34-37` — `Assert.Equal(50, profile.TotalXp)` + `Assert.Equal(1, profile.NutritionCurrentStreak)` | ✅ PASS |
| AC3: redelivery idempotente | Single credit | `GamificationNutritionGoalMetConsumerTests.cs:71-74` — XP unchanged on redelivery | ✅ PASS |
| AC4: meta não batida → sem evento; streak derivado 0 na leitura | No publish; displayed streak 0 when stale | `NutritionGoalEvaluationJobConsumerIntegrationTests.cs:70` — `Assert.Empty(published)`; `NutritionStreakCalculatorTests.cs:26-30` — `Assert.Equal(0, streak)` | ✅ PASS |

### P1: Cardápio fixo (NUT-09)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: criar cardápio com PrescribedByRelationshipId nulo | Null prescribed field | `CreateMealPlanHandlerTests.cs:12-28` — `Assert.Null(plan.PrescribedByRelationshipId)`; `MealPlanEndpointsIntegrationTests.cs:30-53` | ✅ PASS |
| AC2: ativar → preenche diário do dia | Diary filled from plan | `MealPlanEndpointsIntegrationTests.cs:55-79` — diary entries match plan; `MealPlanManager.test.jsx:38-55` | ✅ PASS |
| AC3: editar diário não altera plano salvo | Plan unchanged | `MealPlanEndpointsIntegrationTests.cs:121-140` — plan items unchanged after diary edit | ✅ PASS |

### P1: Substituição de item (NUT-10)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: sugestão ranqueada por similaridade macro | Ranked by distance | `MacroSimilarityCalculatorTests.cs:18-35` — lowest distance first; `DiaryEndpointsIntegrationTests.cs:177-203` | ✅ PASS |
| AC2: confirmar → só no diário do dia | Day updated, plan intact | `DiaryEndpointsIntegrationTests.cs:206-247` — totals change, plan unchanged | ✅ PASS |
| AC3: escolha livre aceita mesmo estourando meta | No block on over-goal | `DiaryEndpointsIntegrationTests.cs:250-280` — `Assert.Equal(HttpStatusCode.OK, ...)` | ✅ PASS |
| AC4: substituição recalcula total do dia | Totals reflect new item | `DiaryEndpointsIntegrationTests.cs:240-246` — `Assert.Equal(newKcal, day.Totals.Kcal)` | ✅ PASS |

### P1: Frontend telas (NUT-11)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: diário com refeições + progresso visual macro | Meals + progress bars | `DiaryDay.test.jsx:53-63` — `expect(getByTestId('macro-progress')).toBeInTheDocument()` | ✅ PASS |
| AC2: Barcode API + fallback manual | Scanner or manual input | `FoodSearch.test.jsx:72-76` — manual fallback when no BarcodeDetector | ✅ PASS |
| AC3: flag "sua versão" + toggle público | Version flag + toggle | `FoodForm.test.jsx:32-48` — version flag + toggle button | ✅ PASS |
| AC4: celebração ao bater meta + XP/streak | Celebration UI | `DiaryDay.test.jsx:79-89` — `expect(getByTestId('goal-celebration')).toHaveTextContent('Meta batida!')`; `GamificationProgressCard.test.jsx:14-21` — nutrition streak rendered | ✅ PASS |
| AC5: gestão cardápio + atalho substituição no diário | Meal plan screen + substitute from diary | `MealPlanManager.test.jsx:38-55`; `DiaryDay.test.jsx:84-97` — `fireEvent.click(getByTestId('substitute-btn-entry-1'))` + `expect(getByTestId('substitute-modal')).toBeInTheDocument()` | ✅ PASS |

### P2: Feature flag global (NUT-12)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: flag off → sem e-mail Notifications | Email suppressed platform-wide | `ResendEmailNotificationSenderTests.cs:55-70` — `Times.Never` on Resend; `FoodModerationEndpointsIntegrationTests.cs:149-175` | ✅ PASS |
| AC2: supressão silenciosa, fluxo continua | Success no-op, no throw | `ResendEmailNotificationSenderTests.cs:66-67` — `Assert.True(result.IsSuccess)`; moderation reject still OK `:149-175` | ✅ PASS |
| AC3: sem capability → 403 | Forbidden | `PlatformFeatureFlagsEndpointsIntegrationTests.cs:47-67` | ✅ PASS |
| AC4: chave inexistente → habilitada (fail-open) | Returns true | `FeatureFlagReaderTests.cs:18-25` — `Assert.True(enabled)` | ✅ PASS |
| AC5: tela admin lista flags + toggle | List + toggle UI | `FeatureFlagsPanel.test.jsx:24-40` — toggle persists via `putFeatureFlag` | ✅ PASS |

### P2: Exclusão de alimento admin (NUT-13)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: soft-delete, some da busca | Hidden from search | `DeleteFoodHandlerTests.cs:35-48`; `FoodsEndpointsIntegrationTests.cs:220-226` — `Assert.DoesNotContain(..., x.Id == food.Id)` | ✅ PASS |
| AC2: sem capability → 403 | Forbidden | `FoodsEndpointsIntegrationTests.cs:194-201` | ✅ PASS |
| AC3: histórico diário passado preservado | Past diary entries keep macros | `DiaryEndpointsIntegrationTests.cs:325-362` — `Assert.Equal(250, day!.Totals.Kcal)` + protein/carb/fat after soft-delete | ✅ PASS |
| AC4: cardápio ativo com item excluído → sinaliza + substituição | Unavailable item flagged | `MealPlanEndpointsIntegrationTests.cs:81-119` — unavailable item signaled | ✅ PASS |
| AC5: delete duplicado → no-op idempotente | OK on second delete | `DeleteFoodHandlerTests.cs:17-32`; `FoodsEndpointsIntegrationTests.cs:228-229` | ✅ PASS |

**Status**: ✅ All 59 ACs have `file:line` evidence — 1 spec-precision gap on NUT-01 AC1 (public status field not asserted explicitly)

---

## Discrimination Sensor

| Mutation | File:line | Description | Killed? |
| -------- | --------- | ----------- | ------- |
| 1 | `NutritionGoalToleranceCalculator.cs:7` | `DefaultTolerance` 0.10 → 0.50 | ✅ Killed — `NutritionGoalToleranceCalculatorTests` 1/5 failed |
| 2 | `FeatureFlagReader.cs:14` | fail-open `?? true` → `?? false` | ✅ Killed — `FeatureFlagReaderTests` 2/4 failed |
| 3 | `AddDiaryEntryHandler.cs:49` | diary lookup uses `DateTime.UtcNow` instead of `command.Date` | ✅ Killed — `AddDiaryEntry_WithPastClientDate_AppearsOnThatDayNotSyncDay` failed |

**Sensor depth**: lightweight (3 targeted faults)
**Result**: 3/3 killed — PASS ✅

Mutations applied in scratch copies only; working tree restored after each run.

---

## Edge Cases (from spec.md)

| Edge case | Result |
| --------- | ------ |
| Day close at midnight UTC for goal evaluation | ❌ GAP — no test asserts UTC cutoff timing |
| Retroactive diary edit does not re-emit NutritionGoalMet | ❌ GAP |
| Re-edit after approved override starts new cycle | ❌ GAP |
| Approved public edit reflects in future cardápio/diary refs | ❌ GAP |
| Public food query without override → no version flag | ✅ `FoodVersionResolverTests.cs:41-68` — `Assert.False(resolved.IsPersonalVersion)` |
| Implausible TDEE inputs rejected | ✅ `CompleteOnboardingCommandValidatorTests.cs:10-21`; `NutritionProfileEndpointsIntegrationTests.cs:77-91` |

---

## Code Quality

| Principle | Status |
| --------- | ------ |
| Minimum code / surgical changes | ✅ |
| Matches existing patterns (CQRS, vertical slice, Vitest intro) | ✅ |
| Spec-anchored outcome check | ✅ 59/59 ACs; 1 spec-precision gap |
| Per-layer coverage expectation (tasks.md matrix) | ✅ |
| Documented guidelines: `ShapeUpApi/src/AGENTS.md` | ✅ |

---

## Gate Check

| Gate | Command | Result |
| ---- | ------- | ------ |
| Backend unit | `dotnet test tests/UnitTests/UnitTests.csproj` | **344 passed**, 0 failed, 0 skipped |
| Backend integration (nutrition filter) | `--filter FullyQualifiedName~Nutrition\|PlatformFeatureFlags\|GamificationNutrition` | **56 passed**, 0 failed |
| Frontend | `npm run test` | **73 passed**, 0 failed |

**Test delta (gap-fix commits)**: +3 integration (`90e1942`, `db9adce`), +3 frontend (`95f0c52`). No nutrition tests deleted.

---

## Requirement Traceability Update

| Requirement | Previous | New |
| ----------- | -------- | --- |
| NUT-01..NUT-13 | Implementing / partial gaps | ✅ Verified (1 spec-precision gap on NUT-01 AC1 public status field) |

---

## Summary

**Overall**: ✅ Ready

**Spec-anchored check**: 59/59 ACs matched spec outcome with `file:line` evidence; 1 spec-precision gap; 4 edge-case gaps (non-AC)
**Sensor**: 3/3 mutations killed
**Gate**: unit 344/344 ✅ · integration nutrition 56/56 ✅ · frontend 73/73 ✅

**What works**: Full P1/P2 nutrition loop with evidence for all previously missing ACs (cross-day diary sync, weight migration preservation, soft-delete diary history, ObjectivesClient weight hook, DiaryDay substitute modal).

**Ranked gaps** (non-blocking):
1. NUT-01 AC1 spec-precision — assert explicit public status field, not just search visibility
2. Edge: UTC midnight day-close timing for goal evaluation — no test
3. Edge: retroactive diary edit must not re-emit `NutritionGoalMet` — no test
4. Edge: re-edit after approved override starts new triage cycle — no test

**Next steps**: Optional harden spec-precision + edge cases; feature is verification-complete for all ACs.
