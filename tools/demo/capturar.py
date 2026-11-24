"""Capture the real browser viewport through Chromium DevTools."""

import base64
from contextlib import contextmanager
from itertools import count
import json
import os
from pathlib import Path
import subprocess
import time
from urllib.request import urlopen

from websockets.sync.client import connect


def esperar(acao, descricao):
    for _ in range(120):
        resultado = acao()
        if resultado:
            return resultado
        time.sleep(0.15)
    raise RuntimeError(f"Tempo excedido aguardando {descricao}.")


@contextmanager
def navegador(base, chrome):
    if chrome is None:
        chrome = Path(os.environ.get("ProgramFiles", r"C:\Program Files")) / "Google/Chrome/Application/chrome.exe"
    perfil = base / "browser"
    comando = [str(chrome), "--headless", "--hide-scrollbars", "--disable-gpu", "--disable-background-networking",
               "--disable-component-update", "--no-first-run", "--remote-debugging-port=0",
               f"--user-data-dir={perfil}", "about:blank"]
    with (base / "chrome.log").open("wb") as log:
        processo = subprocess.Popen(comando, stdout=log, stderr=subprocess.STDOUT,
                                    creationflags=subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0)
        try:
            endereco = perfil / "DevToolsActivePort"
            porta = esperar(lambda: endereco.read_text().splitlines()[0] if endereco.exists() else None,
                            "DevTools")

            def destino():
                try:
                    with urlopen(f"http://127.0.0.1:{porta}/json/list", timeout=1) as resposta:
                        return next((x["webSocketDebuggerUrl"] for x in json.load(resposta) if x["type"] == "page"), None)
                except OSError:
                    return None

            with connect(esperar(destino, "página do Chromium"), proxy=None,
                         max_size=10_000_000, open_timeout=8, close_timeout=1) as ws:
                yield ws
        finally:
            if processo.poll() is None:
                processo.terminate()
                processo.wait(timeout=10)


def galeria(origem, senha, base, saida, chrome=None):
    with navegador(base, chrome) as ws:
        sequencia = count(1)

        def comando(metodo, **parametros):
            id_pedido = next(sequencia)
            ws.send(json.dumps({"id": id_pedido, "method": metodo, "params": parametros}))
            while True:
                mensagem = json.loads(ws.recv(timeout=8))
                if mensagem.get("id") == id_pedido:
                    if "error" in mensagem:
                        raise RuntimeError(f"Falha de captura em {metodo}.")
                    return mensagem["result"]

        def js(expressao):
            resposta = comando("Runtime.evaluate", expression=expressao, awaitPromise=True, returnByValue=True)
            if "exceptionDetails" in resposta:
                raise RuntimeError("A aplicação retornou um erro ao preparar a captura.")
            return resposta["result"].get("value")

        def conteudo(texto):
            esperado = json.dumps(texto)
            esperar(lambda: js(f"document.body?.innerText.includes({esperado}) && !document.querySelector('.loading')"),
                    texto)

        def clicar(seletor, rotulo):
            js(f"Array.from(document.querySelectorAll({json.dumps(seletor)}))"
               f".find(e => e.textContent.includes({json.dumps(rotulo)})).click()")
            js("window.scrollTo(0, 0)")

        def enquadrar(rotulo):
            js("(() => { const titulo = Array.from(document.querySelectorAll('h2,h3'))"
               f".find(e => e.textContent === {json.dumps(rotulo)});"
               "window.scrollTo(0, titulo.getBoundingClientRect().top + window.scrollY - 48); })()")

        imagens = {}

        def foto(nome):
            js("new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))")
            emails = js(r"document.body.innerText.match(/[\w.+-]+@[\w.-]+\.[a-z]{2,}/gi) || []")
            if any(email != "demo@example.com" for email in emails):
                raise RuntimeError("A página contém um endereço diferente da conta de demonstração.")
            if not js("Array.from(document.querySelectorAll('input[type=password]')).every(e=>!e.value)"):
                raise RuntimeError("A captura contém um campo de senha preenchido.")
            imagens[nome] = base64.b64decode(comando("Page.captureScreenshot", format="png")["data"])

        comando("Network.enable")
        comando("Network.setBlockedURLs", urls=["*fonts.googleapis.com*", "*fonts.gstatic.com*"])
        comando("Emulation.setDeviceMetricsOverride", width=1920, height=1080, deviceScaleFactor=1, mobile=False)
        comando("Emulation.setTimezoneOverride", timezoneId="America/Sao_Paulo")
        comando("Emulation.setLocaleOverride", locale="pt-BR")
        comando("Page.navigate", url=origem)
        conteudo("Acesso administrativo")
        foto("01-login.png")
        credenciais = json.dumps({"email": "demo@example.com", "senha": senha})
        login = js("""(async () => {
            const {token} = await (await fetch('/api/auth/csrf')).json();
            return (await fetch('/api/auth/login',{method:'POST',
                headers:{'Content-Type':'application/json','X-CSRF-TOKEN':token},
                body: JSON.stringify(""" + credenciais + """)})).ok;
        })()""")
        if not login:
            raise RuntimeError("A autenticação da conta de demonstração falhou.")
        comando("Page.reload")
        conteudo("Café em grãos")
        foto("02-dashboard.png")

        clicar("nav button", "Estoque")
        conteudo("Copos de papel")
        enquadrar("Saldos e parâmetros")
        foto("03-inventory.png")
        enquadrar("Sugestões de reposição")
        foto("04-restocking.png")

        clicar("nav button", "Desperdícios")
        conteudo("Polpa de maracujá")
        foto("05-waste-analysis.png")

        clicar("nav button", "Borra de café")
        conteudo("Separada após o preparo")
        enquadrar("Registrar borra de café")
        foto("06-coffee-grounds.png")
        clicar("button", "Compostagem na cafeteria")
        conteudo("Antes de montar a composteira")
        foto("07-composting-guide.png")
        clicar("button", "Voltar para borra de café")
        conteudo("Pátios municipais para consultar")
        enquadrar("Destinação responsável")
        foto("08-composting-locations.png")

        clicar("nav button", "Relatórios")
        conteudo("Período anterior")
        foto("09-reports.png")

    saida.mkdir(parents=True, exist_ok=True)
    for nome, dados in imagens.items():
        (saida / nome).write_bytes(dados)
    print(f"{len(imagens)} capturas geradas em {saida}.")
