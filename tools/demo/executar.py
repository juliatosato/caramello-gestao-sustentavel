"""Start an isolated Caramello instance for screenshots on Windows."""

import argparse
from getpass import getpass
from pathlib import Path
import os
import secrets
import shutil
import subprocess
import tempfile
import time
from urllib.request import Request, urlopen

from dados import seed


def origem_api(api, log):
    for _ in range(200):
        for linha in log.read_text(encoding="utf-8", errors="replace").splitlines():
            if linha.startswith("CARAMELLO_DESKTOP_PORT="):
                porta = int(linha.split("=", 1)[1])
                return f"http://127.0.0.1:{porta}"
        if api.poll() is not None:
            break
        time.sleep(0.15)
    raise RuntimeError("A API demonstrativa não informou a porta.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--capturar", action="store_true", help="Salva a galeria com o Chrome.")
    parser.add_argument("--chrome", type=Path, help="Caminho alternativo para o Chromium.")
    args = parser.parse_args()

    raiz = Path(__file__).resolve().parents[2]
    api_dll = raiz / "backend/Caramello.Api/bin/Debug/net10.0/Caramello.Api.dll"
    dist = raiz / "frontend/dist"
    if not api_dll.is_file() or not (dist / "index.html").is_file():
        parser.error("Compile a API em Debug e execute npm run build em frontend.")
    senha = secrets.token_urlsafe(24) + "Aa1!" if args.capturar else getpass("Senha temporária da conta demo: ")
    if len(senha) < 12 or not all(any(teste(c) for c in senha) for teste in
        (str.isupper, str.islower, str.isdigit, lambda c: not c.isalnum())):
        parser.error("Use ao menos 12 caracteres com maiúscula, minúscula, dígito e símbolo.")

    artefatos = raiz / "artifacts"
    artefatos.mkdir(exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="caramello-demo-", dir=artefatos, ignore_cleanup_errors=True) as pasta:
        base = Path(pasta)
        shutil.copytree(dist, base / "wwwroot")
        banco = base / "demo.sqlite"
        ambiente = {
            k: v for k, v in os.environ.items()
            if not k.upper().startswith(("CARAMELLO_", "CONNECTIONSTRINGS", "ASPNETCORE_", "DOTNET_ENVIRONMENT", "DOTNET_CONTENTROOT"))
        }
        ambiente.update(
            ASPNETCORE_ENVIRONMENT="Desktop",
            DOTNET_ENVIRONMENT="Desktop",
            ASPNETCORE_CONTENTROOT=str(base),
            DOTNET_CONTENTROOT=str(base),
            CARAMELLO_DATABASE_PATH=str(banco),
            CARAMELLO_KEYS_PATH=str(base / "keys"),
            CARAMELLO_BACKUP_PATH=str(base / "backups"),
        )
        comando = ["dotnet", str(api_dll)]
        flags = subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0
        with (base / "migrations.log").open("wb") as log:
            subprocess.run([*comando, "--migrate-desktop"], cwd=base, env=ambiente,
                           stdout=log, stderr=subprocess.STDOUT, check=True, timeout=60, creationflags=flags)
        seed(banco)
        token = secrets.token_hex(32)
        ambiente.update(
            CARAMELLO_DESKTOP_SHUTDOWN_TOKEN=token,
            CARAMELLO_BOOTSTRAP_ADMIN_EMAIL="demo@example.com",
            CARAMELLO_BOOTSTRAP_ADMIN_PASSWORD=senha,
        )
        caminho_log = base / "api.log"
        with caminho_log.open("wb") as log:
            api = subprocess.Popen(comando, cwd=base, env=ambiente, stdout=log,
                                   stderr=subprocess.STDOUT, creationflags=flags)
            origem = None
            try:
                origem = origem_api(api, caminho_log)
                with urlopen(origem + "/api/health", timeout=5) as health:
                    if health.status != 200:
                        raise RuntimeError("O banco de demonstração não está pronto.")
                if args.capturar:
                    from capturar import galeria

                    galeria(origem, senha, base, raiz / "docs/screenshots", args.chrome)
                else:
                    print(f"Abra {origem} e entre como demo@example.com.", flush=True)
                    input("Pressione Enter para encerrar e remover o ambiente temporário. ")
            finally:
                if origem and api.poll() is None:
                    pedido = Request(origem + "/api/desktop/shutdown", method="POST",
                                     headers={"X-Caramello-Desktop-Token": token})
                    try:
                        with urlopen(pedido, timeout=3):
                            api.wait(timeout=3)
                    except (OSError, subprocess.TimeoutExpired):
                        pass
                if api.poll() is None:
                    api.kill()
                    api.wait()


if __name__ == "__main__":
    main()
