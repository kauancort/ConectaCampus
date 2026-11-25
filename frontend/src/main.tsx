import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth'
import App from './App'
<<<<<<< HEAD
import { StudentProposalsPage, TeacherProposalsPage } from './PropostasPage'
import AccessPage from './AccessPage'
import ProtectedProfessor from './professor/ProtectedProfessor'
import SupervisedActivityPage from './professor/SupervisedActivityPage'
import StudentsPage from './professor/StudentsPage'
=======
import ActivityRegistrationPage from './professor/ActivityRegistrationPage'
import MyActivitiesPage from './professor/MyActivitiesPage'
import ProfessorRoute from './professor/ProfessorRoute'
>>>>>>> 1524e16cfcfef7673dca4642fc242c4617009a43
import './style.css'

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <Routes>
<<<<<<< HEAD
          <Route path="/professor/alunos/:id" element={<ProtectedProfessor><StudentsPage /></ProtectedProfessor>} />
          <Route path="/professor/alunos" element={<ProtectedProfessor><StudentsPage /></ProtectedProfessor>} />
          <Route path="/professor/aulas/:id" element={<ProtectedProfessor><SupervisedActivityPage /></ProtectedProfessor>} />
          <Route path="/professor/atividades" element={<ProtectedProfessor><TeacherProposalsPage /></ProtectedProfessor>} />
          <Route path="/professor/aulas-aprovadas" element={<ProtectedProfessor><TeacherProposalsPage /></ProtectedProfessor>} />
          <Route path="/professor/atividades/nova" element={<ProtectedProfessor><TeacherProposalsPage /></ProtectedProfessor>} />
          <Route path="/ministrar" element={<StudentProposalsPage />} />
          <Route path="/ministrar/aulas/:id" element={<SupervisedActivityPage instructor />} />
          <Route path="/ministrar/nova" element={<StudentProposalsPage />} />
          <Route path="/acesso" element={<AccessPage />} />
          <Route path="/perfil" element={<AccessPage />} />
          <Route path="/solicitacoes" element={<AccessPage />} />
=======
          <Route element={<ProfessorRoute />}>
            <Route path="/professor/atividades" element={<MyActivitiesPage />} />
            <Route path="/professor/atividades/nova" element={<ActivityRegistrationPage />} />
          </Route>
>>>>>>> 1524e16cfcfef7673dca4642fc242c4617009a43
          <Route path="*" element={<App />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  </React.StrictMode>,
)
