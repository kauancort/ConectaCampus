# Testes do back-end

Requisito: .NET SDK 10. Execute os comandos a partir da raiz do repositório.

## Testes unitários, sem banco

```powershell
dotnet test backend.tests/AprendizagemColaborativa.Api.Tests.csproj --filter FullyQualifiedName~AssentosServiceTests
```

Os 7 casos verificam posições, ordenação, códigos, capacidade, contagem de disponíveis,
status e horários das grades DESKTOP (4 × 6) e NOTEBOOK (4 × 5).

## Suíte completa com MySQL isolado

Os testes de integração usam o provider MySQL real, as migrations e o login JWT existente.
Criam alunos pela API de cadastro e removem os próprios registros ao terminar.

Em um terminal, inicie um banco exclusivo para testes. Mantenha o comando em execução
até os testes terminarem. As credenciais abaixo são apenas para esse contêiner local descartável.
No Windows com Docker pelo WSL:

```powershell
wsl -e docker run --rm --name conectacampus-tests --publish 127.0.0.1:13306:3306 --env MYSQL_RANDOM_ROOT_PASSWORD=yes --env MYSQL_DATABASE=conectacampus_tests --env MYSQL_USER=conectacampus_tests --env MYSQL_PASSWORD=LocalTests2026 mysql:8.4
```

Depois que o MySQL anunciar `ready for connections`, em outro terminal PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=127.0.0.1;Port=13306;Database=conectacampus_tests;User=conectacampus_tests;Password=LocalTests2026'
$env:JWT_ISSUER = 'ConectaCampus.Tests'
$env:JWT_AUDIENCE = 'ConectaCampus.Tests'
$env:JWT_SIGNING_KEY = 'Testes-Isolados-ConectaCampus-Chave-2026-32bytes'
dotnet test backend.tests/AprendizagemColaborativa.Api.Tests.csproj
```

A aplicação aplica as migrations na inicialização. Ao terminar, pare apenas o contêiner
de testes com `wsl -e docker stop conectacampus-tests`.

Os 21 casos novos de integração verificam o mapa público sem dados pessoais, o isolamento
entre atividades, autenticação, uso do aluno do token, consulta das próprias inscrições,
assentos inválidos, duplicidade, concorrência, cancelamento e reinscrição. Também provocam
uma violação real do índice único para verificar HTTP 409 e rollback da transação.
Somados aos testes existentes e unitários, são 37 casos.

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

As reservas usam transação e bloqueio de linha da atividade no MySQL. Os índices únicos
existentes `(AtividadeId, AlunoId)` e `(AtividadeId, AssentoId)` também protegem contra
duplicidade; violações de chave única retornam 409.
