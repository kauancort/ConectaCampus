import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../auth'

export default function ProfessorRoute() {
  const { token, student, checkingSession } = useAuth()
  const location = useLocation()
  if (!token) return <Navigate to="/login" replace state={{ from: location.pathname }} />
  if (checkingSession) return <main className="main-content"><p role="status">Verificando sua conta…</p></main>
  if (student?.accountType !== 'professor') return <Navigate to="/" replace />
  return <Outlet />
}
