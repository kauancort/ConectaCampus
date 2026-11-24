# ConectaCampus

MVP acadêmico para compartilhar atividades de aprendizagem entre alunos. A aplicação integra frontend React, ASP.NET Core Minimal API e PostgreSQL com ASP.NET Identity/JWT. O ambiente local usa Docker Compose; em produção, a API pode executar no Render conectada ao PostgreSQL do Supabase.

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

Preencha `.env` com uma senha local forte e uma chave JWT aleatória de pelo menos 32 bytes. Mantenha `POSTGRES_PASSWORD` igual ao campo `Password` de `ConnectionStrings__DefaultConnection`. No Compose, use `Host=postgres;Port=5432`; `SSL Mode=Disable` é exclusivo do banco local. A API recebe a connection string pela variável de ambiente, sem credenciais no `appsettings.json`. Não envie nem compartilhe `.env`. Depois, na mesma pasta:

```powershell
$projectWslPath = (wsl -e wslpath -u "$PWD").Trim()
wsl --cd $projectWslPath -e docker compose config --quiet
wsl --cd $projectWslPath -e docker compose up --build -d
wsl --cd $projectWslPath -e docker compose ps
```

O Compose sobe PostgreSQL 17, API e frontend. O backend aplica migrations pendentes ao iniciar; o frontend Nginx encaminha `/api` e `/health` para a API.

- **Aplicação:** http://127.0.0.1:5173/
- **API/health:** http://127.0.0.1:5077/health
- **Health pela aplicação:** http://127.0.0.1:5173/health
- **PostgreSQL:** acessível localmente em `127.0.0.1:5432` (ou `POSTGRES_PORT`).

Para parar os serviços sem remover o banco:

```powershell
wsl --cd $projectWslPath -e docker compose down
```

O PostgreSQL usa o novo volume persistente `aprendizagem-colaborativa_postgres_data`. Não use `docker compose down -v` se quiser preservar os dados. O nome do projeto Compose continua `aprendizagem-colaborativa`.

### Preservação do MySQL anterior

A migration PostgreSQL parte de um banco vazio; ela não transfere registros do MySQL. As migrations antigas foram substituídas no código e continuam disponíveis no histórico Git. Nenhum comando de exclusão ou alteração é executado no MySQL: o volume `aprendizagem-colaborativa_mysql_data` permanece separado e não é montado no PostgreSQL. Não remova esse volume nem execute limpeza de volumes. Se o contêiner antigo estiver ativo, ele pode permanecer como órfão do Compose; não é necessário usar `--remove-orphans` para iniciar PostgreSQL.

## Supabase e Render

O Supabase é utilizado somente como PostgreSQL. Cadastro, senhas e tokens continuam sob responsabilidade do ASP.NET Identity/JWT; não são utilizados Supabase Auth, SDK, chave `anon` ou `service_role`.

1. Crie um projeto Supabase com um banco de aplicação vazio. Em **Connect > Session pooler**, copie host, porta `5432`, database `postgres` e usuário `postgres.<project-ref>`. Use a senha do banco. O Session Pooler atende conexões persistentes e redes IPv4; não use a conexão Transaction Pooler da porta `6543` para esta configuração.
2. Em **Database Settings > SSL Configuration**, baixe a CA do banco e habilite a exigência de SSL. No Render, adicione essa CA como **Secret File**, com o nome `supabase-ca.crt` (disponível em `/etc/secrets/supabase-ca.crt`).
3. A aplicação não utiliza a Data API: no painel Supabase, abra **Data API** e desative **Enable Data API**. As migrations criam todas as tabelas, inclusive Identity e histórico, no schema privado `conectacampus`, revogando o acesso de `PUBLIC` ao schema. Não inclua `conectacampus` em **Exposed schemas**, nem conceda acesso a `anon`, `authenticated` ou `service_role`. Assim as tabelas não dependem da exposição padrão do schema `public`.
4. No Render, crie um **Web Service**, runtime **Docker**, **Root Directory** `backend`, **Dockerfile Path** `./Dockerfile` e contexto de build `.` (relativos a `backend`). Configure **Health Check Path** `/health`. O Dockerfile existente publica .NET 10 e escuta em `0.0.0.0:8080`.
5. Configure as variáveis abaixo no painel do Render. No primeiro início, a API aplica automaticamente `InitialPostgreSql` e insere as duas salas e os 44 assentos iniciais. A conta de conexão precisa poder criar o schema/tabelas e aplicar migrations; o usuário `postgres.<project-ref>` do Session Pooler é compatível.

| Variável no Render | Valor |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | Connection string Npgsql abaixo, com os dados do Session Pooler |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_HTTP_PORTS` | `8080` (já definido no Dockerfile) |
| `PORT` | `8080`, para alinhar a porta do Render com a API |
| `JWT_ISSUER` | `ConectaCampus.Api` (ou o emissor já utilizado) |
| `JWT_AUDIENCE` | `ConectaCampus.Client` (ou a audiência já utilizada) |
| `JWT_SIGNING_KEY` | Segredo aleatório de pelo menos 32 bytes; preserve a chave existente se precisar manter tokens válidos |
| `JWT_EXPIRATION_MINUTES` | `60` (opcional; padrão 60) |
| `Cors__AllowedOrigins__0` | Origem HTTPS exata do frontend, como `https://<seu-frontend>.vercel.app`, sem barra final |

Valor de `ConnectionStrings__DefaultConnection` (substitua os placeholders; não é uma URL `postgresql://`):

