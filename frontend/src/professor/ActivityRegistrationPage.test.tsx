// @vitest-environment jsdom
import { afterEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { AuthProvider } from '../auth'
import ActivityRegistrationPage from './ActivityRegistrationPage'

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

afterEach(() => {
  cleanup()
  localStorage.clear()
  vi.unstubAllGlobals()
})

describe('cadastro de atividade', () => {
  it('bloqueia o envio quando os campos obrigatórios ou o intervalo são inválidos', async () => {
    const fetchMock = vi.fn((url: string) => {
      if (url === '/api/salas/tipos') return Promise.resolve(json([{ tipo: 'DESKTOP', capacidade: 24 }]))
      return Promise.reject(new Error(`Rota inesperada: ${url}`))
    })
    vi.stubGlobal('fetch', fetchMock)
    render(<MemoryRouter><AuthProvider><ActivityRegistrationPage /></AuthProvider></MemoryRouter>)

    fireEvent.click(await screen.findByRole('button', { name: 'Cadastrar atividade' }))

    expect(await screen.findByText('Informe o tema da atividade.')).toBeTruthy()
    expect(screen.getByText('Informe a descrição da atividade.')).toBeTruthy()
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('carrega os tipos de sala e envia um intervalo ISO válido para a API', async () => {
    localStorage.setItem('ac-token', 'token-de-teste')
    localStorage.setItem('ac-student', JSON.stringify({ id: 'professor-1', name: 'Professor', email: 'prof@example.test', course: 'DSM' }))
    const fetchMock = vi.fn((url: string, init?: RequestInit) => {
      if (url === '/api/salas/tipos') return Promise.resolve(json([{ tipo: 'DESKTOP', capacidade: 24 }]))
      if (url === '/api/atividades' && init?.method === 'POST') return Promise.resolve(json({
        sala: { nome: 'Laboratório Desktop' }, inicio: '2032-12-15T15:00:00Z', fim: '2032-12-15T17:00:00Z',
      }, 201))
      return Promise.reject(new Error(`Rota inesperada: ${url}`))
    })
    vi.stubGlobal('fetch', fetchMock)
    render(<MemoryRouter><AuthProvider><ActivityRegistrationPage /></AuthProvider></MemoryRouter>)

    fireEvent.change(await screen.findByLabelText('Tema da atividade'), { target: { value: 'Integração contínua' } })
    fireEvent.change(screen.getByLabelText('Descrição'), { target: { value: 'Prática de testes automatizados.' } })
    fireEvent.change(screen.getByLabelText(/Tipo de sala/), { target: { value: 'DESKTOP' } })
    fireEvent.change(screen.getByLabelText('Data'), { target: { value: '2032-12-15' } })
    fireEvent.change(screen.getByLabelText('Horário de início'), { target: { value: '12:00' } })
    fireEvent.change(screen.getByLabelText('Horário de término'), { target: { value: '14:00' } })
    fireEvent.click(screen.getByRole('button', { name: 'Cadastrar atividade' }))

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/atividades', expect.objectContaining({ method: 'POST' })))
    const [, init] = fetchMock.mock.calls.find(([url]) => url === '/api/atividades') as [string, RequestInit]
    expect(JSON.parse(init.body as string)).toMatchObject({
      tema: 'Integração contínua', modalidade: 'Aula', tipoSala: 'DESKTOP',
      inicio: expect.stringMatching(/^2032-12-15T/), fim: expect.stringMatching(/^2032-12-15T/),
    })
  })
})
