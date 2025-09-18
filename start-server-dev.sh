#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

if [ -f .env ]; then
  set -a
  . ./.env
  set +a
fi

echo "🔁 Starting CodexBootstrap in dev hot-reload mode (dotnet watch)"
cd src/CodexBootstrap
DOTNET_URLS=${DOTNET_URLS:-http://localhost:5002} dotnet watch run


