const apiBaseUrl = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '')

export class ProfessorApiError extends Error {
  constructor(
    readonly status: number,
    readonly payload: unknown,
    message: string,
  ) {
    super(message)
    this.name = 'ProfessorApiError'
  }
}

function collectMessages(payload: unknown): string[] {
  if (typeof payload === 'string' && payload.trim()) return [payload.trim()]
  if (!payload || typeof payload !== 'object') return []

  const body = payload as {
    detail?: unknown
    message?: unknown
    title?: unknown
    errors?: Record<string, unknown>
  }
  const messages: string[] = []

  if (body.errors && typeof body.errors === 'object') {
    Object.values(body.errors).forEach(value => {
      if (Array.isArray(value)) {
        value.forEach(item => { if (typeof item === 'string') messages.push(item) })
      } else if (typeof value === 'string') {
        messages.push(value)
      }
    })
  }

  if (typeof body.message === 'string') messages.push(body.message)
  if (typeof body.detail === 'string') messages.push(body.detail)
  if (messages.length === 0 && typeof body.title === 'string') messages.push(body.title)

  return [...new Set(messages)]
}

function fallbackMessage(status: number) {
  if (status === 0) return 'Não foi possível conectar à API. Verifique sua conexão e tente novamente.'
  if (status === 401) return 'Sua sessão expirou ou você precisa entrar para continuar.'
  if (status === 403) return 'Você não tem permissão para realizar esta ação.'
  if (status === 404) return 'O serviço solicitado não foi encontrado.'
  if (status === 409) return 'Não há sala disponível para a data e o horário informados.'
  if (status >= 500) return 'O servidor encontrou um erro. Tente novamente em instantes.'
  return 'Não foi possível concluir a solicitação. Confira os dados e tente novamente.'
}

function parsePayload(text: string): unknown {
  if (!text) return null
  try {
    return JSON.parse(text) as unknown
  } catch {
    return text
  }
}

export async function professorApiRequest<T>(path: string, init: RequestInit, token: string | null): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')
  if (init.body) headers.set('Content-Type', 'application/json')
  if (token) headers.set('Authorization', `Bearer ${token}`)

  let response: Response
  try {
    response = await fetch(`${apiBaseUrl}${path}`, { ...init, headers })
  } catch {
    throw new ProfessorApiError(0, null, fallbackMessage(0))
  }

  const payload = parsePayload(await response.text())
  if (!response.ok) {
    const messages = collectMessages(payload)
    throw new ProfessorApiError(response.status, payload, messages.join(' ') || fallbackMessage(response.status))
  }

  return payload as T
}
