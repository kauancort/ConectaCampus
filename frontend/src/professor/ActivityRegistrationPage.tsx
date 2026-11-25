import SiteFooter from '../SiteFooter'
import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../auth'
import ProfessorHeader from './ProfessorHeader'
import { ProfessorApiError, professorApiRequest } from './professor-api'
import './professor.css'

type Modality = 'Aula' | 'Palestra'

interface FormState {
  tema: string
  descricao: string
  modalidade: Modality
  tipoSala: string
  data: string
  horarioInicio: string
  horarioTermino: string
}

interface RoomType {
  value: string
  label: string
  capacity: number
}

type RoomTypeResponse = unknown[] | { tipos?: unknown[]; data?: unknown[] }

interface CreatedActivityResponse {
  sala?: unknown
  salaId?: unknown
  salaNome?: unknown
  identificacaoSala?: unknown
  inicio?: unknown
  fim?: unknown
  data?: unknown
}

type FormErrors = Partial<Record<keyof FormState, string>>

const ROOM_CAPACITY: Record<string, number> = { DESKTOP: 24, NOTEBOOK: 20 }
const emptyForm: FormState = {
  tema: '',
  descricao: '',
  modalidade: 'Aula',
  tipoSala: '',
  data: '',
  horarioInicio: '',
  horarioTermino: '',
}

function localToday() {
  const now = new Date()
  const offset = now.getTimezoneOffset() * 60_000
  return new Date(now.getTime() - offset).toISOString().slice(0, 10)
}

function roomTypeName(item: unknown): string | null {
  if (typeof item === 'string') return item.trim() || null
  if (!item || typeof item !== 'object') return null
  const room = item as Record<string, unknown>
  const value = room.tipo ?? room.nome ?? room.codigo ?? room.descricao ?? room.id
  return typeof value === 'string' && value.trim() ? value.trim() : null
}

function normalizeRoomTypes(response: RoomTypeResponse): RoomType[] {
  const source = Array.isArray(response)
    ? response
    : Array.isArray(response?.tipos)
      ? response.tipos
      : Array.isArray(response?.data)
        ? response.data
        : []

  const seen = new Set<string>()
  return source.flatMap(item => {
    const value = roomTypeName(item)
    if (!value) return []
    const key = value.toUpperCase()
    const capacity = ROOM_CAPACITY[key]
    if (!capacity || seen.has(key)) return []
    seen.add(key)
    return [{ value, label: key, capacity }]
  })
}

function validate(form: FormState): FormErrors {
  const errors: FormErrors = {}
  if (!form.tema.trim()) errors.tema = 'Informe o tema da atividade.'
  if (!form.descricao.trim()) errors.descricao = 'Informe a descrição da atividade.'
  if (!form.modalidade) errors.modalidade = 'Selecione a modalidade.'
  if (!form.tipoSala) errors.tipoSala = 'Selecione o tipo de sala.'
  if (!form.data) errors.data = 'Informe a data.'
  if (!form.horarioInicio) errors.horarioInicio = 'Informe o horário de início.'
  if (!form.horarioTermino) errors.horarioTermino = 'Informe o horário de término.'

  if (form.data && form.horarioInicio) {
    const start = new Date(`${form.data}T${form.horarioInicio}:00`)
    if (Number.isNaN(start.getTime())) errors.horarioInicio = 'Informe uma data e um horário válidos.'
    else if (start.getTime() <= Date.now()) errors.data = 'A atividade deve ser agendada para uma data e horário futuros.'
  }

  if (form.data && form.horarioInicio && form.horarioTermino) {
    const start = new Date(`${form.data}T${form.horarioInicio}:00`)
    const end = new Date(`${form.data}T${form.horarioTermino}:00`)
    if (!Number.isNaN(start.getTime()) && !Number.isNaN(end.getTime()) && end <= start) {
      errors.horarioTermino = 'O horário de término deve ser posterior ao horário de início.'
    }
  }
  return errors
}

function displayValue(value: unknown): string | null {
  if (typeof value === 'number' && Number.isFinite(value)) return String(value)
  return typeof value === 'string' && value.trim() ? value.trim() : null
}

function roomIdentification(response: CreatedActivityResponse): string | null {
  const direct = displayValue(response.identificacaoSala) ?? displayValue(response.salaNome) ?? displayValue(response.salaId)
  if (direct) return direct
  if (typeof response.sala === 'string') return displayValue(response.sala)
  if (!response.sala || typeof response.sala !== 'object') return null
  const room = response.sala as Record<string, unknown>
  return displayValue(room.identificacao) ?? displayValue(room.nome) ?? displayValue(room.numero) ?? displayValue(room.id)
}

