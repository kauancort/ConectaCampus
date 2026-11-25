import SiteFooter from '../SiteFooter'
import { Link } from 'react-router-dom'
import { useAuth } from '../auth'
import type { ReactNode } from 'react'
import SiteHeader from '../SiteHeader'

export default function ProtectedProfessor({ children }: { children: ReactNode }) {
  const { student } = useAuth()
  if (!student?.professor) return <div className="app-shell"><SiteHeader active="professor" /><main className="main-content workspace"><div className="workspace-heading"><div><h1>Área do professor<span className="heading-period">.</span></h1><p>Esta área é exclusiva para professores autorizados.</p></div></div><section className="workspace-panel restricted-panel"><p>Entre na sua conta para continuar ou acompanhe suas solicitações de acesso.</p><Link className="intro-link" to="/acesso">Solicitar acesso ou entrar <span aria-hidden="true">↗</span></Link></section></main><SiteFooter /></div>
  return children
}
