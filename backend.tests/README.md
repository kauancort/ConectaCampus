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
As classes compartilham uma única instância da API e executam em sequência sobre o banco;
os testes de concorrência continuam enviando requisições simultâneas dentro de cada caso.
O teste de atualização dos perfis cria um banco auxiliar com nome aleatório e o remove ao terminar,
inclusive em falhas. Por isso, a suíte completa exige um usuário com permissão `CREATEDB`;
o `POSTGRES_USER` do contêiner descartável abaixo já atende a esse requisito.

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
Somados aos testes existentes, unitários, de migração PostgreSQL e de perfis de conta, são 58 casos.
Os casos adicionais validam reaplicação das migrations sem duplicar o seed, isolamento
das tabelas/Identity em schema privado e filtros por tema sem distinguir maiúsculas,
com `%`, `_` e `\` tratados como texto literal.

Os testes de conta verificam cadastro, login e perfil de aluno/professor, a claim `account_type`
no JWT, a rejeição de tipos inválidos e o HTTP 403 para alunos nas rotas de criação e listagem
de atividades do professor. Os testes de atividades cadastram os responsáveis como professores;
as inscrições continuam usando a autenticação existente.

A suíte também verifica HTTP 401 sem token, rejeição de JWT com claim adulterada, tentativa
de trocar o perfil pelo corpo do login e novo cadastro com e-mail existente e outro perfil.
O teste de atualização aplica a migration inicial em um banco auxiliar, cria contas e uma
atividade legadas e aplica `AddAccountTypes`: organizador e responsável tornam-se professores,
o aluno permanece aluno, e IDs, dados, hashes de senha e atividade são preservados.

Na validação da integração, os 58 casos passaram em PostgreSQL 17 local, incluindo as novas
verificações de intervalos adjacentes e de atividades canceladas, que saem do catálogo e
liberam a sala. O teste de atualização removeu seu banco auxiliar. A suíte anterior de
52 casos também foi validada com `SSL Mode=VerifyFull` e certificado temporário.

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
