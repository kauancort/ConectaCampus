// @vitest-environment jsdom
import { afterEach, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthProvider } from '../auth'
import SupervisedActivityPage from './SupervisedActivityPage'

afterEach(() => { cleanup(); localStorage.clear(); vi.unstubAllGlobals() })
const profile = { id: 'prof', name: 'Professora', professor: true }
const proposal = { atividadeId: 'a1', titulo: 'Oficina de dados', tema: 'Dados', descricao: 'Prática', aluno: 'Ana' }
function setup(approved = [proposal], instructor = false) {
  const currentProfile = instructor ? { id: 'aluno', name: 'Ana', professor: false, podeMinistrar: true } : profile
  localStorage.setItem('ac-token', 'token'); localStorage.setItem('ac-student', JSON.stringify(currentProfile))
  const fetchMock = vi.fn((url: string, init?: RequestInit) => {
    if (url === '/api/auth/me') return Promise.resolve(Response.json(currentProfile))
    if (url === '/api/notificacoes') return Promise.resolve(Response.json([]))
    if (url === '/api/propostas/aprovadas') return Promise.resolve(Response.json(approved))
    if (url === '/api/propostas/minhas') return Promise.resolve(Response.json(approved))
    if (url === '/api/atividades/a1') return Promise.resolve(Response.json({ status: 'publicada' }))
    if (url === '/api/atividades/a1/assentos') return Promise.resolve(Response.json({ inicio: '2033-01-01T12:00:00Z', sala: { nome: 'Sala 1', tipo: 'NOTEBOOK' }, capacidadeTotal: 2, lugaresDisponiveis: 1, assentos: [{ id: 's1', codigo: 'A1', status: 'OCUPADO' }, { id: 's2', codigo: 'A2', status: 'DISPONIVEL' }] }))
    if (url === '/api/atividades/a1/inscricoes') return Promise.resolve(Response.json([{ alunoId: 'u1', nomeAluno: 'Bia', status: 'ativa', presente: null, assento: { codigo: 'A1' } }, { alunoId: 'u2', nomeAluno: 'Caio', status: 'cancelada', presente: null, assento: null }]))
    if (url === '/api/atividades/a1/inscricoes/u1/presenca' && init?.method === 'PUT') return Promise.resolve(new Response(null, { status: 204 }))
    if (url === '/api/atividades/a1/conteudo' && init?.method === 'PUT') return Promise.resolve(Response.json(JSON.parse(init.body as string)))
    return Promise.reject(new Error('Rota inesperada'))
  })
  vi.stubGlobal('fetch', fetchMock)
  render(<MemoryRouter initialEntries={[instructor ? '/ministrar/aulas/a1' : '/professor/aulas/a1']}><AuthProvider><Routes><Route path="/professor/aulas/:id" element={<SupervisedActivityPage />} /><Route path="/ministrar/aulas/:id" element={<SupervisedActivityPage instructor />} /></Routes></AuthProvider></MemoryRouter>)
  return fetchMock
}

it('apresenta ocupação e inscritos ativos e registra presença', async () => {
  const fetchMock = setup()
  expect(await screen.findByRole('heading', { name: 'Oficina de dados' })).toBeTruthy()
  expect(screen.getByLabelText('Assento A1, ocupado')).toBeTruthy()
  expect(screen.getByLabelText('Assento A2, disponível')).toBeTruthy()
  expect(screen.getByText(/Assento A1 · Presença não registrada/)).toBeTruthy()
  expect(screen.queryByText('Caio')).toBeNull()
  fireEvent.click(screen.getByRole('button', { name: 'Presente' }))
  await waitFor(() => expect(screen.getByText(/Assento A1 · Presente/)).toBeTruthy())
  expect(fetchMock).toHaveBeenCalledWith('/api/atividades/a1/inscricoes/u1/presenca', expect.objectContaining({ method: 'PUT', body: JSON.stringify({ presente: true }) }))
})

it('permite ao ministrante registrar ausência e voltar às próprias aulas', async () => {
  const fetchMock = setup([proposal], true)
  expect(await screen.findByRole('heading', { name: 'Oficina de dados' })).toBeTruthy()
  expect(screen.getByRole('link', { name: '← Voltar às minhas aulas' }).getAttribute('href')).toBe('/ministrar')
  fireEvent.click(screen.getByRole('button', { name: 'Ausente' }))
  await waitFor(() => expect(screen.getByText(/Assento A1 · Ausente/)).toBeTruthy())
  expect(fetchMock).toHaveBeenCalledWith('/api/atividades/a1/inscricoes/u1/presenca', expect.objectContaining({ method: 'PUT', body: JSON.stringify({ presente: false }) }))
  fireEvent.click(screen.getByRole('button', { name: 'Editar aula' }))
  fireEvent.change(screen.getByLabelText('Título da aula'), { target: { value: 'Dados na prática' } })
  fireEvent.click(screen.getByRole('button', { name: 'Salvar alterações' }))
  expect(await screen.findByRole('heading', { name: 'Dados na prática' })).toBeTruthy()
  expect(screen.getByText('Informações da aula atualizadas.')).toBeTruthy()
})

it('não carrega participantes de aula fora da supervisão do professor', async () => {
  const fetchMock = setup([])
  expect((await screen.findByRole('alert')).textContent).toMatch(/não foi encontrada/)
  expect(fetchMock.mock.calls.some(([url]) => url === '/api/atividades/a1/inscricoes')).toBe(false)
})
