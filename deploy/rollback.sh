#!/usr/bin/env bash
set -euo pipefail

base=/opt/caramello
anterior="${1:?Informe o diretorio completo da release anterior em /opt/caramello/releases}"
case "$anterior" in "$base"/releases/*) ;; *) echo "Release invalida" >&2; exit 1 ;; esac
test -d "$anterior"

ln -sfn "$anterior" "$base/current.next"
mv -Tf "$base/current.next" "$base/current"
systemctl restart caramello
curl --fail --retry 20 --retry-delay 2 --retry-connrefused http://127.0.0.1:5000/api/health
echo "Rollback concluido para $anterior"
