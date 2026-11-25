// Popula somente uma instância local, pelo mesmo fluxo de aprovação da aplicação.
const base = new URL(process.env.DEMO_BASE_URL || 'http://127.0.0.1:5173')
if (!['127.0.0.1', 'localhost', '[::1]'].includes(base.hostname) || !['http:', 'https:'].includes(base.protocol)) {
  throw new Error('Este script só pode ser executado contra uma instância local.')
}
const studentEmail = process.env.DEMO_STUDENT_EMAIL || 'aluno@conectacampus.local'
const teacherEmail = process.env.DEMO_TEACHER_EMAIL || 'professor@conectacampus.local'
const password = process.env.DEMO_PASSWORD || 'Campus@123'
const marker = '[Demonstração ConectaCampus]'

const lessons = [
  ['Primeiros passos com HTML e CSS', 'Desenvolvimento web', 'Construa uma página simples e conheça os elementos que dão estrutura e estilo à web.'],
  ['JavaScript na prática', 'Programação', 'Explore variáveis, condições e eventos ao criar pequenas interações no navegador.'],
  ['Git e GitHub para trabalhos em equipe', 'Ferramentas de desenvolvimento', 'Aprenda a versionar arquivos, colaborar em branches e organizar um projeto em grupo.'],
  ['Criando interfaces acessíveis', 'Design e acessibilidade', 'Conheça práticas para tornar interfaces mais fáceis de usar com teclado e leitores de tela.'],
  ['Introdução a bancos de dados', 'Banco de dados', 'Modele informações e pratique consultas SQL em situações comuns do cotidiano acadêmico.'],
  ['Desvendando APIs', 'Desenvolvimento de software', 'Entenda como aplicações trocam informações e experimente requisições HTTP.'],
  ['Lógica de programação sem mistério', 'Programação', 'Resolva problemas passo a passo usando algoritmos, decisões e repetições.'],
  ['Protótipos para tirar ideias do papel', 'Design de interfaces', 'Transforme um problema em esboços de telas e teste caminhos com outras pessoas.'],
  ['Organização de projetos com Kanban', 'Gestão de projetos', 'Planeje tarefas em equipe com um quadro visual e acompanhe o andamento do trabalho.'],
  ['Introdução à análise de dados', 'Ciência de dados', 'Organize uma pequena base de dados e encontre padrões com perguntas simples.'],
  ['Segurança digital no dia a dia', 'Segurança da informação', 'Identifique riscos comuns e conheça boas práticas para proteger contas e informações.'],
  ['Testes automatizados para iniciantes', 'Qualidade de software', 'Descubra como testes ajudam a verificar comportamentos e evitar regressões.'],
  ['Python para começar a programar', 'Programação', 'Escreva seus primeiros programas em Python com exercícios orientados.'],
  ['Planilhas que contam histórias', 'Produtividade', 'Use fórmulas e gráficos para organizar e comunicar informações de maneira clara.'],
  ['Como apresentar seu projeto', 'Comunicação', 'Estruture uma apresentação objetiva e pratique a comunicação de ideias técnicas.'],
  ['Redes de computadores na prática', 'Infraestrutura', 'Explore conceitos de rede e entenda o caminho percorrido por uma requisição.'],
  ['Modelagem de dados para projetos', 'Banco de dados', 'Identifique entidades e relacionamentos a partir de um problema real.'],
  ['Introdução ao desenvolvimento responsivo', 'Desenvolvimento web', 'Adapte uma interface para diferentes tamanhos de tela com práticas de CSS.'],
  ['Pesquisa com usuários para iniciantes', 'Experiência do usuário', 'Aprenda a preparar perguntas e a observar necessidades antes de desenhar uma solução.'],
  ['Do problema à solução em equipe', 'Inovação', 'Pratique a definição de problemas e a construção colaborativa de alternativas.'],
]

