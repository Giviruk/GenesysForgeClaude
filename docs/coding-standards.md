# Coding Standards

## Общие правила

- Предпочитать существующие паттерны проекта.
- Делать изменения маленькими и связанными с задачей.
- Не добавлять новые библиотеки без явной пользы.
- Не смешивать refactor и feature/fix без необходимости.
- Не хранить copyrighted тексты.
- Писать код так, чтобы бизнес-правила можно было тестировать без UI и БД.

## C# стиль

Проект использует modern C# с nullable reference types и implicit usings.

Рекомендации:

- Namespace — file-scoped.
- DTO и commands/queries — `record`, если нет причины использовать class.
- Entities — class с settable properties для EF Core.
- Чистые результаты и value objects — record/record struct по ситуации.
- Асинхронные методы — suffix `Async`, кроме interface handler pattern, где уже принят `Handle`.
- CancellationToken прокидывать из endpoint до EF-запросов.
- Throw использовать для исключительных ситуаций; бизнес-валидация покупок может возвращать `PurchaseResult`.
- `DomainRuleException` использовать для ошибок бизнес-правил, которые должны стать HTTP 400.
- Не использовать `DateTime.Now`; для persisted timestamps использовать UTC.

## C# именование

- Types, methods, properties: `PascalCase`.
- Local variables, parameters: `camelCase`.
- Private fields: `_camelCase`, если fields нужны.
- Interfaces: `IName`.
- Commands: `VerbNounCommand`.
- Queries: `GetThingQuery`.
- Handlers: `VerbNounHandler` или `GetThingHandler`.
- DTO:
  - input: `*Request`;
  - output aggregate: `*Response`;
  - object shape: `*Dto`;
  - list item: `*ListItemDto`.

## Backend layering

- Domain не должен знать про Application, Infrastructure или Api.
- Application не должен знать про Api.
- Infrastructure реализует Application abstractions.
- Api вызывает handlers, но не обращается напрямую к `AppDbContext`, кроме composition/bootstrap сценариев.
- EF entities не возвращаются напрямую наружу.

## TypeScript стиль

Проект использует React, TypeScript и Vite.

Рекомендации:

- Компоненты писать как function components.
- Props типизировать явно через `type`.
- API-типы держать в `src/api/types.ts`.
- HTTP-запросы держать в `src/api/client.ts`.
- Чистые функции держать в `src/utils`.
- Избегать `any`; использовать union types для enum-like значений.
- Не смешивать форматирование, крупный UI refactor и изменение API в одном PR.
- Ошибки API обрабатывать через существующий `ApiError`.

## TypeScript именование

- Components: `PascalCase`.
- Hooks: `useThing`.
- Functions/variables: `camelCase`.
- Types/interfaces: `PascalCase`.
- Constants: `UPPER_SNAKE_CASE`, если значение глобальное и неизменяемое; иначе `camelCase`.
- Files:
  - components/pages: `PascalCase.tsx`;
  - utils/API: `camelCase.ts` или существующий стиль файла.

## CSS и UI

- Не вводить новый дизайн-системный стиль без причины.
- Состояния loading/error/empty должны быть учтены для новых экранов.
- Controls должны быть очевидными: buttons для команд, inputs/selects для ввода, tabs для разделов листа.
- Не добавлять большие instructional тексты в UI, если действие понятно из контекста.
- Изменения layout проверять вручную или браузерным smoke test.

## Правила тестирования

- Новое доменное правило — unit test в `GenesysForge.Domain.Tests`.
- Новый API flow — integration/API test в `GenesysForge.Api.Tests`.
- Новый frontend util — Vitest рядом с util.
- Новый API-client behavior — тест `client.test.ts` или аналогичный.
- Тесты должны проверять observable behavior, а не детали реализации.
- Для багфикса сначала добавить тест, который воспроизводит баг, если это разумно.

## Комментарии

- Комментарии нужны для нетривиальных правил Genesys, компромиссов и юридических ограничений.
- Не комментировать очевидное присваивание.
- Если добавляется paraphrase вместо официального текста, можно кратко отметить, что текст должен оставаться original/non-copyrighted.

