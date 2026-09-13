import { useEffect, useState } from 'react'
import ReviewerPanel from './ReviewerPanel'
import ProfileEditor from './ProfileEditor'
import ProfilePhotoManager from './ProfilePhotoManager'
import VerificationPanel from './VerificationPanel'
import DiscoveryPanel from './DiscoveryPanel'
import MessagingPanel from './MessagingPanel'
import NotificationPanel from './NotificationPanel'

export default function AuthenticatedWorkspace() {
  const [authenticated, setAuthenticated] = useState(() => Boolean(sessionStorage.getItem('milansetu_access_token')))

  useEffect(() => {
    const sync = () => setAuthenticated(Boolean(sessionStorage.getItem('milansetu_access_token')))
    sync()
    const timer = window.setInterval(sync, 500)
    return () => window.clearInterval(timer)
  }, [])

  if (!authenticated) return null

  return <>
    <ProfileEditor />
    <ProfilePhotoManager />
    <VerificationPanel />
    <DiscoveryPanel />
    <MessagingPanel />
    <NotificationPanel />
    <ReviewerPanel />
  </>
}
