import { NavLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth'

export default function ProfessorHeader() {
  const { student, signOut } = useAuth()
  const navigate = useNavigate()

  return <header className="topbar professor-header">
    <NavLink className="brand" to="/professor/atividades">ConectaCampus</NavLink>
    <nav aria-label="Navegação da área do professor">
      <NavLink className="nav-link" to="/">Catálogo</NavLink>
      <NavLink className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'} end to="/professor/atividades">
        Minhas atividades
      </NavLink>
      <NavLink className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'} to="/professor/atividades/nova">
        Cadastrar atividade
      </NavLink>
    </nav>
    {student && <div className="account"><span>{student.name}</span><button className="button secondary small" type="button" onClick={() => { signOut(); navigate('/login') }}>Sair</button></div>}
  </header>
}
