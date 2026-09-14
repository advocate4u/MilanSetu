import { useEffect, useState } from 'react'
import ProfileEditor from './ProfileEditor'
import ProfilePhotoManager from './ProfilePhotoManager'
import DiscoveryPanel from './DiscoveryPanel'
import MessagingPanel from './MessagingPanel'
import NotificationPanel from './NotificationPanel'

const accessTokenKey = 'milansetu_access_token'

function isAuthenticated() {
  return Boolean(sessionStorage.getItem(accessTokenKey))
}

export default function AuthenticatedWorkspace() {
  const [authenticated, setAuthenticated] = useState(isAuthenticated)

  useEffect(() => {
    const sync = () => setAuthenticated(isAuthenticated())
    sync()
    const timer = window.setInterval(sync, 1000)
    window.addEventListener('milansetu:auth-changed', sync)
    return () => {
      window.clearInterval(timer)
      window.removeEventListener('milansetu:auth-changed', sync)
    }
  }, [])

  if (!authenticated) return null

  return <>
    <ProfileEditor />
    <ProfilePhotoManager />
    <DiscoveryPanel />
    <MessagingPanel />
    <NotificationPanel />
  </>
}
