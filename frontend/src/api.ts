export interface Student {
  id: string
  name: string
  email: string
  course: string
}

export interface LoginResponse {
  accessToken: string
  tokenType: string
  expiresAtUtc: string
  student: Student
}

export interface Activity {
  id: string
  tipo: string
  tema: string
  descricao: string
  nivel: string
  dataHora: string
  local: string
  numeroDeVagas: number
  status: 'publicada' | 'cancelada'
}

export interface ActivityDetails extends Activity {
  nomeOrganizador: string
}

export interface Enrollment {
  id: string
  status: 'ativa' | 'cancelada'
  criadaEmUtc: string
  atualizadaEmUtc: string
  atividadeId: string
  tipo: string
  tema: string
  dataHora: string
  local: string
  statusAtividade: 'publicada' | 'cancelada'
}

export interface Participant {
  alunoId: string
  nomeAluno: string
  status: 'ativa' | 'cancelada'
  criadaEmUtc: string
  atualizadaEmUtc: string
}

export interface ActivityInput {
  tipo: string
  tema: string
  descricao: string
  nivel: string
  dataHora: string
  local: string
  numeroDeVagas: number
}

const apiBaseUrl = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '')

export class ApiError extends Error {
  constructor(readonly status: number, message: string) {
    super(message)
    this.name = 'ApiError'
  }
}

function responseMessage(status: number, payload: unknown): string {
  if (status === 0) return `Não foi possível conectar à API${apiBaseUrl ? ` em ${apiBaseUrl}` : ''}. Confirme que ela está em execução.`
  if (status === 400) {
    if (payload && typeof payload === 'object' && 'errors' in payload) {
      const errors = (payload as { errors?: Record<string, string[]> }).errors
      const messages = Object.values(errors || {}).flat()
      if (messages.length) return messages.join(' ')
    }
    return 'Os dados enviados são inválidos. Confira os campos e tente novamente.'
  }
  if (status === 401) return 'Sua sessão expirou ou você precisa entrar para continuar.'
  if (status === 403) return 'Você não tem permissão para realizar esta ação.'
  if (status === 404) return 'O item solicitado não foi encontrado.'
  if (status === 409) {
    if (payload && typeof payload === 'object' && 'message' in payload && typeof (payload as { message: unknown }).message === 'string') {
      return (payload as { message: string }).message
    }
    return 'Esta ação conflita com o estado atual. Atualize os dados e tente novamente.'
  }
  if (status >= 500) return 'O servidor encontrou um erro. Tente novamente em instantes.'
  if (payload && typeof payload === 'object') {
    const body = payload as { message?: unknown; errors?: Record<string, string[]> }
    const fieldMessages = Object.values(body.errors || {}).flat()
    if (fieldMessages.length) return fieldMessages.join(' ')
    if (typeof body.message === 'string') return body.message
  }
  return 'Não foi possível concluir a solicitação. Tente novamente.'
}

export async function apiRequest<T>(path: string, init: RequestInit = {}, token?: string | null): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  if (token) headers.set('Authorization', `Bearer ${token}`)

  let response: Response
  try {
    response = await fetch(`${apiBaseUrl}${path}`, { ...init, headers })
  } catch {
    throw new ApiError(0, responseMessage(0, null))
  }

  if (response.status === 204) return undefined as T
  const text = await response.text()
  const payload = text ? parseJson(text) : null
  if (!response.ok) throw new ApiError(response.status, responseMessage(response.status, payload))
  return payload as T
}

function parseJson(text: string): unknown {
  try {
    return JSON.parse(text) as unknown
  } catch {
    return text
  }
}
