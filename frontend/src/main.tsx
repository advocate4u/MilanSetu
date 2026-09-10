import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'
import NotificationPanel from './NotificationPanel'
import './styles.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
    <NotificationPanel />
  </StrictMode>,
)
