import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { useAuth } from './auth'
import type { Student } from './api'
import SiteHeader from './SiteHeader'
import Pagination, { usePagination } from './Pagination'

type Request = { id: string; status: string; justificativa: string; nome?: string; email?: string; curso?: string }

export default function AccessPage() {
  const { student, token, api, refreshProfile, signIn } = useAuth()
  const requestsView = useLocation().pathname === '/solicitacoes'
  const [request, setRequest] = useState<Request | null>(null)
  const [pending, setPending] = useState<Request[]>([])
  const [name, setName] = useState(student?.name || '')
  const [course, setCourse] = useState(student?.course || '')
  const [justification, setJustification] = useState('')
  const [feedback, setFeedback] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [loading, setLoading] = useState(true)
  const [loadFailed, setLoadFailed] = useState(false)
  const requestPages = usePagination(pending, 5)
  useEffect(() => { setName(student?.name || ''); setCourse(student?.course || '') }, [student?.id, student?.name, student?.course])
  const load = useCallback(async () => {
    if (!student) return
    setLoading(true); setLoadFailed(false)
    try {
      await refreshProfile()
      if (student.professor) setPending(await api<Request[]>('/api/acesso-professor'))
      else setRequest(await api<Request | null>('/api/acesso-professor/minha'))
    } catch (e) { setLoadFailed(true); setError(e instanceof Error ? e.message : 'Não foi possível carregar seus dados.') } finally { setLoading(false) }
  }, [student?.id, student?.professor, api, refreshProfile])
  useEffect(() => { void load() }, [load])
  async function save(event: FormEvent) {
    event.preventDefault(); if (busy || !token) return
    setBusy(true); setError(''); setFeedback('')
    try { const profile = await api<Student>('/api/auth/me', { method: 'PUT', body: JSON.stringify({ name, course }) }); signIn(token, profile); setFeedback('Perfil atualizado.') }
    catch (e) { setError(e instanceof Error ? e.message : 'Não foi possível atualizar o perfil.') } finally { setBusy(false) }
  }
  async function submit(event: FormEvent) {
    event.preventDefault(); if (busy) return
    setBusy(true); setError(''); setFeedback('')
    try { await api('/api/acesso-professor', { method: 'POST', body: JSON.stringify({ justificativa: justification }) }); setJustification(''); setFeedback('Solicitação enviada.'); await load() }
    catch (e) { setError(e instanceof Error ? e.message : 'Não foi possível enviar a solicitação.') } finally { setBusy(false) }
  }
  async function evaluate(id: string, decisao: string) {
    if (busy) return
    setBusy(true); setError(''); setFeedback('')
    try { await api(`/api/acesso-professor/${id}/avaliar`, { method: 'POST', body: JSON.stringify({ decisao }) }); setFeedback('Solicitação avaliada.'); await load() }
    catch (e) { setError(e instanceof Error ? e.message : 'Não foi possível avaliar.') } finally { setBusy(false) }
  }
  return <div className="app-shell"><SiteHeader active="acesso" /><main className="main-content workspace"><div className="workspace-heading"><div><h1>{requestsView ? 'Solicitações de acesso' : 'Meu perfil'}<span className="heading-period">.</span></h1><p>{requestsView ? 'Avalie os pedidos de alunos para ministrar aulas.' : 'Atualize seus dados e acompanhe seu acesso à plataforma.'}</p></div></div>
    {error && <div className="feedback error" role="alert">{error}</div>}{feedback && <div className="feedback success" role="status">{feedback}</div>}
    {!student ? <div className="empty">Entre na sua conta para acessar esta página. <Link to="/entrar">Entrar</Link></div> : loading ? <div className="empty workspace-loading" role="status">Carregando suas informações…</div> : loadFailed ? <div className="empty workspace-empty"><p>Não foi possível carregar seus dados. <button className="text-button" onClick={() => { setError(''); void load() }}>Tentar novamente</button></p></div> : requestsView ? student.professor ? <section className="workspace-panel profile-content"><div className="panel-heading"><h2>Solicitações pendentes</h2><span className="count-label">{pending.length} {pending.length === 1 ? 'PEDIDO' : 'PEDIDOS'}</span></div>{pending.length === 0 ? <div className="workspace-empty"><p>Nenhuma solicitação pendente.</p></div> : requestPages.items.map(item => <article className="request-item" key={item.id}><div className="request-avatar" aria-hidden="true">{item.nome?.trim().charAt(0).toUpperCase()}</div><div className="request-body"><h3>{item.nome}</h3><p className="request-meta">{item.curso} · {item.email}</p><p>{item.justificativa}</p><div className="proposal-actions"><button className="button primary" disabled={busy} onClick={() => void evaluate(item.id, 'aprovada')}>Aprovar</button><button className="button secondary" disabled={busy} onClick={() => void evaluate(item.id, 'recusada')}>Recusar</button></div></div></article>)}<Pagination label="solicitações" {...requestPages} /></section> : <div className="empty">Esta página é exclusiva para professores.</div> : <div className="profile-layout"><section className="workspace-panel"><div className="panel-heading"><h2>Seus dados</h2></div><form className="form-grid" onSubmit={event => void save(event)}><label>Nome<input required maxLength={200} value={name} onChange={e => setName(e.target.value)} /></label><label>E-mail<input value={student.email} readOnly /></label><label>Curso<input required maxLength={120} value={course} onChange={e => setCourse(e.target.value)} /></label><button className="button primary" disabled={busy}>{busy ? 'Salvando…' : 'Salvar alterações'}</button></form></section><section className="workspace-panel"><div className="panel-heading"><h2>{student.professor ? 'Seu acesso' : 'Ministrar atividades'}</h2></div>{student.professor ? <><span className="student-permission allowed">Professor</span><p>Você pode avaliar propostas e solicitações de alunos.</p><Link to="/solicitacoes">Ver solicitações de acesso →</Link></> : student.podeMinistrar ? <><span className="student-permission allowed">✓ Autorizado a ministrar</span><p>Você pode propor aulas para avaliação.</p><Link to="/ministrar">Acessar minhas aulas →</Link></> : request?.status === 'pendente' ? <><span className="student-permission">Aguardando avaliação</span><p>Sua solicitação está aguardando avaliação.</p></> : <><span className="student-permission">Não autorizado a ministrar</span><p>{request?.status === 'recusada' ? 'Sua última solicitação foi recusada. Você pode enviar outra.' : 'Solicite acesso para compartilhar seus conhecimentos com outros alunos.'}</p><form className="form-grid" onSubmit={event => void submit(event)}><label>Justificativa<textarea required maxLength={2000} rows={4} value={justification} onChange={e => setJustification(e.target.value)} placeholder="Conte por que você gostaria de ministrar aulas" /></label><button className="button secondary" disabled={busy}>{busy ? 'Enviando…' : 'Solicitar acesso'}</button></form></>}</section></div>}
  </main></div>
}
