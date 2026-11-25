// @vitest-environment jsdom
import { afterEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter, useLocation } from 'react-router-dom'
import { AuthProvider } from '../auth'
import MyActivitiesPage from './MyActivitiesPage'

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

function CurrentPath() {
  return <output aria-label="Caminho atual">{useLocation().pathname}</output>
}

afterEach(() => {
  cleanup()
  localStorage.clear()
  vi.unstubAllGlobals()
})

describe('minhas atividades do professor', () => {
  it('lista, ordena e abre os detalhes da atividade selecionada', async () => {
    localStorage.setItem('ac-token', 'token-de-teste')
    localStorage.setItem('ac-student', JSON.stringify({ id: 'professor-1', name: 'Professor', email: 'prof@example.test', course: 'DSM' }))
    const first = {
      id: 'atividade-1', tema: 'Atividade antiga', descricao: 'Descrição antiga', modalidade: 'AULA',
      sala: { id: 'sala-1', nome: 'Laboratório Desktop', tipo: 'DESKTOP', capacidade: 24 },
      data: '2032-01-15', inicio: '2032-01-15T14:00:00Z', fim: '2032-01-15T16:00:00Z', status: 'publicada',
    }
    const second = { ...first, id: 'atividade-2', tema: 'Atividade recente', data: '2032-02-15', inicio: '2032-02-15T14:00:00Z', fim: '2032-02-15T16:00:00Z' }
    const fetchMock = vi.fn((url: string) => {
      if (url === '/api/atividades/minhas') return Promise.resolve(json([first, second]))
      if (url === '/api/atividades/atividade-2') return Promise.resolve(json(second))
      if (url === '/api/atividades/atividade-2/inscricoes') return Promise.resolve(json([]))
      return Promise.reject(new Error(`Rota inesperada: ${url}`))
    })
    vi.stubGlobal('fetch', fetchMock)
    render(<MemoryRouter initialEntries={['/professor/atividades']}><AuthProvider><MyActivitiesPage /><CurrentPath /></AuthProvider></MemoryRouter>)

    expect(await screen.findByText('Atividade antiga')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Lançar aula' })).toBeTruthy()
    fireEvent.change(screen.getByLabelText('Ordenar por data'), { target: { value: 'desc' } })
    const titles = Array.from(document.querySelectorAll('.professor-activity-card h2')).map(element => element.textContent)
    expect(titles).toEqual(['Atividade recente', 'Atividade antiga'])
    fireEvent.click(screen.getAllByRole('button', { name: 'Ver detalhes' })[0])

    expect(await screen.findByRole('heading', { name: 'Atividade recente' })).toBeTruthy()
    expect(screen.getByText('Laboratório Desktop')).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Lançar aula' }))
    expect(screen.getByLabelText('Caminho atual').textContent).toBe('/professor/atividades/nova')
  })
})
