// @vitest-environment jsdom
import { afterEach, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthProvider } from '../auth'
import StudentsPage from './StudentsPage'

afterEach(() => { cleanup(); localStorage.clear(); vi.unstubAllGlobals() })

it('filtra alunos autorizados e abre o perfil com suas atividades', async () => {
  const professor = { id: 'prof', name: 'Professora', professor: true }
  const aluno = { id: '1', nome: 'Ana', curso: 'DSM', email: 'ana@example.test', podeMinistrar: true }
  localStorage.setItem('ac-token', 'token')
  localStorage.setItem('ac-student', JSON.stringify(professor))
  vi.stubGlobal('fetch', vi.fn((url: string) => {
    if (url === '/api/auth/me') return Promise.resolve(Response.json(professor))
    if (url === '/api/professor/alunos') return Promise.resolve(Response.json([aluno, { ...aluno, id: '2', nome: 'Bruno', podeMinistrar: false }]))
    if (url === '/api/professor/alunos/1') return Promise.resolve(Response.json({ aluno, inscricoes: [{ id: 'a', titulo: 'Programação', tema: 'Código', dataHoraUtc: '2033-01-01T12:00:00Z', status: 'publicada' }], ministradas: [] }))
    return Promise.resolve(Response.json([]))
  }))
  render(<MemoryRouter initialEntries={['/professor/alunos']}><AuthProvider><Routes><Route path="/professor/alunos" element={<StudentsPage />} /><Route path="/professor/alunos/:id" element={<StudentsPage />} /></Routes></AuthProvider></MemoryRouter>)
  expect(await screen.findByText('Bruno')).toBeTruthy()
  fireEvent.change(screen.getByLabelText('Autorização'), { target: { value: 'autorizados' } })
  expect(screen.queryByText('Bruno')).toBeNull()
  fireEvent.click(screen.getByRole('link', { name: /Ana/ }))
  expect(await screen.findByText('Programação')).toBeTruthy()
  expect(screen.getByText('Nenhuma aula ministrada.')).toBeTruthy()
  expect(screen.getByText('Pode ministrar')).toBeTruthy()
})
