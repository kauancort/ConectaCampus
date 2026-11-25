// @vitest-environment jsdom
import { afterEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { BrowserRouter, MemoryRouter, Route, Routes } from 'react-router-dom'
import App from './App'
import { AuthProvider } from './auth'
import { StudentProposalsPage } from './PropostasPage'

const activity = {
  id: '6d7b5e0f-7e35-4a3b-9050-5f21a7aa1234',
  tema: 'Oficina de testes',
  descricao: 'Aprenda a validar aplicações.',
  modalidade: 'AULA',
  sala: { id: 'sala-desktop', nome: 'Laboratório Desktop', tipo: 'DESKTOP', capacidade: 24 },
  data: '2032-01-15',
  inicio: '2032-01-15T14:00:00Z',
  fim: '2032-01-15T16:00:00Z',
  status: 'publicada',
}

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

function renderApp() {
  return render(<MemoryRouter><AuthProvider><App /></AuthProvider></MemoryRouter>)
}

afterEach(() => {
  cleanup()
  localStorage.clear()
  vi.unstubAllGlobals()
})

it.each([
  { professor: true, podeMinistrar: false, heading: 'Minhas aulas.' },
  { professor: false, podeMinistrar: false, heading: 'Aulas e palestras' },
  { professor: false, podeMinistrar: true, heading: 'Aulas e palestras' },
])('redireciona após login segundo as permissões reais (%j)', async ({ professor, podeMinistrar, heading }) => {
  const profile = { id: '1', name: 'Ada', email: 'ada@example.test', course: 'DSM', professor, podeMinistrar }
  vi.stubGlobal('fetch', vi.fn((url: string) => Promise.resolve(json(url === '/api/auth/login'
    ? { accessToken: 'token', student: profile }
    : url === '/api/auth/me' ? profile : []))))
  window.history.replaceState({}, '', '/entrar')
  render(<BrowserRouter><AuthProvider><Routes>
    <Route path="/ministrar" element={<StudentProposalsPage />} />
    <Route path="*" element={<App />} />
  </Routes></AuthProvider></BrowserRouter>)
  fireEvent.change(screen.getByLabelText('E-mail'), { target: { value: profile.email } })
  fireEvent.change(screen.getByLabelText('Senha'), { target: { value: 'Campus123@' } })
  fireEvent.click(screen.getByText('Entrar', { selector: 'button[type="submit"]' }))
  expect(await screen.findByRole('heading', { name: heading }, { timeout: 5000 })).toBeTruthy()
  expect(screen.getByRole('button', { name: new RegExp(`Perfil de Ada, ${professor ? 'Professor' : 'Aluno'}`) })).toBeTruthy()
})

describe('jornadas da área do aluno', () => {
  it('apresenta o projeto sem carregar ou mostrar atividades para visitantes', async () => {
    const fetchMock = vi.fn().mockResolvedValue(json([activity]))
    vi.stubGlobal('fetch', fetchMock)
    renderApp()
    expect(screen.getByRole('heading', { name: /Todo mundo tem algo para ensinar/ })).toBeTruthy()
    expect(screen.getByRole('heading', { name: 'Como funciona.' })).toBeTruthy()
    expect(screen.queryByText('Oficina de testes')).toBeNull()
    expect(screen.queryByLabelText('Pesquisar por tema')).toBeNull()
    expect(fetchMock.mock.calls.some(([url]) => String(url).startsWith('/api/atividades'))).toBe(false)
    fireEvent.click(screen.getAllByRole('button', { name: 'Criar minha conta' })[0])
    expect(screen.getByRole('button', { name: 'Criar conta' })).toBeTruthy()
  })
  it('mostra erro de credenciais inválidas sem iniciar sessão', async () => {
    vi.stubGlobal('fetch', vi.fn((url: string) => Promise.resolve(url === '/api/auth/login' ? json({ message: 'E-mail ou senha inválidos.' }, 401) : json([]))))
    renderApp()
    fireEvent.click(await screen.findByRole('link', { name: 'Entrar' }))
    fireEvent.change(screen.getByLabelText('E-mail'), { target: { value: 'ada@example.test' } })
    fireEvent.change(screen.getByLabelText('Senha'), { target: { value: 'errada' } })
    fireEvent.click(screen.getByText('Entrar', { selector: 'button[type="submit"]' }))
    expect(await screen.findByRole('alert')).toHaveProperty('textContent', 'E-mail ou senha inválidos.')
    expect(localStorage.getItem('ac-token')).toBeNull()
  })

  it('encerra sessão inválida ao revalidar o perfil salvo', async () => {
    localStorage.setItem('ac-token', 'expirado')
    localStorage.setItem('ac-student', JSON.stringify({ id: '1', name: 'Ada', email: 'ada@example.test', course: 'DSM' }))
    vi.stubGlobal('fetch', vi.fn((url: string) => Promise.resolve(url === '/api/auth/me' ? json({}, 401) : json([]))))
    renderApp()
    await waitFor(() => expect(localStorage.getItem('ac-token')).toBeNull())
    expect(screen.getByRole('link', { name: 'Entrar' })).toBeTruthy()
  })

  it('atualiza o mapa quando outro aluno ocupa o assento', async () => {
    localStorage.setItem('ac-token', 'token')
    localStorage.setItem('ac-student', JSON.stringify({ id: '1', name: 'Ada', email: 'ada@example.test', course: 'DSM' }))
    let occupied = false
    const map = () => ({ atividadeId: activity.id, tema: activity.tema, inicio: activity.inicio, fim: activity.fim, sala: activity.sala, capacidadeTotal: 24, lugaresDisponiveis: occupied ? 23 : 24, assentos: [{ id: 'a1', codigo: 'A1', fileira: 'A', coluna: 1, status: occupied ? 'OCUPADO' : 'DISPONIVEL' }] })
    vi.stubGlobal('fetch', vi.fn((url: string, init?: RequestInit) => {
      if (url === '/api/auth/me') return Promise.resolve(json({ id: '1', name: 'Ada', email: 'ada@example.test', course: 'DSM' }))
      if (url === '/api/atividades') return Promise.resolve(json([activity]))
      if (url === '/api/inscricoes/minhas' || url === '/api/notificacoes') return Promise.resolve(json([]))
      if (url === `/api/atividades/${activity.id}`) return Promise.resolve(json(activity))
      if (url === `/api/atividades/${activity.id}/assentos`) return Promise.resolve(json(map()))
      if (url === `/api/atividades/${activity.id}/inscricoes` && init?.method === 'POST') { occupied = true; return Promise.resolve(json({ message: 'Ocupado' }, 409)) }
      return Promise.reject(new Error(`Rota inesperada: ${url}`))
    }))
    renderApp()
    fireEvent.click(await screen.findByRole('button', { name: 'Ver detalhes' }))
    fireEvent.click(await screen.findByRole('button', { name: 'Escolher assento' }))
    fireEvent.click(await screen.findByRole('button', { name: 'Assento A1, disponível' }))
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar inscrição' }))
    expect(await screen.findByRole('alert')).toHaveProperty('textContent', 'Ocupado')
    expect((screen.getByRole('button', { name: 'Assento A1, ocupado' }) as HTMLButtonElement).disabled).toBe(true)
  })
  it('mostra o próprio assento e impede uma segunda inscrição', async () => {
    const profile = { id: '1', name: 'Ada', email: 'ada@example.test', course: 'DSM' }
    localStorage.setItem('ac-token', 'token')
    localStorage.setItem('ac-student', JSON.stringify(profile))
    vi.stubGlobal('fetch', vi.fn((url: string) => {
      if (url === '/api/auth/me') return Promise.resolve(json(profile))
      if (url === '/api/atividades') return Promise.resolve(json([activity]))
      if (url === '/api/inscricoes/minhas' || url === '/api/notificacoes') return Promise.resolve(json([]))
      if (url === `/api/atividades/${activity.id}`) return Promise.resolve(json(activity))
      if (url === `/api/atividades/${activity.id}/assentos`) return Promise.resolve(json({ atividadeId: activity.id, tema: activity.tema, inicio: activity.inicio, fim: activity.fim, sala: activity.sala, lugaresDisponiveis: 23, jaInscrito: true, meuAssentoId: 'a1', assentos: [{ id: 'a1', codigo: 'A1', fileira: 'A', coluna: 1, status: 'OCUPADO' }, { id: 'a2', codigo: 'A2', fileira: 'A', coluna: 2, status: 'DISPONIVEL' }] }))
      return Promise.reject(new Error(`Rota inesperada: ${url}`))
    }))
    renderApp()
    fireEvent.click(await screen.findByRole('button', { name: 'Ver detalhes' }))
    fireEvent.click(await screen.findByRole('button', { name: 'Escolher assento' }))
    expect((await screen.findByRole('button', { name: 'Assento A1, seu assento' }) as HTMLButtonElement).disabled).toBe(true)
    expect((screen.getByRole('button', { name: 'Assento A2, disponível' }) as HTMLButtonElement).disabled).toBe(true)
    expect(screen.getByText(/Você já está inscrito/)).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Confirmar inscrição' })).toBeNull()
  })
  it('pesquisa atividades por tema e preserva a navegação para a área do professor', async () => {
    const profile = { id: 'aluno-1', name: 'Ada', email: 'ada@example.test', course: 'DSM' }
    localStorage.setItem('ac-token', 'token-de-teste')
    localStorage.setItem('ac-student', JSON.stringify(profile))
    const fetchMock = vi.fn((url: string) => {
      if (url === '/api/auth/me') return Promise.resolve(json(profile))
      if (url === '/api/notificacoes' || url === '/api/inscricoes/minhas') return Promise.resolve(json([]))
      if (url === '/api/atividades') return Promise.resolve(json([]))
      if (url === '/api/inscricoes/minhas') return Promise.resolve(json([]))
      if (url === '/api/atividades?tema=testes') return Promise.resolve(json([activity]))
      return Promise.reject(new Error(`Rota inesperada: ${url}`))
    })
    vi.stubGlobal('fetch', fetchMock)

    renderApp()
    fireEvent.change(await screen.findByLabelText('Pesquisar por tema'), { target: { value: 'testes' } })
    fireEvent.click(screen.getByRole('button', { name: 'Buscar' }))

    expect(await screen.findByText('Oficina de testes')).toBeTruthy()
    expect(screen.queryByRole('link', { name: 'Área do professor' })).toBeNull()
  })

  it('permite criar conta, entrar e sair sem deixar a sessão armazenada', async () => {
    const fetchMock = vi.fn((url: string, init?: RequestInit) => {
      if (url === '/api/atividades') return Promise.resolve(json([]))
      if (url === '/api/auth/register' && init?.method === 'POST') return Promise.resolve(json({ id: 'aluno-1' }, 201))
      if (url === '/api/auth/login' && init?.method === 'POST') return Promise.resolve(json({
        accessToken: 'novo-token', tokenType: 'Bearer', expiresAtUtc: '2032-01-01T00:00:00Z',
        student: { id: 'aluno-1', name: 'Ada', email: 'ada@example.test', course: 'DSM', accountType: 'aluno' },
      }))
      if (url === '/api/inscricoes/minhas') return Promise.resolve(json([]))
      return Promise.reject(new Error(`Rota inesperada: ${url}`))
    })
    vi.stubGlobal('fetch', fetchMock)

    renderApp()
    fireEvent.click(await screen.findByRole('link', { name: 'Entrar' }))
    fireEvent.click(screen.getByRole('button', { name: 'Criar cadastro' }))
    fireEvent.change(screen.getByLabelText('Nome'), { target: { value: 'Ada' } })
    fireEvent.change(screen.getByLabelText('Curso'), { target: { value: 'DSM' } })
    fireEvent.change(screen.getByLabelText('E-mail'), { target: { value: 'ada@example.test' } })
    fireEvent.change(screen.getByLabelText('Senha'), { target: { value: 'Fatec!Teste2026' } })
    fireEvent.change(screen.getByLabelText('Confirmar senha'), { target: { value: 'Fatec!Teste2026' } })
    fireEvent.click(screen.getByRole('button', { name: 'Criar conta' }))
    expect(await screen.findByText('Cadastro realizado. Entre com seu e-mail e senha.')).toBeTruthy()

    fireEvent.change(screen.getByLabelText('Senha'), { target: { value: 'Fatec!Teste2026' } })
    fireEvent.click(screen.getByText('Entrar', { selector: 'button[type="submit"]' }))
    fireEvent.click(await screen.findByRole('button', { name: /Perfil de Ada, Aluno/ }))
    expect(localStorage.getItem('ac-token')).toBe('novo-token')
    fireEvent.click(screen.getByRole('button', { name: 'Sair' }))
    expect(screen.getByRole('link', { name: 'Entrar' })).toBeTruthy()
    expect(localStorage.getItem('ac-token')).toBeNull()
    expect(screen.getByRole('heading', { name: /Todo mundo tem algo para ensinar/ })).toBeTruthy()
    expect(screen.queryByLabelText('Pesquisar por tema')).toBeNull()
  })

  it('exibe apenas atividades publicadas e permite consultar seus detalhes', async () => {
    const profile = { id: 'aluno-1', name: 'Ada', email: 'ada@example.test', course: 'DSM' }
    localStorage.setItem('ac-token', 'token-de-teste')
    localStorage.setItem('ac-student', JSON.stringify(profile))
    const fetchMock = vi.fn((url: string) => {
      if (url === '/api/auth/me') return Promise.resolve(json(profile))
      if (url === '/api/notificacoes' || url === '/api/inscricoes/minhas') return Promise.resolve(json([]))
      if (url === '/api/atividades') return Promise.resolve(json([activity, { ...activity, id: 'cancelada', tema: 'Não deve aparecer', status: 'cancelada' }]))
      if (url === `/api/atividades/${activity.id}`) return Promise.resolve(json(activity))
      return Promise.reject(new Error(`Rota inesperada: ${url}`))
    })
    vi.stubGlobal('fetch', fetchMock)

    renderApp()

    expect(await screen.findByText('Oficina de testes')).toBeTruthy()
    expect(screen.queryByText('Não deve aparecer')).toBeNull()
    fireEvent.click(screen.getByRole('button', { name: 'Ver detalhes' }))

    expect(await screen.findByRole('button', { name: 'Escolher assento' })).toBeTruthy()
    expect(fetchMock).toHaveBeenCalledWith(`/api/atividades/${activity.id}`, expect.anything())
  })

  it('permite cancelar uma inscrição ativa e atualiza a lista', async () => {
    localStorage.setItem('ac-token', 'token-de-teste')
    localStorage.setItem('ac-student', JSON.stringify({ id: 'aluno-1', name: 'Ada', email: 'ada@example.test', course: 'DSM' }))
    const enrollment = {
      id: 'inscricao-1', status: 'ativa', criadaEmUtc: '2032-01-01T10:00:00Z', atualizadaEmUtc: '2032-01-01T10:00:00Z',
      atividadeId: activity.id, tipo: 'aula', tema: activity.tema, dataHora: activity.inicio, local: activity.sala.nome,
      statusAtividade: 'publicada', sala: activity.sala, assento: { id: 'a1', codigo: 'A1', fileira: 'A', coluna: 1 }, inicio: activity.inicio, fim: activity.fim,
    }
    let deleted = false
    const fetchMock = vi.fn((url: string, init?: RequestInit) => {
      if (url === '/api/atividades') return Promise.resolve(json([]))
      if (url === '/api/inscricoes/minhas') return Promise.resolve(json(deleted ? [] : [enrollment]))
      if (url === `/api/atividades/${activity.id}/inscricoes/me` && init?.method === 'DELETE') {
        deleted = true
        return Promise.resolve(new Response(null, { status: 204 }))
      }
      return Promise.reject(new Error(`Rota inesperada: ${url}`))
    })
    vi.stubGlobal('fetch', fetchMock)

    renderApp()
    fireEvent.click(await screen.findByRole('link', { name: 'Minhas inscrições' }))
    fireEvent.click(await screen.findByRole('button', { name: 'Cancelar inscrição' }))

    expect(await screen.findByText(`Inscrição em “${activity.tema}” cancelada.`)).toBeTruthy()
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(
      `/api/atividades/${activity.id}/inscricoes/me`,
      expect.objectContaining({ method: 'DELETE' }),
    ))
    expect(screen.getByText('Você ainda não possui inscrições.')).toBeTruthy()
  })

  it('carrega o mapa, reserva o assento escolhido e atualiza a disponibilidade', async () => {
    localStorage.setItem('ac-token', 'token-de-teste')
    localStorage.setItem('ac-student', JSON.stringify({ id: 'aluno-1', name: 'Ada', email: 'ada@example.test', course: 'DSM' }))
    let reserved = false
    const seatMap = () => ({
      atividadeId: activity.id, tema: activity.tema, data: activity.data, inicio: activity.inicio, fim: activity.fim,
      sala: activity.sala, capacidadeTotal: 24, lugaresDisponiveis: reserved ? 23 : 24,
      assentos: [{ id: 'a1', codigo: 'A1', fileira: 'A', coluna: 1, status: reserved ? 'OCUPADO' : 'DISPONIVEL' }],
    })
    const fetchMock = vi.fn((url: string, init?: RequestInit) => {
      if (url === '/api/atividades') return Promise.resolve(json([activity]))
      if (url === '/api/inscricoes/minhas') return Promise.resolve(json([]))
      if (url === `/api/atividades/${activity.id}`) return Promise.resolve(json(activity))
      if (url === `/api/atividades/${activity.id}/assentos`) return Promise.resolve(json(seatMap()))
      if (url === `/api/atividades/${activity.id}/inscricoes` && init?.method === 'POST') {
        reserved = true
        return Promise.resolve(json({ id: 'inscricao-1' }, 201))
      }
      return Promise.reject(new Error(`Rota inesperada: ${url}`))
    })
    vi.stubGlobal('fetch', fetchMock)

    renderApp()
    fireEvent.click(await screen.findByRole('button', { name: 'Ver detalhes' }))
    fireEvent.click(await screen.findByRole('button', { name: 'Escolher assento' }))
    fireEvent.click(await screen.findByRole('button', { name: 'Assento A1, disponível' }))
    fireEvent.click(screen.getByRole('button', { name: 'Confirmar inscrição' }))

    expect(await screen.findByText('Inscrição confirmada no assento A1.')).toBeTruthy()
    expect(fetchMock).toHaveBeenCalledWith(`/api/atividades/${activity.id}/inscricoes`, expect.objectContaining({ method: 'POST' }))
    expect(await screen.findByText('23')).toBeTruthy()
  })
})

