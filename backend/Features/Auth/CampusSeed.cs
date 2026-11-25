using System.Security.Cryptography;
using System.Text;
using AprendizagemColaborativa.Api.Data;
using AprendizagemColaborativa.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AprendizagemColaborativa.Api.Features.Auth;

/// <summary>Dados fictícios opcionais para apresentação local.</summary>
public static class CampusSeed
{
    private const string Domain = "@exemplo.conectacampus.local";
    private static Guid Key(string value) => new(MD5.HashData(Encoding.UTF8.GetBytes("conectacampus-seed-v1:" + value)));
    private static readonly (string Nome, string Curso, bool Professor, bool Ministrante)[] People =
    [
        ("Ana Beatriz Oliveira", "Desenvolvimento de Software Multiplataforma", false, true),
        ("Lucas Ferreira", "Desenvolvimento de Software Multiplataforma", false, true),
        ("Camila Rodrigues", "Gestão Empresarial", false, true),
        ("Gabriel Santos", "Desenvolvimento de Software Multiplataforma", false, true),
        ("Juliana Martins", "Gestão Empresarial", false, false),
        ("Pedro Henrique Souza", "Desenvolvimento de Software Multiplataforma", false, false),
        ("Larissa Mendes", "Gestão Empresarial", false, false),
        ("Rafael Lima", "Desenvolvimento de Software Multiplataforma", false, false),
        ("Isabela Ribeiro", "Gestão Empresarial", false, false),
        ("Matheus Carvalho", "Desenvolvimento de Software Multiplataforma", false, false),
        ("Beatriz Alves", "Gestão Empresarial", false, false),
        ("Thiago Nascimento", "Desenvolvimento de Software Multiplataforma", false, false),
        ("Fernanda Pereira", "Gestão Empresarial", false, false),
        ("João Vitor Cardoso", "Desenvolvimento de Software Multiplataforma", false, false),
        ("Patrícia Moreira", "Desenvolvimento de Software Multiplataforma", true, false),
        ("Eduardo Nunes", "Desenvolvimento de Software Multiplataforma", true, false),
        ("Cláudia Freitas", "Gestão Empresarial", true, false)
    ];
    private static readonly (string Titulo, string Tema, string Descricao)[] Lessons =
    [
        ("Primeiros passos com HTML e CSS", "Desenvolvimento web", "Construa uma página e conheça os elementos que dão estrutura e estilo à web."),
        ("JavaScript na prática", "Programação", "Explore variáveis e eventos ao criar pequenas interações no navegador."),
        ("Git e GitHub para equipes", "Ferramentas de desenvolvimento", "Aprenda a versionar arquivos e colaborar em um projeto em grupo."),
        ("Interfaces acessíveis", "Design e acessibilidade", "Conheça práticas para criar interfaces acessíveis com teclado e leitores de tela."),
        ("Introdução a bancos de dados", "Banco de dados", "Modele informações e pratique consultas SQL em situações do cotidiano."),
        ("Desvendando APIs", "Desenvolvimento de software", "Entenda como aplicações trocam informações e experimente requisições HTTP."),
        ("Lógica de programação", "Programação", "Resolva problemas passo a passo usando algoritmos, decisões e repetições."),
        ("Protótipos para tirar ideias do papel", "Design de interfaces", "Transforme um problema em esboços de telas e teste caminhos com outras pessoas."),
        ("Organização de projetos com Kanban", "Gestão de projetos", "Planeje tarefas em equipe e acompanhe o andamento do trabalho."),
        ("Introdução à análise de dados", "Ciência de dados", "Organize dados e encontre padrões a partir de perguntas simples."),
        ("Segurança digital no dia a dia", "Segurança da informação", "Identifique riscos comuns e conheça práticas para proteger contas."),
        ("Como apresentar seu projeto", "Comunicação", "Estruture uma apresentação e pratique a comunicação de ideias técnicas.")
    ];
    // Títulos e descrições exatos do antigo script local, para não alterar outras aulas.
    private static readonly (string Titulo, string Descricao)[] LegacyLessons =
    [
        ("Primeiros passos com HTML e CSS", "Construa uma página simples e conheça os elementos que dão estrutura e estilo à web."),
        ("JavaScript na prática", "Explore variáveis, condições e eventos ao criar pequenas interações no navegador."),
        ("Git e GitHub para trabalhos em equipe", "Aprenda a versionar arquivos, colaborar em branches e organizar um projeto em grupo."),
        ("Criando interfaces acessíveis", "Conheça práticas para tornar interfaces mais fáceis de usar com teclado e leitores de tela."),
        ("Introdução a bancos de dados", "Modele informações e pratique consultas SQL em situações comuns do cotidiano acadêmico."),
        ("Desvendando APIs", "Entenda como aplicações trocam informações e experimente requisições HTTP."),
        ("Lógica de programação sem mistério", "Resolva problemas passo a passo usando algoritmos, decisões e repetições."),
        ("Protótipos para tirar ideias do papel", "Transforme um problema em esboços de telas e teste caminhos com outras pessoas."),
        ("Organização de projetos com Kanban", "Planeje tarefas em equipe com um quadro visual e acompanhe o andamento do trabalho."),
        ("Introdução à análise de dados", "Organize uma pequena base de dados e encontre padrões com perguntas simples."),
        ("Segurança digital no dia a dia", "Identifique riscos comuns e conheça boas práticas para proteger contas e informações."),
        ("Testes automatizados para iniciantes", "Descubra como testes ajudam a verificar comportamentos e evitar regressões."),
        ("Python para começar a programar", "Escreva seus primeiros programas em Python com exercícios orientados."),
        ("Planilhas que contam histórias", "Use fórmulas e gráficos para organizar e comunicar informações de maneira clara."),
        ("Como apresentar seu projeto", "Estruture uma apresentação objetiva e pratique a comunicação de ideias técnicas."),
        ("Redes de computadores na prática", "Explore conceitos de rede e entenda o caminho percorrido por uma requisição."),
        ("Modelagem de dados para projetos", "Identifique entidades e relacionamentos a partir de um problema real."),
        ("Introdução ao desenvolvimento responsivo", "Adapte uma interface para diferentes tamanhos de tela com práticas de CSS."),
        ("Pesquisa com usuários para iniciantes", "Aprenda a preparar perguntas e a observar necessidades antes de desenhar uma solução."),
        ("Do problema à solução em equipe", "Pratique a definição de problemas e a construção colaborativa de alternativas.")
    ];

