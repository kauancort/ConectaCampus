import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { apiRequest, type Activity, type ActivityDetails, type ActivityInput, type Enrollment, type LoginResponse, type Participant } from './api'
import { useAuth } from './auth'

type Page = 'catalogo' | 'publicar' | 'minhas' | 'inscricoes' | 'conta'
type FormState = { tipo: string; tema: string; descricao: string; nivel: string; dataHora: string; local: string; numeroDeVagas: string }
const emptyForm: FormState = { tipo: 'aula', tema: '', descricao: '', nivel: '', dataHora: '', local: '', numeroDeVagas: '10' }

function dateLabel(value: string) {
  return new Date(value).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
}

function localDateTimeInput(value: string) {
  const date = new Date(value)
  return new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString().slice(0, 16)
}

function formData(value: FormState): ActivityInput {
  return { ...value, dataHora: new Date(value.dataHora).toISOString(), numeroDeVagas: Number(value.numeroDeVagas) }
}

export default function App() {
  const { token, student, signIn, signOut, api } = useAuth()
  const [page, setPage] = useState<Page>('catalogo')
  const [activities, setActivities] = useState<Activity[]>([])
  const [mine, setMine] = useState<Activity[]>([])
  const [enrollments, setEnrollments] = useState<Enrollment[]>([])
  const [selected, setSelected] = useState<ActivityDetails | null>(null)
  const [participants, setParticipants] = useState<Participant[] | null>(null)
  const [participantsActivityId, setParticipantsActivityId] = useState<string | null>(null)
  const [form, setForm] = useState<FormState>(emptyForm)
  const [editing, setEditing] = useState<string | null>(null)
  const [authMode, setAuthMode] = useState<'login' | 'register'>('login')
  const [authForm, setAuthForm] = useState({ name: '', email: '', course: '', password: '' })
  const [filters, setFilters] = useState({ tema: '', tipo: '', nivel: '', data: '' })
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [loading, setLoading] = useState(false)

  const message = <Args extends unknown[],>(action: (...args: Args) => Promise<void>) => async (...args: Args) => {
    setError(''); setNotice(''); setLoading(true)
    try { await action(...args) } catch (e) { setError(e instanceof Error ? e.message : 'Ocorreu um erro.') }
    finally { setLoading(false) }
  }

  const loadCatalog = async () => {
    const query = new URLSearchParams()
    Object.entries(filters).forEach(([key, value]) => { if (value) query.set(key, value) })
    const queryString = query.toString()
    const list = await api<Activity[]>(`/api/atividades${queryString ? `?${queryString}` : ''}`)
    setActivities(list)
  }
  const loadPrivate = async () => {
    if (!token) return
    const [mineList, ownEnrollments] = await Promise.all([
      api<Activity[]>('/api/atividades/minhas'),
      api<Enrollment[]>('/api/inscricoes/me'),
    ])
    setMine(mineList); setEnrollments(ownEnrollments)
  }

  useEffect(() => { void message(loadCatalog)() }, [])
  useEffect(() => { if (token) void message(loadPrivate)(); else { setMine([]); setEnrollments([]) } }, [token])

  const enrolledActivityIds = useMemo(() => new Set(enrollments.filter(x => x.status === 'ativa').map(x => x.atividadeId)), [enrollments])
  const nav: { id: Page; label: string }[] = [
    { id: 'catalogo', label: 'Explorar atividades' },
    ...(token ? [{ id: 'publicar' as Page, label: 'Publicar' }, { id: 'minhas' as Page, label: 'Minhas atividades' }, { id: 'inscricoes' as Page, label: 'Minhas inscrições' }] : []),
  ]

  const selectActivity = (id: string) => message(async () => {
    const detail = await api<ActivityDetails>(`/api/atividades/${id}`)
    setSelected(detail); setParticipants(null); setParticipantsActivityId(null); setPage('catalogo')
  })
  const submitAuth = message(async (event?: FormEvent) => {
    event?.preventDefault()
    if (authMode === 'register') {
      await apiRequest('/api/auth/register', { method: 'POST', body: JSON.stringify({ name: authForm.name, email: authForm.email, course: authForm.course, password: authForm.password }) })
      setNotice('Cadastro realizado. Entre com seu e-mail e senha.'); setAuthMode('login'); return
    }
    const result = await apiRequest<LoginResponse>('/api/auth/login', { method: 'POST', body: JSON.stringify({ email: authForm.email, password: authForm.password }) })
    signIn(result.accessToken, result.student); setPage('catalogo'); setNotice(`Bem-vindo(a), ${result.student.name}.`)
  })
  const saveActivity = message(async (event?: FormEvent) => {
    event?.preventDefault()
    const input = formData(form)
    if (editing) await api(`/api/atividades/${editing}`, { method: 'PUT', body: JSON.stringify(input) })
    else await api('/api/atividades', { method: 'POST', body: JSON.stringify(input) })
    setNotice(editing ? 'Atividade atualizada.' : 'Atividade publicada.')
    setForm(emptyForm); setEditing(null); setPage('minhas'); await loadPrivate(); await loadCatalog()
  })
  const enroll = (activity: ActivityDetails) => message(async () => {
    await api(`/api/atividades/${activity.id}/inscricoes`, { method: 'POST' })
    setNotice('Inscrição realizada.'); await loadPrivate()
  })
  const cancelEnrollment = (activityId: string) => message(async () => {
    await api(`/api/atividades/${activityId}/inscricoes/me`, { method: 'DELETE' })
    setNotice('Inscrição cancelada.'); await loadPrivate()
  })
  const cancelActivity = (activityId: string) => message(async () => {
    await api(`/api/atividades/${activityId}/cancelar`, { method: 'PATCH' })
    setNotice('Atividade cancelada.'); await loadPrivate(); await loadCatalog()
  })
  const showParticipants = (activityId: string) => message(async () => {
    setParticipants(null); setParticipantsActivityId(activityId)
    setParticipants(await api<Participant[]>(`/api/atividades/${activityId}/inscricoes`))
  })
  const editActivity = (activity: Activity) => {
    setEditing(activity.id)
    setForm({ tipo: activity.tipo, tema: activity.tema, descricao: activity.descricao, nivel: activity.nivel, dataHora: localDateTimeInput(activity.dataHora), local: activity.local, numeroDeVagas: String(activity.numeroDeVagas) })
    setPage('publicar')
  }

  return <div className="app-shell">
    <header className="topbar"><a className="brand" href="#inicio" onClick={e => { e.preventDefault(); setPage('catalogo') }}>ConectaCampus</a>
      <nav aria-label="Navegação principal">{nav.map(item => <button key={item.id} className={page === item.id ? 'nav-link active' : 'nav-link'} onClick={() => { setPage(item.id); setSelected(null); setParticipants(null); setParticipantsActivityId(null); setError(''); setNotice('') }}>{item.label}</button>)}</nav>
      <div className="account">{student ? <><span>{student.name}</span><button className="button secondary small" onClick={() => { signOut(); setPage('catalogo'); setNotice('Você saiu da sua conta.') }}>Sair</button></> : <button className="button primary small" onClick={() => { setPage('conta'); setAuthMode('login') }}>Entrar</button>}</div>
    </header>
    <main className="main-content">
      <div className="heading"><div><p className="eyebrow">FATEC SHUNJI NISHIMURA</p><h1>{page === 'catalogo' ? 'Compartilhe conhecimento' : page === 'publicar' ? (editing ? 'Editar atividade' : 'Publique uma atividade') : page === 'minhas' ? 'Minhas atividades' : page === 'inscricoes' ? 'Minhas inscrições' : 'Acesse sua conta'}</h1><p className="subtitle">Encontre encontros de aprendizagem criados por estudantes.</p></div></div>
      {error && <div className="feedback error" role="alert">{error}</div>}{notice && <div className="feedback success" role="status">{notice}</div>}
      {loading && <p className="loading" role="status">Carregando…</p>}

      {page === 'conta' && <section className="panel narrow"><div className="tabs"><button className={authMode === 'login' ? 'tab active' : 'tab'} onClick={() => setAuthMode('login')}>Entrar</button><button className={authMode === 'register' ? 'tab active' : 'tab'} onClick={() => setAuthMode('register')}>Criar cadastro</button></div>
        <form onSubmit={event => void submitAuth(event)} className="form-grid">
          {authMode === 'register' && <><label>Nome<input required maxLength={200} value={authForm.name} onChange={e => setAuthForm({ ...authForm, name: e.target.value })} /></label><label>Curso<input required maxLength={120} value={authForm.course} onChange={e => setAuthForm({ ...authForm, course: e.target.value })} /></label></>}
          <label>E-mail<input type="email" required autoComplete="email" value={authForm.email} onChange={e => setAuthForm({ ...authForm, email: e.target.value })} /></label>
          <label>Senha<input type="password" required autoComplete={authMode === 'login' ? 'current-password' : 'new-password'} value={authForm.password} onChange={e => setAuthForm({ ...authForm, password: e.target.value })} /></label>
          <button className="button primary" disabled={loading}>{authMode === 'login' ? 'Entrar' : 'Criar conta'}</button>
        </form>
      </section>}

      {page === 'catalogo' && <>
        <section className="panel filters" aria-label="Filtros de atividades">
          <label>Tema<input value={filters.tema} onChange={e => setFilters({ ...filters, tema: e.target.value })} placeholder="Ex.: programação" /></label>
          <label>Tipo<select value={filters.tipo} onChange={e => setFilters({ ...filters, tipo: e.target.value })}><option value="">Todos</option><option value="aula">Aula</option><option value="palestra">Palestra</option><option value="grupo de estudo">Grupo de estudo</option></select></label>
          <label>Nível<input value={filters.nivel} onChange={e => setFilters({ ...filters, nivel: e.target.value })} /></label>
          <label>Data<input type="date" value={filters.data} onChange={e => setFilters({ ...filters, data: e.target.value })} /></label>
          <button className="button primary" onClick={message(loadCatalog)}>Buscar</button>
        </section>
        {selected && <section className="panel detail"><button className="text-button" onClick={() => setSelected(null)}>← Voltar ao catálogo</button><h2>{selected.tema}</h2><p>{selected.descricao}</p><div className="detail-grid"><span><b>Organizador</b>{selected.nomeOrganizador}</span><span><b>Tipo e nível</b>{selected.tipo} · {selected.nivel}</span><span><b>Data e horário</b>{dateLabel(selected.dataHora)}</span><span><b>Local</b>{selected.local}</span><span><b>Vagas</b>{selected.numeroDeVagas}</span><span><b>Status</b>{selected.status}</span></div>
          {student && selected.status === 'publicada' && (enrolledActivityIds.has(selected.id) ? <button className="button secondary" onClick={cancelEnrollment(selected.id)}>Cancelar minha inscrição</button> : <button className="button primary" onClick={enroll(selected)}>Inscrever-me</button>)}{!student && <p>Entre para se inscrever nesta atividade.</p>}</section>}
        <div className="section-row"><h2>Atividades disponíveis</h2><span>{activities.length} encontrada(s)</span></div>
        {activities.length === 0 ? <div className="empty">Nenhuma atividade encontrada. Ajuste os filtros ou tente novamente.</div> : <div className="cards">{activities.map(activity => <article className="card" key={activity.id}><div className="card-top"><span className="tag">{activity.tipo}</span><span className={activity.status === 'publicada' ? 'status' : 'status muted'}>{activity.status}</span></div><h3>{activity.tema}</h3><p>{activity.descricao}</p><div className="meta">{dateLabel(activity.dataHora)} · {activity.local}</div><div className="card-bottom"><span>{activity.numeroDeVagas} vagas · {activity.nivel}</span><button className="text-button" onClick={selectActivity(activity.id)}>Ver detalhes</button></div></article>)}</div>}
      </>}

      {page === 'publicar' && <section className="panel"><form onSubmit={event => void saveActivity(event)} className="form-grid two-col">
        <label>Tipo<select required value={form.tipo} onChange={e => setForm({ ...form, tipo: e.target.value })}><option value="aula">Aula</option><option value="palestra">Palestra</option><option value="grupo de estudo">Grupo de estudo</option></select></label>
        <label>Tema<input required maxLength={200} value={form.tema} onChange={e => setForm({ ...form, tema: e.target.value })} /></label>
        <label className="span-two">Descrição<textarea required maxLength={5000} rows={4} value={form.descricao} onChange={e => setForm({ ...form, descricao: e.target.value })} /></label>
        <label>Nível<input required maxLength={80} value={form.nivel} onChange={e => setForm({ ...form, nivel: e.target.value })} /></label>
        <label>Data e horário<input type="datetime-local" required value={form.dataHora} onChange={e => setForm({ ...form, dataHora: e.target.value })} /></label>
        <label>Local<input required maxLength={300} value={form.local} onChange={e => setForm({ ...form, local: e.target.value })} /></label>
        <label>Número de vagas<input type="number" required min="1" value={form.numeroDeVagas} onChange={e => setForm({ ...form, numeroDeVagas: e.target.value })} /></label>
        <div className="form-actions span-two"><button className="button primary" disabled={loading}>{editing ? 'Salvar alterações' : 'Publicar atividade'}</button>{editing && <button type="button" className="button secondary" onClick={() => { setEditing(null); setForm(emptyForm) }}>Cancelar edição</button>}</div>
      </form></section>}

      {page === 'minhas' && <>{!token ? <div className="empty">Entre para ver suas atividades.</div> : mine.length === 0 ? <div className="empty">Você ainda não publicou atividades.</div> : <div className="cards">{mine.map(activity => <article className="card" key={activity.id}><div className="card-top"><span className="tag">{activity.tipo}</span><span className={activity.status === 'publicada' ? 'status' : 'status muted'}>{activity.status}</span></div><h3>{activity.tema}</h3><p>{dateLabel(activity.dataHora)} · {activity.local}</p><div className="card-bottom"><span>{activity.numeroDeVagas} vagas</span><div className="actions"><button className="text-button" onClick={showParticipants(activity.id)}>Inscritos</button><button className="text-button" onClick={() => editActivity(activity)}>Editar</button>{activity.status === 'publicada' && <button className="text-button danger-text" onClick={cancelActivity(activity.id)}>Cancelar</button>}</div></div>{participants && participantsActivityId === activity.id && <div className="participants"><b>Participantes</b>{participants.filter(person => person.status === 'ativa').length === 0 ? <p>Nenhuma inscrição ativa.</p> : participants.filter(person => person.status === 'ativa').map(person => <p key={person.alunoId}>{person.nomeAluno}</p>)}</div>}</article>)}</div>}</>}

      {page === 'inscricoes' && <>{!token ? <div className="empty">Entre para ver suas inscrições.</div> : enrollments.length === 0 ? <div className="empty">Você ainda não se inscreveu em atividades.</div> : <div className="cards">{enrollments.map(item => <article className="card" key={item.id}><div className="card-top"><span className="tag">{item.tipo}</span><span className={item.status === 'ativa' ? 'status' : 'status muted'}>{item.status}</span></div><h3>{item.tema}</h3><p>{dateLabel(item.dataHora)} · {item.local}</p><div className="card-bottom"><span>Atividade {item.statusAtividade}</span>{item.status === 'ativa' && item.statusAtividade === 'publicada' && <button className="text-button danger-text" onClick={cancelEnrollment(item.atividadeId)}>Cancelar inscrição</button>}</div></article>)}</div>}</>}
    </main>
    <footer>ConectaCampus · Projeto Integrador · Fatec Shunji Nishimura de Pompeia</footer>
  </div>
}
