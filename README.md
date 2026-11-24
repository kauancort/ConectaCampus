# ConectaCampus

ConectaCampus é uma plataforma acadêmica para aproximar quem organiza e quem participa de atividades no campus. A ideia é simples: um responsável agenda uma aula ou palestra, informa o tipo de ambiente necessário e o sistema encontra uma sala compatível, evitando reservas no mesmo horário. Os alunos podem descobrir as atividades publicadas e se inscrever nelas.

A aplicação integra frontend React, ASP.NET Core Minimal API e PostgreSQL com ASP.NET Identity/JWT. O ambiente local é executado com Docker Compose.

O projeto foi desenvolvido como MVP para o Projeto Integrador de Sistemas Inteligentes III da Fatec Shunji Nishimura de Pompeia.

## O que a aplicação resolve

- Centraliza a divulgação de aulas e palestras acadêmicas.
- Evita conflitos de agenda ao reservar automaticamente uma sala física disponível.
- Trabalha com dois tipos de laboratório já preparados no banco:
  - `DESKTOP`: 24 lugares, organizados em 4 × 6.
  - `NOTEBOOK`: 20 lugares, organizados em 4 × 5.
- Permite pesquisar atividades por tema, consultar detalhes e ver as atividades do responsável autenticado.
- Mantém autenticação por conta e token JWT para as ações privadas.

## Como funciona

1. O responsável cria uma atividade com tema, descrição, modalidade, tipo de sala, início e fim.
2. A API procura uma sala disponível do tipo escolhido.
3. A reserva é criada em transação: horários que se sobrepõem na mesma sala são recusados com HTTP `409 Conflict`, inclusive em solicitações simultâneas.
4. A atividade publicada fica disponível no catálogo para os alunos.

As reservas usam transações e `SELECT ... FOR UPDATE` no PostgreSQL. Reservas de sala usam Read Committed para consultar o estado confirmado após a espera pelo bloqueio; índices únicos e SQLSTATE `23505` mantêm HTTP 409 para duplicidade. Datas são gravadas em UTC como `timestamp with time zone`, IDs como `uuid`, e buscas por tema usam `ILIKE` com escape de caracteres literais.

## Tecnologias

| Camada | Tecnologias |
| --- | --- |
| Interface | React, TypeScript, Vite e Nginx |
| API | ASP.NET Core Minimal API (.NET 10) |
| Dados | Entity Framework Core 10, Npgsql 10.0.4 e PostgreSQL 17 |
| Autenticação | ASP.NET Core Identity e JWT |
| Ambiente local | Docker Compose |

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
| `POST` | `/api/atividades` | Sim | Cria uma atividade e reserva uma sala. |
| `GET` | `/api/atividades?tema=...` | Não | Lista atividades, com filtro opcional por tema. |
| `GET` | `/api/atividades/{id}` | Não | Consulta os detalhes de uma atividade. |
| `GET` | `/api/atividades/minhas` | Sim | Lista atividades criadas pelo responsável. |

Exemplo de criação de atividade, usando o token recebido no login:

```bash
curl -X POST http://127.0.0.1:5077/api/atividades \
  -H "Authorization: Bearer SEU_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "tema": "Introdução ao desenvolvimento web",
    "descricao": "Encontro prático para conhecer HTML, CSS e JavaScript.",
    "modalidade": "AULA",
    "tipoSala": "DESKTOP",
    "inicio": "2026-12-01T19:00:00-03:00",
    "fim": "2026-12-01T21:00:00-03:00"
  }'
```

Valores aceitos:

- `modalidade`: `AULA` ou `PALESTRA`.
- `tipoSala`: `DESKTOP` ou `NOTEBOOK`.
- `inicio` e `fim`: data/hora ISO 8601 com fuso, sendo `fim` posterior a `inicio`.

## Testes

Validação local desta migração: build Release e publish concluídos sem erros, imagem construída com o Dockerfile existente e Compose validado. Os 41 testes passaram com PostgreSQL 17, inclusive em uma segunda execução com TLS e `SSL Mode=VerifyFull`. A imagem Linux também iniciou sobre um banco vazio, criou automaticamente as nove tabelas (incluindo o histórico), respondeu HTTP 200 em `/health` e teve conexão TLS 1.3 confirmada em `pg_stat_ssl`. O EF não detectou diferenças entre o modelo e a migration.

Para compilar e executar a suíte com um PostgreSQL exclusivo para testes, consulte [backend.tests/README.md](backend.tests/README.md). Os testes usam o provider real, migrations automáticas, Identity e JWT, incluindo concorrência, duplicidade, rollback, cancelamento e reinscrição. Não aponte testes para o banco local de desenvolvimento.

O frontend não foi alterado nesta migração. Teste interativo completo no navegador, carga k6/JMeter, Lighthouse e viewports responsivos continuam pendentes.

## Equipe

- Kauan Cortiço
- Gabriel Barella
- Wesley Servelin
- Leonardo Cabral
- David Vanjão

## Licença

Projeto acadêmico desenvolvido para fins educacionais.
