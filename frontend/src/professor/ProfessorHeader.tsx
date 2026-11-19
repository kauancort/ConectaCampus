import { NavLink } from 'react-router-dom'
import { useAuth } from '../auth'

export default function ProfessorHeader() {
  const { student } = useAuth()

  return <header className="topbar professor-header">
    <NavLink className="brand" to="/professor/atividades">ConectaCampus</NavLink>
    <nav aria-label="Navegação da área do professor">
      <NavLink className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'} end to="/professor/atividades">
        Minhas atividades
      </NavLink>
      <NavLink className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'} to="/professor/atividades/nova">
        Cadastrar atividade
      </NavLink>
    </nav>
    {student && <div className="account"><span>{student.name}</span></div>}
  </header>
}
