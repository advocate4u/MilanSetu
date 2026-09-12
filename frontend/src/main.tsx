import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'
import DiscoveryPanel from './DiscoveryPanel'
import NotificationPanel from './NotificationPanel'
import ProfileEditor from './ProfileEditor'
import './styles.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
    <ProfileEditor />
    <DiscoveryPanel />
    <NotificationPanel />
  </StrictMode>,
)
