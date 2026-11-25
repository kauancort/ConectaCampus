import Pagination, { usePagination } from './Pagination'
import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from './auth'
import ProfessorHeader from './professor/ProfessorHeader'
import SiteHeader from './SiteHeader'
import TeachingNavigation from './TeachingNavigation'
import { getUserPermissions } from './navigation'

type Proposal = { id: string; titulo: string; tema: string; descricao: string; tipoSala: string; numeroSala?: string | null; inicio: string; fim: string; status: string; aluno: string; professor?: string; motivoRecusa?: string; atividadeId?: string; statusAtividade?: string }
type Participant = { alunoId: string; nomeAluno: string; status: string; presente: boolean | null }
const empty = { titulo: '', tema: '', descricao: '', tipoSala: '', numeroSala: '', data: '', inicio: '', fim: '' }
const schedule = (p: Proposal) => `${new Date(p.inicio).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })} – ${new Date(p.fim).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}`

function ProposalCard({ proposal, children }: { proposal: Proposal; children?: React.ReactNode }) {
  return <article className="panel proposal-card"><div className="proposal-card-top"><span className={`tag proposal-status ${proposal.status}`}>{proposal.status}</span><span>{schedule(proposal)}</span></div><h2>{proposal.titulo}</h2><p className="proposal-topic">{proposal.tema}</p><p className="proposal-description">{proposal.descricao}</p><div className="proposal-meta"><span><b>MINISTRANTE</b>{proposal.aluno}</span><span><b>SALA</b>{proposal.numeroSala ? `Sala ${proposal.numeroSala}` : 'Não informada'}</span><span><b>TIPO DE SALA</b>{proposal.tipoSala}</span>{proposal.professor && <span><b>PROFESSOR RESPONSÁVEL</b>{proposal.professor}</span>}</div>{proposal.motivoRecusa && <div className="proposal-reason"><b>Motivo da recusa:</b> {proposal.motivoRecusa}</div>}{proposal.statusAtividade === 'cancelada' && <p>Atividade cancelada</p>}{children && <div className="proposal-card-actions">{children}</div>}</article>
}

