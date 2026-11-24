# Implantação Ubuntu ARM64

Este roteiro prepara uma única aplicação ASP.NET Core que serve a API e os arquivos compilados do React. Ele não publica nada automaticamente e não aplica migrations sem decisão operacional.

## Arquitetura

- Caddy recebe HTTPS nas portas públicas 80/443.
- Caddy encaminha para Kestrel em `127.0.0.1:5000`.
- Kestrel serve `/api`, autenticação e o frontend em `wwwroot`.
- SQLite, chaves e backups ficam fora da release.
- `systemd` executa como usuário sem login `caramello` e reinicia após falhas.

## 1. Criar o pacote no Windows

Instale o SDK .NET 10 e Node.js 20 ou superior. Pare o Vite antes de publicar, pois `npm ci` substitui `node_modules`.

```powershell
.\deploy\publish-arm64.ps1
tar -czf .\artifacts\caramello-linux-arm64.tar.gz -C .\artifacts\linux-arm64 .
```

O resultado framework-dependent exige o runtime ASP.NET Core 10 na VM. O projeto não inclui nem altera o banco no pacote.

Confirme no pacote a presença de `Caramello.Api.dll`, `wwwroot/index.html`, `wwwroot/assets` e `libe_sqlite3.so` antes de transferi-lo.

## 2. Preparar Ubuntu 24.04 ARM64

Transfira somente a pasta `deploy` e execute na VM:

```bash
chmod 750 deploy/*.sh
sudo ./deploy/install-server.sh
```

O script instala `aspnetcore-runtime-10.0` do repositório do Ubuntu, SQLite CLI e Caddy do repositório oficial; também cria o usuário e os diretórios restritos.

Copie os modelos e edite os valores:

```bash
sudo install -m 0640 -o root -g caramello deploy/caramello.env.example /etc/caramello/caramello.env
sudo install -m 0644 deploy/caramello.service /etc/systemd/system/caramello.service
sudo install -m 0750 deploy/backup.sh /usr/local/sbin/caramello-backup
sudo install -m 0750 deploy/update.sh /usr/local/sbin/caramello-update
sudo install -m 0750 deploy/rollback.sh /usr/local/sbin/caramello-rollback
```

Não coloque senha no arquivo versionado. As variáveis de bootstrap devem existir apenas durante o primeiro início e ser removidas imediatamente depois.

## 3. Rede Oracle Cloud

- Escolha Ubuntu 24.04 ARM64 na forma Ampere A1.
- Na lista de segurança/NSG, permita entrada TCP 22 apenas dos IPs administrativos sempre que possível.
- Permita TCP 80 e 443 para o público.
- Não permita 5000, SQLite ou SSH para toda a internet.
- Mantenha o firewall do sistema coerente com essas regras.

O Kestrel é limitado a `127.0.0.1:5000` pelo serviço. Caddy é o único processo público.

## 4. Primeira release e banco

Envie `caramello-linux-arm64.tar.gz` por SSH/SCP para uma área administrativa. Antes de substituir um banco existente, pare o serviço e faça backup. Nunca envie o `.db` dentro da release.

```bash
sudo install -d -m 0750 -o root -g caramello /opt/caramello/releases/primeira
sudo tar -xzf /tmp/caramello-linux-arm64.tar.gz -C /opt/caramello/releases/primeira
sudo chown -R root:caramello /opt/caramello/releases/primeira
sudo find /opt/caramello/releases/primeira -type d -exec chmod 0750 {} +
sudo find /opt/caramello/releases/primeira -type f -exec chmod 0640 {} +
sudo ln -sfn /opt/caramello/releases/primeira /opt/caramello/current
```

Para migrar um banco já existente:

1. Pare `caramello`.
2. Execute `/usr/local/sbin/caramello-backup` e valide `PRAGMA integrity_check`.
3. Liste as migrations pendentes com uma ferramenta EF 10 em ambiente administrativo.
4. Aplique conscientemente `dotnet ef database update` ou um migration bundle previamente validado.
5. Inicie e teste o health check antes de liberar tráfego.

A aplicação não executa migrations ao iniciar.

## 5. Caddy e systemd

Substitua `SEU_DOMINIO_AQUI` por um domínio cujo DNS aponta para a VM:

```bash
sudo install -m 0644 deploy/Caddyfile.example /etc/caddy/Caddyfile
sudo editor /etc/caddy/Caddyfile
sudo caddy validate --config /etc/caddy/Caddyfile
sudo systemctl reload caddy
sudo systemctl daemon-reload
sudo systemctl enable --now caramello
```

Valide local e externamente:

```bash
curl --fail http://127.0.0.1:5000/api/health
curl --fail https://SEU_DOMINIO/api/health
sudo systemctl status caramello
sudo journalctl -u caramello -n 100 --no-pager
```

Caddy envia `X-Forwarded-For` e `X-Forwarded-Proto`; a aplicação aceita esses cabeçalhos somente do loopback. Não defina `ASPNETCORE_FORWARDEDHEADERS_ENABLED`, pois essa opção confiaria em proxies arbitrários.

## 6. Atualizar e reverter

O update cria uma release imutável, faz backup, troca o symlink e valida `/api/health`:

```bash
sudo /usr/local/sbin/caramello-update /tmp/caramello-linux-arm64.tar.gz
```

Ele deliberadamente não aplica migrations. Se uma atualização trouxer migration, faça o procedimento manual antes da troca. Para voltar o código:

```bash
ls -1 /opt/caramello/releases
sudo /usr/local/sbin/caramello-rollback /opt/caramello/releases/RELEASE_ANTERIOR
```

Rollback de código não desfaz esquema. Se a migration não for compatível com a release anterior, restaure um backup validado com o serviço parado e preserve o banco substituído para investigação.

## 7. Backup automático e cópia externa

Exemplo de timer diário:

```ini
# /etc/systemd/system/caramello-backup.service
[Unit]
Description=Backup SQLite Caramello
[Service]
Type=oneshot
ExecStart=/usr/local/sbin/caramello-backup
```

```ini
# /etc/systemd/system/caramello-backup.timer
[Unit]
Description=Backup diario Caramello
[Timer]
OnCalendar=*-*-* 03:15:00
Persistent=true
RandomizedDelaySec=15m
[Install]
WantedBy=timers.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now caramello-backup.timer
```

O script usa `.backup` e valida a cópia. Backups no mesmo volume não protegem contra perda da VM: copie periodicamente arquivos já fechados para outro dispositivo ou armazenamento externo gratuito/controlado, com criptografia e teste de restauração. Não copie o banco ativo diretamente.

## Checklist antes da publicação

- Domínio e HTTPS válidos.
- Conta Oracle protegida com MFA e orçamento/alertas configurados.
- SSH por chave, sem senha, e acesso administrativo restrito.
- Banco, backups e chaves em volume persistente com modo `0700`.
- Bootstrap removido do ambiente.
- Login, logout, troca de senha, CSRF, exportações e backup testados pelo domínio HTTPS.
- Restauração ensaiada em diretório separado.
- Política de privacidade, retenção e resposta a incidentes definida.
