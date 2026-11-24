# Aplicativo Windows

O Caramello Gestão Sustentável pode ser distribuído como aplicativo Windows 10/11 offline. O instalador contém WinForms, WebView2, frontend React e API ASP.NET Core self-contained; o computador da cafeteria não precisa de Node.js nem SDK/runtime .NET.

## Arquitetura

- `CaramelloGestao.exe` controla a janela, a instância única e o ciclo de vida.
- `server/Caramello.Api.exe` escuta numa porta efêmera em `127.0.0.1`.
- O WebView2 navega somente na origem loopback escolhida; links HTTPS, `mailto` e `tel` são abertos pelo Windows.
- Nenhuma API nativa ou acesso irrestrito a arquivos é exposto ao React.
- Ao fechar a janela, um token aleatório e exclusivo solicita o encerramento gracioso do servidor; se ele não responder, o processo é finalizado após o tempo limite.

## Dados persistentes

Por padrão:

```text
%LOCALAPPDATA%\CaramelloGestao\data\caramello.db
%LOCALAPPDATA%\CaramelloGestao\keys\
%LOCALAPPDATA%\CaramelloGestao\backups\
%LOCALAPPDATA%\CaramelloGestao\webview2\
%LOCALAPPDATA%\CaramelloGestao\logs\
```

Esses diretórios ficam fora de `Program Files`; atualização e desinstalação não os removem. O menu `Dados` permite selecionar outra pasta de backups e abrir a pasta persistente.

Antes de uma migration pendente, o inicializador executa um backup consistente do banco existente. A API não aplica migrations em uma inicialização normal. Backups automáticos são executados a cada 24 horas enquanto o aplicativo permanece aberto, com retenção das 30 cópias mais recentes; o cabeçalho oferece backup manual.

## Diagnóstico de inicialização

Depois da preparação do banco, o desktop aguarda até 60 segundos pela porta atribuída pelo Windows e por uma resposta HTTP 200 com `status: ok` de `/api/health`. O endpoint verifica a conexão com o SQLite e se há migrations pendentes. Cada tentativa tem até dois segundos, com intervalo de 350 ms.

Se a abertura falhar, use **Copiar detalhes** na janela de erro. O caminho do log da execução aparece nessa janela. A pasta padrão é `%LOCALAPPDATA%\CaramelloGestao\logs`, com retenção dos dez logs de inicialização mais recentes. O log registra as etapas, os códigos de saída e diagnósticos filtrados da saída do subprocesso.

Para validar a versão instalada, feche o Caramello, instale a atualização sobre a pasta existente e abra pelo Menu Iniciar. Confirme que o log contém **API pronta** e **Interface carregada**, que o login e os registros existentes continuam acessíveis e que o processo `Caramello.Api.exe` encerra ao fechar a janela.

## Primeira execução

Se não existir usuário, a interface apresenta `Configuração inicial`. A proprietária informa e-mail, senha forte e confirmação. Identity cria o hash e o papel `Administrador`, inicia a sessão e fecha definitivamente o assistente. Se já houver qualquer conta, `/api/auth/setup` retorna 404.

O assistente local de primeira execução utiliza uma conta definida pela pessoa responsável pela instalação.

## Gerar o pacote

Pré-requisitos apenas no computador de desenvolvimento:

- SDK .NET 10;
- Node.js 20 ou superior;
- Inno Setup 6 gratuito;
- acesso à internet para restaurar pacotes e baixar o bootstrapper WebView2.

Feche o Vite e execute na raiz:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\deploy\publish-windows.ps1
```

O script:

1. executa `npm ci` e compila o React;
2. publica API e WinForms como `win-x64` self-contained;
3. baixa o bootstrapper oficial WebView2;
4. chama `ISCC.exe` quando o Inno Setup está disponível.

Saída esperada:

```text
artifacts\installer\CaramelloSetup.exe
```

O instalador cria atalho no Menu Iniciar e oferece atalho na área de trabalho. Ele detecta WebView2 e executa o bootstrapper somente quando necessário. O bootstrapper requer internet; para instalação totalmente desconectada, baixe o Evergreen Standalone Installer x64 oficial e adapte a entrada `[Files]` do Inno antes da entrega.

Sem assinatura digital, o Windows SmartScreen pode exibir um alerta de editor desconhecido. Na distribuição, informe a origem do instalador e registre seu hash.

## Atualizações

Gere um novo instalador com versão incrementada no `.csproj` desktop e no `.iss`. Instalar sobre a mesma pasta atualiza somente binários. O banco, chaves e backups permanecem em `%LOCALAPPDATA%`.

Antes de entregar uma atualização:

1. abra a versão anterior e crie backup manual;
2. instale a nova versão;
3. confirme login, saldo, histórico, auditoria e backup;
4. feche e abra novamente para verificar persistência.

Não use downgrade se uma migration nova não for compatível. Preserve o instalador anterior e um backup anterior à atualização.

## Restaurar em outro computador

1. No computador antigo, feche o aplicativo e copie um backup validado para mídia externa protegida.
2. Instale a mesma versão ou uma versão mais recente no novo computador e abra/feche uma vez.
3. Preserve qualquer banco criado no novo computador.
4. Com o aplicativo fechado, copie o backup escolhido para `%LOCALAPPDATA%\CaramelloGestao\data\caramello.db`.
5. Abra e valide login e registros.

As chaves de Data Protection protegem cookies, não as senhas do Identity; uma sessão anterior pode expirar após transferência, mas o login continuará funcionando. Nunca substitua um banco enquanto o aplicativo estiver aberto.

Backups no mesmo computador não protegem contra roubo, defeito ou perda do dispositivo. Mantenha cópias periódicas em mídia externa protegida e teste a restauração.

## Checklist de entrega

- Testar `CaramelloSetup.exe` em Windows 10 e 11 limpos, sem SDK .NET/Node.js.
- Testar com e sem WebView2 previamente instalado.
- Testar assistente inicial, login, CSRF, logout e troca de senha.
- Fechar pelo botão da janela e confirmar encerramento do servidor.
- Atualizar sobre versão anterior e verificar preservação de conta e dados.
- Desinstalar e confirmar que `%LOCALAPPDATA%\CaramelloGestao` foi preservado.
- Validar backup e restauração em outro computador.
- Definir ícone somente quando houver um ativo visual autorizado; o projeto não inventa um logotipo oficial.
