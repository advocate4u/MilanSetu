import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'
import DiscoveryPanel from './DiscoveryPanel'
import NotificationPanel from './NotificationPanel'
import ProfileEditor from './ProfileEditor'
import ProfilePhotoManager from './ProfilePhotoManager'
import MessagingPanel from './MessagingPanel'
import './styles.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
    <ProfileEditor />
    <ProfilePhotoManager />
    <DiscoveryPanel />
    <MessagingPanel />
    <NotificationPanel />
  </StrictMode>,
)
