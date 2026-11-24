import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth'
import App from './App'
import ActivityRegistrationPage from './professor/ActivityRegistrationPage'
import MyActivitiesPage from './professor/MyActivitiesPage'
import ProfessorRoute from './professor/ProfessorRoute'
import './style.css'

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
