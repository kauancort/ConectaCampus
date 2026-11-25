import { useEffect, useRef, useState, type MouseEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from './auth'
import NotificationBell from './NotificationBell'

type ActivePage = 'atividades' | 'inscricoes' | 'professor' | 'alunos' | 'ministrar' | 'acesso' | 'conta'

export default function SiteHeader({ active, onCatalog, onEnrollments, onLogin, onLogout }: {
  active: ActivePage
  onCatalog?: () => void
  onEnrollments?: () => void
  onLogin?: () => void
  onLogout?: () => void
}) {
  const { student, signOut } = useAuth()
  const navigate = useNavigate()
  const [menuOpen, setMenuOpen] = useState(false)
  const menu = useRef<HTMLDivElement>(null)
  const trigger = useRef<HTMLButtonElement>(null)
  useEffect(() => {
    if (!menuOpen) return
    const outside = (event: PointerEvent) => { if (event.target instanceof Node && !menu.current?.contains(event.target)) setMenuOpen(false) }
    const escape = (event: KeyboardEvent) => { if (event.key === 'Escape') { setMenuOpen(false); trigger.current?.focus() } }
    document.addEventListener('pointerdown', outside)
    document.addEventListener('keydown', escape)
    return () => { document.removeEventListener('pointerdown', outside); document.removeEventListener('keydown', escape) }
  }, [menuOpen])
  const local = (handler?: () => void) => (event: MouseEvent<HTMLAnchorElement>) => {
    if (!handler || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return
    event.preventDefault(); handler()
  }
  const navClass = (page: ActivePage) => page === active ? 'nav-link active' : 'nav-link'
  const logout = () => { signOut(); if (onLogout) onLogout(); else navigate('/') }

  return <header className="topbar">
    <Link className="brand" to="/" onClick={local(onCatalog)}><span className="brand-symbol" aria-hidden="true"><i /><i /><i /><i /></span><span>Conecta<span className="brand-campus">Campus</span></span></Link>
    <nav aria-label="Navegação principal">
      {student ? <Link className={navClass('atividades')} to="/" onClick={local(onCatalog)} aria-current={active === 'atividades' ? 'page' : undefined}>Atividades</Link> : <a className="nav-link" href="/#como-funciona">Como funciona</a>}
      {student?.professor && <Link className={navClass('professor')} to="/professor/atividades" aria-current={active === 'professor' ? 'page' : undefined}>Área do professor</Link>}
      {student?.professor && <Link className={navClass('alunos')} to="/professor/alunos" aria-current={active === 'alunos' ? 'page' : undefined}>Alunos</Link>}
      {student && <div className="area-menu" ref={menu}>
        <button ref={trigger} type="button" className={`nav-link area-trigger ${['inscricoes', 'ministrar', 'acesso'].includes(active) ? 'active' : ''}`} aria-expanded={menuOpen} aria-controls="area-menu" onClick={() => setMenuOpen(open => !open)}>Minha área <span aria-hidden="true">⌄</span></button>
        {menuOpen && <div className="area-dropdown" id="area-menu">
          <p>MINHA ÁREA</p>
          <Link to="/inscricoes" onClick={event => { setMenuOpen(false); local(onEnrollments)(event) }} aria-current={active === 'inscricoes' ? 'page' : undefined}>Minhas inscrições <span aria-hidden="true">↗</span></Link>
          {student.podeMinistrar && !student.professor && <Link to="/ministrar" onClick={() => setMenuOpen(false)} aria-current={active === 'ministrar' ? 'page' : undefined}>Minhas aulas <span aria-hidden="true">↗</span></Link>}
          <Link to="/perfil" onClick={() => setMenuOpen(false)} aria-current={active === 'acesso' ? 'page' : undefined}>Meu perfil <span aria-hidden="true">↗</span></Link>
          {student.professor && <Link to="/solicitacoes" onClick={() => setMenuOpen(false)}>Solicitações de acesso <span aria-hidden="true">↗</span></Link>}
        </div>}
      </div>}
    </nav>
    <div className="account">{student ? <><NotificationBell /><span title={student.name}>{student.name}</span><button className="button secondary small" type="button" onClick={logout}>Sair</button></> : <Link className="button primary small header-login" to="/entrar" onClick={local(onLogin)}>Entrar</Link>}</div>
  </header>
}
