import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'
import AuthenticatedWorkspace from './AuthenticatedWorkspace'
import './styles.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
    <AuthenticatedWorkspace />
  </StrictMode>,
)
