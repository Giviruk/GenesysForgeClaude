# Валидация NPC + соответствие правилам создания adversary (u15-npc-validation)

- **Roadmap:** U-15 (см. [unified-roadmap.md](../unified-roadmap.md)); источник GF-009 / Аудит §5.
- **Ветка:** `feature/u15-npc-validation` (от master **после** слияния U-14 — зависит от `NpcAttack`).
- **Базовая ветка:** `master`.
- **PR:** _(пока нет)_
- **Статус:** ⬜ Todo

## Контекст

Аудит U-14 показал: NPC создаются лишь частично по правилам adversary из
[_books/_npc/genesys_adversary_creation_rules_for_claude.json](../../_books/_npc/genesys_adversary_creation_rules_for_claude.json).
Enforced только жёсткие инварианты типов (Minion без strain, Nemesis со strain, характеристики 1–6).
Не реализованы гарды чеклиста (defense ≤4, soak, лимит навыков, magic, minion group-skills) и
поля `silhouette`/`tactics`; генератор отклоняется от формул JSON.

Решения с пользователем (все 4 блока включены):
1. Гарды валидации (errors/warnings).
2. Minion group_skills — без новой сущности: `NpcSkill` миньона = групповой навык, `Ranks` не значимы (=0),
   ранг группы (размер−1) считается за столом.
3. Поля `Npc.Silhouette` и `Npc.Tactics` (модель+миграция+DTO+UI).
4. `NpcDraftGenerator` — точные формулы порогов из JSON, Rival без strain, финальный урон миньона.

## План выполнения

### Блок 1 — гарды валидации
- [ ] `NpcValidationResult { List<string> Errors, List<string> Warnings }` в Domain; `NpcValidator.Validate`
      возвращает результат (не только бросает). Errors → `DomainRuleException` в хендлерах; warnings → в DTO.
- [ ] Правила:
  - **Errors:** существующие (имя, характеристики 1–6, wound>0, soak≥0, defense≥0, strain≥0, Nemesis strain) +
    defense >6; Minion со strain или с рангами навыков; атака без skill/damage/range или crit<1 (непустой).
  - **Warnings:** defense >4 (≤6); soak >7; навыков >8; Rival со strain; magic NPC (есть магнавык, но нет
    заклинаний/способностей — или наоборот); качество атаки не из справочника (custom).
- [ ] `NpcDetailDto.Warnings` (errors уже как 400). Прокинуть из Create/Update. Frontend показывает warnings.

### Блок 2 — Minion group_skills
- [ ] `NpcValidator`: Minion — все `Skills.Ranks` должны быть 0 (иначе error). `NpcMapper.Apply` обнуляет
      ранги навыков у миньона (как делает со strain).
- [ ] `NpcDraftGenerator`: для Minion основной навык добавляется с `Ranks=0`.
- [ ] Frontend `SkillsEditor`/детали/печать: для миньона скрывать поле рангов, подпись «Групповые навыки»,
      пояснение «ранг = размер группы − 1».

### Блок 3 — silhouette + tactics
- [ ] `Npc.Silhouette` (int, default 1) и `Npc.Tactics` (string). Миграция `AddNpcSilhouetteAndTactics`.
- [ ] `NpcDetailDto`/`NpcInput` + `NpcMapper`. Валидатор: для `Silhouette ≥ 2` warning, если
      `WoundThreshold < Silhouette*10`.
- [ ] Frontend: поля silhouette (число) и tactics (textarea) в форме; вывод в детали и печати (GM).

### Блок 4 — генератор по формулам JSON
- [ ] `NpcDraftGenerator`: wound — Rival `8+Brawn`, Nemesis `12+Brawn`; strain — Nemesis `10+Willpower`,
      Rival `null` по умолчанию; Minion wound оставить 3–6, без strain. Уровень силы влияет на характеристики
      (как сейчас), а пороги считать по итоговым Brawn/Willpower.
- [ ] Урон оружия миньона/ближнего боя — финальное число вместо `+X` (раскрыть как Brawn+X при генерации
      атаки, либо хранить абсолют). Учесть, что атака теперь структурная (`NpcAttack` из U-14).
- [ ] `silhouette` у крупных монстров (`NpcRole.Monster`) генерить ≥2 с соответствующим wound.

## Тесты
- [ ] Domain: `NpcValidator` (errors vs warnings по каждому правилу), генератор (формулы порогов, Rival без
      strain, minion ranks=0, silhouette монстра/wound).
- [ ] Api: сохранение с error → 400; с warning → 201 + warnings в DTO; QuickDraft проходит чеклист.
- [ ] Vitest: форма показывает warnings; скрытие рангов у миньона; поля silhouette/tactics.

## DoD
- Errors блокируют сохранение, warnings показываются и не блокируют.
- QuickDraft создаёт NPC без errors и по формулам JSON.
- Финальный чеклист из JSON (`final_validation_checklist`) проходит для сгенерированных и ручных NPC.

## Заметки / решения
- Группа миньонов (размер) и ранг = размер−1 — концепция стола (encounter/game-table уже хранит количество),
  поэтому в профиле миньона хранятся только имена групп-навыков, ранги не значимы.
- `Adversary N` талант и «hook/мотивация» Nemesis из JSON — guidance, не гарды (можно warning, опционально).
- Зависит от U-14 (`NpcAttack`): валидация атак и финальный урон работают по структурной модели.
