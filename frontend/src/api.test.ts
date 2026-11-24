import { afterEach, describe, expect, it, vi } from 'vitest'
import { apiRequest } from './api'

const fetchMock = vi.fn()

describe('apiRequest', () => {
  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  it('envia JSON e o token de acesso quando presentes', async () => {
    fetchMock.mockResolvedValue(new Response(JSON.stringify({ id: 'atividade-1' }), {
      status: 201,
      headers: { 'Content-Type': 'application/json' },
    }))
    vi.stubGlobal('fetch', fetchMock)

    await expect(apiRequest<{ id: string }>('/api/atividades', {
      method: 'POST',
      body: JSON.stringify({ tema: 'Teste' }),
    }, 'token-de-teste')).resolves.toEqual({ id: 'atividade-1' })

    expect(fetchMock).toHaveBeenCalledWith('/api/atividades', expect.objectContaining({
      method: 'POST',
      headers: expect.any(Headers),
    }))
    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit]
    const headers = init.headers as Headers
    expect(headers.get('Content-Type')).toBe('application/json')
    expect(headers.get('Authorization')).toBe('Bearer token-de-teste')
  })

  it('converte erros de validação da API em uma mensagem utilizável', async () => {
    fetchMock.mockResolvedValue(new Response(JSON.stringify({
      errors: { tema: ['O tema é obrigatório.'], inicio: ['Use um horário válido.'] },
    }), { status: 400, headers: { 'Content-Type': 'application/json' } }))
    vi.stubGlobal('fetch', fetchMock)

    await expect(apiRequest('/api/atividades')).rejects.toMatchObject({
      name: 'ApiError',
      status: 400,
      message: 'O tema é obrigatório. Use um horário válido.',
    })
  })

  it('representa respostas sem conteúdo como undefined', async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetchMock)

    await expect(apiRequest<void>('/api/atividades/id/inscricoes/me', {
      method: 'DELETE',
    })).resolves.toBeUndefined()
  })

  it('informa uma falha de rede sem expor detalhes técnicos', async () => {
    fetchMock.mockRejectedValue(new TypeError('network failed'))
    vi.stubGlobal('fetch', fetchMock)

    await expect(apiRequest('/api/atividades')).rejects.toMatchObject({
      status: 0,
      message: 'Não foi possível conectar à API. Confirme que ela está em execução.',
    })
  })
})