export function StudentProposalsPage() {
  const { student, api } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const { professor, canTeach } = getUserPermissions(student)
  const creating = location.pathname === '/ministrar/nova' || location.pathname === '/professor/atividades/nova'
  const [items, setItems] = useState<Proposal[]>([])
  const proposalPages = usePagination(items, 5)
  const [form, setForm] = useState(empty)
  const message = (location.state as { message?: string } | null)?.message || ''
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [loading, setLoading] = useState(true)
  const [loadFailed, setLoadFailed] = useState(false)
  const load = useCallback(async () => { setLoading(true); setLoadFailed(false); try { setItems(await api<Proposal[]>('/api/propostas/minhas')) } catch (e) { setLoadFailed(true); setError(e instanceof Error ? e.message : 'Não foi possível carregar as propostas.') } finally { setLoading(false) } }, [api])
  useEffect(() => { if (canTeach && !creating) void load() }, [canTeach, creating, load])
  async function submit(event: FormEvent) {
    event.preventDefault(); setError('')
    const start = new Date(`${form.data}T${form.inicio}:00`); const end = new Date(`${form.data}T${form.fim}:00`)
    if (!Number.isFinite(start.getTime()) || !Number.isFinite(end.getTime()) || start <= new Date() || end <= start) { setError('Informe um horário futuro e término posterior ao início.'); return }
    setBusy(true)
    try {
      await api('/api/propostas', { method: 'POST', body: JSON.stringify({ titulo: form.titulo, tema: form.tema, descricao: form.descricao, tipoSala: form.tipoSala, numeroSala: form.numeroSala, inicio: start.toISOString(), fim: end.toISOString() }) })
      setForm(empty); navigate('/ministrar', { replace: true, state: { message: professor ? 'Aula publicada. Os alunos já podem se inscrever.' : 'Proposta enviada. Aguarde a avaliação de um professor.' } })
    } catch (e) { setError(e instanceof Error ? e.message : 'Não foi possível enviar sua proposta.') } finally { setBusy(false) }
  }
  return <div className="app-shell"><SiteHeader active="ministrar" /><main className="main-content workspace"><div className="workspace-heading"><div><h1>{creating ? <>{professor ? 'Publicar uma' : 'Propor uma'} <em>aula.</em></> : <>Minhas <em>aulas.</em></>}</h1><p>{creating ? (professor ? 'Crie sua aula e publique diretamente para os alunos.' : 'Conte sua ideia e escolha quando ela vai acontecer.') : (professor ? 'Gerencie as aulas que você ministra.' : 'Acompanhe suas propostas e a avaliação de um professor.')}</p></div></div>
    {!creating && <TeachingNavigation />}
    {!canTeach ? <div className="empty">Esta área é exclusiva para alunos autorizados a ministrar. <Link to="/acesso">Solicitar acesso</Link></div> : <>
      {error && <div className="feedback error" role="alert">{error}</div>}{message && <div className="feedback success" role="status">{message}</div>}
      {creating ? <><Link className="text-button back-button" to="/ministrar">← Voltar às minhas aulas</Link><div className="workspace-grid proposal-workspace"><div className="workspace-primary"><section className="workspace-panel proposal-editor"><div className="panel-heading"><div><h2>Dados da aula</h2></div><span className="panel-mark" aria-hidden="true">↗</span></div><p>{professor ? 'Informe o número, o tipo de sala e o horário. A aula será publicada com a reserva da sala.' : 'Informe o número, o tipo de sala e o horário. A reserva só será confirmada se a proposta for aprovada.'}</p><form className="form-grid proposal-form" onSubmit={event => void submit(event)}>
        <label>Título da aula<input required maxLength={200} value={form.titulo} onChange={e => setForm({ ...form, titulo: e.target.value })} /></label>
        <label>Tema<input required maxLength={200} value={form.tema} onChange={e => setForm({ ...form, tema: e.target.value })} /></label>
        <label>Descrição<textarea required maxLength={5000} rows={4} value={form.descricao} onChange={e => setForm({ ...form, descricao: e.target.value })} /></label>
        <label>Tipo de sala<select required value={form.tipoSala} onChange={e => setForm({ ...form, tipoSala: e.target.value })}><option value="">Selecione</option><option value="DESKTOP">Desktop · 24 lugares</option><option value="NOTEBOOK">Notebook · 20 lugares</option></select></label>
        <label>Número da sala<input required inputMode="numeric" pattern="[0-9]{1,6}" maxLength={6} placeholder="Ex.: 101" value={form.numeroSala} onChange={e => setForm({ ...form, numeroSala: e.target.value })} /><small>Informe até 6 dígitos, sem letras.</small></label>
        <label>Data<input required type="date" value={form.data} onChange={e => setForm({ ...form, data: e.target.value })} /></label>
        <label>Horário de início<input required type="time" value={form.inicio} onChange={e => setForm({ ...form, inicio: e.target.value })} /></label>
        <label>Horário de término<input required type="time" value={form.fim} onChange={e => setForm({ ...form, fim: e.target.value })} /></label>
        <button className="button primary" disabled={busy}>{busy ? (professor ? 'Publicando…' : 'Enviando…') : (professor ? 'Publicar aula' : 'Enviar proposta')}</button>
      </form></section></div></div></> : <><div className="workspace-section-head"><div><h2 className="proposal-heading">{professor ? 'Aulas que ministro' : 'Minhas propostas'}</h2><p>{professor ? 'Consulte suas aulas publicadas e acompanhe os participantes.' : 'Veja o andamento das aulas que você enviou.'}</p></div></div>{loading ? <div className="empty workspace-empty" role="status"><span aria-hidden="true">◷</span><p>Carregando propostas…</p></div> : loadFailed ? <div className="empty workspace-empty"><p>Não foi possível carregar suas propostas. <button className="text-button" onClick={() => { setError(''); void load() }}>Tentar novamente</button></p></div> : items.length ? proposalPages.items.map(p => <ProposalCard proposal={p} key={p.id}>{p.status === 'aprovada' && p.atividadeId && <Link className="button primary proposal-new-link" to={`/ministrar/aulas/${p.atividadeId}`}>Abrir aula <span aria-hidden="true">↗</span></Link>}</ProposalCard>) : <div className="empty workspace-empty"><span aria-hidden="true">✳</span><p>{professor ? 'Você ainda não publicou aulas.' : 'Você ainda não enviou propostas.'}</p></div>}<Pagination label="minhas propostas" {...proposalPages} /></>}
    </>}</main></div>
}

