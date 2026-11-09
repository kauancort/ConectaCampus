# ConectaCampus

MVP acadêmico para compartilhar atividades de aprendizagem entre alunos. A aplicação integra frontend React, ASP.NET Core Minimal API e MySQL com Identity/JWT, e agora sobe em um único comando Docker Compose.

## Identificação acadêmica

- **Projeto:** ConectaCampus — Plataforma de Compartilhamento de Conhecimentos entre Alunos
- **Equipe:** Aprendizagem Colaborativa
- **Instituição/disciplina:** Fatec Shunji Nishimura de Pompeia — Projeto Integrador de Sistemas Inteligentes III
- **Professor:** Ronaldo Fernandes Medeiros
- **Integrantes:** Kauan Cortiço (2591612523023), Gabriel Barella (2591612523028), Wesley Servelin (2591612523042), Leonardo Cabral (2591612523003) e David Vanjão (2591612523021)
- **Responsável pelo envio:** Kauan Cortiço
- **Versão sugerida:** `v0.1.0-mvp`; não há tag Git nem implantação externa.

## Execução simplificada com Docker

Requisito: Docker Engine + Docker Compose disponíveis no WSL. .NET SDK e Node.js não são necessários para executar a versão em contêineres.

No PowerShell, na pasta que contém este README:

```powershell
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
```

Preencha `.env` com senhas locais fortes e uma chave JWT de pelo menos 32 bytes. Não envie nem compartilhe esse arquivo. Depois, na mesma pasta:

```powershell
$projectWslPath = (wsl -e wslpath -u "$PWD").Trim()
wsl --cd $projectWslPath -e docker compose config --quiet
wsl --cd $projectWslPath -e docker compose up --build -d
wsl --cd $projectWslPath -e docker compose ps
```

O Compose sobe MySQL, API e frontend. O backend aplica migrations pendentes ao iniciar; o frontend Nginx encaminha `/api` e `/health` para a API.

- **Aplicação:** http://127.0.0.1:5173/
- **API/health:** http://127.0.0.1:5077/health
- **Health pela aplicação:** http://127.0.0.1:5173/health
- **MySQL:** acessível localmente em `127.0.0.1:3306`.

Para parar os serviços sem remover o banco:

```powershell
wsl --cd $projectWslPath -e docker compose down
```

O banco usa volume nomeado persistente. Não use `docker compose down -v` se quiser preservar os dados. O `name: aprendizagem-colaborativa` no Compose foi mantido intencionalmente para reutilizar o volume existente; o nome exibido para a aplicação e imagens Docker é ConectaCampus.

## Stack

- Frontend React + TypeScript servido por Nginx.
- Backend C# / ASP.NET Core Minimal API em .NET 10.
- ASP.NET Core Identity + JWT.
- Entity Framework Core e provider MySQL `MySql.EntityFrameworkCore`.
- MySQL 8.4 oficial em contêiner Docker.

O `.env.example` documenta variáveis sem credenciais reais. O Compose injeta segredos no contêiner da API e define o host do banco como `mysql`. O banco mantém o nome interno `aprendizagem_colaborativa` para preservar a instalação e o volume já existentes.

## Funcionalidades

- Cadastro, login JWT, perfil autenticado e saída que remove o token local.
- Publicação, edição e cancelamento lógico de atividades.
- Catálogo com filtros por tema, tipo, nível e data; detalhes públicos do organizador.
- Inscrição/cancelamento, consulta das próprias inscrições e lista de inscritos restrita ao organizador.
- Vagas concorrentes protegidas por transação e bloqueio de linha no MySQL; inscrições duplicadas e atividades lotadas retornam conflito.
- Mensagens de erro da API e da interface em português.
- RF09 (presença/QR Code) e RF10 (avaliação) não fazem parte do MVP. MongoDB Atlas e Python/Pandas estão planejados, não implementados.

## Validação e limitações

`dotnet build` e `npm run build` passaram. MySQL ficou `healthy`, migrations foram aplicadas e a API respondeu HTTP 200. A checagem funcional cobriu cadastro/login, publicação, busca, detalhe, inscrição, duplicidade, lotação, autorização e consulta de inscritos; duas inscrições simultâneas para uma vaga retornaram 201 e 409.

Cinco testes de integração de autenticação passaram anteriormente. Não foram executados testes automatizados de RF03–RF08/frontend nesta entrega. Teste interativo completo no navegador, carga k6/JMeter, Lighthouse e viewports responsivos continuam pendentes. Consulte `docs/matriz-validacao.md` e `docs/evidencias/validacao-funcional.md`.

## Documentação

- `docs/progresso-mvp.md` — etapas e evidências.
- `docs/Ficha_Desenvolvimento_MVP.md` — ficha de desenvolvimento.
- `docs/Ficha_Tecnica_Topico_12_RASCUNHO.md` — rascunho provisório; o modelo oficial não foi fornecido.
- `docs/Roteiro_Demonstracao.md` — demonstração local.
- `docs/Plano_Piloto.md` e `docs/piloto-registro.csv` — piloto proposto, ainda não executado.
- `CHANGELOG.md` — alterações da versão MVP.

O pacote de envio exclui os itens ignorados pelo `.gitignore`, incluindo `.env`, configurações locais de agente, documentos-fonte de referência, dependências e builds. Inclui `.env.example`, código e documentação acadêmica.