import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useAuth } from '../auth'
import SiteHeader from '../SiteHeader'
import Pagination, { usePagination } from '../Pagination'

type Student = { id: string; nome: string; email: string; curso: string; podeMinistrar: boolean }
type Activity = { id: string; titulo: string; tema: string; dataHoraUtc: string; status: string }
type Profile = { aluno: Student; inscricoes: Activity[]; ministradas: Activity[] }

function Permission({ allowed }: { allowed: boolean }) {
  return <span className={`student-permission ${allowed ? 'allowed' : ''}`}><span aria-hidden="true">{allowed ? '✓' : '○'}</span>{allowed ? 'Pode ministrar' : 'Não autorizado a ministrar'}</span>
}

function ActivityList({ title, items, empty }: { title: string; items: Activity[]; empty: string }) {
  return <section className="workspace-panel student-activities"><div className="panel-heading"><h2>{title}</h2><span className="count-label">{items.length}</span></div>{items.length === 0 ? <p>{empty}</p> : items.map(item => <article className="student-activity" key={item.id}><div><h3>{item.titulo || item.tema}</h3><p>{new Date(item.dataHoraUtc).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })} · {item.tema}</p></div><span className="tag">{item.status}</span></article>)}</section>
}

export default function StudentsPage() {
  const { id } = useParams()
  const { api } = useAuth()
  const [students, setStudents] = useState<Student[]>([])
  const [profile, setProfile] = useState<Profile | null>(null)
  const [search, setSearch] = useState('')
  const [filter, setFilter] = useState('todos')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [attempt, setAttempt] = useState(0)
  const filtered = students.filter(student => `${student.nome} ${student.curso} ${student.email}`.toLocaleLowerCase('pt-BR').includes(search.trim().toLocaleLowerCase('pt-BR')) && (filter === 'todos' || student.podeMinistrar === (filter === 'autorizados')))
  const pages = usePagination(filtered, 9)

  useEffect(() => {
    let active = true
    setLoading(true); setError(''); setProfile(null)
    const path = id ? `/api/professor/alunos/${encodeURIComponent(id)}` : '/api/professor/alunos'
    api<Profile | Student[]>(path).then(data => { if (active) { if (id) setProfile(data as Profile); else setStudents(data as Student[]) } }).catch(e => { if (active) setError(e instanceof Error ? e.message : 'Não foi possível carregar os dados.') }).finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [api, id, attempt])

  return <div className="app-shell"><SiteHeader active="alunos" /><main className="main-content workspace"><div className="workspace-heading"><div><h1>{id ? 'Perfil do aluno' : 'Alunos' }<span className="heading-period">.</span></h1><p>{id ? 'Acompanhe as atividades e a autorização para ministrar.' : 'Conheça os alunos e acompanhe sua participação no campus.'}</p></div></div>
    {id && <Link className="text-button back-button students-back" to="/professor/alunos">← Voltar para alunos</Link>}
    {loading ? <div className="empty workspace-loading" role="status">Carregando…</div> : error ? <div className="feedback error" role="alert">{error} <button className="text-button" onClick={() => setAttempt(value => value + 1)}>Tentar novamente</button></div> : id && profile ? <><section className="workspace-panel student-profile"><div><h2>{profile.aluno.nome}</h2><p>{profile.aluno.curso} · {profile.aluno.email}</p></div><Permission allowed={profile.aluno.podeMinistrar} /></section><div className="student-profile-grid"><ActivityList title="Inscrições ativas" items={profile.inscricoes} empty="Nenhuma inscrição ativa." /><ActivityList title="Aulas que ministra" items={profile.ministradas} empty="Nenhuma aula ministrada." /></div></> : <><div className="student-filters"><label>Buscar aluno<input value={search} onChange={e => { setSearch(e.target.value); pages.setPage(1) }} placeholder="Nome, curso ou e-mail" /></label><label>Autorização<select value={filter} onChange={e => { setFilter(e.target.value); pages.setPage(1) }}><option value="todos">Todos os alunos</option><option value="autorizados">Podem ministrar</option><option value="nao-autorizados">Não autorizados</option></select></label></div><div className="section-row"><h2>Alunos cadastrados</h2><span>{filtered.length} {filtered.length === 1 ? 'aluno' : 'alunos'}</span></div>{filtered.length === 0 ? <div className="empty student-list-empty">Nenhum aluno encontrado.</div> : <div className="student-list">{pages.items.map(student => <Link className="student-card" to={`/professor/alunos/${encodeURIComponent(student.id)}`} key={student.id}><span className="student-avatar" aria-hidden="true">{student.nome.trim().charAt(0).toUpperCase()}</span><span className="student-card-main"><strong>{student.nome}</strong><span>{student.curso}</span><Permission allowed={student.podeMinistrar} /></span><span className="student-card-arrow" aria-hidden="true">↗</span></Link>)}</div>}<Pagination label="alunos" {...pages} /></>}
  </main></div>
}
