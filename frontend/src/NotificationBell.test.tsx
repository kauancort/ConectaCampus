// @vitest-environment jsdom
import { afterEach, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { AuthProvider } from './auth'
import SiteHeader from './SiteHeader'

afterEach(() => { cleanup(); localStorage.clear(); vi.unstubAllGlobals() })

function setup() {
  const profile = { id: 'aluno', name: 'Ana', email: 'ana@example.test', course: 'DSM', professor: false }
  localStorage.setItem('ac-token', 'token')
  localStorage.setItem('ac-student', JSON.stringify(profile))
  let read = false
  const fetchMock = vi.fn((url: string, init?: RequestInit) => {
    if (url === '/api/auth/me') return Promise.resolve(Response.json(profile))
    if (url === '/api/notificacoes') return Promise.resolve(Response.json([{ id: 'n1', mensagem: 'Sua proposta foi aprovada.', lida: read, criadaEmUtc: '2032-01-15T12:00:00Z' }, { id: 'n2', mensagem: 'Aviso anterior.', lida: true }]))
    if (url === '/api/notificacoes/n1/lida' && init?.method === 'POST') { read = true; return Promise.resolve(new Response(null, { status: 204 })) }
    return Promise.reject(new Error(`Rota inesperada: ${url}`))
  })
  vi.stubGlobal('fetch', fetchMock)
  render(<MemoryRouter><AuthProvider><SiteHeader active="atividades" /></AuthProvider></MemoryRouter>)
  return fetchMock
}

it('mostra notificações recentes, marca uma como lida e atualiza o contador', async () => {
  const fetchMock = setup()
  const bell = await screen.findByRole('button', { name: 'Notificações, 1 não lida' })
  expect(bell.getAttribute('aria-expanded')).toBe('false')
  fireEvent.click(bell)
  const panel = screen.getByRole('region', { name: 'Notificações recentes' })
  expect(bell.getAttribute('aria-expanded')).toBe('true')
  expect(within(panel).getByText('Sua proposta foi aprovada.')).toBeTruthy()
  expect(within(panel).getByText('Aviso anterior.')).toBeTruthy()
  fireEvent.click(within(panel).getByRole('button', { name: 'Marcar como lida: Sua proposta foi aprovada.' }))
  await waitFor(() => expect(screen.getByRole('button', { name: 'Notificações' })).toBeTruthy())
  expect(fetchMock).toHaveBeenCalledWith('/api/notificacoes/n1/lida', expect.objectContaining({ method: 'POST' }))
  expect(within(panel).queryByRole('button', { name: 'Marcar como lida: Sua proposta foi aprovada.' })).toBeNull()
})

it('fecha ao pressionar Escape ou clicar fora e devolve o foco ao sino com Escape', async () => {
  setup()
  const bell = await screen.findByRole('button', { name: 'Notificações, 1 não lida' })
  fireEvent.click(bell); fireEvent.keyDown(document, { key: 'Escape' })
  expect(screen.queryByRole('region', { name: 'Notificações recentes' })).toBeNull()
  expect(document.activeElement).toBe(bell)
  fireEvent.click(bell); fireEvent.pointerDown(document.body)
  expect(screen.queryByRole('region', { name: 'Notificações recentes' })).toBeNull()
})

it('exibe erro de carregamento com opção de tentar novamente', async () => {
  const profile = { id: 'aluno', name: 'Ana', email: 'ana@example.test', course: 'DSM', professor: false }
  localStorage.setItem('ac-token', 'token'); localStorage.setItem('ac-student', JSON.stringify(profile))
  let offline = true
  vi.stubGlobal('fetch', vi.fn((url: string) => {
    if (url === '/api/auth/me') return Promise.resolve(Response.json(profile))
    return offline ? Promise.reject(new Error('offline')) : Promise.resolve(Response.json([]))
  }))
  render(<MemoryRouter><AuthProvider><SiteHeader active="atividades" /></AuthProvider></MemoryRouter>)
  fireEvent.click(screen.getByRole('button', { name: 'Notificações' }))
  expect(await screen.findByRole('alert')).toBeTruthy()
  expect(screen.queryByText('Nenhuma notificação ainda.')).toBeNull()
  offline = false
  fireEvent.click(screen.getByRole('button', { name: 'Tentar novamente' }))
  expect(await screen.findByText('Nenhuma notificação ainda.')).toBeTruthy()
})