```text
Host=<session-pooler-host>;Port=5432;Database=postgres;Username=postgres.<project-ref>;Password="<database-password>";SSL Mode=VerifyFull;Root Certificate=/etc/secrets/supabase-ca.crt;GSS Encryption Mode=Disable;Maximum Pool Size=10;Timeout=30;Command Timeout=60
```

`VerifyFull` exige TLS e valida CA e hostname. A senha fica entre aspas duplas para aceitar ponto e vírgula; se contiver aspas duplas, duplique-as conforme o formato Npgsql. Não coloque aspas em volta de toda a variável no painel do Render. `Maximum Pool Size=10` limita conexões por instância da API; considere esse total ao aumentar o número de instâncias. O Session Pooler suporta o bloqueio de sessão usado pelas migrations. Não foi habilitado retry automático do EF, pois os serviços usam transações explícitas.

As origens CORS são configuráveis sem alterações no frontend. Para outras origens, use `Cors__AllowedOrigins__1`, `Cors__AllowedOrigins__2` etc. O deploy do frontend precisa apontar para a URL HTTPS da API, conforme a configuração de frontend já existente.

Não configure variáveis `MYSQL_*`, `POSTGRES_*` ou chaves de API do Supabase no Render: as variáveis `POSTGRES_*` são exclusivas do contêiner PostgreSQL local. Segredos e certificado são configurados no Render, sem entrar no repositório.

Referências: [Session Pooler](https://supabase.com/docs/guides/database/connecting-to-postgres), [SSL no Supabase](https://supabase.com/docs/guides/platform/ssl-enforcement), [segurança da Data API](https://supabase.com/docs/guides/api/securing-your-api), [SSL do Npgsql](https://www.npgsql.org/doc/security.html) e [Docker no Render](https://render.com/docs/docker).

## Stack

- Frontend React + TypeScript servido por Nginx.
- Backend C# / ASP.NET Core Minimal API em .NET 10.
- ASP.NET Core Identity + JWT.
- Entity Framework Core 10 e provider `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.4, compatível com .NET 10.
- PostgreSQL 17 oficial em contêiner Docker local e PostgreSQL do Supabase em produção.

O `.env.example` documenta variáveis sem credenciais reais. O Compose injeta `ConnectionStrings__DefaultConnection` no contêiner da API. O `appsettings.json` mantém a conexão vazia, e a API falha com uma mensagem de configuração se a conexão não for fornecida. Para executar `dotnet` fora do Compose, exporte as variáveis no processo e troque o host para `127.0.0.1`; o ASP.NET não carrega `.env` automaticamente.

## Funcionalidades

- Cadastro, login JWT, perfil autenticado e saída que remove o token local.
- Publicação, edição e cancelamento lógico de atividades.
- Catálogo com filtros por tema, tipo, nível e data; detalhes públicos do organizador.
- Inscrição/cancelamento, consulta das próprias inscrições e lista de inscritos restrita ao organizador.
- Vagas concorrentes protegidas por transação e `SELECT ... FOR UPDATE` no PostgreSQL; índices únicos e SQLSTATE `23505` mantêm HTTP 409 para duplicidade. Reservas de sala usam Read Committed com bloqueio das salas para consultar o estado confirmado após a espera. Datas são gravadas em UTC como `timestamp with time zone`, IDs como `uuid`, e buscas por tema usam `ILIKE` com escape de caracteres literais.
- Mensagens de erro da API e da interface em português.
- RF09 (presença/QR Code) e RF10 (avaliação) não fazem parte do MVP. MongoDB Atlas e Python/Pandas estão planejados, não implementados.

## Validação e limitações

Validação local desta migração: build Release e publish concluídos sem erros, imagem construída com o Dockerfile existente e Compose validado. Os 41 testes passaram com PostgreSQL 17, inclusive em uma segunda execução com TLS e `SSL Mode=VerifyFull`. A imagem Linux também iniciou sobre um banco vazio, criou automaticamente as nove tabelas (incluindo o histórico), respondeu HTTP 200 em `/health` e teve conexão TLS 1.3 confirmada em `pg_stat_ssl`. O EF não detectou diferenças entre o modelo e a migration.

Para compilar e executar a suíte com um PostgreSQL exclusivo para testes, consulte [backend.tests/README.md](backend.tests/README.md). Os testes usam o provider real, migrations automáticas, Identity e JWT, incluindo concorrência, duplicidade, rollback, cancelamento e reinscrição. Não aponte testes para bancos de produção ou para o MySQL preservado.

O frontend não foi alterado nesta migração. O acesso ao Supabase e o deploy efetivo no Render dependem da configuração da conta e dos segredos de produção; a validação local não comprova conectividade com esse projeto remoto. Teste interativo completo no navegador, carga k6/JMeter, Lighthouse e viewports responsivos continuam pendentes.

## Documentação

- `docs/progresso-mvp.md` — etapas e evidências.
- `docs/Ficha_Desenvolvimento_MVP.md` — ficha de desenvolvimento.
- `docs/Ficha_Tecnica_Topico_12_RASCUNHO.md` — rascunho provisório; o modelo oficial não foi fornecido.
- `docs/Roteiro_Demonstracao.md` — demonstração local.
- `docs/Plano_Piloto.md` e `docs/piloto-registro.csv` — piloto proposto, ainda não executado.
- `CHANGELOG.md` — alterações da versão MVP.

O pacote de envio exclui os itens ignorados pelo `.gitignore`, incluindo `.env`, configurações locais de agente, documentos-fonte de referência, dependências e builds. Inclui `.env.example`, código e documentação acadêmica.
