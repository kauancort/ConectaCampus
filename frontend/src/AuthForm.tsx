import { useRef, useState, type FormEvent } from 'react'
import { ApiError, apiRequest, type LoginResponse } from './api'

type Mode = 'login' | 'register'
type Field = 'name' | 'course' | 'email' | 'password' | 'confirmPassword'
type Values = Record<Field, string>
type Errors = Partial<Record<Field, string>>

const passwordRules = [
  { label: 'Pelo menos 6 caracteres', test: (value: string) => value.length >= 6 },
  { label: 'Uma letra maiúscula e uma minúscula', test: (value: string) => /[A-Z]/.test(value) && /[a-z]/.test(value) },
  { label: 'Um número', test: (value: string) => /[0-9]/.test(value) },
  { label: 'Um caractere especial, como ! ou @', test: (value: string) => /[^a-zA-Z0-9]/.test(value) },
]

function validate(values: Values, mode: Mode): Errors {
  const errors: Errors = {}
  if (mode === 'register') {
    if (!values.name.trim()) errors.name = 'Informe seu nome.'
    if (!values.course.trim()) errors.course = 'Informe seu curso.'
  }
  if (!values.email.trim()) errors.email = 'Informe seu e-mail.'
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.email.trim())) errors.email = 'Informe um e-mail válido.'
  if (!values.password) errors.password = 'Informe sua senha.'
  else if (mode === 'register' && !passwordRules.every(rule => rule.test(values.password))) errors.password = 'Sua senha ainda não atende a todos os requisitos abaixo.'
  if (mode === 'register') {
    if (!values.confirmPassword) errors.confirmPassword = 'Confirme sua senha.'
    else if (values.password !== values.confirmPassword) errors.confirmPassword = 'As senhas não coincidem.'
  }
  return errors
}

