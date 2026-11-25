import Pagination, { usePagination } from './Pagination'
import { useEffect, useState, type CSSProperties } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { ApiError, type Activity, type Enrollment, type Seat, type SeatMap } from './api'
import { useAuth } from './auth'
import AuthForm from './AuthForm'
import SiteHeader from './SiteHeader'
import LandingPage from './LandingPage'

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
  const { token, student, signIn, api } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const [page, setPage] = useState<Page>(() => location.pathname === '/inscricoes' ? 'inscricoes' : ['/entrar', '/cadastro'].includes(location.pathname) ? 'conta' : 'catalogo')
  const [authMode, setAuthMode] = useState<'login' | 'register'>(() => location.pathname === '/cadastro' ? 'register' : 'login')
  const [activities, setActivities] = useState<Activity[]>([])
  const [theme, setTheme] = useState('')
  const [catalogLoading, setCatalogLoading] = useState(true)
  const [detailLoading, setDetailLoading] = useState(false)
  const [seatLoading, setSeatLoading] = useState(false)
  const [confirmingSeat, setConfirmingSeat] = useState(false)
  const [cancellingEnrollmentId, setCancellingEnrollmentId] = useState<string | null>(null)
  const [enrollmentsLoading, setEnrollmentsLoading] = useState(false)
  const [selected, setSelected] = useState<Activity | null>(null)
  const [seatMap, setSeatMap] = useState<SeatMap | null>(null)
  const [selectedSeat, setSelectedSeat] = useState<Seat | null>(null)
  const [enrollments, setEnrollments] = useState<Enrollment[]>([])
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [returnToSeat, setReturnToSeat] = useState(false)
  const catalogPages = usePagination(activities, 6)
  const enrollmentPages = usePagination(enrollments, 6)

  const clearFeedback = () => { setError(''); setNotice('') }

  const loadCatalog = async (search = theme, clear = true) => {
    setCatalogLoading(true)
    if (clear) clearFeedback()
    try {
      const query = search.trim() ? `?tema=${encodeURIComponent(search.trim())}` : ''
      const list = await api<Activity[]>(`/api/atividades${query}`)
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
      setSeatMap(await api<SeatMap>(`/api/atividades/${encodeURIComponent(activityId)}/assentos`))
    } catch (requestError) {
      setSeatMap(null)
      setSelectedSeat(null)
      setError(requestError instanceof Error ? requestError.message : 'Não foi possível carregar o mapa de assentos.')
    } finally {
      setSeatLoading(false)
    }
  }

  useEffect(() => {
    if (token) void loadCatalog('', false)
    else { setActivities([]); setSelected(null); setSeatMap(null); setSelectedSeat(null); setCatalogLoading(false) }
  }, [token])
  useEffect(() => {
    if (location.pathname === '/inscricoes') setPage('inscricoes')
    else if (location.pathname === '/entrar' || location.pathname === '/cadastro') { setAuthMode(location.pathname === '/cadastro' ? 'register' : 'login'); setPage('conta') }
    else if (location.pathname === '/') setPage('catalogo')
  }, [location.pathname])
  useEffect(() => {
    if (token) void loadEnrollments(false)
    else setEnrollments([])
  }, [token])

  const openActivity = async (id: string) => {
    setDetailLoading(true)
    clearFeedback()
    try {
      const activity = await api<Activity>(`/api/atividades/${encodeURIComponent(id)}`)
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

  const confirmEnrollment = async () => {
    if (!selected || !selectedSeat) return
    if (!token) {
      setReturnToSeat(true)
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
        await Promise.all([loadSeatMap(selected.id, true), loadEnrollments(false)])
        setError(requestError.message || 'Não foi possível confirmar a inscrição. O mapa foi atualizado.')
      } else {
        setError(requestError instanceof Error ? requestError.message : 'Não foi possível confirmar sua inscrição.')
      }
    } finally {
      setConfirmingSeat(false)
    }
  }

  const cancelEnrollment = async (enrollment: Enrollment) => {
    if (!token) {
      setPage('conta')
      setError('Entre na sua conta para cancelar a inscrição.')
      return
    }

    setCancellingEnrollmentId(enrollment.id)
    clearFeedback()
    try {
      await api<void>(`/api/atividades/${encodeURIComponent(enrollment.atividadeId)}/inscricoes/me`, {
        method: 'DELETE',
      })
      await loadEnrollments(false)
      if (selected?.id === enrollment.atividadeId) await loadSeatMap(enrollment.atividadeId, true)
      setNotice(`Inscrição em “${enrollment.tema}” cancelada.`)
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Não foi possível cancelar sua inscrição.')
    } finally {
      setCancellingEnrollmentId(null)
    }
  }

  const goTo = (nextPage: Page) => {
    clearFeedback()
    if (nextPage !== page) { document.documentElement.scrollTop = 0; document.body.scrollTop = 0 }
    setPage(nextPage)
    if (nextPage === 'catalogo' && location.pathname !== '/') navigate('/')
    if (nextPage === 'inscricoes' && location.pathname !== '/inscricoes') navigate('/inscricoes')
  }

  const columns = seatMap?.sala.tipo.toUpperCase() === 'NOTEBOOK' ? 5 : 6
  const authenticated = Boolean(token && student)
  const landing = !authenticated && page !== 'conta'

  return <div className="app-shell student-area">
    <SiteHeader active={page === 'inscricoes' ? 'inscricoes' : page === 'conta' ? 'conta' : 'atividades'} onCatalog={() => goTo('catalogo')} onEnrollments={() => { goTo('inscricoes'); void loadEnrollments() }} onLogin={() => { setAuthMode('login'); goTo('conta') }} onLogout={() => { goTo('catalogo'); setNotice('Você saiu da sua conta.') }} />

    <main className="main-content">
      {landing ? <LandingPage onRegister={() => { setAuthMode('register'); goTo('conta') }} onLogin={() => { setAuthMode('login'); goTo('conta') }} /> : <div className="workspace-heading student-heading">
        <div>

          <h1>{page === 'catalogo' ? 'Aulas e palestras' : page === 'detalhe' ? 'Detalhes da atividade' : page === 'assentos' ? 'Escolha seu assento' : page === 'inscricoes' ? 'Minhas inscrições' : 'Acesse sua conta'}</h1>
          <p>{page === 'catalogo' ? 'Encontre seu próximo encontro de aprendizado no campus.' : page === 'assentos' ? 'A seleção só é confirmada depois da inscrição.' : page === 'inscricoes' ? 'Acompanhe as atividades em que você se inscreveu.' : page === 'detalhe' ? 'Conheça os detalhes antes de reservar seu lugar.' : 'Seu lugar na comunidade começa aqui.'}</p>
        </div>

      </div>}

      {error && page !== 'conta' && !landing && <div className="feedback error" role="alert">{error}</div>}
      {notice && <div className="feedback success" role="status">{notice}</div>}

      {authenticated && page === 'catalogo' && <section className="catalog-section member-catalog" id="atividades">
        <div className="catalog-section-heading"><div><h2>Encontre uma atividade<span className="heading-period">.</span></h2><p>Procure por um tema e reserve seu lugar.</p></div></div>
        <form className="panel filters student-search" onSubmit={event => { event.preventDefault(); catalogPages.setPage(1); void loadCatalog() }}>
          <label>Pesquisar por tema
            <input value={theme} onChange={event => setTheme(event.target.value)} placeholder="Ex.: programação, carreira" />
          </label>
          <button className="button primary" disabled={catalogLoading}>Buscar <span aria-hidden="true">↗</span></button>
        </form>
        <div className="section-row"><h2>Atividades disponíveis</h2><span>{catalogLoading ? 'Carregando…' : `${activities.length} encontrada(s)`}</span></div>
        {catalogLoading ? <div className="empty" role="status">Carregando atividades…</div> : activities.length === 0 ? <div className="empty catalog-empty"><span className="empty-graphic" aria-hidden="true">✳</span><div><strong>Por enquanto, a agenda está tranquila.</strong><p>Nenhuma atividade encontrada. Tente outro tema.</p></div></div> : <div className="cards activity-list">
          {catalogPages.items.map(activity => {
            const finished = new Date(activity.inicio).getTime() <= Date.now()
            const unavailable = activity.lugaresDisponiveis === 0
            const availability = activity.lugaresDisponiveis == null ? null : Math.max(0, Math.min(100, (activity.lugaresDisponiveis / activity.sala.capacidade) * 100))
            const status = finished ? 'Encerrada' : unavailable ? 'Sem vagas' : 'Disponível'

            return <article className="card activity-card" key={activity.id}>
              <div className="card-top"><span className="tag">{titleCase(activity.modalidade)}</span><span className={`status${finished || unavailable ? ' muted' : ''}`}><i aria-hidden="true" />{status}</span></div>
              <h2>{activity.titulo || activity.tema}</h2>
              <p className="activity-description">{activity.descricao}</p>
              <div className="activity-schedule">
                <span className="activity-schedule-icon" aria-hidden="true">◷</span>
                <span><b>Data e horário</b>{scheduleLabel(activity.inicio, activity.fim)}</span>
              </div>
              <div className="activity-meta">
                <span><b>Sala</b>{activity.sala.nome}</span>
                <span><b>Ministrante</b>{activity.nomeMinistrante || activity.nomeProfessor || 'Não informado'}</span>
                {activity.titulo && activity.titulo !== activity.tema && <span><b>Tema</b>{activity.tema}</span>}
                <span><b>Ambiente</b>{titleCase(activity.sala.tipo)}</span>
              </div>
              <div className="activity-capacity">
                <div><span>{activity.lugaresDisponiveis == null ? 'Capacidade da sala' : 'Vagas disponíveis'}</span><b>{activity.lugaresDisponiveis == null ? `${activity.sala.capacidade} lugares` : `${activity.lugaresDisponiveis} de ${activity.sala.capacidade}`}</b></div>
                {availability != null && <span className="capacity-track" aria-hidden="true"><i style={{ '--availability': `${availability}%` } as CSSProperties} /></span>}
              </div>
              <div className="card-bottom"><button className="text-button detail-link" type="button" disabled={detailLoading} onClick={() => void openActivity(activity.id)}>{detailLoading ? 'Carregando…' : <>Ver detalhes <span aria-hidden="true">↗</span></>}</button></div>
            </article>
          })}
        </div>}
        {!catalogLoading && <Pagination label="atividades" {...catalogPages} />}
      </section>}

      {authenticated && page === 'detalhe' && <>
        <button className="text-button back-button" type="button" onClick={() => goTo('catalogo')}>← Voltar às atividades</button>
        {detailLoading ? <div className="empty" role="status">Carregando atividade…</div> : selected && <section className="activity-detail-layout">
          <div className="detail-main"><div className="detail-overline"><span className="tag">{titleCase(selected.modalidade)}</span></div><h2>{selected.titulo || selected.tema}</h2><div className="detail-divider" /><p className="detail-description">{selected.descricao}</p>{selected.titulo && selected.titulo !== selected.tema && <div className="detail-subject"><span>TEMA</span><b>{selected.tema}</b></div>}</div>
          <aside className="detail-aside"><h3>Informações do encontro<span>.</span></h3><div className="detail-grid">
            <span><b>Data e horário</b>{scheduleLabel(selected.inicio, selected.fim)}</span>
            <span><b>Sala</b>{selected.sala.nome}</span>
            <span><b>Aluno ministrante</b>{selected.nomeMinistrante || selected.nomeProfessor || 'Não informado'}</span>
            <span><b>Tipo de sala</b>{titleCase(selected.sala.tipo)}</span>
            <span><b>Capacidade</b>{selected.sala.capacidade} lugares</span>
          </div>
          {selected.status === 'publicada' && new Date(selected.inicio).getTime() > Date.now() && <button className="button primary" type="button" onClick={() => void openSeatMap()}>Escolher assento</button>}
          <small>Seu lugar é confirmado após a inscrição.</small></aside>
        </section>}
      </>}

      {authenticated && page === 'assentos' && <>
        <button className="text-button back-button" type="button" onClick={() => { setSelectedSeat(null); goTo('detalhe') }}>← Voltar aos detalhes</button>
        {seatLoading ? <div className="empty" role="status">Carregando mapa de assentos…</div> : seatMap && <section className="panel seat-panel">
          <div className="seat-panel-heading">
            <div><span className="tag">{titleCase(seatMap.sala.tipo)}</span><h2>{seatMap.tema}</h2><p>{scheduleLabel(seatMap.inicio, seatMap.fim)} · {seatMap.sala.nome}</p></div>
            <div className="availability"><b>{seatMap.lugaresDisponiveis}</b><span>vagas disponíveis</span></div>
          </div>
          <div className="seat-legend" aria-label="Legenda do mapa de assentos"><span><i className="seat-sample available" />Disponível</span><span><i className="seat-sample occupied" />Ocupado</span><span><i className="seat-sample chosen" />Selecionado</span>{seatMap.jaInscrito && <span><i className="seat-sample mine" />Seu assento</span>}</div>
          <div className="seat-front">Frente da sala · Ministrante</div>
          <div className="seat-map" style={{ '--seat-columns': columns } as CSSProperties} aria-label={`Mapa de assentos da sala ${seatMap.sala.nome}`}>
            {seatMap.assentos.map(seat => {
              const occupied = seat.status === 'OCUPADO'
              const mine = seat.id === seatMap.meuAssentoId
              const chosen = selectedSeat?.id === seat.id
              return <button key={seat.id} className={`seat ${mine ? 'mine' : occupied ? 'occupied' : chosen ? 'selected' : 'available'}`} type="button" disabled={occupied || confirmingSeat || Boolean(seatMap.jaInscrito)} aria-pressed={chosen} aria-label={`Assento ${seat.codigo}${mine ? ', seu assento' : occupied ? ', ocupado' : chosen ? ', selecionado' : ', disponível'}`} onClick={() => { setSelectedSeat(seat); clearFeedback() }}>{seat.codigo}</button>
            })}
          </div>
          <div className="seat-selection">
            <div>{seatMap.jaInscrito ? <>Você já está inscrito{seatMap.meuAssentoId && <> no assento <b>{seatMap.assentos.find(seat => seat.id === seatMap.meuAssentoId)?.codigo}</b></>}. Para escolher outro lugar, cancele a inscrição em Minhas inscrições.</> : selectedSeat ? <>Assento selecionado: <b>{selectedSeat.codigo}</b></> : 'Selecione um assento disponível no mapa.'}</div>
            {seatMap.jaInscrito ? <button className="button secondary" type="button" onClick={() => { goTo('inscricoes'); void loadEnrollments() }}>Minhas inscrições</button> : <button className="button primary" type="button" disabled={!selectedSeat || confirmingSeat} onClick={() => void confirmEnrollment()}>{confirmingSeat ? 'Confirmando…' : 'Confirmar inscrição'}</button>}
          </div>
        </section>}
      </>}

      {authenticated && page === 'inscricoes' && <>
        {!student ? <div className="empty">Entre para consultar suas inscrições.</div> : enrollmentsLoading ? <div className="empty" role="status">Carregando inscrições…</div> : enrollments.length === 0 ? <div className="empty workspace-empty"><span aria-hidden="true">✳</span><p>Você ainda não possui inscrições.</p></div> : <div className="cards enrollment-cards">
          {enrollmentPages.items.map(item => <article className="card activity-card" key={item.id}>
            <div className="card-top"><span className="tag">{titleCase(item.tipo)}</span><span className={item.status === 'ativa' ? 'status' : 'status muted'}>{titleCase(item.status)}</span></div>
            <h2>{item.tema}</h2>
            <div className="activity-meta">
              <span><b>Horário</b>{scheduleLabel(item.inicio ?? item.dataHora, item.fim)}</span>
              <span><b>Sala</b>{item.sala?.nome ?? item.local}</span>
              <span><b>Assento</b>{item.assento?.codigo ?? 'Não informado'}</span>
            </div>
            <div className="card-bottom">
              <button className="text-button" type="button" onClick={() => void openActivity(item.atividadeId)}>Ver atividade</button>
              {item.status === 'ativa' && <button className="text-button" type="button" disabled={cancellingEnrollmentId !== null} onClick={() => void cancelEnrollment(item)}>
                {cancellingEnrollmentId === item.id ? 'Cancelando…' : 'Cancelar inscrição'}
              </button>}
            </div>
          </article>)}
        </div>}
        {!enrollmentsLoading && <Pagination label="inscrições" {...enrollmentPages} />}
      </>}

      {page === 'conta' && <div className="account-layout"><aside className="account-intro"><div><span className="account-intro-star" aria-hidden="true">✳</span><h2>Um espaço para aprender <em>em conjunto.</em></h2><p>Entre para acompanhar suas inscrições e fazer parte dos próximos encontros.</p></div></aside><section className="panel narrow">
        <AuthForm key={authMode} initialMode={authMode} initialMessage={error} onSignIn={result => { const resumeSeat = Boolean(returnToSeat && selected); signIn(result.accessToken, result.student); setPage(resumeSeat ? 'assentos' : 'catalogo'); if (!resumeSeat && location.pathname !== '/') navigate('/', { replace: true }); setReturnToSeat(false); setError(''); setNotice(`Bem-vindo(a), ${result.student.name}.`) }} />
      </section></div>}
    </main>
  </div>
}
