# private-content

Полный (private) справочный контент для режима `ContentMode=PrivateFull`: собственные
расширенные парафраз-описания на русском, **не** копирующие текст официальных книг.

## Формат

Один файл на систему:

- `genesys-core.ru.json`
- `realms-of-terrinoth.ru.json`

```json
{
  "system": "GenesysCore",
  "descriptions": {
    "<stable Code справочной записи>": "полное описание",
    ...
  }
}
```

Ключ — стабильный `Code` сущности (см. `SeedData.Code(...)`), например `gc.talent.parry`,
`rot.item.plate-armor`. Запись без описания просто остаётся с safe-описанием.

## Как подключается

Каталог лежит в `backend/private-content/` (внутри Docker build context `./backend`, чтобы
`*.ru.json` попадали в образ — Dockerfile копирует его перед `dotnet publish`). Файлы подключены
как **embedded resource** в `GenesysForge.Infrastructure.csproj` и читаются `PrivateContentStore`.
Рабочий каталог значения не имеет; в `ContentMode=PublicSafe` файлы не используются.

## ⚠️ Перед публичным открытием репозитория

Эти файлы содержат полный приватный контент и предназначены только для приватного развёртывания.
**Перед тем как сделать репозиторий публичным, удалите `*.ru.json` (сам каталог с этим README
оставьте — иначе `COPY` в Dockerfile упадёт) или вынесите контент во внешнее приватное хранилище,
затем пересоберите образ.** Сборка не сломается: glob в csproj, не нашедший файлов, просто ничего
не подключит, а `PrivateContentStore` останется пустым. Публичная версия должна работать в
`ContentMode=PublicSafe`, которому эти файлы не нужны.
