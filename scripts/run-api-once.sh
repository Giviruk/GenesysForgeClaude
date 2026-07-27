#!/usr/bin/env bash
# Запускает API один раз до готовности и гасит его.
#
# Нужен джобе «Migrations (non-empty DB)»: при старте приложение применяет миграции и выполняет
# сид, то есть делает ровно то, что делает продакшен. Успешный ответ health-эндпоинта означает,
# что миграции легли на текущее содержимое базы; падение процесса — что не легли.
#
# Запускать из каталога backend. Строка подключения берётся из окружения.
set -euo pipefail

PORT="${API_PORT:-5199}"
PROJECT="src/GenesysForge.Api/GenesysForge.Api.csproj"
LOG="$(mktemp)"

dotnet run --project "$PROJECT" --urls "http://127.0.0.1:${PORT}" >"$LOG" 2>&1 &
API_PID=$!

cleanup() {
    # Гасим и приложение, и его дочерние процессы сборки, чтобы шаг не завис.
    kill "$API_PID" 2>/dev/null || true
    wait "$API_PID" 2>/dev/null || true
}
trap cleanup EXIT

for _ in $(seq 1 120); do
    if curl --fail --silent --show-error "http://127.0.0.1:${PORT}/api/auth/providers" >/dev/null 2>&1; then
        echo "API поднялся: миграции и сид применились к текущему состоянию базы."
        exit 0
    fi
    if ! kill -0 "$API_PID" 2>/dev/null; then
        echo "API упал при старте — миграции не применились к текущему содержимому базы:"
        cat "$LOG"
        exit 1
    fi
    sleep 2
done

echo "API не ответил за отведённое время:"
cat "$LOG"
exit 1
