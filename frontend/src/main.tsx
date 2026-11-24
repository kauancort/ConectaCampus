import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter, Navigate, Outlet, Route, Routes, useLocation } from 'react-router-dom'
import { AuthProvider, useAuth } from './auth'
import App from './App'
import ActivityRegistrationPage from './professor/ActivityRegistrationPage'
import MyActivitiesPage from './professor/MyActivitiesPage'
import './style.css'

function ProfessorRoute() {
  const { token, student } = useAuth()
  const location = useLocation()
  if (!token) return <Navigate to="/login" replace state={{ from: location.pathname }} />
  if (student?.accountType !== 'professor') return <Navigate to="/" replace />
  return <Outlet />
}

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route element={<ProfessorRoute />}>
            <Route path="/professor/atividades" element={<MyActivitiesPage />} />
            <Route path="/professor/atividades/nova" element={<ActivityRegistrationPage />} />
          </Route>
          <Route path="*" element={<App />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  </React.StrictMode>,
)
