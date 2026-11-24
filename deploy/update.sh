#!/usr/bin/env bash
set -euo pipefail
umask 027

pacote="${1:?Uso: sudo ./update.sh /caminho/publicacao.tar.gz}"
test -f "$pacote"
base=/opt/caramello
timestamp="$(date -u +%Y%m%d-%H%M%S)"
release="$base/releases/$timestamp"

/usr/local/sbin/caramello-backup
install -d -m 0750 -o root -g caramello "$release"
tar -xzf "$pacote" -C "$release"
chown -R root:caramello "$release"
find "$release" -type d -exec chmod 0750 {} +
find "$release" -type f -exec chmod 0640 {} +

ln -sfn "$release" "$base/current.next"
mv -Tf "$base/current.next" "$base/current"
systemctl restart caramello

for tentativa in {1..20}; do
  if curl --fail --silent http://127.0.0.1:5000/api/health >/dev/null; then
    echo "Atualizacao concluida: $release"
    exit 0
  fi
  sleep 2
done

echo "Health check falhou. Consulte: journalctl -u caramello" >&2
exit 1
