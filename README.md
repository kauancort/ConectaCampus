# ConectaCampus

ConectaCampus é uma plataforma acadêmica para aproximar quem organiza e quem participa de atividades no campus. A ideia é simples: um responsável agenda uma aula ou palestra, informa o tipo de ambiente necessário e o sistema encontra uma sala compatível, evitando reservas no mesmo horário. Os alunos podem descobrir as atividades publicadas e se inscrever nelas.

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

## Tecnologias

| Camada | Tecnologias |
| --- | --- |
| Interface | React, TypeScript, Vite e Nginx |
| API | ASP.NET Core Minimal API (.NET 10) |
| Dados | Entity Framework Core e MySQL 8.4 |
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
Copy-Item .env.example .env
```

Abra o `.env` e substitua as senhas de exemplo. A chave `JWT_SIGNING_KEY` deve ter ao menos 32 bytes. Em sistemas com OpenSSL, uma chave pode ser gerada assim:

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
| `mysql` | Banco de dados MySQL | `127.0.0.1:3306` |

As migrations e as salas de exemplo são aplicadas automaticamente quando a API inicia. Para acompanhar a inicialização:

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

O MySQL usa um volume persistente. Use `docker compose down -v` apenas se quiser apagar o banco local e começar do zero.

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

O projeto possui testes de integração para autenticação e reservas de sala, incluindo conflitos sequenciais e simultâneos de horário. A última validação executou 9 testes com sucesso.

Para rodar os testes localmente, tenha um MySQL de desenvolvimento configurado pelas variáveis `MYSQL_*` e as variáveis JWT definidas, então execute:

```bash
dotnet test backend.tests/AprendizagemColaborativa.Api.Tests.csproj
```

## Equipe

- Kauan Cortiço
- Gabriel Barella
- Wesley Servelin
- Leonardo Cabral
- David Vanjão

## Licença

Projeto acadêmico desenvolvido para fins educacionais.