function timeLabel(value: unknown, fallback: string) {
  const raw = displayValue(value)
  if (!raw) return fallback
  const time = raw.match(/(?:T|^)(\d{2}:\d{2})/)
  return time?.[1] ?? fallback
}

function confirmedTime(response: CreatedActivityResponse, form: FormState) {
  const start = timeLabel(response.inicio, form.horarioInicio)
  const end = timeLabel(response.fim, form.horarioTermino)
  return `${start}–${end}`
}

function toIsoDateTime(data: string, horario: string) {
  return new Date(`${data}T${horario}:00`).toISOString()
}

function isRoomUnavailable(error: unknown) {
  return error instanceof ProfessorApiError && (
    error.status === 409 || /(?:n[aã]o (?:h[aá]|existe)|sem).{0,30}sala|sala.{0,30}indispon/i.test(error.message)
  )
}

export default function ActivityRegistrationPage() {
  const { token } = useAuth()
  const navigate = useNavigate()
  const [form, setForm] = useState<FormState>(emptyForm)
  const [errors, setErrors] = useState<FormErrors>({})
  const [roomTypes, setRoomTypes] = useState<RoomType[]>([])
  const [loadingRooms, setLoadingRooms] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [feedback, setFeedback] = useState<{ kind: 'error' | 'success'; message: string } | null>(null)

  const selectedRoom = useMemo(
    () => roomTypes.find(room => room.value === form.tipoSala),
    [roomTypes, form.tipoSala],
  )

  useEffect(() => {
    const controller = new AbortController()
    async function loadRoomTypes() {
      setLoadingRooms(true)
      try {
        const response = await professorApiRequest<RoomTypeResponse>(
          '/api/salas/tipos',
          { method: 'GET', signal: controller.signal },
          token,
        )
        const availableTypes = normalizeRoomTypes(response)
        setRoomTypes(availableTypes)
        if (availableTypes.length === 0) {
          setFeedback({ kind: 'error', message: 'Não há tipos de sala disponíveis para cadastro no momento.' })
        }
      } catch (error) {
        if (controller.signal.aborted) return
        setFeedback({
          kind: 'error',
          message: error instanceof Error ? error.message : 'Não foi possível carregar os tipos de sala.',
        })
      } finally {
        if (!controller.signal.aborted) setLoadingRooms(false)
      }
    }
    void loadRoomTypes()
    return () => controller.abort()
  }, [token])

  function updateField<K extends keyof FormState>(field: K, value: FormState[K]) {
    setForm(current => ({ ...current, [field]: value }))
    setErrors(current => ({ ...current, [field]: undefined }))
    if (feedback?.kind === 'error') setFeedback(null)
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const validationErrors = validate(form)
    setErrors(validationErrors)
    setFeedback(null)
    if (Object.keys(validationErrors).length > 0) return

    setSubmitting(true)
    try {
      const response = await professorApiRequest<CreatedActivityResponse>(
        '/api/atividades',
        {
          method: 'POST',
          body: JSON.stringify({
            tema: form.tema.trim(),
            descricao: form.descricao.trim(),
            modalidade: form.modalidade,
            tipoSala: form.tipoSala,
            inicio: toIsoDateTime(form.data, form.horarioInicio),
            fim: toIsoDateTime(form.data, form.horarioTermino),
          }),
        },
        token,
      )

      const room = roomIdentification(response)
      const schedule = confirmedTime(response, form)
      const roomLabel = room && /^sala\b/i.test(room) ? room : room ? `Sala ${room}` : null
      const successMessage = roomLabel
        ? `Atividade cadastrada com sucesso. ${roomLabel}, horário confirmado: ${schedule}.`
        : `Atividade cadastrada com sucesso. Horário confirmado: ${schedule}.`
      setForm(emptyForm)
      setErrors({})
      navigate('/professor/atividades', { state: { createdMessage: successMessage } })
    } catch (error) {
      setFeedback({
        kind: 'error',
        message: isRoomUnavailable(error)
          ? 'Não há sala disponível para a data, o horário e o tipo selecionados.'
          : error instanceof Error
            ? error.message
            : 'Não foi possível cadastrar a atividade.',
      })
    } finally {
      setSubmitting(false)
    }
  }

  return <div className="app-shell professor-registration">
    <ProfessorHeader />

    <main className="main-content professor-content">
      <div className="heading">
        <div>

          <h1>Cadastrar atividade</h1>
          <p className="subtitle">Informe os dados para reservar uma sala e publicar a atividade.</p>
        </div>
      </div>

      {feedback && <div className={`feedback ${feedback.kind}`} role={feedback.kind === 'error' ? 'alert' : 'status'}>{feedback.message}</div>}

      <section className="panel registration-panel" aria-labelledby="activity-form-title">
        <div className="form-intro">
          <h2 id="activity-form-title">Dados da atividade</h2>
          <p>Todos os campos são obrigatórios.</p>
        </div>

        <form className="form-grid two-col" onSubmit={submit} noValidate>
          <label className="span-two">Tema da atividade
            <input
              value={form.tema}
              onChange={event => updateField('tema', event.target.value)}
              maxLength={200}
              aria-invalid={Boolean(errors.tema)}
              aria-describedby={errors.tema ? 'tema-error' : undefined}
              disabled={submitting}
            />
            {errors.tema && <span className="field-error" id="tema-error">{errors.tema}</span>}
          </label>

          <label className="span-two">Descrição
            <textarea
              value={form.descricao}
              onChange={event => updateField('descricao', event.target.value)}
              maxLength={5000}
              rows={5}
              aria-invalid={Boolean(errors.descricao)}
              aria-describedby={errors.descricao ? 'descricao-error' : undefined}
              disabled={submitting}
            />
            {errors.descricao && <span className="field-error" id="descricao-error">{errors.descricao}</span>}
          </label>

          <label>Modalidade
            <select
              value={form.modalidade}
              onChange={event => updateField('modalidade', event.target.value as Modality)}
              aria-invalid={Boolean(errors.modalidade)}
              disabled={submitting}
            >
              <option value="Aula">Aula</option>
              <option value="Palestra">Palestra</option>
            </select>
            {errors.modalidade && <span className="field-error">{errors.modalidade}</span>}
          </label>

          <label>Tipo de sala
            <select
              value={form.tipoSala}
              onChange={event => updateField('tipoSala', event.target.value)}
              aria-invalid={Boolean(errors.tipoSala)}
              aria-describedby={errors.tipoSala ? 'tipo-sala-error' : 'room-capacity'}
              disabled={loadingRooms || submitting || roomTypes.length === 0}
            >
              <option value="">{loadingRooms ? 'Carregando tipos…' : 'Selecione'}</option>
              {roomTypes.map(room => <option key={room.label} value={room.value}>{room.label}</option>)}
            </select>
            {errors.tipoSala && <span className="field-error" id="tipo-sala-error">{errors.tipoSala}</span>}
            <span className="room-capacity" id="room-capacity" aria-live="polite">
              {selectedRoom ? `${selectedRoom.label} — ${selectedRoom.capacity} lugares.` : 'DESKTOP — 24 lugares. NOTEBOOK — 20 lugares.'}
            </span>
          </label>

          <label>Data
            <input
              type="date"
              value={form.data}
              min={localToday()}
              onChange={event => updateField('data', event.target.value)}
              aria-invalid={Boolean(errors.data)}
              aria-describedby={errors.data ? 'data-error' : undefined}
              disabled={submitting}
            />
            {errors.data && <span className="field-error" id="data-error">{errors.data}</span>}
          </label>

          <div className="time-fields">
            <label>Horário de início
              <input
                type="time"
                value={form.horarioInicio}
                onChange={event => updateField('horarioInicio', event.target.value)}
                aria-invalid={Boolean(errors.horarioInicio)}
                disabled={submitting}
              />
              {errors.horarioInicio && <span className="field-error">{errors.horarioInicio}</span>}
            </label>
            <label>Horário de término
              <input
                type="time"
                value={form.horarioTermino}
                min={form.horarioInicio || undefined}
                onChange={event => updateField('horarioTermino', event.target.value)}
                aria-invalid={Boolean(errors.horarioTermino)}
                disabled={submitting}
              />
              {errors.horarioTermino && <span className="field-error">{errors.horarioTermino}</span>}
            </label>
          </div>

          <div className="form-actions span-two">
            <button className="button primary submit-button" type="submit" disabled={submitting || loadingRooms || roomTypes.length === 0}>
              {submitting ? <><span className="spinner" aria-hidden="true" /> Cadastrando…</> : 'Cadastrar atividade'}
            </button>
          </div>
        </form>
      </section>
    </main>

    <SiteFooter />
  </div>
}
