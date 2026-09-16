import { useEffect, useState } from 'react'
import App from './App'
import AuthenticatedWorkspace from './AuthenticatedWorkspace'

const accessTokenKey = 'milansetu_access_token'

function hasSession() {
  return Boolean(sessionStorage.getItem(accessTokenKey))
}

export default function AppShell() {
  const [authenticated, setAuthenticated] = useState(hasSession)

  useEffect(() => {
    const sync = () => setAuthenticated(hasSession())
    sync()
    window.addEventListener('milansetu:auth-changed', sync)
    const timer = window.setInterval(sync, 250)
    return () => {
      window.clearInterval(timer)
      window.removeEventListener('milansetu:auth-changed', sync)
    }
  }, [])

  return authenticated ? <AuthenticatedWorkspace /> : <App />
}
