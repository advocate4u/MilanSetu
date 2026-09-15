import { useState } from 'react'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7001'

export default function AccountSessionPanel() {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  const logout = async () => {
    setBusy(true)
    setError('')
    try {
      await fetch(`${apiBaseUrl}/api/auth/logout`, {
        method: 'POST',
        credentials: 'include',
        headers: { Authorization: `Bearer ${sessionStorage.getItem('milansetu_access_token') ?? ''}` },
      })
    } catch {
      // Clear the local session even if the server is unreachable.
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
    <button className="secondary-button" type="button" onClick={() => void logout()} disabled={busy}>
      {busy ? 'Signing out…' : 'Sign out'}
    </button>
  </section>
}
