# U-21 · Профиль / управление аккаунтом

**Статус:** 🚧 In progress · ветка `feature/u21-account-profile` (от master) · PR #64
**Roadmap:** [unified-roadmap.md](../unified-roadmap.md) U-21 · Источник: Аудит §1.2

## Зачем
Пользователю негде посмотреть/изменить своё имя и аватар и сменить пароль внутри сессии.

## Что сделано

### Backend
- `User.AvatarUrl` (string?, nullable) + миграция `AddUserAvatar`, конфиг `HasMaxLength(1000)`.
- `Features/Account/`:
  - `GetAccountQuery`/`GetAccountHandler` → `AccountDto` (id/email/displayName/avatarUrl/createdAt).
  - `UpdateAccountCommand`/Handler: меняет displayName (непустой) и avatarUrl (пустая строка → null);
    null-поля не трогаются.
  - `ChangePasswordCommand`/Handler: verify текущего пароля (`IPasswordHasherService.Verify`),
    новый ≥6 символов, отзыв всех активных refresh-токенов пользователя.
- `AccountEndpoints` (`/api/account`, RequireAuthorization): `GET /`, `PATCH /`,
  `POST /change-password` (под `AuthRateLimiting.SensitivePolicy`; после смены — `IssueAsync`
  свежего refresh-cookie текущему устройству, чтобы не разлогинить себя). Регистрация в DI + Program.

### Frontend
- `api.account` / `api.updateAccount` / `api.changePassword`, тип `Account`.
- `ProfilePage` (область роутера `account`, кнопка «Профиль» в топбаре): карточка аккаунта
  (email read-only, аватар/инициалы, правка имени и URL аватара) + форма смены пароля
  (текущий/новый/повтор, клиентская валидация совпадения и длины). Мин. CSS `.avatar`/`.profile-identity`.

## Тесты
- Api `AccountTests`: GET профиля; неаутентифицированный → 401; PATCH имени/аватара (+ очистка пустой
  строкой, null не трогает имя); пустое имя → 400; смена пароля с неверным текущим → 400; успешная
  смена — старый пароль не логинит (401), новый логинит (200).
- Front `ProfilePage.test.tsx`: загрузка/сохранение профиля; валидация несовпадения паролей до запроса;
  успешная смена пароля.
- Итог: backend Domain 102 / Api 202, frontend 99 — зелёные; `npm run build` (tsc -b) чист.

## DoD
- [x] Пользователь видит/редактирует имя и аватар.
- [x] Меняет пароль в сессии (старые сессии отзываются, текущая остаётся).
