// @vitest-environment jsdom
import { afterEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import App from './App'
import { AuthProvider } from './auth'

const activity = {
  id: '6d7b5e0f-7e35-4a3b-9050-5f21a7aa1234',
  tema: 'Oficina de testes',
  descricao: 'Aprenda a validar aplicações.',
  modalidade: 'AULA',
  sala: { id: 'sala-desktop', nome: 'Laboratório Desktop', tipo: 'DESKTOP', capacidade: 24 },
  data: '2032-01-15',
  inicio: '2032-01-15T14:00:00Z',
  fim: '2032-01-15T16:00:00Z',
  status: 'publicada',
}

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

function renderApp() {
  return render(<MemoryRouter><AuthProvider><App /></AuthProvider></MemoryRouter>)
}

afterEach(() => {
  cleanup()
  localStorage.clear()
  vi.unstubAllGlobals()
})

describe('jornadas da área do aluno', () => {
  it('pesquisa atividades por tema e preserva a navegação para a área do professor', async () => {
    const fetchMock = vi.fn((url: string) => {
      if (url === '/api/atividades') return Promise.resolve(json([]))
      if (url === '/api/atividades?tema=testes') return Promise.resolve(json([activity]))
      return Promise.reject(new Error(`Rota inesperada: ${url}`))
    })
    vi.stubGlobal('fetch', fetchMock)

    renderApp()
    fireEvent.change(await screen.findByLabelText('Pesquisar por tema'), { target: { value: 'testes' } })
    fireEvent.click(screen.getByRole('button', { name: 'Buscar' }))

    expect(await screen.findByText('Oficina de testes')).toBeTruthy()
    expect(screen.getByRole('link', { name: 'Área do professor' }).getAttribute('href')).toBe('/professor/atividades')
  })

  it('permite criar conta, entrar e sair sem deixar a sessão armazenada', async () => {
    const fetchMock = vi.fn((url: string, init?: RequestInit) => {
      if (url === '/api/atividades') return Promise.resolve(json([]))
      if (url === '/api/auth/register' && init?.method === 'POST') return Promise.resolve(json({ id: 'aluno-1' }, 201))
      if (url === '/api/auth/login' && init?.method === 'POST') return Promise.resolve(json({
        accessToken: 'novo-token', tokenType: 'Bearer', expiresAtUtc: '2032-01-01T00:00:00Z',
        student: { id: 'aluno-1', name: 'Ada', email: 'ada@example.test', course: 'DSM' },
      }))
      if (url === '/api/inscricoes/minhas') return Promise.resolve(json([]))
      return Promise.reject(new Error(`Rota inesperada: ${url}`))
    })
    vi.stubGlobal('fetch', fetchMock)

    renderApp()
    fireEvent.click(await screen.findByRole('button', { name: 'Entrar' }))
    fireEvent.click(screen.getByRole('button', { name: 'Criar cadastro' }))
    fireEvent.change(screen.getByLabelText('Nome'), { target: { value: 'Ada' } })
    fireEvent.change(screen.getByLabelText('Curso'), { target: { value: 'DSM' } })
    fireEvent.change(screen.getByLabelText('E-mail'), { target: { value: 'ada@example.test' } })
    fireEvent.change(screen.getByLabelText('Senha'), { target: { value: 'Fatec!Teste2026' } })
    fireEvent.click(screen.getByRole('button', { name: 'Criar conta' }))
    expect(await screen.findByText('Cadastro realizado. Entre com seu e-mail e senha.')).toBeTruthy()

    fireEvent.change(screen.getByLabelText('Senha'), { target: { value: 'Fatec!Teste2026' } })
    fireEvent.click(screen.getByText('Entrar', { selector: 'button[type="submit"]' }))
    expect(await screen.findByText('Bem-vindo(a), Ada.')).toBeTruthy()
    expect(localStorage.getItem('ac-token')).toBe('novo-token')
    fireEvent.click(screen.getByRole('button', { name: 'Sair' }))
    expect(screen.getByRole('button', { name: 'Entrar' })).toBeTruthy()
    expect(localStorage.getItem('ac-token')).toBeNull()
  })

  it('exibe apenas atividades publicadas e permite consultar seus detalhes', async () => {
    const fetchMock = vi.fn((url: string) => {
      if (url === '/api/atividades') return Promise.resolve(json([activity, { ...activity, id: 'cancelada', tema: 'Não deve aparecer', status: 'cancelada' }]))
      if (url === `/api/atividades/${activity.id}`) return Promise.resolve(json(activity))
      return Promise.reject(new Error(`Rota inesperada: ${url}`))
    })
    vi.stubGlobal('fetch', fetchMock)

    renderApp()

    expect(await screen.findByText('Oficina de testes')).toBeTruthy()
    expect(screen.queryByText('Não deve aparecer')).toBeNull()
    fireEvent.click(screen.getByRole('button', { name: 'Ver detalhes' }))

    expect(await screen.findByRole('button', { name: 'Escolher assento' })).toBeTruthy()
    expect(fetchMock).toHaveBeenCalledWith(`/api/atividades/${activity.id}`, expect.anything())
  })

  it('permite cancelar uma inscrição ativa e atualiza a lista', async () => {
    localStorage.setItem('ac-token', 'token-de-teste')
    localStorage.setItem('ac-student', JSON.stringify({ id: 'aluno-1', name: 'Ada', email: 'ada@example.test', course: 'DSM' }))
    const enrollment = {
      id: 'inscricao-1', status: 'ativa', criadaEmUtc: '2032-01-01T10:00:00Z', atualizadaEmUtc: '2032-01-01T10:00:00Z',
      atividadeId: activity.id, tipo: 'aula', tema: activity.tema, dataHora: activity.inicio, local: activity.sala.nome,
      statusAtividade: 'publicada', sala: activity.sala, assento: { id: 'a1', codigo: 'A1', fileira: 'A', coluna: 1 }, inicio: activity.inicio, fim: activity.fim,
    }
    let deleted = false
    const fetchMock = vi.fn((url: string, init?: RequestInit) => {
      if (url === '/api/atividades') return Promise.resolve(json([]))
      if (url === '/api/inscricoes/minhas') return Promise.resolve(json(deleted ? [] : [enrollment]))
      if (url === `/api/atividades/${activity.id}/inscricoes/me` && init?.method === 'DELETE') {
        deleted = true
        return Promise.resolve(new Response(null, { status: 204 }))
      }
      return Promise.reject(new Error(`Rota inesperada: ${url}`))
    })
    vi.stubGlobal('fetch', fetchMock)

    renderApp()
    fireEvent.click(await screen.findByRole('button', { name: 'Minhas inscrições' }))
    fireEvent.click(await screen.findByRole('button', { name: 'Cancelar inscrição' }))

    expect(await screen.findByText(`Inscrição em “${activity.tema}” cancelada.`)).toBeTruthy()
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(
      `/api/atividades/${activity.id}/inscricoes/me`,
      expect.objectContaining({ method: 'DELETE' }),
    ))
    expect(screen.getByText('Você ainda não possui inscrições.')).toBeTruthy()
  })

  it('carrega o mapa, reserva o assento escolhido e atualiza a disponibilidade', async () => {
    localStorage.setItem('ac-token', 'token-de-teste')
    localStorage.setItem('ac-student', JSON.stringify({ id: 'aluno-1', name: 'Ada', email: 'ada@example.test', course: 'DSM' }))
    let reserved = false
    const seatMap = () => ({
      atividadeId: activity.id, tema: activity.tema, data: activity.data, inicio: activity.inicio, fim: activity.fim,
      sala: activity.sala, capacidadeTotal: 24, lugaresDisponiveis: reserved ? 23 : 24,
      assentos: [{ id: 'a1', codigo: 'A1', fileira: 'A', coluna: 1, status: reserved ? 'OCUPADO' : 'DISPONIVEL' }],
    })
    const fetchMock = vi.fn((url: string, init?: RequestInit) => {
      if (url === '/api/atividades') return Promise.resolve(json([activity]))
      if (url === '/api/inscricoes/minhas') return Promise.resolve(json([]))
      if (url === `/api/atividades/${activity.id}`) return Promise.resolve(json(activity))
      if (url === `/api/atividades/${activity.id}/assentos`) return Promise.resolve(json(seatMap()))
      if (url === `/api/atividades/${activity.id}/inscricoes` && init?.method === 'POST') {
        reserved = true
        return Promise.resolve(json({ id: 'inscricao-1' }, 201))
      }
      return Promise.reject(new Error(`Rota inesperada: ${url}`))
    })
    vi.stubGlobal('fetch', fetchMock)

    renderApp()
    fireEvent.click(await screen.findByRole('button', { name: 'Ver detalhes' }))
    fireEvent.click(await screen.findByRole('button', { name: 'Escolher assento' }))
    fireEvent.click(await screen.findByRole('button', { name: 'Assento A1, disponível' }))
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar inscrição' }))

    expect(await screen.findByText('Inscrição confirmada no assento A1.')).toBeTruthy()
    expect(fetchMock).toHaveBeenCalledWith(`/api/atividades/${activity.id}/inscricoes`, expect.objectContaining({ method: 'POST' }))
    expect(await screen.findByText('23')).toBeTruthy()
  })
})
