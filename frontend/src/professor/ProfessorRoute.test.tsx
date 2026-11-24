// @vitest-environment jsdom
import { afterEach, describe, expect, it, vi } from 'vitest'
import { act, cleanup, render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthProvider } from '../auth'
import ProfessorRoute from './ProfessorRoute'

const profile = { id: 'professor-1', name: 'Professor', email: 'prof@example.test', course: 'DSM' }

function renderProtectedPage() {
  return render(<MemoryRouter initialEntries={['/professor/atividades/nova']}>
    <AuthProvider><Routes>
      <Route element={<ProfessorRoute />}><Route path="/professor/atividades/nova" element={<h1>Formulário de aula</h1>} /></Route>
      <Route path="/" element={<h1>Catálogo do aluno</h1>} />
      <Route path="/login" element={<h1>Entrar novamente</h1>} />
    </Routes></AuthProvider>
  </MemoryRouter>)
}

afterEach(() => {
  cleanup()
  localStorage.clear()
  vi.unstubAllGlobals()
})

describe('acesso ao cadastro de aulas', () => {
  it('recupera o perfil de professor de uma sessão antiga antes de decidir a rota', async () => {
    localStorage.setItem('ac-token', 'token-de-teste')
    localStorage.setItem('ac-student', JSON.stringify(profile))
    let resolveProfile!: (response: Response) => void
    const fetchMock = vi.fn(() => new Promise<Response>(resolve => { resolveProfile = resolve }))
    vi.stubGlobal('fetch', fetchMock)

    renderProtectedPage()
    expect(screen.getByRole('status').textContent).toBe('Verificando sua conta…')
    expect(screen.queryByText('Catálogo do aluno')).toBeNull()
    await act(async () => resolveProfile(new Response(JSON.stringify({ ...profile, accountType: 'professor' }))))

    expect(await screen.findByRole('heading', { name: 'Formulário de aula' })).toBeTruthy()
    expect(JSON.parse(localStorage.getItem('ac-student')!).accountType).toBe('professor')
    expect(fetchMock).toHaveBeenCalledWith('/api/auth/me', expect.objectContaining({
      headers: expect.any(Headers),
    }))
    const headers = (fetchMock.mock.calls[0] as unknown as [string, RequestInit])[1].headers as Headers
    expect(headers.get('Authorization')).toBe('Bearer token-de-teste')
  })

  it('usa o perfil da API e bloqueia um aluno mesmo com professor salvo no navegador', async () => {
    localStorage.setItem('ac-token', 'token-de-teste')
    localStorage.setItem('ac-student', JSON.stringify({ ...profile, accountType: 'professor' }))
    vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(new Response(JSON.stringify({ ...profile, accountType: 'aluno' })))))

    renderProtectedPage()

    expect(await screen.findByRole('heading', { name: 'Catálogo do aluno' })).toBeTruthy()
    expect(screen.queryByText('Formulário de aula')).toBeNull()
  })

  it('encerra a sessão expirada e pede novo login', async () => {
    localStorage.setItem('ac-token', 'token-expirado')
    localStorage.setItem('ac-student', JSON.stringify({ ...profile, accountType: 'professor' }))
    vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(new Response(null, { status: 401 }))))

    renderProtectedPage()

    expect(await screen.findByRole('heading', { name: 'Entrar novamente' })).toBeTruthy()
    expect(localStorage.getItem('ac-token')).toBeNull()
    expect(localStorage.getItem('ac-student')).toBeNull()
  })
})
