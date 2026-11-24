#!/usr/bin/env bash
set -euo pipefail

if [[ "${EUID}" -ne 0 ]]; then echo "Execute como root: sudo ./install-server.sh" >&2; exit 1; fi

apt-get update
apt-get install -y aspnetcore-runtime-10.0 sqlite3 curl ca-certificates debian-keyring debian-archive-keyring apt-transport-https gnupg

curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' > /etc/apt/sources.list.d/caddy-stable.list
chmod o+r /usr/share/keyrings/caddy-stable-archive-keyring.gpg /etc/apt/sources.list.d/caddy-stable.list
apt-get update
apt-get install -y caddy

getent group caramello >/dev/null || groupadd --system caramello
id caramello >/dev/null 2>&1 || useradd --system --gid caramello --home-dir /var/lib/caramello --shell /usr/sbin/nologin caramello
install -d -m 0750 -o root -g caramello /opt/caramello/releases
install -d -m 0700 -o caramello -g caramello /var/lib/caramello /var/lib/caramello/keys /var/backups/caramello
install -d -m 0750 -o root -g caramello /etc/caramello

echo "Base instalada. Copie e revise caramello.env, o servico systemd, o Caddyfile e a primeira release."
