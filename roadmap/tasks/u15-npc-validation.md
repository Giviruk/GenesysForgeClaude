# Валидация NPC + соответствие правилам создания adversary (u15-npc-validation)

- **Roadmap:** U-15 (см. [unified-roadmap.md](../unified-roadmap.md)); источник GF-009 / Аудит §5.
- **Ветка:** `feature/u15-npc-validation` (от master после слияния U-14 — зависит от `NpcAttack`).
- **Базовая ветка:** `master`.
- **PR:** [#55](https://github.com/Giviruk/GenesysForgeClaude/pull/55)
- **Статус:** 🚧 In progress

## Контекст

Аудит U-14 показал: NPC создаются лишь частично по правилам adversary из
[_books/_npc/genesys_adversary_creation_rules_for_claude.json](../../_books/_npc/genesys_adversary_creation_rules_for_claude.json).
Enforced были только жёсткие инварианты типов. Не было гардов чеклиста (defense ≤4, soak, лимит навыков,
magic, minion group-skills) и полей `silhouette`/`tactics`; генератор отклонялся от формул JSON.

Решения с пользователем (включены все 4 блока):
1. Гарды валидации (errors/warnings).
2. Minion group_skills — без новой сущности: `NpcSkill` миньона = групповой навык, `Ranks` не значимы (=0),
   ранг группы (размер−1) считается за столом.
3. Поля `Npc.Silhouette` и `Npc.Tactics` (модель+миграция+DTO+UI).
4. `NpcDraftGenerator` — точные формулы порогов из JSON, Rival без strain.

## План выполнения

### Блок 1 — гарды валидации
- [x] `NpcValidationResult { List<string> Errors, List<string> Warnings; bool IsValid }`; `NpcValidator.Validate`
      возвращает результат (не бросает), `ValidateAndThrow` бросает на errors. Errors → 400 в хендлерах; warnings → DTO.
- [x] Правила:
  - **Errors:** имя, характеристики 1–6, wound>0, soak≥0, defense≥0, strain≥0, silhouette≥0, Nemesis strain,
    defense >6; Minion со strain или с рангами навыков; атака без skill/damage/range или crit<1 (непустой).
  - **Warnings:** defense >4 (≤6); soak >7; навыков >8; Rival со strain; magic NPC без заклинаний/способностей;
    качество атаки не из справочника (custom); silhouette ≥2 и wound < sil×10.
- [x] `NpcDetailDto.Warnings` считается в `NpcMapper.ToDetail` (на каждом GET/после save). Frontend показывает.

### Блок 2 — Minion group_skills
- [x] `NpcValidator`: Minion — все `Skills.Ranks` = 0 (иначе error). `NpcMapper.Apply` обнуляет ранги миньона.
- [x] `NpcDraftGenerator`: навыки миньона с `Ranks=0`.
- [x] Frontend `SkillsEditor` (проп `groupSkills`)/детали/печать: ранги скрыты, подпись «Групповые навыки»,
      пояснение «ранг = размер группы − 1».

### Блок 3 — silhouette + tactics
- [x] `Npc.Silhouette` (int, default 1) и `Npc.Tactics` (string). Миграция `AddNpcSilhouetteAndTactics`
      (existing rows Silhouette=1).
- [x] `NpcDetailDto`/`NpcInput` + `NpcMapper`. Валидатор: silhouette ≥2 → warning, если wound < sil×10.
- [x] Frontend: поля silhouette (число) и tactics (textarea) в форме; вывод в детали и печати (GM/markdown).

### Блок 4 — генератор по формулам JSON
- [x] `NpcDraftGenerator`: wound — Rival `8+Brawn`, Nemesis `12+Brawn`; strain — Nemesis `10+Willpower`,
      Rival/Minion `null`; Minion wound 3–6. Пороги по итоговым Brawn/Willpower.
- [x] Крупные монстры (`NpcRole.Monster`, уровень ≥2) → silhouette 2, wound ≥ sil×10.
- [~] Финальный урон вместо `+X` — НЕ хранится абсолютом: модель/каталог хранят `+N`, UI/печать раскрывают как
      Brawn+N (`npcAttackViews`, `attackLine`). Семантику «+N масштабируется с Мощью» сохранили намеренно.

## Тесты
- [x] Domain: `NpcValidatorTests` (10), `NpcDraftGeneratorTests` (5) — формулы, Rival без strain, minion ranks=0,
      монстр silhouette, все сгенерированные проходят валидацию.
- [x] Api: HighDefense warning → 201+warnings; ExcessiveDefense → 400; minion ranks→0; silhouette/tactics round-trip.
- [~] Vitest UI: пропущено — в репо нет компонентных тестов `NpcsPage`; data-логика покрыта `npcStats` (front 77).

Итог: Domain 87 / Api 182 / front 77 — зелёные.

## DoD
- [x] Errors блокируют сохранение, warnings показываются и не блокируют.
- [x] QuickDraft создаёт NPC без errors и по формулам JSON (`Generated_Npc_PassesValidation`).
- [x] Финальный чеклист JSON проходит для сгенерированных и ручных NPC.

## Что осталось / блокеры
- Открыть PR.
- Применить миграцию + перезапуск backend на проде.

## Заметки / решения
- Размер группы и ранг = размер−1 — концепция стола (encounter/game-table хранит количество), поэтому в профиле
  миньона хранятся только имена групповых навыков, ранги не значимы.
- `Adversary N` талант и hook/мотивация Nemesis из JSON — guidance, не гарды (не реализовано).
- Финальный урон оставлен как `+N` (раскрытие в UI), чтобы не терять масштабирование с Мощью.
- Зависит от U-14 (`NpcAttack`): валидация атак и урон работают по структурной модели.
