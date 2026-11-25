# ConectaCampus

ConectaCampus é uma plataforma acadêmica de aprendizagem entre alunos. Alunos autorizados propõem aulas; professores avaliam as propostas, assumem a supervisão das que aprovam e acompanham os participantes. A aprovação reserva uma sala compatível e publica a aula para inscrições.

A aplicação integra frontend React, ASP.NET Core Minimal API e PostgreSQL com ASP.NET Identity/JWT. O ambiente local é executado com Docker Compose.

Visitantes podem conhecer o projeto na página inicial e criar uma conta. O catálogo de aulas, seus detalhes e o mapa de assentos exigem login.

O projeto foi desenvolvido como MVP para o Projeto Integrador de Sistemas Inteligentes III da Fatec Shunji Nishimura de Pompeia.

## O que a aplicação resolve

- Centraliza a divulgação de aulas e palestras acadêmicas.
- Evita conflitos de agenda entre aulas cadastradas para o mesmo número de sala.
- Trabalha com dois tipos de laboratório já preparados no banco:
  - `DESKTOP`: 24 lugares, organizados em 4 × 6.
  - `NOTEBOOK`: 20 lugares, organizados em 4 × 5.
- Permite pesquisar atividades por tema, consultar detalhes e ver as atividades do responsável autenticado.
- Mantém autenticação por conta e token JWT para as ações privadas.

## Como funciona

1. O aluno solicita autorização para ministrar; um professor avalia o pedido.
2. O aluno autorizado envia uma proposta com título, tema, descrição, número e tipo de sala e horário. Professores veem uma fila compartilhada.
3. Ao aprovar, o professor assume a responsabilidade e o sistema reserva a sala informada. Se ela estiver ocupada no horário, a proposta continua pendente e o professor pode recusá-la com uma justificativa.
4. A aula aprovada aparece no catálogo. Os alunos escolhem assentos e o professor responsável acompanha inscrições e registra presença.

Contas aprovadas como ministrantes pelo fluxo anterior são migradas para a permissão de aluno ministrante; elas deixam de ser professores, pois essa permissão havia sido concedida automaticamente. Atividades antigas permanecem no banco, mas não recebem um professor aprovador ou número de sala inventado.

O número da sala aceita até seis dígitos; zeros à esquerda são desconsiderados. O tipo de uma sala numerada deve permanecer consistente. A primeira aprovação cria o mapa de lugares conforme o tipo informado; essa informação deve ser conferida pelo professor, pois a plataforma não consulta um cadastro externo de salas.

As reservas usam transações, bloqueio por número de sala e `SELECT ... FOR UPDATE` no PostgreSQL. Reservas de sala usam Read Committed para consultar o estado confirmado após a espera pelo bloqueio; índices únicos e SQLSTATE `23505` mantêm HTTP 409 para duplicidade. Datas são gravadas em UTC como `timestamp with time zone`, IDs como `uuid`, e buscas por tema usam `ILIKE` com escape de caracteres literais.

## Tecnologias

| Camada | Tecnologias |
| --- | --- |
| Interface | React, TypeScript, Vite e Nginx |
| API | ASP.NET Core Minimal API (.NET 10) |
| Dados | Entity Framework Core 10, Npgsql 10.0.4 e PostgreSQL 17 |
| Autenticação | ASP.NET Core Identity e JWT |
| Ambiente local | Docker Compose |

## Cadastro de aluno e professor

Na tela de acesso (`/login`), escolha **Entrar**, **Cadastro de aluno** ou **Cadastro de professor**. O login é único para os dois perfis: professores seguem para **Minhas atividades**, com o botão **Lançar aula**; alunos seguem para o catálogo e suas inscrições. Professores também encontram **Lançar aula** diretamente no catálogo. Ao reabrir uma sessão, o frontend consulta `/api/auth/me` e atualiza o perfil salvo, aguardando essa consulta antes de decidir o acesso às rotas do professor.