    public static async Task SeedAsync(ApplicationDbContext db, UserManager<Aluno> users, ILogger logger)
    {
        // IDs próprios evitam associar dados de exemplo a uma conta existente com o mesmo e-mail.
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(471936125)");
        // Atualiza apenas as marcas exatas deixadas pelo seed e pelo antigo script local.
        var legacyStudent = await users.FindByEmailAsync(DemoAccountSeeder.StudentEmail);
        var legacyTeacher = await users.FindByEmailAsync(DemoAccountSeeder.TeacherEmail);
        if (legacyStudent?.Nome == "Aluno de demonstração") legacyStudent.Nome = "Mariana Costa";
        if (legacyTeacher?.Nome == "Professor de demonstração") legacyTeacher.Nome = "Ricardo Almeida";
        if (legacyStudent?.Curso == "Demonstração") legacyStudent.Curso = "Desenvolvimento de Software Multiplataforma";
        if (legacyTeacher?.Curso == "Demonstração") legacyTeacher.Curso = "Desenvolvimento de Software Multiplataforma";
        const string marker = "\n\n[Demonstração ConectaCampus]";
        if (legacyStudent is not null)
        {
            var legacyProposals = await db.PropostasAula.Include(p => p.Atividade).Where(p => p.AlunoId == legacyStudent.Id && p.Descricao.EndsWith(marker)).ToListAsync();
            foreach (var proposal in legacyProposals)
            {
                proposal.Descricao = proposal.Descricao[..^marker.Length];
                if (proposal.Atividade?.Descricao.EndsWith(marker) == true) proposal.Atividade.Descricao = proposal.Atividade.Descricao[..^marker.Length];
            }
        }
        await db.SaveChangesAsync();
        var accounts = new List<Aluno>();
        for (var i = 0; i < People.Length; i++)
        {
            var email = $"pessoa{i + 1:00}{Domain}";
            var id = Key($"person:{i}").ToString();
            var existing = await users.FindByEmailAsync(email);
            if (existing is not null && existing.Id != id)
            {
                logger.LogWarning("Dados de apresentação não foram criados: o e-mail {Email} já pertence a outra conta.", email);
                return;
            }
            if (existing is null)
            {
                var person = People[i];
                existing = new Aluno { Id = id, UserName = email, Email = email, Nome = person.Nome, Curso = person.Curso, Professor = person.Professor, PodeMinistrar = person.Ministrante };
                var result = await users.CreateAsync(existing, DemoAccountSeeder.Password);
                if (!result.Succeeded) throw new InvalidOperationException($"Falha ao criar {email}: {string.Join(", ", result.Errors.Select(e => e.Code))}");
            }
            accounts.Add(existing);
        }

        var rooms = await db.Salas.AsNoTracking().Where(s => s.Id == SalaSeed.SalaDesktopId || s.Id == SalaSeed.SalaNotebookId).ToListAsync();
        if (rooms.Count != 2) throw new InvalidOperationException("Salas iniciais não encontradas para os dados de apresentação.");
        if (legacyStudent is { Professor: false, Nome: "Mariana Costa" } && await users.CheckPasswordAsync(legacyStudent, DemoAccountSeeder.Password))
        {
            var candidates = await db.PropostasAula.AsNoTracking().Include(p => p.Atividade)
                .Where(p => p.AlunoId == legacyStudent.Id && p.Status == "aprovada" && p.AtividadeId != null).ToListAsync();
            foreach (var proposal in candidates.Where(p => p.Atividade is { Status: "publicada", SalaId: not null } && LegacyLessons.Contains((p.Titulo, p.Descricao))))
            {
                var activity = (await db.Atividades.FromSqlInterpolated($"""SELECT * FROM conectacampus."Atividades" WHERE "Id" = {proposal.AtividadeId} FOR UPDATE""").ToListAsync()).SingleOrDefault();
                if (activity is not { Status: "publicada", SalaId: not null }) continue;
                var existing = await db.Inscricoes.AsNoTracking().Where(i => i.AtividadeId == activity.Id).ToListAsync();
                var seats = await db.Assentos.AsNoTracking().Where(s => s.SalaId == activity.SalaId).OrderBy(s => s.Fileira).ThenBy(s => s.Coluna).ToListAsync();
                var used = existing.Where(i => i.AssentoId != null).Select(i => i.AssentoId!.Value).ToHashSet();
                foreach (var person in accounts.Take(8).Where(a => !a.Professor && a.Id != legacyStudent.Id))
                {
                    if (existing.Any(i => i.AlunoId == person.Id)) continue;
                    var seat = seats.FirstOrDefault(s => !used.Contains(s.Id));
                    if (seat is null || used.Count >= activity.NumeroDeVagas) break;
                    used.Add(seat.Id);
                    var now = DateTime.UtcNow;
                    db.Inscricoes.Add(new Inscricao { Id = Key($"legacy:enrollment:{activity.Id}:{person.Id}"), AtividadeId = activity.Id, AlunoId = person.Id, AssentoId = seat.Id, Status = "ativa", Data = now, CriadaEmUtc = now, AtualizadaEmUtc = now });
                }
                await db.SaveChangesAsync();
            }
        }
        var day = DateTime.UtcNow.Date.AddDays(1);
        for (var i = 0; i < Lessons.Length; i++)
        {
            var activityId = Key($"activity:{i}");
            if (await db.Atividades.AnyAsync(a => a.Id == activityId)) continue;
            var room = rooms.Single(s => s.Id == (i % 2 == 0 ? SalaSeed.SalaDesktopId : SalaSeed.SalaNotebookId));
            DateTime start;
            do
            {
                start = day.AddHours(22); // 19h em São Paulo
                day = day.AddDays(1);
            } while (start.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || await db.Atividades.AnyAsync(a => a.SalaId == room.Id && a.Status == "publicada" && a.Inicio < start.AddMinutes(90) && a.Fim > start));
            var lesson = Lessons[i];
            var instructor = accounts[i % 4];
            var teacher = accounts[14 + i % 3];
            db.Atividades.Add(new Atividade { Id = activityId, Titulo = lesson.Titulo, Tema = lesson.Tema, Descricao = lesson.Descricao, Modalidade = "AULA", Tipo = "aula", Nivel = "não informado", SalaId = room.Id, Inicio = start, Fim = start.AddMinutes(90), DataHoraUtc = start, Local = room.Nome, NumeroDeVagas = room.Capacidade, ResponsavelId = instructor.Id, OrganizadorId = instructor.Id, Status = "publicada" });
            db.PropostasAula.Add(new PropostaAula { Id = Key($"proposal:{i}"), AlunoId = instructor.Id, ProfessorId = teacher.Id, AtividadeId = activityId, Titulo = lesson.Titulo, Tema = lesson.Tema, Descricao = lesson.Descricao, TipoSala = room.Tipo, Inicio = start, Fim = start.AddMinutes(90), Status = "aprovada", AvaliadaEmUtc = DateTime.UtcNow });
            var seats = await db.Assentos.AsNoTracking().Where(s => s.SalaId == room.Id).OrderBy(s => s.Fileira).ThenBy(s => s.Coluna).ToListAsync();
            var participants = accounts.Take(14).Where(a => a.Id != instructor.Id).Skip(i % 4).Take(6 + i % 5).ToArray();
            for (var j = 0; j < participants.Length; j++)
            {
                var now = DateTime.UtcNow;
                db.Inscricoes.Add(new Inscricao { Id = Key($"enrollment:{i}:{j}"), AtividadeId = activityId, AlunoId = participants[j].Id, AssentoId = seats[(j * 2 + i) % seats.Count].Id, Status = "ativa", Data = now, CriadaEmUtc = now, AtualizadaEmUtc = now });
            }
            await db.SaveChangesAsync();
        }
        var pendingId = Key("proposal:pending");
        if (!await db.PropostasAula.AnyAsync(p => p.Id == pendingId))
        {
            var start = day.AddHours(22);
            db.PropostasAula.Add(new PropostaAula { Id = pendingId, AlunoId = accounts[0].Id, Titulo = "Introdução a testes automatizados", Tema = "Qualidade de software", Descricao = "Vamos escrever testes para verificar comportamentos de uma aplicação.", TipoSala = "DESKTOP", Inicio = start, Fim = start.AddMinutes(90), Status = "pendente" });
        }
        var requestId = Key("request:pending");
        if (!await db.SolicitacoesProfessor.AnyAsync(s => s.Id == requestId) && !await db.SolicitacoesProfessor.AnyAsync(s => s.AlunoId == accounts[4].Id && s.Status == "pendente"))
            db.SolicitacoesProfessor.Add(new SolicitacaoProfessor { Id = requestId, AlunoId = accounts[4].Id, Justificativa = "Gostaria de compartilhar com a turma o que aprendi sobre organização de projetos.", Status = "pendente" });
        await db.SaveChangesAsync();

        var extraProposals = new[] {
            ("Fundamentos de Python", "Programação", "Vamos praticar variáveis, listas e funções com exercícios do cotidiano."),
            ("Organização financeira pessoal", "Finanças", "Conheça ferramentas para planejar gastos e construir um orçamento."),
            ("Dashboard com planilhas", "Análise de dados", "Transforme dados em indicadores e gráficos para apoiar decisões."),
            ("Acessibilidade na web", "Acessibilidade", "Explore recursos para tornar interfaces mais inclusivas."),
            ("Comunicação para projetos", "Comunicação", "Pratique como apresentar ideias com clareza para diferentes públicos."),
            ("Introdução ao SQL", "Banco de dados", "Aprenda a consultar e organizar informações em um banco de dados."),
            ("Prototipagem colaborativa", "Design", "Crie e avalie protótipos de interfaces junto com seus colegas."),
            ("Automação de tarefas", "Produtividade", "Descubra como automatizar tarefas repetitivas com pequenos scripts.")
        };
        for (var i = 0; i < extraProposals.Length; i++)
        {
            var proposalId = Key($"extra:pending:{i}");
            if (await db.PropostasAula.AnyAsync(p => p.Id == proposalId)) continue;
            var item = extraProposals[i];
            var start = DateTime.UtcNow.Date.AddDays(20 + i * 2).AddHours(22);
            while (start.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) start = start.AddDays(1);
            db.PropostasAula.Add(new PropostaAula { Id = proposalId, AlunoId = accounts[i % 4].Id, Titulo = item.Item1, Tema = item.Item2, Descricao = item.Item3, TipoSala = i % 2 == 0 ? "DESKTOP" : "NOTEBOOK", NumeroSala = (201 + i).ToString(), Inicio = start, Fim = start.AddMinutes(90), Status = "pendente" });
        }
        for (var i = 5; i < 10; i++)
        {
            var id = Key($"extra:request:{i}");
            if (accounts[i].PodeMinistrar || await db.SolicitacoesProfessor.AnyAsync(s => s.Id == id || (s.AlunoId == accounts[i].Id && s.Status == "pendente"))) continue;
            db.SolicitacoesProfessor.Add(new SolicitacaoProfessor { Id = id, AlunoId = accounts[i].Id, Justificativa = "Quero compartilhar experiências e ajudar colegas a aprender por meio de atividades práticas.", Status = "pendente" });
        }
        await db.SaveChangesAsync();

        // A conta principal de apresentação recebe exemplos próprios uma única vez.
        // Nunca usamos uma conta existente cuja senha, nome ou perfil tenham sido alterados.
        var sampleId = Key("principal:approved");
        if (legacyStudent is { Professor: false, Nome: "Mariana Costa" } && legacyTeacher is { Professor: true, Nome: "Ricardo Almeida" }
            && await users.CheckPasswordAsync(legacyStudent, DemoAccountSeeder.Password)
            && await users.CheckPasswordAsync(legacyTeacher, DemoAccountSeeder.Password))
        {
            if (!await db.PropostasAula.AnyAsync(p => p.Id == sampleId))
            {
                var start = DateTime.UtcNow.Date.AddDays(2).AddHours(22);
                var room = rooms.Single(s => s.Id == SalaSeed.SalaDesktopId);
                while (start.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || await db.Atividades.AnyAsync(a => a.SalaId == room.Id && a.Status == "publicada" && a.Inicio < start.AddMinutes(90) && a.Fim > start)) start = start.AddDays(1);
                var activityId = Key("principal:activity");
                if (!await db.Atividades.AnyAsync(a => a.Id == activityId))
                {
                    legacyStudent.PodeMinistrar = true;
                    const string title = "Criando páginas para a web";
                    const string description = "Uma oficina prática para construir e publicar sua primeira página.";
                    db.Atividades.Add(new Atividade { Id = activityId, Titulo = title, Tema = "Desenvolvimento web", Descricao = description, Modalidade = "AULA", Tipo = "aula", Nivel = "não informado", SalaId = room.Id, Inicio = start, Fim = start.AddMinutes(90), DataHoraUtc = start, Local = room.Nome, NumeroDeVagas = room.Capacidade, ResponsavelId = legacyStudent.Id, OrganizadorId = legacyStudent.Id, Status = "publicada" });
                    db.PropostasAula.Add(new PropostaAula { Id = sampleId, AlunoId = legacyStudent.Id, ProfessorId = legacyTeacher.Id, AtividadeId = activityId, Titulo = title, Tema = "Desenvolvimento web", Descricao = description, TipoSala = room.Tipo, Inicio = start, Fim = start.AddMinutes(90), Status = "aprovada", AvaliadaEmUtc = DateTime.UtcNow });
                    var seats = await db.Assentos.AsNoTracking().Where(s => s.SalaId == room.Id).OrderBy(s => s.Fileira).ThenBy(s => s.Coluna).Take(8).ToListAsync();
                    for (var j = 0; j < seats.Count; j++) db.Inscricoes.Add(new Inscricao { Id = Key($"principal:enrollment:{j}"), AtividadeId = activityId, AlunoId = accounts[j].Id, AssentoId = seats[j].Id, Status = "ativa", Data = DateTime.UtcNow, CriadaEmUtc = DateTime.UtcNow, AtualizadaEmUtc = DateTime.UtcNow });
                    await db.SaveChangesAsync();
                }
            }
            var pendingPrincipal = Key("principal:pending");
            if (!await db.PropostasAula.AnyAsync(p => p.Id == pendingPrincipal))
            {
                var start = DateTime.UtcNow.Date.AddDays(30).AddHours(22);
                db.PropostasAula.Add(new PropostaAula { Id = pendingPrincipal, AlunoId = legacyStudent.Id, Titulo = "Introdução ao design de interfaces", Tema = "Design", Descricao = "Vamos planejar telas claras e fáceis de usar.", TipoSala = "NOTEBOOK", NumeroSala = "102", Inicio = start, Fim = start.AddMinutes(90), Status = "pendente" });
            }
            var rejectedId = Key("principal:rejected");
            if (!await db.PropostasAula.AnyAsync(p => p.Id == rejectedId))
            {
                var start = DateTime.UtcNow.Date.AddDays(35).AddHours(22);
                db.PropostasAula.Add(new PropostaAula { Id = rejectedId, AlunoId = legacyStudent.Id, ProfessorId = legacyTeacher.Id, Titulo = "Oficina de protótipos", Tema = "Experiência do usuário", Descricao = "Conheça técnicas para testar uma ideia antes de desenvolvê-la.", TipoSala = "NOTEBOOK", NumeroSala = "102", Inicio = start, Fim = start.AddMinutes(90), Status = "recusada", MotivoRecusa = "Ajuste o planejamento e detalhe os materiais necessários antes de reenviar.", AvaliadaEmUtc = DateTime.UtcNow });
            }
            await db.SaveChangesAsync();
            for (var i = 0; i < 4; i++)
            {
                var id = Key($"extra:approved:{i}");
                var activityId = Key($"extra:approved-activity:{i}");
                if (await db.PropostasAula.AnyAsync(p => p.Id == id) || await db.Atividades.AnyAsync(a => a.Id == activityId)) continue;
                var item = extraProposals[i + 4];
                var room = rooms.Single(s => s.Id == (i % 2 == 0 ? SalaSeed.SalaDesktopId : SalaSeed.SalaNotebookId));
                var start = DateTime.UtcNow.Date.AddDays(5 + i).AddHours(22);
                while (start.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || await db.Atividades.AnyAsync(a => a.SalaId == room.Id && a.Status == "publicada" && a.Inicio < start.AddMinutes(90) && a.Fim > start)) start = start.AddDays(1);
                var instructor = accounts[i];
                db.Atividades.Add(new Atividade { Id = activityId, Titulo = item.Item1, Tema = item.Item2, Descricao = item.Item3, Modalidade = "AULA", Tipo = "aula", Nivel = "não informado", SalaId = room.Id, Inicio = start, Fim = start.AddMinutes(90), DataHoraUtc = start, Local = room.Nome, NumeroDeVagas = room.Capacidade, ResponsavelId = instructor.Id, OrganizadorId = instructor.Id, Status = "publicada" });
                db.PropostasAula.Add(new PropostaAula { Id = id, AlunoId = instructor.Id, ProfessorId = legacyTeacher.Id, AtividadeId = activityId, Titulo = item.Item1, Tema = item.Item2, Descricao = item.Item3, TipoSala = room.Tipo, Inicio = start, Fim = start.AddMinutes(90), Status = "aprovada", AvaliadaEmUtc = DateTime.UtcNow });
                var seats = await db.Assentos.AsNoTracking().Where(s => s.SalaId == room.Id).OrderBy(s => s.Fileira).ThenBy(s => s.Coluna).Take(7).ToListAsync();
                var participants = accounts.Where(a => !a.Professor && a.Id != instructor.Id).Take(seats.Count).ToArray();
                for (var j = 0; j < participants.Length; j++) db.Inscricoes.Add(new Inscricao { Id = Key($"extra:approved-seat:{i}:{j}"), AtividadeId = activityId, AlunoId = participants[j].Id, AssentoId = seats[j].Id, Status = "ativa", Data = DateTime.UtcNow, CriadaEmUtc = DateTime.UtcNow, AtualizadaEmUtc = DateTime.UtcNow });
                await db.SaveChangesAsync();
            }
        }

        foreach (var talk in new[] {
            (Key: "career", Titulo: "Caminhos para a carreira em tecnologia", Tema: "Carreira", Descricao: "Uma conversa sobre as possibilidades de atuação e os primeiros passos profissionais."),
            (Key: "innovation", Titulo: "Inovação e colaboração no campus", Tema: "Inovação", Descricao: "Uma conversa sobre como transformar ideias em projetos com impacto na comunidade.")
        })
        {
        var talkId = Key($"talk:{talk.Key}");
        if (!await db.Atividades.AnyAsync(a => a.Id == talkId))
        {
            var room = rooms.Single(s => s.Id == SalaSeed.SalaNotebookId);
            var start = DateTime.UtcNow.Date.AddDays(3).AddHours(22);
            while (start.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || await db.Atividades.AnyAsync(a => a.SalaId == room.Id && a.Status == "publicada" && a.Inicio < start.AddMinutes(90) && a.Fim > start)) start = start.AddDays(1);
            var teacher = legacyTeacher is { Professor: true } ? legacyTeacher : accounts[14];
            db.Atividades.Add(new Atividade { Id = talkId, Titulo = talk.Titulo, Tema = talk.Tema, Descricao = talk.Descricao, Modalidade = "PALESTRA", Tipo = "palestra", Nivel = "não informado", SalaId = room.Id, Inicio = start, Fim = start.AddMinutes(90), DataHoraUtc = start, Local = room.Nome, NumeroDeVagas = room.Capacidade, ResponsavelId = teacher.Id, OrganizadorId = teacher.Id, Status = "publicada" });
            var seats = await db.Assentos.AsNoTracking().Where(s => s.SalaId == room.Id).OrderBy(s => s.Fileira).ThenBy(s => s.Coluna).Take(10).ToListAsync();
            var attendees = accounts.Take(9).Select(a => a.Id).ToList();
            if (legacyStudent is { Professor: false, Nome: "Mariana Costa" } && await users.CheckPasswordAsync(legacyStudent, DemoAccountSeeder.Password)) attendees.Add(legacyStudent.Id);
            for (var j = 0; j < attendees.Count; j++) db.Inscricoes.Add(new Inscricao { Id = Key(talk.Key == "career" ? $"talk:enrollment:{j}" : $"talk:{talk.Key}:enrollment:{j}"), AtividadeId = talkId, AlunoId = attendees[j], AssentoId = seats[j].Id, Status = "ativa", Data = DateTime.UtcNow, CriadaEmUtc = DateTime.UtcNow, AtualizadaEmUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        }
        await transaction.CommitAsync();
    }
}
