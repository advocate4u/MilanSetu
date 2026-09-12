import { useEffect, useState } from 'react'
import { getVerificationStatus, requestVerification, sendVerificationCode, verifyCode, type VerificationItem, type VerificationType } from './verificationApi'
import './verification.css'

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
  const [codeFor, setCodeFor] = useState<'Mobile' | 'Email' | null>(null)
  const [code, setCode] = useState('')
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  const load = async () => {
    try { setItems(await getVerificationStatus()) }
    catch (e) { setError(e instanceof Error ? e.message : 'Unable to load verification status.') }
  }

  useEffect(() => {
    if (sessionStorage.getItem('milansetu_access_token')) void load()
  }, [])

  if (!sessionStorage.getItem('milansetu_access_token')) return null

  const statusFor = (type: VerificationType) => items.find(x => x.type === type)?.status ?? 'NotStarted'

  const startOtp = async (type: 'Mobile' | 'Email') => {
    setBusy(type); setError(''); setSuccess('')
    try {
      await sendVerificationCode(type)
      setCodeFor(type); setCode(''); setSuccess(`A ${type === 'Mobile' ? 'mobile' : 'email'} verification code has been sent. It expires in 10 minutes.`)
      await load()
    } catch (e) { setError(e instanceof Error ? e.message : 'Verification delivery is unavailable.') }
    finally { setBusy(null) }
  }

  const submitCode = async () => {
    if (!codeFor) return
    setBusy(codeFor); setError(''); setSuccess('')
    try {
      const result = await verifyCode(codeFor, code)
      setSuccess(result?.message ?? 'Verification completed successfully.')
      setCodeFor(null); setCode(''); await load()
    } catch (e) { setError(e instanceof Error ? e.message : 'The verification code is invalid or expired.') }
    finally { setBusy(null) }
  }

  const request = async (type: VerificationType) => {
    if (type === 'Mobile' || type === 'Email') return startOtp(type)
    setBusy(type); setError(''); setSuccess('')
    try {
      const result = await requestVerification(type)
      setItems(current => current.map(item => item.type === type ? { ...item, status: 'Pending' } : item))
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
          {verified ? <strong className="verification-badge">✓ Verified</strong> : item.type === codeFor ? <div className="verification-code-form"><input aria-label={`${item.label} verification code`} inputMode="numeric" autoComplete="one-time-code" maxLength={6} value={code} onChange={e => setCode(e.target.value.replace(/\D/g, ''))} placeholder="6-digit code" /><button type="button" className="secondary-button" disabled={busy !== null || code.length !== 6} onClick={() => void submitCode()}>{busy === item.type ? 'Verifying…' : 'Verify code'}</button></div> : <button type="button" className="secondary-button" disabled={busy !== null || status === 'Pending'} onClick={() => void request(item.type)}>{busy === item.type ? 'Sending…' : status === 'Pending' ? 'Under review' : item.type === 'Mobile' || item.type === 'Email' ? 'Send verification code' : 'Request verification'}</button>}
        </article>
      })}
    </div>
    <p className="verification-note">Codes are never returned by the API or written to logs. A verification badge appears only after the server confirms the challenge.</p>
  </section>
}