O cadastro recebe `accountType` com o valor `aluno` ou `professor`, escolhido pelo usuário. O perfil é salvo no banco e retornado no cadastro, login e `/api/auth/me`; o JWT inclui a claim `account_type`. Requisições antigas de cadastro sem esse campo continuam criando contas de aluno. A API exige perfil de professor em `POST /api/atividades` e `GET /api/atividades/minhas`; contas de aluno recebem HTTP 403 nessas rotas.

A migration automática `AddAccountTypes` adiciona o perfil sem apagar usuários ou atividades. Contas que já organizavam ou eram responsáveis por atividades recebem o perfil de professor; as demais recebem aluno. Após atualizar o backend e o frontend, saia e entre novamente para renovar o token e o perfil salvo no navegador. O `vercel.json` do frontend permite abrir e recarregar `/login` e as rotas do professor diretamente na Vercel.

## Executar localmente

### Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) ou Docker Engine com Docker Compose.
- Git para obter o repositório.

Não é necessário instalar .NET SDK ou Node.js para executar a aplicação com Docker.

### 1. Criar o arquivo de ambiente

Na raiz do projeto, copie o exemplo:

```bash
cp .env.example .env
```

No PowerShell:

```powershell
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
```

Preencha `.env` com uma senha local forte e uma chave JWT aleatória de pelo menos 32 bytes. Mantenha `POSTGRES_PASSWORD` igual ao campo `Password` de `ConnectionStrings__DefaultConnection`. No Compose, use `Host=postgres;Port=5432`; `SSL Mode=Disable` é exclusivo do banco local. A API recebe a connection string pela variável de ambiente, sem credenciais no `appsettings.json`. Não envie nem compartilhe `.env`.

Em sistemas com OpenSSL, uma chave pode ser gerada assim:

```bash
openssl rand -base64 48
```

> O `.env` contém segredos locais e não deve ser enviado ao Git.

### 2. Iniciar os serviços

```bash
docker compose config --quiet
docker compose up --build -d
docker compose ps
```

O primeiro build pode levar alguns minutos. O Compose inicia três serviços:

| Serviço | Responsabilidade | Endereço local |
| --- | --- | --- |
| `web` | Interface React servida por Nginx | http://127.0.0.1:5173 |
| `api` | API ASP.NET Core | http://127.0.0.1:5077 |
| `postgres` | Banco de dados PostgreSQL 17 | `127.0.0.1:5432` (ou `POSTGRES_PORT`) |

As migrations, as duas salas e os 44 assentos iniciais são aplicados automaticamente quando a API inicia. O Nginx encaminha `/api` e `/health` para a API. Para acompanhar a inicialização:

```bash
docker compose logs -f api
```

### 3. Verificar se está funcionando

```bash
curl http://127.0.0.1:5077/health
```

A resposta esperada é:

```json
{ "status": "ok" }
```

Depois, abra http://127.0.0.1:5173 no navegador.

### Contas de demonstração

Com `DEMO_ACCOUNTS_ENABLED=true` no `.env` (como no exemplo), a API cria estas contas na primeira inicialização:

| Perfil | E-mail | Senha |
| --- | --- | --- |
| Aluno | `aluno@conectacampus.local` | `Campus@123` |
| Professor | `professor@conectacampus.local` | `Campus@123` |

Use essas credenciais apenas no ambiente local: a senha é pública. Para desativar a criação automática, defina `DEMO_ACCOUNTS_ENABLED=false` e reinicie a API; a desativação não remove contas já criadas. A configuração é desativada por padrão quando a variável não está definida e não deve ser habilitada em produção.

A inicialização não altera senha, perfil ou outros dados de uma conta que já use esses e-mails. Nesse caso, as credenciais da tabela só funcionarão se corresponderem à conta existente.

### Dados para apresentação

