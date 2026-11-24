#!/usr/bin/env bash
set -euo pipefail
umask 077

source /etc/caramello/caramello.env
: "${CARAMELLO_DATABASE_PATH:?CARAMELLO_DATABASE_PATH nao definido}"
: "${CARAMELLO_BACKUP_PATH:?CARAMELLO_BACKUP_PATH nao definido}"

install -d -m 0700 -o caramello -g caramello "$CARAMELLO_BACKUP_PATH"
destino="$CARAMELLO_BACKUP_PATH/caramello-$(date -u +%Y%m%d-%H%M%S).db"

# O comando .backup usa a API consistente do SQLite, inclusive com o banco em WAL.
sudo -u caramello sqlite3 "$CARAMELLO_DATABASE_PATH" ".timeout 10000" ".backup '$destino'"
sudo -u caramello sqlite3 "$destino" "PRAGMA integrity_check;" | grep -qx "ok"
find "$CARAMELLO_BACKUP_PATH" -maxdepth 1 -type f -name 'caramello-*.db' -mtime +30 -delete
echo "Backup validado: $destino"
