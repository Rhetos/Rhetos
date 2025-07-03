#!/usr/bin/env bash
set -euo pipefail

# On Windows, you can install the Docker Engine in WSL (https://docs.docker.com/engine/install/),
# and run this script from command prompt: wsl bash -ic "'./setup postgresql db.sh'"

# ---- config --------------------------------------------------------------
IMAGE="postgres:16.2"
CONTAINER="rhetos_test_postgres"
PORT=5432
DB1="rhetos6testappdirect"
DB2="rhetos6commonconceptstestapp"
# -------------------------------------------------------------------------

docker pull "$IMAGE"

# Start fresh every time (silently ignore 'not found')
docker rm -f "$CONTAINER" >/dev/null 2>&1 || true
docker run -d --name "$CONTAINER" -e POSTGRES_HOST_AUTH_METHOD=trust -p 127.0.0.1:"$PORT":5432 "$IMAGE"

echo "Waiting for Postgres to accept connections (max 10 s)"
for _ in {1..10}; do
  docker exec "$CONTAINER" psql -U postgres -c '\q' >/dev/null 2>&1 && break
  sleep 1
done

echo "Creating the databases"
for db in "$DB1" "$DB2"; do
  docker exec "$CONTAINER" psql -U postgres -c "CREATE DATABASE $db;" 2>/dev/null
done

echo "Testing the databases"
for db in "$DB1" "$DB2"; do
  docker exec "$CONTAINER" psql -U postgres -d "$db" -c '\conninfo'
done

echo "PostgreSQL container '$CONTAINER' is ready."