export default function AuthForm({ onSignIn, initialMessage, initialMode = 'login' }: { onSignIn: (response: LoginResponse) => void; initialMessage?: string; initialMode?: Mode }) {
  const [mode, setMode] = useState<Mode>(initialMode)
  const [values, setValues] = useState<Values>({ name: '', course: '', email: '', password: '', confirmPassword: '' })
  const [errors, setErrors] = useState<Errors>({})
  const [message, setMessage] = useState(initialMessage ?? '')
  const [success, setSuccess] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const formRef = useRef<HTMLFormElement>(null)

  function update(field: Field, value: string) {
    setValues(current => ({ ...current, [field]: value }))
    setErrors(current => ({ ...current, [field]: undefined, ...(field === 'password' ? { confirmPassword: undefined } : {}) }))
    setMessage(''); setSuccess('')
  }
  function changeMode(next: Mode) {
    setMode(next); setErrors({}); setMessage(''); setSuccess(''); setShowPassword(false)
    setValues(current => ({ ...current, password: '', confirmPassword: '' }))
  }
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (submitting) return
    setMessage(''); setSuccess('')
    const validation = validate(values, mode)
    setErrors(validation)
    if (Object.keys(validation).length) {
      const first = (['name', 'course', 'email', 'password', 'confirmPassword'] as Field[]).find(field => validation[field])
      if (first) formRef.current?.elements.namedItem(first) instanceof HTMLElement && (formRef.current.elements.namedItem(first) as HTMLElement).focus()
      return
    }
    setSubmitting(true)
    try {
      if (mode === 'register') {
        await apiRequest('/api/auth/register', { method: 'POST', body: JSON.stringify({ name: values.name.trim(), course: values.course.trim(), email: values.email.trim(), password: values.password }) })
        setMode('login'); setValues(current => ({ ...current, password: '', confirmPassword: '' })); setShowPassword(false)
        setSuccess('Cadastro realizado. Entre com seu e-mail e senha.')
      } else {
        const result = await apiRequest<LoginResponse>('/api/auth/login', { method: 'POST', body: JSON.stringify({ email: values.email.trim(), password: values.password }) })
        onSignIn(result)
      }
    } catch (error) {
      if (error instanceof ApiError && mode === 'register' && error.status === 409) {
        setErrors({ email: 'Já existe uma conta com este e-mail. Entre na sua conta.' })
      } else if (error instanceof ApiError && mode === 'register' && error.status === 400 && Object.keys(error.errors).length) {
        const mapped: Errors = {}
        for (const [key, messages] of Object.entries(error.errors)) {
          const field = key.toLowerCase()
          const target: Field | undefined = field.startsWith('password') ? 'password' : (['name', 'course', 'email'].includes(field) ? field as Field : undefined)
          if (target && Array.isArray(messages)) mapped[target] = [mapped[target], ...messages].filter(Boolean).join(' ')
        }
        if (Object.keys(mapped).length) setErrors(mapped)
        else setMessage(error.message)
      } else setMessage(error instanceof Error ? error.message : 'Não foi possível concluir. Tente novamente.')
    } finally { setSubmitting(false) }
  }

  return <>
    <div className="tabs"><button className={mode === 'login' ? 'tab active' : 'tab'} type="button" disabled={submitting} onClick={() => changeMode('login')}>Entrar</button><button className={mode === 'register' ? 'tab active' : 'tab'} type="button" disabled={submitting} onClick={() => changeMode('register')}>Criar cadastro</button></div>
    <div className="auth-title"><h2>{mode === 'login' ? 'Que bom ter você aqui.' : 'Faça parte da comunidade.'}</h2><p>{mode === 'login' ? 'Entre com os dados da sua conta.' : 'Preencha seus dados para começar.'}</p></div>
    {message && <div className="auth-message error" role="alert">{message}</div>}
    {success && <div className="auth-message success" role="status">{success}</div>}
    <form ref={formRef} onSubmit={event => void submit(event)} className="form-grid auth-form" noValidate>
      <fieldset disabled={submitting} className="auth-fields">
      {mode === 'register' && <>
        <div className="auth-field"><label htmlFor="auth-name">Nome</label><input id="auth-name" name="name" autoComplete="name" maxLength={200} value={values.name} onChange={event => update('name', event.target.value)} aria-invalid={!!errors.name} aria-describedby={errors.name ? 'auth-name-error' : undefined} />{errors.name && <span className="auth-field-error" id="auth-name-error">{errors.name}</span>}</div>
        <div className="auth-field"><label htmlFor="auth-course">Curso</label><input id="auth-course" name="course" maxLength={120} value={values.course} onChange={event => update('course', event.target.value)} aria-invalid={!!errors.course} aria-describedby={errors.course ? 'auth-course-error' : undefined} />{errors.course && <span className="auth-field-error" id="auth-course-error">{errors.course}</span>}</div>
      </>}
      <div className="auth-field"><label htmlFor="auth-email">E-mail</label><input id="auth-email" name="email" type="email" autoComplete="email" maxLength={256} value={values.email} onChange={event => update('email', event.target.value)} aria-invalid={!!errors.email} aria-describedby={errors.email ? 'auth-email-error' : undefined} />{errors.email && <span className="auth-field-error" id="auth-email-error">{errors.email}</span>}</div>
      <div className="auth-field"><label htmlFor="auth-password">Senha</label><div className="password-input"><input id="auth-password" name="password" type={showPassword ? 'text' : 'password'} autoComplete={mode === 'login' ? 'current-password' : 'new-password'} value={values.password} onChange={event => update('password', event.target.value)} aria-invalid={!!errors.password} aria-describedby={[errors.password ? 'auth-password-error' : '', mode === 'register' ? 'password-requirements' : ''].filter(Boolean).join(' ') || undefined} /><button type="button" className="password-toggle" aria-label={showPassword ? 'Ocultar senha' : 'Mostrar senha'} aria-pressed={showPassword} onClick={() => setShowPassword(!showPassword)}>{showPassword ? 'Ocultar' : 'Mostrar'}</button></div>
        {errors.password && <span className="auth-field-error" id="auth-password-error">{errors.password}</span>}
        {mode === 'register' && <div className="password-requirements" id="password-requirements"><span>Sua senha precisa ter:</span><ul>{passwordRules.map(rule => <li key={rule.label} className={values.password && rule.test(values.password) ? 'met' : ''}><span aria-hidden="true">{values.password && rule.test(values.password) ? '✓' : '○'}</span> {rule.label}</li>)}</ul></div>}
      </div>
      {mode === 'register' && <div className="auth-field"><label htmlFor="auth-confirm">Confirmar senha</label><input id="auth-confirm" name="confirmPassword" type={showPassword ? 'text' : 'password'} autoComplete="new-password" value={values.confirmPassword} onChange={event => update('confirmPassword', event.target.value)} aria-invalid={!!errors.confirmPassword} aria-describedby={errors.confirmPassword ? 'auth-confirm-error' : undefined} />{errors.confirmPassword && <span className="auth-field-error" id="auth-confirm-error">{errors.confirmPassword}</span>}</div>}
      </fieldset>
      <button className="button primary auth-submit" type="submit" disabled={submitting}>{submitting ? (mode === 'login' ? 'Entrando…' : 'Criando conta…') : mode === 'login' ? 'Entrar' : 'Criar conta'}</button>
    </form>
  </>
}
