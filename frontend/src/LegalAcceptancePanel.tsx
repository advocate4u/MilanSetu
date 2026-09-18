import { useEffect, useState } from 'react'

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL?.trim() || '').replace(/\/$/, '')
const tokenKey = 'milansetu_access_token'

export default function LegalAcceptancePanel() {
  const [required, setRequired] = useState(true)
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState('')

  useEffect(() => {
    if (!apiBaseUrl) return
    const token = sessionStorage.getItem(tokenKey)
    if (!token) return
    fetch(`${apiBaseUrl}/api/legal/acceptance`, { headers: { Authorization: `Bearer ${token}` } })
      .then(r => r.ok ? r.json() : null)
      .then(data => { if (data) setRequired(!data.allRequiredAccepted) })
      .catch(() => {})
  }, [])

  if (!required) return null

  const accept = async () => {
    setBusy(true); setMessage('')
    try {
      const token = sessionStorage.getItem(tokenKey)
      const response = await fetch(`${apiBaseUrl}/api/legal/acceptance`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token || ''}` },
        body: JSON.stringify({ termsOfUse: true, privacyPolicy: true, personalInformationResponsibility: true, independentProfileVerification: true })
      })
      const data = await response.json().catch(() => ({}))
      if (!response.ok) throw new Error(data.message || 'Unable to save your acceptance.')
      setRequired(false)
    } catch (error) { setMessage(error instanceof Error ? error.message : 'Unable to save your acceptance.') }
    finally { setBusy(false) }
  }

  return <section className="panel legal-panel" aria-label="Required legal acknowledgements">
    <div className="panel-header"><div><p className="eyebrow">ACTION REQUIRED</p><h2>Review and accept MilanSetu terms</h2></div></div>
    <p>Before continuing, please confirm that you understand your responsibility for information you provide and your responsibility to independently verify other users.</p>
    <ul>
      <li>You are responsible for the personal and profile information you choose to provide or share.</li>
      <li>Verification features provide additional assurance but do not guarantee every profile statement or claim.</li>
      <li>MilanSetu provides a platform to help people connect and potentially find a partner; your relationship and other decisions remain your responsibility.</li>
    </ul>
    <button className="primary-button" type="button" onClick={accept} disabled={busy}>{busy ? 'Saving…' : 'I understand and accept'}</button>
    {message && <p role="alert">{message}</p>}
  </section>
}
