# Contexto do projeto para Codex

Você está trabalhando no Caramello Gestão Sustentável, projeto de extensão universitária de Ciência da Computação alinhado à ODS 12.

Objetivo: reduzir desperdício de alimentos, embalagens e borra de café por meio de controle de insumos, movimentações de estoque, registro de descartes, indicadores e orientações responsáveis.

Identidade visual do aplicativo: verde-escuro `#10291E`, creme `#F8E5CA`, caramelo `#BC8152` e fundo `#FAF7F0`. Não inventar logotipo oficial nem utilizar marcas de terceiros sem permissão.

Stack: React, TypeScript e Vite no frontend. ASP.NET Core 10 LTS, EF Core 10 e SQLite no backend. Código e interface em português brasileiro, com nomes de classes e APIs consistentes.

Entrega principal: aplicativo Windows 10/11 com WinForms e Microsoft WebView2. Os dados desktop ficam em `%LOCALAPPDATA%\CaramelloGestao`, nunca em `Program Files`. O servidor interno deve permanecer restrito ao loopback, com porta efêmera e encerramento controlado. A publicação Linux ARM64 é alternativa, não a estratégia principal.

Persistência: o caminho do SQLite deve ser configurável por `CARAMELLO_DATABASE_PATH`. O padrão local é `backend/Caramello.Api/data/caramello.db`. Nunca versionar arquivos `.db`, `.db-wal`, `.db-shm`, `.sqlite` ou `.sqlite3`. Não apagar, substituir ou recriar bancos existentes sem confirmação. Não usar diretórios temporários para dados persistentes.

Estoque: toda alteração de saldo deve gerar uma movimentação e ocorrer em transação. Quantidades aceitam no máximo três casas decimais e são persistidas como milésimos inteiros. Não permitir saldo negativo, quantidade de movimentação menor ou igual a zero, alteração de unidade com histórico ou exclusão que comprometa movimentações.

Indicadores: consumo e desperdício devem usar as movimentações vinculadas aos insumos. Registros ambientais e registros de borra são fontes separadas e não podem ser somados novamente às movimentações. Sempre agrupar quantidades por unidade e nunca somar kg, g, L, ml e unidades entre si. Sugestões de reposição usam cálculos estatísticos simples e devem explicar a metodologia e sinalizar histórico insuficiente.

Priorizar UX acessível e mobile, validação, testes e segurança. Não inserir dados falsos de clientes, estoques ou resultados. Não alegar IA preditiva sem histórico e validação.

Segurança: a aplicação usa ASP.NET Core Identity com cookie HttpOnly, papel `Administrador`, antiforgery e auditoria. Não criar cadastro público nem guardar credenciais no frontend. A primeira conta deve ser criada somente pelas variáveis temporárias documentadas e essas variáveis devem ser removidas logo após o primeiro início.

IMPORTANTE: mesmo com a Fase 4, não publicar sem HTTPS, proxy configurado, diretório persistente para banco/chaves/backups, cópia externa dos backups, secrets seguros, política de privacidade e validação operacional da hospedagem.

Ao modificar arquivos, explicar mudanças, executar testes disponíveis e documentar comandos para Windows. Execute comandos Git e modifique arquivos apenas dentro da raiz do projeto.
