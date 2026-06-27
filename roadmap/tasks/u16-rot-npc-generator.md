# NPC Draft Generator под Realms of Terrinoth (u16-rot-npc-generator)

- **Roadmap:** U-16 (см. [unified-roadmap.md](../unified-roadmap.md)); источник GF-010 / Аудит §3.2/§5.
- **Ветка:** `feature/u16-rot-npc-generator` (от master после U-15).
- **Базовая ветка:** `master`.
- **PR:** [#56](https://github.com/Giviruk/GenesysForgeClaude/pull/56)
- **Статус:** ✅ Done

## Контекст

`NpcDraftGenerator` даёт базовый черновик по роли/типу/уровню, но не учитывает типы существ
(нежить/зверь/дракон/…), магшколу и окружение, и не генерит структурные атаки сам (их собирает
`QuickDraftNpcHandler` из каталога). U-16 расширяет генератор под RoT-бестиарий.

Решения с пользователем:
1. Типы существ — отдельный enum `CreatureTemplate` **в запросе генератора** (не в модели Npc); выражается
   через теги/способности/природные атаки/terror/resist.
2. Расширить UI `QuickDraftForm` новыми параметрами (тип существа, магшкола, окружение) — end-to-end.

## План выполнения

- [x] Domain: enum `CreatureTemplate { None, Undead, Beast, Dragon, Demon, Construct }`. `NpcDraftRequest` +
      `Template`, `MagicSkill`, `Environment`.
- [x] `NpcDraftGenerator.ApplyTemplate` — тег существа, способности (Ужас у undead/dragon/demon, иммунитеты у
      construct), природная структурная `NpcAttack`, природный soak, silhouette ≥2 у дракона (+дыхание).
      Магшкола → основной навык + способность «Заклинания». Окружение → тег. Оружие гуманоида не добавляется
      существам.
- [x] `QuickDraftRequest` DTO + `QuickDraftNpcHandler`: прокинуты Template/MagicSkill/Environment. Для
      template-существ каталожное оружие/броня не применяются, боевой навык приводится к каталожному
      (RoT-корректному) + синхронизируется навык природных атак. Для Magic — `ResolveSkillByLabel` заданной магшколы.
- [x] RoT-корректность: навыки из каталога системы (Core-only не попадают). Тест `QuickDraft_RoT_UsesOnlyRotCatalogSkills`.
- [x] Frontend: типы `CreatureTemplate` + поля `QuickDraftRequest`; labels `CREATURE_TEMPLATE_LABELS`;
      `QuickDraftForm` — селект типа существа, магшколы (reference.skills kind=magic, при стиле «магия»), окружение.
- [x] Тесты: Domain (`NpcDraftGeneratorTests` — шаблоны/terror/дракон/магшкола/окружение), Api
      (RoT-навыки ⊆ каталог; template-существо имеет природную атаку+тег). Domain 98 / Api 184 / front 77 зелёные.
- [~] docs: схема БД не менялась (генератор-only, без миграций) — database.md/domain-model.md не трогаем.
- [ ] PR авто-открыть.

## DoD
- [x] Разные NPC для RoT и Core; RoT не получает Core-навыки; генерит структурные атаки + теги + проходит валидацию.

## Заметки / решения
- `CreatureTemplate` — параметр генерации, не поле Npc (тип выражается тегами/способностями).
- Природные атаки template-существ не перетираются каталожным оружием в хендлере (guard по Template).
- Зависит от U-14 (`NpcAttack`) и U-15 (валидация/формулы).