async function request(path, { token, ...options } = {}) {
  let response
  try {
    response = await fetch(new URL(path, base), { ...options, headers: { ...(options.body ? { 'Content-Type': 'application/json' } : {}), ...(token ? { Authorization: `Bearer ${token}` } : {}) } })
  } catch {
    throw new Error('Não foi possível conectar à aplicação local. Inicie o projeto e tente novamente.')
  }
  const text = await response.text()
  let body
  try { body = text ? JSON.parse(text) : null } catch { body = null }
  if (!response.ok) throw new Error(`${path}: ${response.status} ${body?.message || 'não foi possível concluir a operação.'}`)
  return body
}

const post = (path, token, body) => request(path, { method: 'POST', token, body: JSON.stringify(body) })

async function login(email) {
  try { return await post('/api/auth/login', null, { email, password }) }
  catch { throw new Error(`Não foi possível entrar na conta de demonstração ${email}. Confira as contas locais e a senha configurada.`) }
}

function localDateAfter(days) {
  const parts = Object.fromEntries(new Intl.DateTimeFormat('en-US', { timeZone: 'America/Sao_Paulo', year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(new Date()).filter(p => p.type !== 'literal').map(p => [p.type, p.value]))
  const date = new Date(Date.UTC(Number(parts.year), Number(parts.month) - 1, Number(parts.day) + days, 12))
  return date.toISOString().slice(0, 10)
}

async function main() {
  const [student, teacher] = await Promise.all([login(studentEmail), login(teacherEmail)])
  if (student.student.professor || !teacher.student.professor) throw new Error('As contas configuradas não possuem os perfis de aluno e professor esperados.')
  const studentToken = student.accessToken
  const teacherToken = teacher.accessToken
  if (!student.student.podeMinistrar) {
    let access = await request('/api/acesso-professor/minha', { token: studentToken })
    if (!access || access.status !== 'pendente') access = await post('/api/acesso-professor', studentToken, { justificativa: 'Preparação de aulas de demonstração para explorar a plataforma.' })
    await post(`/api/acesso-professor/${encodeURIComponent(access.id)}/avaliar`, teacherToken, { decisao: 'aprovada' })
    console.log('Aluno de demonstração autorizado a ministrar aulas.')
  }

  const proposals = await request('/api/propostas/minhas', { token: studentToken })
  const catalog = await request('/api/atividades', { token: studentToken })
  const published = new Set(catalog.map(item => item.id))
  let day = 1
  let created = 0
  let existing = 0
  for (const [index, [titulo, tema, descricao]] of lessons.entries()) {
    const prior = proposals.find(p => p.titulo === titulo && p.descricao.replace(`\n\n${marker}`, '') === descricao && new Date(p.inicio).getTime() > Date.now() && (p.status === 'pendente' || (p.status === 'aprovada' && published.has(p.atividadeId))))
    if (prior?.status === 'aprovada') { existing++; continue }
    let proposal = prior
    if (!proposal) {
      const tipoSala = index % 2 === 0 ? 'DESKTOP' : 'NOTEBOOK'
      let inicio, fim
      for (;;) {
        const date = localDateAfter(day++)
        const weekday = new Date(`${date}T12:00:00Z`).getUTCDay()
        if (weekday === 0 || weekday === 6) continue
        inicio = new Date(`${date}T19:00:00-03:00`)
        fim = new Date(`${date}T20:30:00-03:00`)
        if (!catalog.some(item => item.sala.tipo === tipoSala && new Date(item.inicio) < fim && inicio < new Date(item.fim))) break
      }
      proposal = await post('/api/propostas', studentToken, { titulo, tema, descricao, tipoSala, numeroSala: tipoSala === 'DESKTOP' ? '101' : '102', inicio: inicio.toISOString(), fim: fim.toISOString() })
    }
    const approval = await post(`/api/propostas/${encodeURIComponent(proposal.id)}/avaliar`, teacherToken, { decisao: 'aprovada' })
    const activity = await request(`/api/atividades/${encodeURIComponent(approval.atividadeId)}`, { token: studentToken })
    catalog.push(activity)
    created++
    console.log(`Aula aprovada: ${titulo}`)
  }
  console.log(`Concluído: ${created} aula(s) criada(s), ${existing} já existente(s).`)
}

main().catch(error => { console.error(error instanceof Error ? error.message : 'Falha ao criar aulas de demonstração.'); process.exitCode = 1 })
