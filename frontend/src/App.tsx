import { useEffect, useState, type CSSProperties, type FormEvent } from 'react'
import { ApiError, apiRequest, type Activity, type Enrollment, type LoginResponse, type Seat, type SeatMap } from './api'
import { useAuth } from './auth'

type Page = 'catalogo' | 'detalhe' | 'assentos' | 'inscricoes' | 'conta'

function dateLabel(value: string | null | undefined) {
  if (!value) return 'Não informada'
  if (/^\d{4}-\d{2}-\d{2}$/.test(value)) {
    const [year, month, day] = value.split('-')
    return `${day}/${month}/${year}`
  }
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? 'Não informada' : date.toLocaleDateString('pt-BR')
}

function timeLabel(value: string | null | undefined) {
  if (!value) return 'Não informado'
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? 'Não informado'
    : date.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
}

function scheduleLabel(start: string | null | undefined, end: string | null | undefined) {
  return `${dateLabel(start)} · ${timeLabel(start)}${end ? `–${timeLabel(end)}` : ''}`
}

function titleCase(value: string) {
  return value ? `${value.slice(0, 1).toUpperCase()}${value.slice(1).toLowerCase()}` : 'Não informado'
}

export default function App() {
  const { token, student, signIn, signOut, api } = useAuth()
  const [page, setPage] = useState<Page>('catalogo')
  const [activities, setActivities] = useState<Activity[]>([])
  const [theme, setTheme] = useState('')
  const [catalogLoading, setCatalogLoading] = useState(true)
  const [detailLoading, setDetailLoading] = useState(false)
  const [seatLoading, setSeatLoading] = useState(false)
  const [confirmingSeat, setConfirmingSeat] = useState(false)
  const [enrollmentsLoading, setEnrollmentsLoading] = useState(false)
  const [selected, setSelected] = useState<Activity | null>(null)
  const [seatMap, setSeatMap] = useState<SeatMap | null>(null)
  const [selectedSeat, setSelectedSeat] = useState<Seat | null>(null)
  const [enrollments, setEnrollments] = useState<Enrollment[]>([])
  const [authMode, setAuthMode] = useState<'login' | 'register'>('login')
  const [authForm, setAuthForm] = useState({ name: '', email: '', course: '', password: '' })
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  const clearFeedback = () => { setError(''); setNotice('') }

  const loadCatalog = async (search = theme) => {
    setCatalogLoading(true)
    clearFeedback()
    try {
      const query = search.trim() ? `?tema=${encodeURIComponent(search.trim())}` : ''
      const list = await apiRequest<Activity[]>(`/api/atividades${query}`)
      setActivities(list.filter(activity => activity.status === 'publicada'))
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Não foi possível carregar as atividades.')
    } finally {
      setCatalogLoading(false)
    }
  }

  const loadEnrollments = async (showFeedback = true) => {
    if (!token) return
    setEnrollmentsLoading(true)
    if (showFeedback) clearFeedback()
    try {
      setEnrollments(await api<Enrollment[]>('/api/inscricoes/minhas'))
    } catch (requestError) {
      if (showFeedback) setError(requestError instanceof Error ? requestError.message : 'Não foi possível carregar suas inscrições.')
    } finally {
      setEnrollmentsLoading(false)
    }
  }

  const loadSeatMap = async (activityId: string, keepFeedback = false) => {
    setSeatLoading(true)
    if (!keepFeedback) clearFeedback()
    try {
      setSeatMap(await apiRequest<SeatMap>(`/api/atividades/${encodeURIComponent(activityId)}/assentos`))
    } catch (requestError) {
      setSeatMap(null)
      setSelectedSeat(null)
      setError(requestError instanceof Error ? requestError.message : 'Não foi possível carregar o mapa de assentos.')
    } finally {
      setSeatLoading(false)
    }
  }

  useEffect(() => { void loadCatalog('') }, [])
  useEffect(() => {
    if (token) void loadEnrollments(false)
    else setEnrollments([])
  }, [token])

  const openActivity = async (id: string) => {
    setDetailLoading(true)
    clearFeedback()
    try {
      const activity = await apiRequest<Activity>(`/api/atividades/${encodeURIComponent(id)}`)
      setSelected(activity)
      setSeatMap(null)
      setSelectedSeat(null)
      setPage('detalhe')
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Não foi possível carregar os detalhes da atividade.')
    } finally {
      setDetailLoading(false)
    }
  }

  const openSeatMap = async () => {
    if (!selected) return
    setSelectedSeat(null)
    setPage('assentos')
    await loadSeatMap(selected.id)
  }

  const submitAuth = async (event: FormEvent) => {
    event.preventDefault()
    clearFeedback()
    try {
      if (authMode === 'register') {
        await apiRequest('/api/auth/register', {
          method: 'POST',
          body: JSON.stringify({ name: authForm.name, email: authForm.email, course: authForm.course, password: authForm.password }),
        })
        setAuthMode('login')
        setAuthForm(current => ({ ...current, password: '' }))
        setNotice('Cadastro realizado. Entre com seu e-mail e senha.')
        return
      }
      const result = await apiRequest<LoginResponse>('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email: authForm.email, password: authForm.password }),
      })
      signIn(result.accessToken, result.student)
      setPage('catalogo')
      setNotice(`Bem-vindo(a), ${result.student.name}.`)
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Não foi possível acessar sua conta.')
    }
  }

  const confirmEnrollment = async () => {
    if (!selected || !selectedSeat) return
    if (!token) {
      setPage('conta')
      setError('Entre na sua conta para confirmar a inscrição.')
      return
    }
    setConfirmingSeat(true)
    clearFeedback()
    try {
      await api(`/api/atividades/${encodeURIComponent(selected.id)}/inscricoes`, {
        method: 'POST',
        body: JSON.stringify({ assentoId: selectedSeat.id }),
      })
      const seatCode = selectedSeat.codigo
      setSelectedSeat(null)
      await Promise.all([loadSeatMap(selected.id, true), loadEnrollments(false)])
      setNotice(`Inscrição confirmada no assento ${seatCode}.`)
    } catch (requestError) {
      if (requestError instanceof ApiError && requestError.status === 409) {
        setSelectedSeat(null)
        await loadSeatMap(selected.id, true)
        setError('Esse lugar não está mais disponível. O mapa foi atualizado; escolha outro assento.')
      } else {
        setError(requestError instanceof Error ? requestError.message : 'Não foi possível confirmar sua inscrição.')
      }
    } finally {
      setConfirmingSeat(false)
    }
  }

  const goTo = (nextPage: Page) => {
    clearFeedback()
    setPage(nextPage)
  }

  const columns = seatMap?.sala.tipo.toUpperCase() === 'NOTEBOOK' ? 5 : 6

  return <div className="app-shell student-area">
    <header className="topbar">
      <button className="brand brand-button" type="button" onClick={() => goTo('catalogo')}>ConectaCampus</button>
      <nav aria-label="Navegação da área do aluno">
        <button className={page === 'catalogo' || page === 'detalhe' || page === 'assentos' ? 'nav-link active' : 'nav-link'} type="button" onClick={() => goTo('catalogo')}>Atividades</button>
        {student && <button className={page === 'inscricoes' ? 'nav-link active' : 'nav-link'} type="button" onClick={() => { goTo('inscricoes'); void loadEnrollments() }}>Minhas inscrições</button>}
      </nav>
      <div className="account">
        {student
          ? <><span>{student.name}</span><button className="button secondary small" type="button" onClick={() => { signOut(); goTo('catalogo'); setNotice('Você saiu da sua conta.') }}>Sair</button></>
          : <button className="button primary small" type="button" onClick={() => { setAuthMode('login'); goTo('conta') }}>Entrar</button>}
      </div>
    </header>

    <main className="main-content">
      <div className="heading">
        <div>
          <p className="eyebrow">ÁREA DO ALUNO</p>
          <h1>{page === 'catalogo' ? 'Aulas e palestras' : page === 'detalhe' ? 'Detalhes da atividade' : page === 'assentos' ? 'Escolha seu assento' : page === 'inscricoes' ? 'Minhas inscrições' : 'Acesse sua conta'}</h1>
          <p className="subtitle">{page === 'assentos' ? 'A seleção só é confirmada depois da inscrição.' : 'Encontre atividades acadêmicas e participe.'}</p>
        </div>
      </div>

      {error && <div className="feedback error" role="alert">{error}</div>}
      {notice && <div className="feedback success" role="status">{notice}</div>}

      {page === 'catalogo' && <>
        <form className="panel filters student-search" onSubmit={event => { event.preventDefault(); void loadCatalog() }}>
          <label>Pesquisar por tema
            <input value={theme} onChange={event => setTheme(event.target.value)} placeholder="Ex.: programação, carreira" />
          </label>
          <button className="button primary" disabled={catalogLoading}>Buscar</button>
        </form>
        <div className="section-row"><h2>Atividades disponíveis</h2><span>{catalogLoading ? 'Carregando…' : `${activities.length} encontrada(s)`}</span></div>
        {catalogLoading ? <div className="empty" role="status">Carregando atividades…</div> : activities.length === 0 ? <div className="empty">Nenhuma atividade encontrada. Tente outro tema.</div> : <div className="cards">
          {activities.map(activity => <article className="card activity-card" key={activity.id}>
            <div className="card-top"><span className="tag">{titleCase(activity.modalidade)}</span><span className="status">Disponível</span></div>
            <h2>{activity.tema}</h2>
            <p>{activity.descricao}</p>
            <div className="activity-meta">
              <span><b>Data e horário</b>{scheduleLabel(activity.inicio, activity.fim)}</span>
              <span><b>Sala</b>{activity.sala.nome}</span>
              <span><b>Tipo de sala</b>{titleCase(activity.sala.tipo)}</span>
              <span><b>Capacidade</b>{activity.sala.capacidade} lugares</span>
            </div>
            <div className="card-bottom"><button className="text-button" type="button" disabled={detailLoading} onClick={() => void openActivity(activity.id)}>{detailLoading ? 'Carregando…' : 'Ver detalhes'}</button></div>
          </article>)}
        </div>}
      </>}

      {page === 'detalhe' && <>
        <button className="text-button back-button" type="button" onClick={() => goTo('catalogo')}>← Voltar às atividades</button>
        {detailLoading ? <div className="empty" role="status">Carregando atividade…</div> : selected && <section className="panel detail student-detail">
          <div className="detail-title"><span className="tag">{titleCase(selected.modalidade)}</span><h2>{selected.tema}</h2></div>
          <p>{selected.descricao}</p>
          <div className="detail-grid">
            <span><b>Data e horário</b>{scheduleLabel(selected.inicio, selected.fim)}</span>
            <span><b>Sala</b>{selected.sala.nome}</span>
            <span><b>Tipo de sala</b>{titleCase(selected.sala.tipo)}</span>
            <span><b>Capacidade</b>{selected.sala.capacidade} lugares</span>
          </div>
          {selected.status === 'publicada' && <button className="button primary" type="button" onClick={() => void openSeatMap()}>Escolher assento</button>}
        </section>}
      </>}

      {page === 'assentos' && <>
        <button className="text-button back-button" type="button" onClick={() => { setSelectedSeat(null); goTo('detalhe') }}>← Voltar aos detalhes</button>
        {seatLoading ? <div className="empty" role="status">Carregando mapa de assentos…</div> : seatMap && <section className="panel seat-panel">
          <div className="seat-panel-heading">
            <div><span className="tag">{titleCase(seatMap.sala.tipo)}</span><h2>{seatMap.tema}</h2><p>{scheduleLabel(seatMap.inicio, seatMap.fim)} · {seatMap.sala.nome}</p></div>
            <div className="availability"><b>{seatMap.lugaresDisponiveis}</b><span>vagas disponíveis</span></div>
          </div>
          <div className="seat-legend" aria-label="Legenda do mapa de assentos"><span><i className="seat-sample available" />Disponível</span><span><i className="seat-sample occupied" />Ocupado</span><span><i className="seat-sample chosen" />Selecionado</span></div>
          <div className="seat-map" style={{ '--seat-columns': columns } as CSSProperties} aria-label={`Mapa de assentos da sala ${seatMap.sala.nome}`}>
            {seatMap.assentos.map(seat => {
              const occupied = seat.status === 'OCUPADO'
              const chosen = selectedSeat?.id === seat.id
              return <button key={seat.id} className={`seat ${occupied ? 'occupied' : chosen ? 'selected' : 'available'}`} type="button" disabled={occupied || confirmingSeat} aria-pressed={chosen} aria-label={`Assento ${seat.codigo}${occupied ? ', ocupado' : chosen ? ', selecionado' : ', disponível'}`} onClick={() => { setSelectedSeat(seat); clearFeedback() }}>{seat.codigo}</button>
            })}
          </div>
          <div className="seat-selection">
            <div>{selectedSeat ? <>Assento selecionado: <b>{selectedSeat.codigo}</b></> : 'Selecione um assento disponível no mapa.'}</div>
            <button className="button primary" type="button" disabled={!selectedSeat || confirmingSeat} onClick={() => void confirmEnrollment()}>{confirmingSeat ? 'Confirmando…' : 'Confirmar inscrição'}</button>
          </div>
        </section>}
      </>}

      {page === 'inscricoes' && <>
        {!student ? <div className="empty">Entre para consultar suas inscrições.</div> : enrollmentsLoading ? <div className="empty" role="status">Carregando inscrições…</div> : enrollments.length === 0 ? <div className="empty">Você ainda não possui inscrições.</div> : <div className="cards">
          {enrollments.map(item => <article className="card activity-card" key={item.id}>
            <div className="card-top"><span className="tag">{titleCase(item.tipo)}</span><span className={item.status === 'ativa' ? 'status' : 'status muted'}>{titleCase(item.status)}</span></div>
            <h2>{item.tema}</h2>
            <div className="activity-meta">
              <span><b>Horário</b>{scheduleLabel(item.inicio ?? item.dataHora, item.fim)}</span>
              <span><b>Sala</b>{item.sala?.nome ?? item.local}</span>
              <span><b>Assento</b>{item.assento?.codigo ?? 'Não informado'}</span>
            </div>
            <div className="card-bottom"><button className="text-button" type="button" onClick={() => void openActivity(item.atividadeId)}>Ver atividade</button></div>
          </article>)}
        </div>}
      </>}

      {page === 'conta' && <section className="panel narrow">
        <div className="tabs"><button className={authMode === 'login' ? 'tab active' : 'tab'} type="button" onClick={() => setAuthMode('login')}>Entrar</button><button className={authMode === 'register' ? 'tab active' : 'tab'} type="button" onClick={() => setAuthMode('register')}>Criar cadastro</button></div>
        <form onSubmit={event => void submitAuth(event)} className="form-grid">
          {authMode === 'register' && <><label>Nome<input required maxLength={200} value={authForm.name} onChange={event => setAuthForm({ ...authForm, name: event.target.value })} /></label><label>Curso<input required maxLength={120} value={authForm.course} onChange={event => setAuthForm({ ...authForm, course: event.target.value })} /></label></>}
          <label>E-mail<input type="email" required autoComplete="email" value={authForm.email} onChange={event => setAuthForm({ ...authForm, email: event.target.value })} /></label>
          <label>Senha<input type="password" required autoComplete={authMode === 'login' ? 'current-password' : 'new-password'} value={authForm.password} onChange={event => setAuthForm({ ...authForm, password: event.target.value })} /></label>
          <button className="button primary" type="submit">{authMode === 'login' ? 'Entrar' : 'Criar conta'}</button>
        </form>
      </section>}
    </main>
    <footer>ConectaCampus · Projeto Integrador · Fatec Shunji Nishimura de Pompeia</footer>
  </div>
}
