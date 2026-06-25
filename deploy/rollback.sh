#!/bin/sh
set -eu

if [ "$#" -ne 1 ]; then
  echo "Usage: $0 <previous-image-tag>" >&2
  exit 2
fi

previous_tag="$1"
compose_file="${COMPOSE_FILE:-docker-compose.prod.yml}"

case "$previous_tag" in
  *[!A-Za-z0-9._-]*|"")
    echo "Invalid image tag." >&2
    exit 2
    ;;
esac

echo "Rolling back application images to tag: $previous_tag"
IMAGE_TAG="$previous_tag" docker compose -f "$compose_file" pull api api-public web web-public
IMAGE_TAG="$previous_tag" docker compose -f "$compose_file" up -d api api-public web web-public caddy

echo "Rollback deployed. Persist IMAGE_TAG=$previous_tag in .env after verification."
