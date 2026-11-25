// @vitest-environment jsdom
import { afterEach, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth'
import App from './App'
import AccessPage from './AccessPage'
import { StudentProposalsPage, TeacherProposalsPage } from './PropostasPage'
import SiteHeader from './SiteHeader'

afterEach(() => { cleanup(); localStorage.clear(); vi.unstubAllGlobals() })

function renderRoutes(profile: object) {
  localStorage.setItem('ac-token', 'token')
  localStorage.setItem('ac-student', JSON.stringify(profile))
  vi.stubGlobal('fetch', vi.fn((url: string) => {
    if (url === '/api/auth/me') return Promise.resolve(Response.json(profile))
    if (url === '/api/acesso-professor/minha') return Promise.resolve(Response.json(null))
    return Promise.resolve(Response.json([]))
  }))
  render(<MemoryRouter><AuthProvider><Routes>
    <Route path="/perfil" element={<AccessPage />} />
    <Route path="/ministrar" element={<StudentProposalsPage />} />
    <Route path="/professor/atividades" element={<TeacherProposalsPage />} />
    <Route path="*" element={<App />} />
  </Routes></AuthProvider></MemoryRouter>)
}

it('mantém o menu do aluno entre as telas sem exibir rodapé', async () => {
  renderRoutes({ id: 'aluno', name: 'Ana', email: 'ana@example.test', course: 'DSM', professor: false, podeMinistrar: true })
  const nav = () => within(screen.getByRole('navigation', { name: 'Navegação principal' }))
  const open = () => fireEvent.click(screen.getByRole('button', { name: /Perfil de Ana/ }))
  expect(screen.queryByRole('contentinfo')).toBeNull()
  open()
  expect(nav().getByRole('link', { name: 'Minhas aulas' })).toBeTruthy()
  fireEvent.click(screen.getByRole('link', { name: 'Meu perfil' }))
  expect(await screen.findByRole('heading', { name: /Meu perfil/ })).toBeTruthy()
  expect(screen.queryByRole('contentinfo')).toBeNull()
  open()
  expect(screen.getByRole('link', { name: 'Meu perfil' }).getAttribute('aria-current')).toBe('page')
  fireEvent.click(nav().getByRole('link', { name: 'Minhas inscrições' }))
  expect(await screen.findByRole('heading', { name: 'Minhas inscrições' })).toBeTruthy()
  expect(screen.queryByRole('contentinfo')).toBeNull()
  open()
  fireEvent.click(nav().getByRole('link', { name: 'Minhas aulas' }))
  expect(await screen.findByRole('heading', { name: /Minhas aulas/ })).toBeTruthy()
  expect(screen.queryByRole('contentinfo')).toBeNull()
  fireEvent.click(nav().getByRole('link', { name: 'Explorar atividades' }))
  expect(await screen.findByText('Atividades disponíveis')).toBeTruthy()
})

it('mantém a navegação do professor e permite fechar o submenu com Escape', async () => {
  renderRoutes({ id: 'professor', name: 'Marina', email: 'p@example.test', course: 'DSM', professor: true, podeMinistrar: false })
  const nav = () => within(screen.getByRole('navigation', { name: 'Navegação principal' }))
  fireEvent.click(nav().getByRole('link', { name: 'Minhas aulas' }))
  expect(await screen.findByRole('heading', { name: /Minhas aulas/ })).toBeTruthy()
  fireEvent.click(screen.getByRole('link', { name: 'Propostas para avaliar' }))
  expect(await screen.findByRole('heading', { name: /Supervisão de aulas/ })).toBeTruthy()
  expect(nav().getByRole('link', { name: 'Minhas aulas' }).getAttribute('aria-current')).toBe('page')
  const trigger = screen.getByRole('button', { name: /Perfil de Marina, Professor/ })
  fireEvent.click(trigger)
  expect(trigger.getAttribute('aria-expanded')).toBe('true')
  expect(nav().getByRole('link', { name: 'Minhas aulas' }).getAttribute('href')).toBe('/ministrar')
  fireEvent.keyDown(document, { key: 'Escape' })
  expect(trigger.getAttribute('aria-expanded')).toBe('false')
  expect(document.activeElement).toBe(trigger)
  fireEvent.click(trigger)
  fireEvent.click(screen.getByRole('button', { name: 'Sair' }))
  expect(await screen.findByRole('link', { name: 'Entrar' })).toBeTruthy()
  expect(localStorage.getItem('ac-token')).toBeNull()
})

it.each([false, true])('mostra somente os atalhos permitidos para aluno (ministrante: %s)', async canTeach => {
  renderRoutes({ id: 'aluno', name: 'Ana', email: 'a@example.test', course: 'DSM', professor: false, accountType: 'professor', podeMinistrar: canTeach })
  const nav = within(screen.getByRole('navigation', { name: 'Navegação principal' }))
  expect(nav.getAllByRole('link').map(link => link.textContent)).toEqual(canTeach ? ['Explorar atividades', 'Minhas inscrições', 'Minhas aulas'] : ['Explorar atividades', 'Minhas inscrições'])
  expect(screen.queryByRole('link', { name: 'Criar aula' })?.getAttribute('href') ?? null).toBe(canTeach ? '/ministrar/nova' : null)
  expect(screen.queryByRole('link', { name: 'Alunos' })).toBeNull()
  expect(screen.queryByRole('link', { name: 'Solicitações de acesso' })).toBeNull()
  fireEvent.click(screen.getByRole('button', { name: /Perfil de Ana, Aluno/ }))
  expect(screen.queryByRole('link', { name: 'Solicitar acesso para ministrar' }) !== null).toBe(!canTeach)
  await screen.findByRole('link', { name: 'Meu perfil' })
})

it('prioriza as aulas do professor e não duplica solicitações no perfil', async () => {
  renderRoutes({ id: 'professor', name: 'Ada Lovelace', email: 'ada@example.test', course: 'DSM', professor: true })
  const nav = within(screen.getByRole('navigation', { name: 'Navegação principal' }))
  expect(nav.getAllByRole('link').map(link => link.textContent)).toEqual(['Minhas aulas', 'Explorar atividades', 'Alunos', 'Solicitações de acesso'])
  expect(screen.getByRole('link', { name: 'Criar aula' }).getAttribute('href')).toBe('/professor/atividades/nova')
  expect(screen.getByRole('link', { name: 'ConectaCampus — início' }).getAttribute('href')).toBe('/ministrar')
  fireEvent.click(screen.getByRole('button', { name: 'Perfil de Ada Lovelace, Professor' }))
  const profile = within(screen.getByRole('region', { name: 'Opções da conta' }))
  expect(profile.queryByRole('link', { name: 'Solicitações de acesso' })).toBeNull()
  expect(profile.getByRole('link', { name: 'Minhas inscrições' })).toBeTruthy()
  expect(screen.getByText('AL')).toBeTruthy()
  await screen.findByRole('link', { name: 'Meu perfil' })
})

it('fecha o perfil ao clicar fora, sair pelo teclado ou trocar de painel', async () => {
  const profile = { id: 'prof', name: 'Ana', email: 'a@example.test', course: 'DSM', professor: true }
  localStorage.setItem('ac-token', 'token'); localStorage.setItem('ac-student', JSON.stringify(profile))
  vi.stubGlobal('fetch', vi.fn((url: string) => Promise.resolve(Response.json(url === '/api/auth/me' ? profile : []))))
  render(<MemoryRouter><AuthProvider><SiteHeader active="atividades" /><button>Depois do header</button></AuthProvider></MemoryRouter>)
  const user = userEvent.setup()
  const trigger = screen.getByRole('button', { name: /Perfil de Ana/ })
  fireEvent.click(trigger)
  expect(document.activeElement).toBe(screen.getByRole('link', { name: 'Meu perfil' }))
  await user.tab(); await user.tab(); await user.tab()
  expect(document.activeElement).toBe(screen.getByRole('button', { name: 'Depois do header' }))
  expect(screen.queryByRole('region', { name: 'Opções da conta' })).toBeNull()
  fireEvent.click(trigger); fireEvent.pointerDown(document.body)
  expect(trigger.getAttribute('aria-expanded')).toBe('false')
  fireEvent.click(trigger); fireEvent.click(screen.getByRole('button', { name: 'Notificações' }))
  expect(screen.queryByRole('region', { name: 'Opções da conta' })).toBeNull()
  expect(screen.getByRole('region', { name: 'Notificações recentes' })).toBeTruthy()
  fireEvent.click(screen.getByRole('button', { name: 'Abrir navegação' }))
  expect(screen.queryByRole('region', { name: 'Notificações recentes' })).toBeNull()
  fireEvent.keyDown(document, { key: 'Escape' })
  expect(screen.getByRole('button', { name: 'Abrir navegação' }).getAttribute('aria-expanded')).toBe('false')
})

it('atualiza os atalhos quando a API revalida as permissões da sessão', async () => {
  const stored = { id: '1', name: 'Ana', email: 'a@example.test', course: 'DSM', professor: true }
  localStorage.setItem('ac-token', 'token'); localStorage.setItem('ac-student', JSON.stringify(stored))
  vi.stubGlobal('fetch', vi.fn((url: string) => Promise.resolve(Response.json(url === '/api/auth/me' ? { ...stored, professor: false, podeMinistrar: false } : []))))
  render(<MemoryRouter><AuthProvider><SiteHeader active="atividades" /></AuthProvider></MemoryRouter>)
  await waitFor(() => expect(screen.queryByRole('link', { name: 'Criar aula' })).toBeNull())
  expect(screen.queryByRole('link', { name: 'Alunos' })).toBeNull()
  expect(screen.getByRole('button', { name: /Perfil de Ana, Aluno/ })).toBeTruthy()
})
