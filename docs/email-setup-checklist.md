# SMTP: что предоставить для отправки писем

Чтобы заработала реальная отправка писем сброса пароля, нужно завести SMTP-relay и заполнить
переменные в `.env` на сервере. Код уже готов — он только читает эти значения
(см. [operator-notes.md](operator-notes.md) → раздел «Email», реализация — `SmtpEmailSender`).

## Шаг 1. Завести relay

Выберите, через что реально уходят письма (любой вариант):

- собственный почтовый сервер;
- транзакционный провайдер в SMTP-режиме: **Resend / SendGrid / Mailgun / Postmark / Amazon SES**;
- обычный ящик с паролем приложения (Яндекс 360, Gmail) — годится для старта.

## Шаг 2. Подтвердить домен отправителя

`From`-адрес должен быть на домене, **верифицированном у relay** (записи SPF/DKIM в DNS, их выдаёт
провайдер). Иначе письма уходят в спам или отклоняются. Для обычного ящика этот шаг уже сделан.

## Шаг 3. Данные, которые нужно передать

Заполните таблицу (это и есть всё, что от вас требуется):

| Переменная `.env`         | Что это                              | Пример                       | Обязательно |
|---------------------------|--------------------------------------|------------------------------|-------------|
| `EMAIL_PROVIDER`          | Режим. Для отправки — `Smtp`         | `Smtp`                       | да          |
| `EMAIL_FROM`              | Адрес отправителя (на верифиц. домене)| `no-reply@вашдомен.ru`       | да          |
| `EMAIL_FROM_NAME`         | Имя отправителя в письме             | `GenesysForge`               | нет         |
| `EMAIL_SMTP_HOST`         | Хост SMTP-relay                      | `smtp.resend.com`            | да          |
| `EMAIL_SMTP_PORT`         | Порт (587 STARTTLS / 465 implicit TLS)| `587`                       | да          |
| `EMAIL_SMTP_USERNAME`     | Логин SMTP                           | `resend` / `apikey` / e-mail | да*         |
| `EMAIL_SMTP_PASSWORD`     | Пароль SMTP (у API — это API-ключ)   | `re_...` / `SG....`          | да*         |
| `EMAIL_SMTP_USE_STARTTLS` | `true` для 587, `false` для 465      | `true`                       | нет         |

\* без логина/пароля будет попытка анонимной отправки — почти всегда нужен и логин, и пароль.

Также проверьте, что **`PRIVATE_HOSTNAME`** в `.env` — ваш реальный `https://`-хост: из него
строится ссылка в письме (`App__BaseUrl`).

## Готовые примеры

**Resend** (SMTP-режим):
```ini
EMAIL_PROVIDER=Smtp
EMAIL_FROM=no-reply@вашдомен.ru
EMAIL_SMTP_HOST=smtp.resend.com
EMAIL_SMTP_PORT=587
EMAIL_SMTP_USERNAME=resend
EMAIL_SMTP_PASSWORD=re_xxxxxxxxxxxx
EMAIL_SMTP_USE_STARTTLS=true
```

**SendGrid:**
```ini
EMAIL_PROVIDER=Smtp
EMAIL_FROM=no-reply@вашдомен.ru
EMAIL_SMTP_HOST=smtp.sendgrid.net
EMAIL_SMTP_PORT=587
EMAIL_SMTP_USERNAME=apikey
EMAIL_SMTP_PASSWORD=SG.xxxxxxxxxxxx
EMAIL_SMTP_USE_STARTTLS=true
```

## Шаг 4. Применить и проверить

1. Сохранить `.env` на сервере, передеплоить: `docker compose -f docker-compose.prod.yml up -d`.
2. На странице входа → «Забыли пароль?» → ввести свой e-mail.
3. Письмо должно прийти. Если нет — `docker compose -f docker-compose.prod.yml logs api`:
   - успех: строка `Письмо сброса пароля отправлено … через SMTP {Host}`;
   - ошибка коннекта/аутентификации видна там же.

## Если не работает

- **Таймаут коннекта.** Хостер блокирует исходящий 25/587 — откройте порт через поддержку VPS.
- **Auth failed.** Неверные `USERNAME`/`PASSWORD` (у API-провайдеров username часто фиксирован:
  `resend`, `apikey`).
- **Письма в спам / отклонены.** Не настроены SPF/DKIM на домене `EMAIL_FROM`.
- **Порт 465 не коннектится по STARTTLS.** Поставьте `EMAIL_SMTP_USE_STARTTLS=false`.
- **`EMAIL_PROVIDER` ≠ `Smtp`** → работает заглушка: письмо не уходит, ссылка только в логе API.