it('pagina o catálogo e volta à primeira página ao pesquisar', async () => {
  const profile = { id: '1', name: 'Ada', email: 'ada@example.test', course: 'DSM' }
  localStorage.setItem('ac-token', 'token'); localStorage.setItem('ac-student', JSON.stringify(profile))
  const activities = Array.from({ length: 13 }, (_, i) => ({ ...activity, id: `a${i}`, titulo: `Aula ${i + 1}` }))
  vi.stubGlobal('fetch', vi.fn((url: string) => {
    if (url === '/api/auth/me') return Promise.resolve(json(profile))
    if (url === '/api/atividades') return Promise.resolve(json(activities))
    if (url === '/api/atividades?tema=testes') return Promise.resolve(json(activities.slice(0, 2)))
    return Promise.resolve(json([]))
  }))
  renderApp()
  expect(await screen.findByText('Aula 1')).toBeTruthy()
  expect(screen.queryByText('Aula 7')).toBeNull()
  fireEvent.click(screen.getByRole('button', { name: 'Próxima →' }))
  expect(screen.getByText('Aula 7')).toBeTruthy()
  expect(screen.getByText('Página 2 de 3')).toBeTruthy()
  fireEvent.change(screen.getByLabelText('Pesquisar por tema'), { target: { value: 'testes' } })
  fireEvent.click(screen.getByRole('button', { name: 'Buscar' }))
  expect(await screen.findByText('Aula 1')).toBeTruthy()
  expect(screen.queryByText('Aula 7')).toBeNull()
  expect(screen.queryByRole('navigation', { name: 'Paginação: atividades' })).toBeNull()
})
