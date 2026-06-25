#!/bin/sh
set -eu

if [ "$#" -ne 2 ]; then
  echo "Usage: $0 <private|public> <backup.dump>" >&2
  exit 2
fi

target="$1"
backup_file="$2"
compose_file="${COMPOSE_FILE:-docker-compose.prod.yml}"

if [ ! -f "$backup_file" ]; then
  echo "Backup not found: $backup_file" >&2
  exit 2
fi

case "$target" in
  private)
    service="postgres"
    database="genesysforge"
    api_service="api"
    ;;
  public)
    service="postgres-public"
    database="genesysforge_public"
    api_service="api-public"
    ;;
  *)
    echo "Target must be private or public." >&2
    exit 2
    ;;
esac

echo "This will replace data in $database from $backup_file."
printf "Type RESTORE-%s to continue: " "$target"
read -r confirmation
[ "$confirmation" = "RESTORE-$target" ] || {
  echo "Restore cancelled."
  exit 1
}

docker compose -f "$compose_file" stop "$api_service"
docker compose -f "$compose_file" exec -T "$service" \
  dropdb -U genesys --if-exists "$database"
docker compose -f "$compose_file" exec -T "$service" \
  createdb -U genesys "$database"
docker compose -f "$compose_file" exec -T "$service" \
  pg_restore -U genesys -d "$database" --clean --if-exists < "$backup_file"
docker compose -f "$compose_file" up -d

echo "Restore complete. Verify /api/health and application smoke tests."
