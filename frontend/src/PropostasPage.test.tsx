// @vitest-environment jsdom
import { afterEach, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth'
import { StudentProposalsPage, TeacherProposalsPage } from './PropostasPage'

afterEach(() => { cleanup(); localStorage.clear(); vi.unstubAllGlobals() })
const base = { id: 'p1', titulo: 'Oficina de dados', tema: 'Dados', descricao: 'Exercícios práticos', tipoSala: 'DESKTOP', inicio: '2033-01-01T12:00:00Z', fim: '2033-01-01T13:00:00Z', status: 'pendente', aluno: 'Ana' }
function loggedIn(profile: object) { localStorage.setItem('ac-token', 'token'); localStorage.setItem('ac-student', JSON.stringify(profile)) }

it('impede que um aluno sem autorização acesse o formulário', () => {
  loggedIn({ id: '1', name: 'Ana', professor: false, podeMinistrar: false })
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(Response.json({ id: '1', name: 'Ana', professor: false, podeMinistrar: false })))
  render(<MemoryRouter><AuthProvider><StudentProposalsPage /></AuthProvider></MemoryRouter>)
  expect(screen.getByText(/área é exclusiva para alunos autorizados/)).toBeTruthy()
  expect(screen.queryByLabelText('Título da aula')).toBeNull()
})

it('mostra a fila compartilhada e exige motivo antes de recusar', async () => {
  const profile = { id: 'prof', name: 'Professora', professor: true, podeMinistrar: false }
  loggedIn(profile)
  let refused = false
  const fetchMock = vi.fn((url: string, init?: RequestInit) => {
    if (url === '/api/auth/me') return Promise.resolve(Response.json(profile))
    if (url === '/api/propostas/pendentes') return Promise.resolve(Response.json(refused ? [] : [base]))
    if (url === '/api/propostas/aprovadas') return Promise.resolve(Response.json([]))
    if (url === '/api/propostas/p1/avaliar' && init?.method === 'POST') { refused = true; return Promise.resolve(Response.json({ status: 'recusada' })) }
    return Promise.reject(new Error(`Rota inesperada: ${url}`))
  })
  vi.stubGlobal('fetch', fetchMock)
  render(<MemoryRouter><AuthProvider><TeacherProposalsPage /></AuthProvider></MemoryRouter>)
  fireEvent.click(await screen.findByRole('button', { name: 'Recusar aula' }))
  expect(screen.getByRole('alert').textContent).toBe('Informe o motivo da recusa.')
  expect(fetchMock.mock.calls.some(([url]) => url === '/api/propostas/p1/avaliar')).toBe(false)
  fireEvent.change(screen.getByLabelText('Motivo da recusa (necessário para recusar)'), { target: { value: 'Ajuste o horário.' } })
  fireEvent.click(screen.getByRole('button', { name: 'Recusar aula' }))
  await waitFor(() => expect(screen.getByText('Nenhuma proposta aguardando avaliação.')).toBeTruthy())
  const call = fetchMock.mock.calls.find(([url]) => url === '/api/propostas/p1/avaliar')
  expect(JSON.parse(call?.[1]?.body as string)).toEqual({ decisao: 'recusada', motivo: 'Ajuste o horário.' })
})

it('separa a fila das aulas aprovadas em páginas navegáveis', async () => {
  const profile = { id: 'prof', name: 'Professora', professor: true }
  loggedIn(profile)
  vi.stubGlobal('fetch', vi.fn((url: string) => Promise.resolve(Response.json(url === '/api/auth/me' ? profile : url === '/api/propostas/pendentes' ? [base] : url === '/api/propostas/aprovadas' ? [{ ...base, id: 'p2', titulo: 'Aula aprovada', status: 'aprovada' }] : []))))
  render(<MemoryRouter initialEntries={['/professor/atividades']}><AuthProvider><Routes><Route path="/professor/atividades" element={<TeacherProposalsPage />} /><Route path="/professor/aulas-aprovadas" element={<TeacherProposalsPage />} /></Routes></AuthProvider></MemoryRouter>)
  expect(await screen.findByText('Oficina de dados')).toBeTruthy()
  expect(screen.queryByText('Aula aprovada')).toBeNull()
  fireEvent.click(screen.getByRole('link', { name: /Aulas que aprovei/ }))
  expect(await screen.findByText('Aula aprovada')).toBeTruthy()
  expect(screen.queryByText('Oficina de dados')).toBeNull()
})

