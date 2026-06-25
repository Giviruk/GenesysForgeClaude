#!/bin/sh
set -eu

compose_file="${COMPOSE_FILE:-docker-compose.prod.yml}"
timestamp="$(date -u +%Y%m%dT%H%M%SZ)"

docker compose -f "$compose_file" exec -T postgres \
  pg_dump -U genesys -d genesysforge -Fc > "private-${timestamp}.dump"
docker compose -f "$compose_file" exec -T postgres-public \
  pg_dump -U genesys -d genesysforge_public -Fc > "public-${timestamp}.dump"

echo "Created private-${timestamp}.dump and public-${timestamp}.dump"
