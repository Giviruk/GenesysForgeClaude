# Реальный email provider + публичный password reset (u06-email-provider-password-reset)

- **Roadmap:** U-06 — Реальный email provider + публичный password reset (см. [unified-roadmap.md](../unified-roadmap.md))
- **Ветка:** `feature/u06-email-provider-password-reset`
- **Базовая ветка:** `master` (PR #35 слит, открытых PR нет)
- **PR:** pending
- **Статус:** 🚧 In progress

## Контекст

Сейчас отправка письма сброса пароля — заглушка [`LoggingEmailSender`](../backend/src/GenesysForge.Infrastructure/Auth/LoggingEmailSender.cs)
(пишет ссылку в лог). Нужен реальный провайдер. Выбран **SMTP через MailKit** (универсально для любого
relay — свой сервер или SMTP-режим Resend/SendGrid/Mailgun), без привязки к вендорскому SDK.

**Что уже готово (проверено в коде, менять не нужно):**
- Токен: только SHA-256 hash в БД, expiry 1 час, single-use, прежние активные токены гасятся,
  request всегда возвращает 204 без раскрытия наличия аккаунта
  ([RequestPasswordResetHandler](../backend/src/GenesysForge.Application/Features/Auth/RequestPasswordResetHandler.cs),
  [ConfirmPasswordResetHandler](../backend/src/GenesysForge.Application/Features/Auth/ConfirmPasswordResetHandler.cs)).
- Rate limiting: `/password-reset/request` и `/confirm` под `SensitivePolicy` (из U-05).
- Frontend: экраны reset-request/reset-confirm в [AuthPage](../frontend/src/pages/AuthPage.tsx) с
  нейтральным success-сообщением (без enumeration), чисткой токена из URL и обработкой ошибок — **готово**.

## План выполнения

- [x] `SmtpEmailSender : IEmailSender` на MailKit (`MimeMessage` + `SmtpClient` на отправку)
- [x] `EmailOptions` (Provider/From/FromName/Smtp:Host/Port/Username/Password/UseStartTls)
- [x] DI-выбор провайдера: `Email:Provider=Smtp` → `SmtpEmailSender`, иначе `LoggingEmailSender` (дефолт)
- [x] **Session revoke**: `ConfirmPasswordResetHandler` гасит активные refresh-токены пользователя (DoD)
- [x] MailKit (4.17.0) добавлен в `GenesysForge.Infrastructure.csproj` (без NU1902-уязвимостей)
- [x] Тесты: revoke refresh-токенов при сбросе; выбор SMTP-провайдера в DI; existing expiry/reuse/invalid (62+135 зелёные)
- [x] `.env.example` + `docs/operator-notes.md` (Email__* конфиг) + `docs/current-state.md` + prod compose (App__BaseUrl + Email__*)
- [x] Миграции не требуются (модель не меняется)
- [x] Copyright: транзакционный текст письма — собственный, book text не используется
- [x] Статус в `unified-roadmap.md` обновлён (U-05 → Done #35, U-06 → In progress)
- [ ] PR открыт
- [ ] Docker image builds (локальный Docker недоступен; проверит CI/deploy Buildx)

## Что осталось / блокеры

(заполняется по ходу)

## Заметки / решения

- Провайдер выбран пользователем: **SMTP (MailKit)**. Default `Email:Provider` = Logging — dev/tests
  остаются на заглушке без реального SMTP.
- `SmtpClient` MailKit создаётся на каждую отправку (не разделяется между потоками); sender — singleton.
- Ссылка сброса строится из `App:BaseUrl` + `/reset-password?token=...` (как в заглушке).