Com a opção acima ativada, o banco recebe automaticamente alunos e professores fictícios, aulas e palestra, propostas aprovadas, pendentes e recusadas, além de inscrições com assentos ocupados. Reiniciar a aplicação não duplica esses registros; os horários são definidos na primeira criação. Nomes, curso e marcadores exatos criados pela versão antiga dos dados locais são atualizados para retirar o texto de demonstração; outros dados e senhas existentes são preservados. As contas principais recebem exemplos de participação e de supervisão quando ainda usam os dados e a senha iniciais. Se um e-mail reservado já pertencer a outra conta, os dados fictícios não são associados a ela.

O antigo script `scripts/popular-aulas-demo.mjs` é opcional e independente; não é necessário para os dados iniciais.

### Windows com WSL

Se o Docker estiver no WSL e o comando for executado pelo PowerShell, abra um terminal WSL na pasta do projeto e rode os mesmos comandos acima. Uma alternativa é:

```powershell
$projectWslPath = (wsl -e wslpath -u "$PWD").Trim()
wsl --cd $projectWslPath -e docker compose up --build -d
```

## Parar ou reiniciar

Para parar os contêineres, preservando os dados locais:

```bash
docker compose down
```

Para iniciá-los novamente:

```bash
docker compose up -d
```

O PostgreSQL usa o novo volume persistente `aprendizagem-colaborativa_postgres_data`. Não use `docker compose down -v` se quiser preservar os dados. O nome do projeto Compose continua `aprendizagem-colaborativa`.

### Substituição do MySQL por PostgreSQL

A migração atual parte de um banco PostgreSQL vazio; ela não transfere registros do MySQL. O Compose cria e usa apenas o volume `aprendizagem-colaborativa_postgres_data`. O volume MySQL anterior não é montado nem alterado.

O `.env.example` documenta as variáveis locais sem credenciais reais. O Compose injeta `ConnectionStrings__DefaultConnection` no contêiner da API. Para executar `dotnet` fora do Compose, exporte as variáveis no processo e troque o host da connection string para `127.0.0.1`; o ASP.NET não carrega `.env` automaticamente.

## API principal

| Método | Rota | Autenticação | Finalidade |
| --- | --- | --- | --- |
| `POST` | `/api/auth/register` | Não | Cria uma conta de aluno/responsável. |
| `POST` | `/api/auth/login` | Não | Retorna um token JWT. |
| `GET` | `/api/salas/tipos` | Não | Lista os tipos de sala disponíveis. |
| `POST` | `/api/propostas` | Aluno autorizado | Envia uma proposta para a fila compartilhada. |
| `GET` | `/api/propostas/minhas` | Aluno | Consulta suas propostas e decisões. |
| `GET` | `/api/propostas/pendentes` | Professor | Lista a fila compartilhada. |
| `POST` | `/api/propostas/{id}/avaliar` | Professor | Aprova e reserva sala ou recusa com motivo. |
| `GET` | `/api/propostas/aprovadas` | Professor | Lista aulas que o professor aprovou. |
| `PUT` | `/api/atividades/{id}/inscricoes/{alunoId}/presenca` | Professor responsável | Registra ou limpa a presença de um inscrito. |
| `GET` | `/api/atividades?tema=...` | Sim | Lista atividades, com filtro opcional por tema. |
| `GET` | `/api/atividades/{id}` | Sim | Consulta os detalhes de uma atividade. |

Exemplo de proposta de aula, usando o token de um aluno autorizado:

```bash
curl -X POST http://127.0.0.1:5077/api/propostas \
  -H "Authorization: Bearer SEU_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "titulo": "Primeiros passos na web",
    "tema": "Desenvolvimento web",
    "descricao": "Encontro prático para conhecer HTML, CSS e JavaScript.",
    "tipoSala": "DESKTOP",
    "numeroSala": "101",
    "inicio": "2026-12-01T19:00:00-03:00",
    "fim": "2026-12-01T21:00:00-03:00"
  }'
```

Valores aceitos:

- `tipoSala`: `DESKTOP` ou `NOTEBOOK`.
- `numeroSala`: número da sala em texto, com até seis dígitos.
- `inicio` e `fim`: data/hora ISO 8601 com fuso, sendo `fim` posterior a `inicio`.