it('apresenta falha de carregamento sem afirmar que a fila está vazia e permite tentar novamente', async () => {
  const profile = { id: 'prof', name: 'Professora', professor: true }
  loggedIn(profile)
  let offline = true
  vi.stubGlobal('fetch', vi.fn((url: string) => {
    if (url === '/api/auth/me') return Promise.resolve(Response.json(profile))
    if (offline && url === '/api/propostas/pendentes') return Promise.reject(new Error('offline'))
    return Promise.resolve(Response.json([]))
  }))
  render(<MemoryRouter><AuthProvider><TeacherProposalsPage /></AuthProvider></MemoryRouter>)
  expect(await screen.findByText(/Não foi possível carregar as aulas/)).toBeTruthy()
  expect(screen.queryByText('Nenhuma proposta aguardando avaliação.')).toBeNull()
  offline = false
  fireEvent.click(screen.getByRole('button', { name: 'Tentar novamente' }))
  expect(await screen.findByText('Nenhuma proposta aguardando avaliação.')).toBeTruthy()
})

it('envia o número da sala na proposta', async () => {
  const profile = { id: 'aluno', name: 'Ana', professor: false, podeMinistrar: true }
  loggedIn(profile)
  const fetchMock = vi.fn((url: string, init?: RequestInit) => {
    if (url === '/api/auth/me') return Promise.resolve(Response.json(profile))
    if (url === '/api/notificacoes' || url === '/api/propostas/minhas') return Promise.resolve(Response.json([]))
    if (url === '/api/propostas' && init?.method === 'POST') return Promise.resolve(Response.json({ id: 'p1' }, { status: 201 }))
    return Promise.reject(new Error('Rota inesperada'))
  })
  vi.stubGlobal('fetch', fetchMock)
  render(<MemoryRouter><AuthProvider><StudentProposalsPage /></AuthProvider></MemoryRouter>)
  expect(screen.queryByLabelText('Título da aula')).toBeNull()
  fireEvent.click(screen.getByRole('link', { name: 'Propor uma aula' }))
  fireEvent.change(screen.getByLabelText('Título da aula'), { target: { value: 'Oficina' } })
  fireEvent.change(screen.getByLabelText('Tema'), { target: { value: 'Dados' } })
  fireEvent.change(screen.getByLabelText('Descrição'), { target: { value: 'Atividade prática' } })
  fireEvent.change(screen.getByLabelText('Tipo de sala'), { target: { value: 'DESKTOP' } })
  fireEvent.change(screen.getByLabelText(/Número da sala/), { target: { value: '101' } })
  fireEvent.change(screen.getByLabelText('Data'), { target: { value: '2099-01-01' } })
  fireEvent.change(screen.getByLabelText('Horário de início'), { target: { value: '10:00' } })
  fireEvent.change(screen.getByLabelText('Horário de término'), { target: { value: '12:00' } })
  fireEvent.click(screen.getByRole('button', { name: 'Enviar proposta' }))
  await waitFor(() => expect(screen.getByText('Proposta enviada. Aguarde a avaliação de um professor.')).toBeTruthy())
  expect(screen.queryByLabelText('Título da aula')).toBeNull()
  const call = fetchMock.mock.calls.find(([url]) => url === '/api/propostas')
  expect(JSON.parse(call?.[1]?.body as string).numeroSala).toBe('101')
})