export function TeacherProposalsPage() {
  const { student, api } = useAuth()
  const approvedView = useLocation().pathname === '/professor/aulas-aprovadas'
  const [pending, setPending] = useState<Proposal[]>([])
  const [approved, setApproved] = useState<Proposal[]>([])
  const pendingPages = usePagination(pending, 5)
  const approvedPages = usePagination(approved, 5)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const [rejection, setRejection] = useState<Record<string, string>>({})
  const [busy, setBusy] = useState<string | null>(null)
  const [participants, setParticipants] = useState<Record<string, Participant[]>>({})
  const [loading, setLoading] = useState(true)
  const [loadFailed, setLoadFailed] = useState(false)
  const load = useCallback(async () => { setLoading(true); setLoadFailed(false); try { const [a, b] = await Promise.all([api<Proposal[]>('/api/propostas/pendentes'), api<Proposal[]>('/api/propostas/aprovadas')]); setPending(a); setApproved(b) } catch (e) { setLoadFailed(true); setError(e instanceof Error ? e.message : 'Não foi possível carregar as aulas.') } finally { setLoading(false) } }, [api])
  useEffect(() => { if (student?.professor) void load() }, [student?.professor, load])
  async function evaluate(id: string, decisao: 'aprovada' | 'recusada') {
    if (decisao === 'recusada' && !rejection[id]?.trim()) { setError('Informe o motivo da recusa.'); return }
    setError(''); setMessage(''); setBusy(id)
    try { await api(`/api/propostas/${id}/avaliar`, { method: 'POST', body: JSON.stringify({ decisao, motivo: rejection[id] || null }) }); setMessage(decisao === 'aprovada' ? 'Aula aprovada e publicada.' : 'Proposta recusada.'); await load() }
    catch (e) { setError(e instanceof Error ? e.message : 'Não foi possível avaliar a proposta.'); await load() } finally { setBusy(null) }
  }
  async function showParticipants(p: Proposal) {
    if (!p.atividadeId) return
    try { const list = await api<Participant[]>(`/api/atividades/${p.atividadeId}/inscricoes`); setParticipants(current => ({ ...current, [p.id]: list })) } catch (e) { setError(e instanceof Error ? e.message : 'Não foi possível carregar os participantes.') }
  }
  async function setPresence(p: Proposal, person: Participant, presente: boolean | null) {
    if (!p.atividadeId) return
    try { await api(`/api/atividades/${p.atividadeId}/inscricoes/${encodeURIComponent(person.alunoId)}/presenca`, { method: 'PUT', body: JSON.stringify({ presente }) }); await showParticipants(p) }
    catch (e) { setError(e instanceof Error ? e.message : 'Não foi possível registrar a presença.') }
  }
  async function cancelActivity(p: Proposal) {
    if (!p.atividadeId || !window.confirm('Cancelar esta aula? As pessoas inscritas serão notificadas.')) return
    setError(''); setMessage(''); setBusy(p.id)
    try { await api(`/api/atividades/${p.atividadeId}/cancelar`, { method: 'POST' }); setMessage('Aula cancelada.'); await load() }
    catch (e) { setError(e instanceof Error ? e.message : 'Não foi possível cancelar a aula.') } finally { setBusy(null) }
  }
  return <div className="app-shell professor-registration"><ProfessorHeader /><main className="main-content workspace"><div className="workspace-heading supervisor-heading"><div><h1>Supervisão de <em>aulas.</em></h1><p>{approvedView ? 'Acompanhe as aulas que você aprovou.' : 'Avalie propostas enviadas pelos alunos.'}</p></div></div><TeachingNavigation pendingCount={!loading && !loadFailed ? pending.length : undefined} approvedCount={!loading && !loadFailed ? approved.length : undefined} />
    {error && <div className="feedback error" role="alert">{error}</div>}{message && <div className="feedback success" role="status">{message}</div>}
    {loading ? <div className="empty workspace-empty workspace-loading" role="status"><span aria-hidden="true">◷</span><p>Carregando propostas e aulas…</p></div> : loadFailed ? <div className="empty workspace-empty workspace-loading"><p>Não foi possível carregar as aulas. <button className="text-button" onClick={() => { setError(''); void load() }}>Tentar novamente</button></p></div> : !approvedView ? <><div className="workspace-section-head"><div><h2 className="proposal-heading">Fila compartilhada</h2><p>Uma aprovação reserva a sala e publica a aula para inscrições.</p></div><span className="count-label">{pending.length} PENDENTE(S)</span></div>{pending.length ? pendingPages.items.map(p => <ProposalCard proposal={p} key={p.id}><label>Motivo da recusa (necessário para recusar)<textarea rows={3} maxLength={2000} value={rejection[p.id] || ''} onChange={e => setRejection({ ...rejection, [p.id]: e.target.value })} /></label><div className="proposal-actions"><button className="button primary" disabled={busy !== null} onClick={() => void evaluate(p.id, 'aprovada')}>Aprovar aula</button><button className="button secondary" disabled={busy !== null} onClick={() => void evaluate(p.id, 'recusada')}>Recusar aula</button></div></ProposalCard>) : <div className="empty workspace-empty"><span aria-hidden="true">✳</span><p>Nenhuma proposta aguardando avaliação.</p></div>}<Pagination label="fila de propostas" {...pendingPages} /></> : <>
    <div className="workspace-section-head"><div><h2 className="proposal-heading">Aulas que aprovei</h2></div><span className="count-label">{approved.length} AULA(S)</span></div>{approved.length ? approvedPages.items.map(p => <ProposalCard proposal={p} key={p.id}><div className="proposal-actions">{p.atividadeId && <Link className="button primary" to={`/professor/aulas/${p.atividadeId}`}>Detalhes da aula</Link>}<button className="button secondary" onClick={() => void showParticipants(p)}>Ver participantes</button>{p.statusAtividade !== 'cancelada' && <button className="button secondary" disabled={busy !== null} onClick={() => void cancelActivity(p)}>Cancelar aula</button>}</div>{participants[p.id] && <div className="participants"><h3>Participantes e presença</h3>{participants[p.id].length ? participants[p.id].map(person => <div key={person.alunoId}><p>{person.nomeAluno} · {person.status}{person.status === 'ativa' && ` · ${person.presente === null ? 'Presença não registrada' : person.presente ? 'Presente' : 'Ausente'}`}</p>{person.status === 'ativa' && <div className="proposal-actions"><button className="button secondary small" onClick={() => void setPresence(p, person, true)}>Presente</button><button className="button secondary small" onClick={() => void setPresence(p, person, false)}>Ausente</button><button className="text-button" onClick={() => void setPresence(p, person, null)}>Limpar presença</button></div>}</div>) : <p>Nenhuma inscrição ainda.</p>}</div>}</ProposalCard>) : <div className="empty workspace-empty"><span aria-hidden="true">○</span><p>Você ainda não aprovou nenhuma aula.</p></div>}<Pagination label="aulas aprovadas" {...approvedPages} />
    </>}</main></div>
}
