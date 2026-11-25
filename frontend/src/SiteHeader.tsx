import { useCallback, useEffect, useId, useRef, useState, type MouseEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from './auth'
import NotificationBell from './NotificationBell'

import { getActiveHeaderPage, getUserNavigation, type HeaderPage, type NavigationItem } from './navigation'
import useDismissiblePopover from './useDismissiblePopover'
import './SiteHeader.css'

type Panel = 'navigation' | 'profile' | 'notifications' | null

function HeaderIcon({ name }: { name: 'menu' | 'close' | 'plus' | 'chevron' | 'user' | 'logout' }) {
  const paths = {
    menu: <path d="M4 6h16M4 12h16M4 18h16" />,
    close: <path d="m6 6 12 12M6 18 18 6" />,
    plus: <path d="M12 5v14M5 12h14" />,
    chevron: <path d="m7 10 5 5 5-5" />,
    user: <><circle cx="12" cy="8" r="4" /><path d="M5 21v-2a7 7 0 0 1 14 0v2" /></>,
    logout: <><path d="M9 4H4v16h5M10 12h10m-4-4 4 4-4 4" /></>,
  }
  return <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">{paths[name]}</svg>
}

function initials(name: string) {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  return `${parts[0]?.[0] || ''}${parts.length > 1 ? parts[parts.length - 1][0] : ''}`.toLocaleUpperCase('pt-BR') || 'U'
}

export default function SiteHeader({ active, onCatalog, onEnrollments, onLogin, onLogout }: {
  active: HeaderPage
  onCatalog?: () => void
  onEnrollments?: () => void
  onLogin?: () => void
  onLogout?: () => void
}) {
  const { student, signOut } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const navigation = getUserNavigation(student)
  const selected = getActiveHeaderPage(location.pathname, active)
  const [panel, setPanel] = useState<Panel>(null)
  const navId = useId()
  const profileId = useId()
  const navContainer = useRef<HTMLDivElement>(null)
  const navTrigger = useRef<HTMLButtonElement>(null)
  const profileContainer = useRef<HTMLDivElement>(null)
  const profileTrigger = useRef<HTMLButtonElement>(null)
  const close = useCallback(() => setPanel(null), [])
  useDismissiblePopover(panel === 'navigation', close, navContainer, navTrigger)
  useDismissiblePopover(panel === 'profile', close, profileContainer, profileTrigger)

  useEffect(close, [location.pathname, student?.id, close])
  useEffect(() => {
    window.addEventListener('resize', close)
    return () => window.removeEventListener('resize', close)
  }, [close])

  const local = (handler?: () => void) => (event: MouseEvent<HTMLAnchorElement>) => {
    close()
    if (!handler || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return
    event.preventDefault()
    handler()
  }
  const renderLink = (item: NavigationItem, profile = false) => <Link
    key={item.to}
    className={profile ? 'profile-popover__link' : 'site-nav__link'}
    to={item.to}
    aria-current={selected === item.id && (!profile || location.pathname === item.to) ? 'page' : undefined}
    onClick={local(item.id === 'atividades' ? onCatalog : item.id === 'inscricoes' ? onEnrollments : undefined)}
  >{item.label}</Link>
  const logout = () => {
    close()
    signOut()
    if (onLogout) onLogout()
    else navigate('/', { replace: true })
  }

  return <header className={`site-header${student ? '' : ' site-header--guest'}`}>
    <div className="site-header__inner">
      <Link className="site-brand" to={navigation.home} aria-label="ConectaCampus — início" onClick={local(navigation.home === '/' ? onCatalog : undefined)}>
        <span className="brand-symbol" aria-hidden="true"><i /><i /><i /><i /></span>
        <span className="site-brand__name">Conecta<span>Campus</span></span>
      </Link>
      <div className="site-navigation" ref={navContainer}>
        <button className="site-header__menu-toggle" type="button" ref={navTrigger} aria-label={panel === 'navigation' ? 'Fechar navegação' : 'Abrir navegação'} aria-expanded={panel === 'navigation'} aria-controls={navId} onClick={() => setPanel(current => current === 'navigation' ? null : 'navigation')}>
          <HeaderIcon name={panel === 'navigation' ? 'close' : 'menu'} />
        </button>
        <nav className="site-nav" id={navId} aria-label="Navegação principal" data-open={panel === 'navigation'} data-popover-panel>
          <span className="site-nav__caption">Navegação</span>
          {student ? navigation.primary.map(item => renderLink(item)) : <a className="site-nav__link" href="/#como-funciona" onClick={close}>Como funciona</a>}
        </nav>
      </div>
      <div className="site-header__actions">
        {student ? <>
          {navigation.createPath && <Link className="site-header__create" to={navigation.createPath} onClick={close} aria-current={location.pathname === navigation.createPath ? 'page' : undefined}>
            <HeaderIcon name="plus" /><span>Criar aula</span>
          </Link>}
          <NotificationBell open={panel === 'notifications'} onToggle={() => setPanel(current => current === 'notifications' ? null : 'notifications')} onClose={close} />
          <div className="profile-menu" ref={profileContainer}>
            <button className="profile-trigger" type="button" ref={profileTrigger} aria-label={`Perfil de ${student.name}, ${navigation.role}`} aria-expanded={panel === 'profile'} aria-controls={profileId} onClick={() => setPanel(current => current === 'profile' ? null : 'profile')}>
              <span className="profile-avatar" aria-hidden="true">{initials(student.name)}</span>
              <span className="profile-trigger__copy"><strong>{student.name}</strong><span>{navigation.role}</span></span>
              <HeaderIcon name="chevron" />
            </button>
            {panel === 'profile' && <section className="profile-popover" id={profileId} aria-label="Opções da conta" data-popover-panel>
              <div className="profile-popover__identity"><strong>{student.name}</strong><span>{navigation.role} · {student.email}</span></div>
              {navigation.profile.map(item => renderLink(item, true))}
              <button type="button" className="profile-popover__logout" onClick={logout}><HeaderIcon name="logout" />Sair</button>
            </section>}
          </div>
        </> : <Link className="site-header__login" to="/entrar" onClick={local(onLogin)}><HeaderIcon name="user" />Entrar</Link>}
      </div>
    </div>
  </header>
}
