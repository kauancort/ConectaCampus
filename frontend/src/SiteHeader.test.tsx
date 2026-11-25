// @vitest-environment jsdom
import { afterEach, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth'
import App from './App'
import AccessPage from './AccessPage'
import { StudentProposalsPage, TeacherProposalsPage } from './PropostasPage'

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

it('mantém o menu do aluno e o rodapé iguais entre as telas', async () => {
  renderRoutes({ id: 'aluno', name: 'Ana', email: 'ana@example.test', course: 'DSM', professor: false, podeMinistrar: true })
  const nav = () => within(screen.getByRole('navigation', { name: 'Navegação principal' }))
  const open = () => fireEvent.click(nav().getByRole('button', { name: 'Minha área' }))
  const footer = screen.getByRole('contentinfo').textContent
  open()
  expect(nav().getByRole('link', { name: 'Minhas aulas' })).toBeTruthy()
  fireEvent.click(nav().getByRole('link', { name: 'Meu perfil' }))
  expect(await screen.findByRole('heading', { name: /Meu perfil/ })).toBeTruthy()
  expect(screen.getByRole('contentinfo').textContent).toBe(footer)
  open()
  expect(nav().getByRole('link', { name: 'Meu perfil' }).getAttribute('aria-current')).toBe('page')
  fireEvent.click(nav().getByRole('link', { name: 'Minhas inscrições' }))
  expect(await screen.findByRole('heading', { name: 'Minhas inscrições' })).toBeTruthy()
  expect(screen.getByRole('contentinfo').textContent).toBe(footer)
  open()
  fireEvent.click(nav().getByRole('link', { name: 'Minhas aulas' }))
  expect(await screen.findByRole('heading', { name: /Minhas aulas/ })).toBeTruthy()
  expect(screen.getByRole('contentinfo').textContent).toBe(footer)
  fireEvent.click(nav().getByRole('link', { name: 'Atividades' }))
  expect(await screen.findByText('Atividades disponíveis')).toBeTruthy()
})

it('mantém a navegação do professor e permite fechar o submenu com Escape', async () => {
  renderRoutes({ id: 'professor', name: 'Marina', email: 'p@example.test', course: 'DSM', professor: true, podeMinistrar: false })
  const nav = () => within(screen.getByRole('navigation', { name: 'Navegação principal' }))
  fireEvent.click(nav().getByRole('link', { name: 'Área do professor' }))
  expect(await screen.findByRole('heading', { name: /Supervisão de aulas/ })).toBeTruthy()
  expect(nav().getByRole('link', { name: 'Área do professor' }).getAttribute('aria-current')).toBe('page')
  const trigger = nav().getByRole('button', { name: 'Minha área' })
  fireEvent.click(trigger)
  expect(trigger.getAttribute('aria-expanded')).toBe('true')
  expect(nav().queryByRole('link', { name: 'Minhas aulas' })).toBeNull()
  fireEvent.keyDown(document, { key: 'Escape' })
  expect(trigger.getAttribute('aria-expanded')).toBe('false')
  expect(document.activeElement).toBe(trigger)
  fireEvent.click(screen.getByRole('button', { name: 'Sair' }))
  expect(await screen.findByRole('link', { name: 'Entrar' })).toBeTruthy()
  expect(localStorage.getItem('ac-token')).toBeNull()
})
