import { Link, useLocation } from 'react-router-dom'
import { useAuth } from './auth'
import { getUserPermissions } from './navigation'

// Gestão das próprias aulas e supervisão compartilham a mesma área do produto.
const items = [
  { to: '/ministrar', label: 'Aulas que ministro' },
  { to: '/professor/atividades', label: 'Propostas para avaliar' },
  { to: '/professor/aulas-aprovadas', label: 'Aulas que aprovei' },
]

export default function TeachingNavigation({ pendingCount, approvedCount }: { pendingCount?: number; approvedCount?: number }) {
  const { student } = useAuth()
  const { pathname } = useLocation()
  if (!getUserPermissions(student).professor) return null
  return <nav className="supervision-nav" aria-label="Gestão de aulas">
    {items.map(item => <Link key={item.to} to={item.to} aria-current={pathname === item.to ? 'page' : undefined}>
      {item.label}
      {item.to === '/professor/atividades' && pendingCount !== undefined && <span>{pendingCount}</span>}
      {item.to === '/professor/aulas-aprovadas' && approvedCount !== undefined && <span>{approvedCount}</span>}
    </Link>)}
  </nav>
}
