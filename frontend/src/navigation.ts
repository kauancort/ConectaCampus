import type { Student } from './api'

export type HeaderPage = 'atividades' | 'inscricoes' | 'professor' | 'alunos' | 'ministrar' | 'acesso' | 'conta' | 'solicitacoes'
export type NavigationItem = { id: HeaderPage; label: string; to: string }

// As permissões vêm da API. accountType não concede acesso.
export function getUserPermissions(student: Student | null) {
  const professor = Boolean(student?.professor)
  return { professor, canTeach: professor || Boolean(student?.podeMinistrar) }
}

export function getLoginDestination(student: Student) {
  return getUserPermissions(student).professor ? '/ministrar' : '/'
}

export function getUserNavigation(student: Student | null) {
  const { professor, canTeach } = getUserPermissions(student)
  const explore: NavigationItem = { id: 'atividades', label: 'Explorar atividades', to: '/' }
  const lessons: NavigationItem = { id: 'ministrar', label: 'Minhas aulas', to: '/ministrar' }
  const enrollments: NavigationItem = { id: 'inscricoes', label: 'Minhas inscrições', to: '/inscricoes' }
  const primary: NavigationItem[] = professor
    ? [lessons, explore, { id: 'alunos', label: 'Alunos', to: '/professor/alunos' }, { id: 'solicitacoes', label: 'Solicitações de acesso', to: '/solicitacoes' }]
    : [explore, enrollments, ...(canTeach ? [lessons] : [])]
  const profile: NavigationItem[] = [{ id: 'acesso', label: 'Meu perfil', to: '/perfil' }]
  // O professor também pode participar de atividades; o atalho fica na conta.
  if (professor) profile.push(enrollments)
  if (student && !canTeach) profile.push({ id: 'acesso', label: 'Solicitar acesso para ministrar', to: '/acesso' })
  return {
    primary, profile,
    role: professor ? 'Professor' : 'Aluno',
    home: student ? getLoginDestination(student) : '/',
    createPath: canTeach ? (professor ? '/professor/atividades/nova' : '/ministrar/nova') : null,
  }
}

export function getActiveHeaderPage(pathname: string, fallback: HeaderPage): HeaderPage {
  if (pathname === '/solicitacoes') return 'solicitacoes'
  if (pathname === '/perfil' || pathname === '/acesso') return 'acesso'
  if (pathname.startsWith('/professor/alunos')) return 'alunos'
  if (pathname.startsWith('/ministrar') || pathname.startsWith('/professor/')) return 'ministrar'
  return fallback
}
