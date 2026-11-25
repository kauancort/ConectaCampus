// @vitest-environment jsdom
import { afterEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import AuthForm from './AuthForm'

afterEach(() => { cleanup(); vi.unstubAllGlobals() })

function register() {
  render(<AuthForm onSignIn={vi.fn()} />)
  fireEvent.click(screen.getByRole('button', { name: 'Criar cadastro' }))
}

function fillRegistration(password = 'Fatec!2026', confirmation = password) {
  fireEvent.change(screen.getByLabelText('Nome'), { target: { value: 'Ana' } })
  fireEvent.change(screen.getByLabelText('Curso'), { target: { value: 'DSM' } })
  fireEvent.change(screen.getByLabelText('E-mail'), { target: { value: 'ana@example.test' } })
  fireEvent.change(screen.getByLabelText('Senha'), { target: { value: password } })
  fireEvent.change(screen.getByLabelText('Confirmar senha'), { target: { value: confirmation } })
}

describe('acesso e cadastro', () => {
  it('explica os requisitos e impede senha fraca ou confirmação diferente', () => {
    const fetchMock = vi.fn()
    vi.stubGlobal('fetch', fetchMock)
    register(); fillRegistration('abcdef', 'outra')
    expect(screen.getByText('Um caractere especial, como ! ou @')).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Criar conta' }))
    expect(screen.getByText('Sua senha ainda não atende a todos os requisitos abaixo.')).toBeTruthy()
    expect(screen.getByText('As senhas não coincidem.')).toBeTruthy()
    expect(screen.getByLabelText('Senha').getAttribute('aria-invalid')).toBe('true')
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('indica erros de campos vazios e coloca o foco no primeiro', () => {
    register(); fireEvent.click(screen.getByRole('button', { name: 'Criar conta' }))
    expect(screen.getByText('Informe seu nome.')).toBeTruthy()
    expect(screen.getByText('Confirme sua senha.')).toBeTruthy()
    expect(document.activeElement).toBe(screen.getByLabelText('Nome'))
  })

  it('permite mostrar e ocultar as senhas', () => {
    register(); fireEvent.click(screen.getByRole('button', { name: 'Mostrar senha' }))
    expect(screen.getByLabelText('Senha').getAttribute('type')).toBe('text')
    expect(screen.getByLabelText('Confirmar senha').getAttribute('type')).toBe('text')
    fireEvent.click(screen.getByRole('button', { name: 'Ocultar senha' }))
    expect(screen.getByLabelText('Senha').getAttribute('type')).toBe('password')
  })

  it('mostra e-mail duplicado junto ao campo e conserva os dados', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ message: 'Já existe uma conta com este e-mail.' }), { status: 409 })))
    register(); fillRegistration(); fireEvent.click(screen.getByRole('button', { name: 'Criar conta' }))
    expect(await screen.findByText('Já existe uma conta com este e-mail. Entre na sua conta.')).toBeTruthy()
    expect((screen.getByLabelText('Nome') as HTMLInputElement).value).toBe('Ana')
    expect(screen.getByLabelText('E-mail').getAttribute('aria-invalid')).toBe('true')
  })

  it('impede envio duplicado enquanto aguarda resposta e mantém os dados em erro de conexão', async () => {
    let rejectRequest: (reason: Error) => void = () => {}
    const fetchMock = vi.fn().mockImplementation(() => new Promise((_resolve, reject) => { rejectRequest = reject }))
    vi.stubGlobal('fetch', fetchMock)
    register(); fillRegistration(); fireEvent.click(screen.getByRole('button', { name: 'Criar conta' }))
    expect((screen.getByRole('button', { name: 'Criando conta…' }) as HTMLButtonElement).disabled).toBe(true)
    expect(fetchMock).toHaveBeenCalledTimes(1)
    rejectRequest(new Error('offline'))
    expect(await screen.findByRole('alert')).toHaveProperty('textContent', expect.stringContaining('Não foi possível conectar à API'))
    await waitFor(() => expect((screen.getByRole('button', { name: 'Criar conta' }) as HTMLButtonElement).disabled).toBe(false))
    expect((screen.getByLabelText('E-mail') as HTMLInputElement).value).toBe('ana@example.test')
  })

  it('apresenta erros de validação enviados pela API no campo correspondente', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ errors: { PasswordTooShort: ['A senha não atende ao tamanho mínimo exigido.'] } }), { status: 400 })))
    register(); fillRegistration(); fireEvent.click(screen.getByRole('button', { name: 'Criar conta' }))
    expect(await screen.findByText('A senha não atende ao tamanho mínimo exigido.')).toBeTruthy()
    expect(screen.getByLabelText('Senha').getAttribute('aria-describedby')).toContain('auth-password-error')
  })
})
