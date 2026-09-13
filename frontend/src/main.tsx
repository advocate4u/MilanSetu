import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'
import DiscoveryPanel from './DiscoveryPanel'
import MessagingPanel from './MessagingPanel'
import NotificationPanel from './NotificationPanel'
import ProfileEditor from './ProfileEditor'
import ProfilePhotoManager from './ProfilePhotoManager'
import VerificationPanel from './VerificationPanel'
import ReviewerPanel from './ReviewerPanel'
import './styles.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
    <ReviewerPanel />
    <ProfileEditor />
    <ProfilePhotoManager />
    <VerificationPanel />
    <DiscoveryPanel />
    <MessagingPanel />
    <NotificationPanel />
  </StrictMode>,
)
