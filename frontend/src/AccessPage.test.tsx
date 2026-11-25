// @vitest-environment jsdom
import { afterEach, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { AuthProvider } from './auth'
import AccessPage from './AccessPage'

afterEach(() => { cleanup(); localStorage.clear(); vi.unstubAllGlobals() })
function session(profile: object) { localStorage.setItem('ac-token', 'token'); localStorage.setItem('ac-student', JSON.stringify(profile)) }

it('atualiza nome e curso e mostra autorização para ministrar', async () => {
  let profile = { id: 'aluno', name: 'Ana', email: 'ana@example.test', course: 'DSM', professor: false, podeMinistrar: true }
  session(profile)
  vi.stubGlobal('fetch', vi.fn((url: string, init?: RequestInit) => {
    if (url === '/api/auth/me' && init?.method === 'PUT') { profile = { ...profile, ...JSON.parse(init.body as string) }; return Promise.resolve(Response.json(profile)) }
    if (url === '/api/auth/me') return Promise.resolve(Response.json(profile))
    if (url === '/api/acesso-professor/minha') return Promise.resolve(Response.json(null))
    return Promise.resolve(Response.json([]))
  }))
  render(<MemoryRouter initialEntries={['/perfil']}><AuthProvider><AccessPage /></AuthProvider></MemoryRouter>)
  expect(await screen.findByText('✓ Autorizado a ministrar')).toBeTruthy()
  fireEvent.change(screen.getByLabelText('Nome'), { target: { value: 'Ana Souza' } })
  fireEvent.change(screen.getByLabelText('Curso'), { target: { value: 'Gestão' } })
  fireEvent.click(screen.getByRole('button', { name: 'Salvar alterações' }))
  expect(await screen.findByText('Perfil atualizado.')).toBeTruthy()
  expect(JSON.parse(localStorage.getItem('ac-student')!).name).toBe('Ana Souza')
  expect((screen.getByLabelText('E-mail') as HTMLInputElement).readOnly).toBe(true)
})

it('permite solicitar acesso e mostra o estado pendente', async () => {
  const profile = { id: 'aluno', name: 'Ana', email: 'ana@example.test', course: 'DSM', professor: false }
  session(profile); let pending = false
  vi.stubGlobal('fetch', vi.fn((url: string, init?: RequestInit) => {
    if (url === '/api/auth/me') return Promise.resolve(Response.json(profile))
    if (url === '/api/acesso-professor/minha') return Promise.resolve(Response.json(pending ? { id: 'p', status: 'pendente' } : null))
    if (url === '/api/acesso-professor' && init?.method === 'POST') { pending = true; return Promise.resolve(Response.json({ id: 'p' })) }
    return Promise.resolve(Response.json([]))
  }))
  render(<MemoryRouter><AuthProvider><AccessPage /></AuthProvider></MemoryRouter>)
  fireEvent.change(await screen.findByLabelText('Justificativa'), { target: { value: 'Ensinar' } })
  fireEvent.click(screen.getByRole('button', { name: 'Solicitar acesso' }))
  expect(await screen.findByText('Sua solicitação está aguardando avaliação.')).toBeTruthy()
})

it('professor avalia pedidos na página própria e lê todas as notificações no sino', async () => {
  const profile = { id: 'prof', name: 'Professor', email: 'prof@example.test', course: 'DSM', professor: true }
  session(profile); let evaluated = false; let read = false
  vi.stubGlobal('fetch', vi.fn((url: string, init?: RequestInit) => {
    if (url === '/api/auth/me') return Promise.resolve(Response.json(profile))
    if (url === '/api/notificacoes') return Promise.resolve(Response.json(Array.from({ length: 7 }, (_, i) => ({ id: `n${i}`, mensagem: `Notícia ${i}`, lida: i === 6 ? read : true }))))
    if (url === '/api/notificacoes/n6/lida') { read = true; return Promise.resolve(new Response(null, { status: 204 })) }
    if (url === '/api/acesso-professor/p1/avaliar' && init?.method === 'POST') { evaluated = true; return Promise.resolve(Response.json({})) }
    if (url === '/api/acesso-professor') return Promise.resolve(Response.json(evaluated ? [] : [{ id: 'p1', nome: 'Ana', justificativa: 'Ensinar' }]))
    return Promise.reject(new Error('Rota inesperada'))
  }))
  render(<MemoryRouter initialEntries={['/solicitacoes']}><AuthProvider><AccessPage /></AuthProvider></MemoryRouter>)
  fireEvent.click(await screen.findByRole('button', { name: 'Recusar' }))
  expect(await screen.findByText('Nenhuma solicitação pendente.')).toBeTruthy()
  fireEvent.click(await screen.findByRole('button', { name: 'Notificações, 1 não lida' }))
  const panel = screen.getByRole('region', { name: 'Notificações recentes' })
  fireEvent.click(within(panel).getByRole('button', { name: 'Marcar como lida: Notícia 6' }))
  await waitFor(() => expect(screen.getByRole('button', { name: 'Notificações' })).toBeTruthy())
})
