import { useState } from 'react'
import './account-session.css'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim()?.replace(/\/$/, '') ?? ''

export default function AccountSessionPanel() {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  const logout = async () => {
    setBusy(true)
    setError('')
    try {
      if (!apiBaseUrl) throw new Error('MilanSetu API is not configured for this deployment.')
      const response = await fetch(`${apiBaseUrl}/api/auth/logout`, {
        method: 'POST',
        credentials: 'include',
        headers: { Authorization: `Bearer ${sessionStorage.getItem('milansetu_access_token') ?? ''}` },
      })
      if (!response.ok && response.status !== 401) setError('The server could not confirm sign-out. Your local session was cleared.')
    } catch {
      setError('The server is unreachable. Your local session was still cleared.')
    } finally {
      sessionStorage.removeItem('milansetu_access_token')
      window.dispatchEvent(new Event('milansetu:auth-changed'))
      setBusy(false)
    }
  }

  return <section className="account-session-panel" aria-label="Account session">
    <div>
      <p className="eyebrow">ACCOUNT</p>
      <h2>Your private workspace</h2>
      <p>Manage your profile, photos, discovery, conversations and notifications from your signed-in workspace.</p>
      {error && <p className="discovery-error" role="alert">{error}</p>}
    </div>
    <button className="secondary-button" type="button" onClick={() => void logout()} disabled={busy} aria-busy={busy}>
      {busy ? 'Signing out…' : 'Sign out'}
    </button>
  </section>
}
