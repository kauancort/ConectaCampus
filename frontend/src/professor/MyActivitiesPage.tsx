import { useCallback, useEffect, useMemo, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth'
import ProfessorHeader from './ProfessorHeader'
import { professorApiRequest } from './professor-api'
import './professor.css'

type SortDirection = 'asc' | 'desc'
type ApiRecord = Record<string, unknown>

interface ActivityView {
  id: string
  theme: string
  description: string | null
  modality: string | null
  date: string | null
  startTime: string | null
  endTime: string | null
  room: string | null
  roomType: string | null
  capacity: number | null
  sortTimestamp: number
}

interface NavigationState {
  createdMessage?: string
}

function asRecord(value: unknown): ApiRecord | null {
  return value && typeof value === 'object' && !Array.isArray(value) ? value as ApiRecord : null
}

function textValue(...values: unknown[]): string | null {
  for (const value of values) {
    if (typeof value === 'string' && value.trim()) return value.trim()
    if (typeof value === 'number' && Number.isFinite(value)) return String(value)
  }
  return null
}

function numberValue(...values: unknown[]): number | null {
  for (const value of values) {
    if (typeof value === 'number' && Number.isFinite(value)) return value
    if (typeof value === 'string' && value.trim() && Number.isFinite(Number(value))) return Number(value)
  }
  return null
}

function timeFrom(value: unknown): string | null {
  const raw = textValue(value)
  if (!raw) return null
  return raw.match(/(?:T|^)(\d{2}:\d{2})/)?.[1] ?? null
}

function dateFrom(value: unknown): string | null {
  const raw = textValue(value)
  if (!raw) return null
  return raw.match(/^(\d{4}-\d{2}-\d{2})/)?.[1] ?? null
}

function titleCase(value: string | null) {
  return value ? value.charAt(0).toUpperCase() + value.slice(1).toLowerCase() : null
}

function normalizeActivity(value: unknown): ActivityView | null {
  const activity = asRecord(value)
  if (!activity) return null
  const room = asRecord(activity.sala)
  const roomType = asRecord(activity.tipoSala) ?? asRecord(room?.tipoSala) ?? asRecord(room?.tipo)
  const startSource = activity.inicio ?? activity.horarioInicio ?? activity.horaInicio ?? activity.dataHoraInicio ?? activity.dataHora
  const endSource = activity.fim ?? activity.horarioTermino ?? activity.horaTermino ?? activity.dataHoraTermino
  const date = dateFrom(activity.data ?? activity.dataAtividade ?? activity.inicio ?? activity.dataHoraInicio ?? activity.dataHora)
  const startTime = timeFrom(startSource)
  const id = textValue(activity.id, activity.atividadeId)
  if (!id) return null

  const timestamp = date
    ? new Date(`${date}T${startTime ?? '00:00'}:00`).getTime()
    : Number.POSITIVE_INFINITY

  return {
    id,
    theme: textValue(activity.tema, activity.titulo) ?? 'Atividade sem tema',
    description: textValue(activity.descricao),
    modality: titleCase(textValue(activity.modalidade, activity.tipo)),
    date,
    startTime,
    endTime: timeFrom(endSource),
    room: textValue(
      activity.identificacaoSala,
      activity.salaNome,
      activity.salaNumero,
      room?.identificacao,
      room?.nome,
      room?.numero,
      activity.local,
    ),
    roomType: textValue(
      typeof activity.tipoSala === 'string' ? activity.tipoSala : null,
      roomType?.nome,
      roomType?.tipo,
      roomType?.codigo,
      typeof room?.tipo === 'string' ? room.tipo : null,
    )?.toUpperCase() ?? null,
    capacity: numberValue(activity.capacidadeTotal, activity.capacidade, room?.capacidadeTotal, room?.capacidade),
    sortTimestamp: Number.isNaN(timestamp) ? Number.POSITIVE_INFINITY : timestamp,
  }
}

function listFromResponse(response: unknown): unknown[] {
  if (Array.isArray(response)) return response
  const body = asRecord(response)
  const list = body?.atividades ?? body?.items ?? body?.data
  return Array.isArray(list) ? list : []
}

function detailFromResponse(response: unknown): unknown {
  const body = asRecord(response)
  const wrappedActivity = asRecord(body?.atividade)
  const wrappedData = asRecord(body?.data)
  return wrappedActivity ?? wrappedData ?? response
}

function dateLabel(value: string | null) {
  if (!value) return 'Não informada'
  const [year, month, day] = value.split('-')
  return `${day}/${month}/${year}`
}

function timeRange(activity: ActivityView) {
  if (activity.startTime && activity.endTime) return `${activity.startTime}–${activity.endTime}`
  return activity.startTime ?? activity.endTime ?? 'Não informado'
}

function InfoItem({ label, value }: { label: string; value: string }) {
  return <span><b>{label}</b>{value}</span>
}

export default function MyActivitiesPage() {
  const { token } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const navigationState = location.state as NavigationState | null
  const [activities, setActivities] = useState<ActivityView[]>([])
  const [selected, setSelected] = useState<ActivityView | null>(null)
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc')
  const [loading, setLoading] = useState(true)
  const [loadingDetailId, setLoadingDetailId] = useState<string | null>(null)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState(navigationState?.createdMessage ?? '')

  const loadActivities = useCallback(async (signal?: AbortSignal) => {
    setLoading(true)
    setError('')
    try {
      const response = await professorApiRequest<unknown>(
        '/api/atividades/minhas',
        { method: 'GET', signal },
        token,
      )
      setActivities(listFromResponse(response).flatMap(item => {
        const activity = normalizeActivity(item)
        return activity ? [activity] : []
      }))
    } catch (requestError) {
      if (signal?.aborted) return
      setError(requestError instanceof Error ? requestError.message : 'Não foi possível carregar suas atividades.')
    } finally {
      if (!signal?.aborted) setLoading(false)
    }
  }, [token])

  useEffect(() => {
    const controller = new AbortController()
    void loadActivities(controller.signal)
    if (navigationState?.createdMessage) {
      navigate(location.pathname, { replace: true, state: null })
    }
    return () => controller.abort()
  }, [loadActivities, location.pathname, navigate, navigationState?.createdMessage])

  const orderedActivities = useMemo(() => [...activities].sort((first, second) => {
    const comparison = first.sortTimestamp - second.sortTimestamp
    return sortDirection === 'asc' ? comparison : -comparison
  }), [activities, sortDirection])

  async function showDetails(id: string) {
    setLoadingDetailId(id)
    setError('')
    setNotice('')
    try {
      const response = await professorApiRequest<unknown>(
        `/api/atividades/${encodeURIComponent(id)}`,
        { method: 'GET' },
        token,
      )
      const activity = normalizeActivity(detailFromResponse(response))
      if (!activity) throw new Error('A API retornou dados inválidos para esta atividade.')
      setSelected(activity)
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Não foi possível carregar os detalhes da atividade.')
    } finally {
      setLoadingDetailId(null)
    }
  }

  return <div className="app-shell professor-registration professor-activities">
    <ProfessorHeader />

    <main className="main-content activities-content">
      <div className="heading activities-heading">
        <div>
          <p className="eyebrow">ATIVIDADES ACADÊMICAS</p>
          <h1>Minhas atividades</h1>
          <p className="subtitle">Consulte as atividades cadastradas e os detalhes das salas reservadas.</p>
        </div>
        <div className="professor-heading-actions">
          <button className="button primary" type="button" onClick={() => navigate('/professor/atividades/nova')}>Lançar aula</button>
          {!selected && <label className="sort-control">Ordenar por data
          <select value={sortDirection} onChange={event => setSortDirection(event.target.value as SortDirection)}>
            <option value="asc">Mais próximas primeiro</option>
            <option value="desc">Mais distantes primeiro</option>
          </select>
          </label>}
        </div>
      </div>

      {error && <div className="feedback error" role="alert">{error}</div>}
      {notice && <div className="feedback success" role="status">{notice}</div>}

      {selected ? <section className="panel activity-detail" aria-labelledby="activity-detail-title">
        <button className="text-button back-button" type="button" onClick={() => setSelected(null)}>← Voltar para minhas atividades</button>
        <div className="activity-detail-title">
          <span className="tag">{selected.modality ?? 'Modalidade não informada'}</span>
          <h2 id="activity-detail-title">{selected.theme}</h2>
        </div>
        {selected.description && <p className="activity-description">{selected.description}</p>}
        <div className="detail-grid professor-detail-grid">
          <InfoItem label="Data" value={dateLabel(selected.date)} />
          <InfoItem label="Horário" value={timeRange(selected)} />
          <InfoItem label="Sala" value={selected.room ?? 'Não informada'} />
          <InfoItem label="Tipo de sala" value={selected.roomType ?? 'Não informado'} />
          <InfoItem label="Capacidade total" value={selected.capacity === null ? 'Não informada' : `${selected.capacity} lugares`} />
        </div>
      </section> : <>
        {loading && <div className="panel activities-loading" role="status"><span className="spinner dark" aria-hidden="true" /> Carregando atividades…</div>}

        {!loading && activities.length === 0 && !error && <div className="empty activities-empty">
          <h2>Nenhuma atividade cadastrada</h2>
          <p>Quando você cadastrar uma atividade, ela aparecerá aqui.</p>
          <button className="button primary" type="button" onClick={() => navigate('/professor/atividades/nova')}>Cadastrar atividade</button>
        </div>}

        {!loading && orderedActivities.length > 0 && <div className="cards professor-activity-cards">
          {orderedActivities.map(activity => <article className="card professor-activity-card" key={activity.id}>
            <div className="card-top">
              <span className="tag">{activity.modality ?? 'Não informada'}</span>
              <span className="activity-date">{dateLabel(activity.date)}</span>
            </div>
            <h2>{activity.theme}</h2>
            <div className="activity-card-info">
              <InfoItem label="Horário" value={timeRange(activity)} />
              <InfoItem label="Sala" value={activity.room ?? 'Não informada'} />
              <InfoItem label="Tipo" value={activity.roomType ?? 'Não informado'} />
              <InfoItem label="Capacidade" value={activity.capacity === null ? 'Não informada' : `${activity.capacity} lugares`} />
            </div>
            <div className="card-bottom">
              <button className="text-button details-button" type="button" disabled={loadingDetailId !== null} onClick={() => void showDetails(activity.id)}>
                {loadingDetailId === activity.id ? 'Carregando…' : 'Ver detalhes'}
              </button>
            </div>
          </article>)}
        </div>}
      </>}
    </main>

    <footer>ConectaCampus · Fatec Shunji Nishimura de Pompeia</footer>
  </div>
}
