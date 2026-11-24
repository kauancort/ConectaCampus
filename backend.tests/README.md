# Testes do back-end

Requisito: .NET SDK 10. Execute os comandos a partir da raiz do repositório.

## Testes unitários, sem banco

```powershell
dotnet test backend.tests/AprendizagemColaborativa.Api.Tests.csproj --filter FullyQualifiedName~AssentosServiceTests
```

Os 7 casos verificam posições, ordenação, códigos, capacidade, contagem de disponíveis,
status e horários das grades DESKTOP (4 × 6) e NOTEBOOK (4 × 5).

## Suíte completa com PostgreSQL isolado

Os testes de integração usam o provider Npgsql real, as migrations PostgreSQL e o login JWT existente.
Criam alunos pela API de cadastro e removem os próprios registros ao terminar.

Em um terminal, inicie um banco exclusivo para testes e mantenha o contêiner ativo
até os testes terminarem. Gere uma senha aleatória para esse contêiner local descartável.
No Windows com Docker pelo WSL:

```powershell
$testDbPassword = [Guid]::NewGuid().ToString('N')
wsl -e docker run --detach --rm --name conectacampus-tests --publish 127.0.0.1:15432:5432 --env POSTGRES_DB=conectacampus_tests --env POSTGRES_USER=conectacampus_tests --env "POSTGRES_PASSWORD=$testDbPassword" postgres:17
wsl -e docker exec conectacampus-tests pg_isready -U conectacampus_tests -d conectacampus_tests
```

Depois que `pg_isready` indicar `accepting connections`, no mesmo terminal PowerShell
(repita a checagem se o banco ainda estiver iniciando):

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=127.0.0.1;Port=15432;Database=conectacampus_tests;Username=conectacampus_tests;Password=$testDbPassword;SSL Mode=Disable"
$env:JWT_ISSUER = 'ConectaCampus.Tests'
$env:JWT_AUDIENCE = 'ConectaCampus.Tests'
$env:JWT_SIGNING_KEY = [Guid]::NewGuid().ToString('N')
dotnet tool restore
dotnet build backend/AprendizagemColaborativa.Api.csproj --configuration Release
dotnet test backend.tests/AprendizagemColaborativa.Api.Tests.csproj
```

A aplicação aplica as migrations na inicialização. Ao terminar, pare apenas o contêiner
de testes com `wsl -e docker stop conectacampus-tests`. Esse contêiner é descartável
e não monta nenhum volume do MySQL ou do ambiente local. Nunca use a conexão de produção.
Se o WSL encerrar automaticamente quando não houver uma sessão aberta, mantenha um terminal
WSL aberto enquanto executa os testes.

Os 21 casos novos de integração verificam o mapa público sem dados pessoais, o isolamento
entre atividades, autenticação, uso do aluno do token, consulta das próprias inscrições,
assentos inválidos, duplicidade, concorrência, cancelamento e reinscrição. Também provocam
uma violação real do índice único para verificar HTTP 409 e rollback da transação.
Somados aos testes existentes, unitários e de migração PostgreSQL, são 41 casos.
Os casos adicionais validam reaplicação das migrations sem duplicar o seed, isolamento
das tabelas/Identity em schema privado e filtros por tema sem distinguir maiúsculas,
com `%`, `_` e `\` tratados como texto literal.

## Contratos implementados

- `GET /api/atividades/{id}/assentos`: identificação, tema, data, início/fim em UTC,
  sala, capacidade total, lugares disponíveis e assentos ordenados por fileira/coluna.
  Status: `DISPONIVEL` ou `OCUPADO`, considerando somente inscrições ativas dessa atividade.
  Atividade inexistente retorna 404; atividade legada sem sala retorna 409.
- `POST /api/atividades/{id}/inscricoes`: exige JWT e corpo `{"assentoId":"UUID"}`.
  Obtém o aluno exclusivamente do token. Retorna 201 em reservas e reinscrições,
  400 para assento inválido, 401 sem autenticação, 404 para atividade inexistente e
  409 em duplicidade, assento ocupado, falta de vagas ou atividade cancelada.
- `GET /api/inscricoes/minhas`: exige JWT e inclui atividade, sala, início/fim e assento.
  O caminho anterior `/api/inscricoes/me` continua disponível com a mesma resposta.
- O cancelamento existente (`DELETE /api/atividades/{id}/inscricoes/me`) libera o assento.
  Inscrições canceladas continuam no histórico, sem assento reservado.

As reservas usam transação e bloqueio de linha da atividade no PostgreSQL. Os índices únicos
existentes `(AtividadeId, AlunoId)` e `(AtividadeId, AssentoId)` também protegem contra
duplicidade; violações de chave única retornam 409.