## Testes

Além dos testes de unidade e integração da API com PostgreSQL, o frontend possui testes de componentes e testes de ponta a ponta com Chromium, API e banco reais. O fluxo de navegador cobre cadastro, login, solicitação e aprovação de acesso, publicação, seleção de assento, cancelamento, notificações e disputa simultânea pelo mesmo lugar. As três suítes também são executadas no GitHub Actions.

Para executar os testes de navegador localmente, use um banco **descartável e exclusivo** chamado `conectacampus_e2e`; os testes criam contas e atividades e concedem acesso de professor diretamente nesse banco. Nunca use o banco de desenvolvimento ou produção. Com Docker e .NET SDK 10 instalados:

```bash
docker run -d --rm --name conectacampus-e2e -p 127.0.0.1:15433:5432 -e POSTGRES_DB=conectacampus_e2e -e POSTGRES_USER=conectacampus_e2e -e POSTGRES_PASSWORD=local_e2e_password postgres:17
```

Em outro terminal, aguarde o PostgreSQL iniciar e execute a API com esta configuração de testes:

```bash
export ConnectionStrings__DefaultConnection='Host=127.0.0.1;Port=15433;Database=conectacampus_e2e;Username=conectacampus_e2e;Password=local_e2e_password;SSL Mode=Disable'
export JWT_ISSUER=ConectaCampus.E2E JWT_AUDIENCE=ConectaCampus.E2E JWT_SIGNING_KEY=test-only-signing-key-with-at-least-32-bytes
export ASPNETCORE_URLS=http://127.0.0.1:5077 ASPNETCORE_ENVIRONMENT=Development
dotnet run --project backend/AprendizagemColaborativa.Api.csproj
```

Com a API ativa, execute em outro terminal:

```bash
cd frontend
npm ci
npx playwright install chromium
E2E_DATABASE_URL='postgres://conectacampus_e2e:local_e2e_password@127.0.0.1:15433/conectacampus_e2e' npm run test:e2e
```

O teste inicia o Vite na porta 5173; as portas 5173 e 5077 devem estar livres antes de iniciar os respectivos serviços. Após os testes, encerre a API e rode `docker stop conectacampus-e2e` para remover o contêiner e os dados descartáveis.

### Conceder acesso de professor manualmente

Além da conta de demonstração opcional, é possível criar uma conta pela interface e conceder acesso de professor usando a mesma configuração de banco e JWT da API. No ambiente Compose, com a API já iniciada:

```bash
docker compose run --rm api conceder-professor pessoa@exemplo.com
```

Os demais usuários podem solicitar acesso na página **Acesso e notificações**; um professor existente avalia a solicitação. A permissão é verificada no banco em cada ação protegida.

Validação local desta migração: build Release e publish concluídos sem erros, imagem construída com o Dockerfile existente e Compose validado. Os 41 testes passaram com PostgreSQL 17, inclusive em uma segunda execução com TLS e `SSL Mode=VerifyFull`. A imagem Linux também iniciou sobre um banco vazio, criou automaticamente as nove tabelas (incluindo o histórico), respondeu HTTP 200 em `/health` e teve conexão TLS 1.3 confirmada em `pg_stat_ssl`. O EF não detectou diferenças entre o modelo e a migration.

Para compilar e executar a suíte com um PostgreSQL exclusivo para testes, consulte [backend.tests/README.md](backend.tests/README.md). Os testes usam o provider real, migrations automáticas, Identity e JWT, incluindo concorrência, duplicidade, rollback, cancelamento e reinscrição. Não aponte testes para o banco local de desenvolvimento.

Essa validação foi realizada durante a migração para PostgreSQL. Desde então, o frontend e suas suítes foram ampliados; testes de carga e Lighthouse continuam pendentes.

## Equipe

- Kauan Cortiço
- Gabriel Barella
- Wesley Servelin
- Leonardo Cabral
- David Vanjão

## Licença

Projeto acadêmico desenvolvido para fins educacionais.

<!-- deploy vercel -->
