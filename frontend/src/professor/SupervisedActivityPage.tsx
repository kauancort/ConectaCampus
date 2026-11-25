import { useCallback, useEffect, useState, type CSSProperties, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { type Activity, type ReservedSeat, type SeatMap } from '../api'
import { useAuth } from '../auth'
import SiteHeader from '../SiteHeader'

type Proposal = { atividadeId?: string; titulo: string; tema: string; descricao: string; aluno: string; professor?: string }
type Participant = { alunoId: string; nomeAluno: string; status: string; presente: boolean | null; assento: ReservedSeat | null }
type Details = { proposal: Proposal; activity: Activity; map: SeatMap; participants: Participant[] }

export default function SupervisedActivityPage({ instructor = false }: { instructor?: boolean }) {
  const { id = '' } = useParams()
  const { api } = useAuth()
  const [details, setDetails] = useState<Details | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState<string | null>(null)
  const [notice, setNotice] = useState('')
  const [editing, setEditing] = useState(false)
  const [form, setForm] = useState({ titulo: '', tema: '', descricao: '' })

  const load = useCallback(async () => {
    setLoading(true); setError('')
    try {
      const proposals = await api<Proposal[]>(instructor ? '/api/propostas/minhas' : '/api/propostas/aprovadas')
      const proposal = proposals.find(item => item.atividadeId === id)
      if (!proposal) { setDetails(null); setError('Esta aula não foi encontrada entre as aulas sob sua responsabilidade.'); return }
      const path = `/api/atividades/${encodeURIComponent(id)}`
      const [activity, map, participants] = await Promise.all([api<Activity>(path), api<SeatMap>(`${path}/assentos`), api<Participant[]>(`${path}/inscricoes`)])
      setDetails({ proposal, activity, map, participants })
    } catch (e) { setDetails(null); setError(e instanceof Error ? e.message : 'Não foi possível carregar os detalhes da aula.') }
    finally { setLoading(false) }
  }, [api, id, instructor])
  useEffect(() => { void load() }, [load])

  async function setPresence(person: Participant, presente: boolean | null) {
    setBusy(person.alunoId); setError(''); setNotice('')
    try {
      await api(`/api/atividades/${encodeURIComponent(id)}/inscricoes/${encodeURIComponent(person.alunoId)}/presenca`, { method: 'PUT', body: JSON.stringify({ presente }) })
      setDetails(current => current && { ...current, participants: current.participants.map(item => item.alunoId === person.alunoId ? { ...item, presente } : item) })
      setNotice(`Presença de ${person.nomeAluno} atualizada.`)
    } catch (e) { setError(e instanceof Error ? e.message : 'Não foi possível atualizar a presença.') }
    finally { setBusy(null) }
  }

  async function saveContent(event: FormEvent) {
    event.preventDefault(); if (busy) return
    setBusy('edit'); setError(''); setNotice('')
    try {
      const updated = await api<typeof form>(`/api/atividades/${encodeURIComponent(id)}/conteudo`, { method: 'PUT', body: JSON.stringify(form) })
      setDetails(current => current && { ...current, proposal: { ...current.proposal, ...updated } })
      setEditing(false); setNotice('Informações da aula atualizadas.')
    } catch (e) { setError(e instanceof Error ? e.message : 'Não foi possível atualizar a aula.') }
    finally { setBusy(null) }
  }

  const activeParticipants = details?.participants.filter(person => person.status === 'ativa') ?? []
  const date = (value: string) => new Date(value).toLocaleString('pt-BR', { dateStyle: 'full', timeStyle: 'short' })
  return <div className="app-shell"><SiteHeader active={instructor ? 'ministrar' : 'professor'} /><main className="main-content workspace">
    <Link className="text-button back-button" to={instructor ? '/ministrar' : '/professor/aulas-aprovadas'}>{instructor ? '← Voltar às minhas aulas' : '← Voltar às aulas aprovadas'}</Link>
    <div className="workspace-heading"><div><h1>Detalhes da <em>aula.</em></h1><p>Acompanhe a ocupação e os participantes da aula sob sua responsabilidade.</p></div></div>
    {error && <div className="feedback error" role="alert">{error} {!details && <button className="text-button" onClick={() => void load()}>Tentar novamente</button>}</div>}
    {notice && <div className="feedback success" role="status">{notice}</div>}
    {loading ? <div className="empty workspace-empty" role="status">Carregando detalhes da aula…</div> : details && <>
      <section className="workspace-panel supervision-detail"><div className="supervision-detail-top"><div><p className="eyebrow">{details.proposal.tema}</p><h2>{details.proposal.titulo}</h2></div><span className="tag">{details.activity.status === 'cancelada' ? 'CANCELADA' : 'PUBLICADA'}</span></div><p>{details.proposal.descricao}</p><div className="proposal-meta"><span><b>MINISTRANTE</b>{details.proposal.aluno}</span><span><b>DATA E HORÁRIO</b>{date(details.map.inicio)}{details.map.fim && ` – ${new Date(details.map.fim).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}`}</span><span><b>SALA</b>{details.map.sala.nome} · {details.map.sala.tipo}</span><span><b>PROFESSOR RESPONSÁVEL</b>{details.proposal.professor || (instructor ? 'Não informado' : 'Você')}</span></div></section>
      {instructor && details.activity.status === 'publicada' && new Date(details.map.inicio).getTime() > Date.now() && <section className="workspace-panel lesson-editor"><div className="panel-heading"><div><h2>Informações da aula</h2><p>Atualize o título, o tema ou a descrição.</p></div>{!editing && <button className="button secondary" type="button" onClick={() => { setForm({ titulo: details.proposal.titulo, tema: details.proposal.tema, descricao: details.proposal.descricao }); setEditing(true); setError(''); setNotice('') }}>Editar aula</button>}</div>{editing && <form className="form-grid" onSubmit={event => void saveContent(event)}><p>O professor e os alunos inscritos serão notificados. A data e a sala permanecem as mesmas.</p><label>Título da aula<input required maxLength={200} value={form.titulo} onChange={e => setForm({ ...form, titulo: e.target.value })} /></label><label>Tema<input required maxLength={200} value={form.tema} onChange={e => setForm({ ...form, tema: e.target.value })} /></label><label>Descrição<textarea required maxLength={5000} rows={4} value={form.descricao} onChange={e => setForm({ ...form, descricao: e.target.value })} /></label><div className="proposal-actions"><button className="button primary" disabled={busy !== null}>{busy === 'edit' ? 'Salvando…' : 'Salvar alterações'}</button><button className="button secondary" type="button" disabled={busy !== null} onClick={() => setEditing(false)}>Cancelar edição</button></div></form>}</section>}
      <div className="supervision-grid"><section className="workspace-panel supervision-seats"><div className="panel-heading"><div><h2>Mapa de assentos</h2></div><span className="panel-mark" aria-hidden="true">↗</span></div><p>{details.map.capacidadeTotal - details.map.lugaresDisponiveis} ocupados · {details.map.lugaresDisponiveis} disponíveis · {details.map.capacidadeTotal} lugares</p><div className="seat-legend" aria-label="Legenda do mapa de assentos"><span><i className="seat-sample available" />Disponível</span><span><i className="seat-sample occupied" />Ocupado</span></div><div className="seat-front">Frente da sala</div><div className="seat-map" style={{ '--seat-columns': details.map.sala.tipo.toUpperCase() === 'NOTEBOOK' ? 5 : 6 } as CSSProperties} aria-label={`Mapa de assentos da sala ${details.map.sala.nome}`}>{details.map.assentos.map(seat => <span key={seat.id} className={`seat supervision-seat ${seat.status === 'OCUPADO' ? 'occupied' : 'available'}`} aria-label={`Assento ${seat.codigo}, ${seat.status === 'OCUPADO' ? 'ocupado' : 'disponível'}`}>{seat.codigo}</span>)}</div></section>
      <section className="workspace-panel supervision-participants"><div className="panel-heading"><div><h2>Participantes e presença</h2></div><span className="count-label">{activeParticipants.length} INSCRITO(S)</span></div><p>Registre a presença dos alunos inscritos.</p>{activeParticipants.length ? activeParticipants.map(person => <article className="supervision-person" key={person.alunoId}><div><h3>{person.nomeAluno}</h3><p>{person.assento ? `Assento ${person.assento.codigo}` : 'Assento não informado'} · {person.presente === null ? 'Presença não registrada' : person.presente ? 'Presente' : 'Ausente'}</p></div><div className="proposal-actions"><button className="button secondary small" disabled={busy !== null} aria-pressed={person.presente === true} onClick={() => void setPresence(person, true)}>Presente</button><button className="button secondary small" disabled={busy !== null} aria-pressed={person.presente === false} onClick={() => void setPresence(person, false)}>Ausente</button><button className="text-button" disabled={busy !== null || person.presente === null} onClick={() => void setPresence(person, null)}>Limpar</button></div></article>) : <div className="empty workspace-empty"><span aria-hidden="true">○</span><p>Nenhuma inscrição ativa ainda.</p></div>}</section></div>
    </>}
  </main></div>
}
