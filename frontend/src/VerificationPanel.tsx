import { useEffect, useState } from 'react'
import { getVerificationStatus, requestVerification, type VerificationItem, type VerificationType } from './verificationApi'

const labels: Array<{ type: VerificationType; label: string; description: string }> = [
  { type: 'Mobile', label: 'Mobile number', description: 'Confirms that you control the mobile number on your account.' },
  { type: 'Email', label: 'Email address', description: 'Confirms that you control the email address on your account.' },
  { type: 'Identity', label: 'Identity', description: 'Secure review can establish an identity-verified badge without publishing documents.' },
  { type: 'Education', label: 'Education', description: 'A verified education record can help others assess profile information with more confidence.' },
  { type: 'Employment', label: 'Employment', description: 'A verified employment record can help distinguish stated career details from reviewed details.' },
]

export default function VerificationPanel() {
  const [items, setItems] = useState<VerificationItem[]>([])
  const [busy, setBusy] = useState<VerificationType | null>(null)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  useEffect(() => {
    if (!sessionStorage.getItem('milansetu_access_token')) return
    void getVerificationStatus().then(setItems).catch(e => setError(e instanceof Error ? e.message : 'Unable to load verification status.'))
  }, [])

  if (!sessionStorage.getItem('milansetu_access_token')) return null

  const statusFor = (type: VerificationType) => items.find(x => x.type === type)?.status ?? 'NotStarted'

  const request = async (type: VerificationType) => {
    setBusy(type); setError(''); setSuccess('')
    try {
      const result = await requestVerification(type)
      setItems(current => current.map(item => item.type === type ? { ...item, status: 'Pending' } : item))
      if (!items.some(x => x.type === type)) setItems(current => [...current, { type, status: 'Pending', requestedAt: new Date().toISOString(), verifiedAt: null }])
      setSuccess(result?.message ?? 'Verification request submitted.')
    } catch (e) { setError(e instanceof Error ? e.message : 'Unable to request verification.') }
    finally { setBusy(null) }
  }

  return <section className="verification-panel" aria-label="Profile verification">
    <div className="verification-head"><div><p className="eyebrow">TRUST & VERIFICATION</p><h2>Build confidence, privately</h2><p>Verification is separate from profile visibility. Verification documents are never published as part of your public profile.</p></div></div>
    {error && <p className="profile-error" role="alert">{error}</p>}
    {success && <p className="profile-success" role="status">{success}</p>}
    <div className="verification-grid">
      {labels.map(item => {
        const status = statusFor(item.type)
        const verified = status === 'Verified'
        return <article className="verification-card" key={item.type}>
          <div className="verification-card-top"><div><h3>{item.label}</h3><p>{item.description}</p></div><span className={`verification-status verification-${status.toLowerCase()}`}>{status === 'NotStarted' ? 'Not started' : status}</span></div>
          {verified ? <strong className="verification-badge">✓ Verified</strong> : <button type="button" className="secondary-button" disabled={busy !== null || status === 'Pending'} onClick={() => void request(item.type)}>{busy === item.type ? 'Requesting…' : status === 'Pending' ? 'Under review' : 'Request verification'}</button>}
        </article>
      })}
    </div>
    <p className="verification-note">Verification badges only appear after a verification is actually completed. A request or uploaded document alone does not create a verified badge.</p>
  </section>
}
