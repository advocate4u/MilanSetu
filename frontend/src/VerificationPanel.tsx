import { useEffect, useState } from 'react'
import { getVerificationStatus, requestVerification, verifyVerificationCode, type VerificationItem, type VerificationType } from './verificationApi'
import './verification.css'

const labels: Array<{ type: VerificationType; label: string; description: string; otp: boolean }> = [
  { type: 'Email', label: 'Email address', description: 'Confirms that you control the email address on your account.', otp: true },
  { type: 'Identity', label: 'Identity', description: 'Secure review can establish an identity-verified badge without publishing documents.', otp: false },
  { type: 'Education', label: 'Education', description: 'A verified education record can help others assess profile information with more confidence.', otp: false },
  { type: 'Employment', label: 'Employment', description: 'A verified employment record can help distinguish stated career details from reviewed details.', otp: false },
]

type OtpType = 'Mobile' | 'Email'

export default function VerificationPanel() {
  const [authenticated, setAuthenticated] = useState(() => Boolean(sessionStorage.getItem('milansetu_access_token')))
  const [items, setItems] = useState<VerificationItem[]>([])
  const [busy, setBusy] = useState<VerificationType | null>(null)
  const [otpOpen, setOtpOpen] = useState<OtpType | null>(null)
  const [code, setCode] = useState('')
  const [cooldown, setCooldown] = useState(0)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  useEffect(() => {
    const syncAuth = () => setAuthenticated(Boolean(sessionStorage.getItem('milansetu_access_token')))
    const timer = window.setInterval(syncAuth, 500)
    return () => window.clearInterval(timer)
  }, [])

  useEffect(() => {
    if (!authenticated) { setItems([]); return }
    void getVerificationStatus().then(setItems).catch(e => setError(e instanceof Error ? e.message : 'Unable to load verification status.'))
  }, [authenticated])

  useEffect(() => { const timer = window.setInterval(() => setCooldown(current => Math.max(0, current - 1)), 1000); return () => window.clearInterval(timer) }, [])
  if (!authenticated) return null
  const statusFor = (type: VerificationType) => items.find(x => x.type === type)?.status ?? 'NotStarted'

  const requestOtp = async (type: OtpType) => {
    setBusy(type); setError(''); setSuccess('')
    try {
      const result = await requestVerification(type)
      setItems(current => current.some(item => item.type === type) ? current.map(item => item.type === type ? { ...item, status: 'Pending' } : item) : [...current, { type, status: 'Pending', requestedAt: new Date().toISOString(), verifiedAt: null }])
      setOtpOpen(type); setCode('')
      if ('resendAfterSeconds' in result) setCooldown(result.resendAfterSeconds)
      setSuccess(result.message)
    } catch (e) { setError(e instanceof Error ? e.message : `Unable to send the ${type.toLowerCase()} verification code.`) }
    finally { setBusy(null) }
  }

  const verifyOtp = async (type: OtpType) => {
    setBusy(type); setError(''); setSuccess('')
    try {
      const result = await verifyVerificationCode(type, code)
      setItems(current => current.map(item => item.type === 'Email' ? { ...item, status: 'Verified', verifiedAt: result.verifiedAt } : item))
      setCode(''); setOtpOpen(null); setSuccess(result.message)
    } catch (e) { setError(e instanceof Error ? e.message : `Unable to verify the ${type.toLowerCase()} code.`) }
    finally { setBusy(null) }
  }

  return <section className="verification-panel" aria-label="Profile verification">
    <div className="verification-head"><div><p className="eyebrow">TRUST & VERIFICATION</p><h2>Build trust with verification</h2><p>Verify your email or mobile number with a one-time code. Identity, education and employment requests are queued for human review. Verification documents are never published as part of your public profile.</p></div></div>
    {error && <p className="profile-error" role="alert">{error}</p>}{success && <p className="profile-success" role="status">{success}</p>}
    <div className="verification-grid">{labels.map(item => { const status = statusFor(item.type); const verified = status === 'Verified'; const isEmail = item.type === 'Email'; return <article className="verification-card" key={item.type}>
      <div className="verification-card-top"><div><h3>{item.label}</h3><p>{item.description}</p></div><span className={`verification-status verification-${status.toLowerCase()}`}>{status === 'NotStarted' ? 'Not started' : status}</span></div>
      {verified ? <strong className="verification-badge">✓ Verified</strong> : (isEmail || item.type === 'Mobile') ? <>
        <button type="button" className="secondary-button" disabled={busy !== null || cooldown > 0} onClick={() => void requestOtp(item.type as OtpType)}>{busy === item.type ? 'Sending…' : cooldown > 0 ? `Resend in ${cooldown}s` : status === 'Pending' ? 'Send code again' : `Send ${item.label.toLowerCase()} code`}</button>
        {otpOpen && <form className="verification-otp" onSubmit={e => { e.preventDefault(); void verifyOtp(item.type as OtpType) }}><label htmlFor="verification-email-code">6-digit email code</label><div className="verification-otp-row"><input id={`verification-${item.type.toLowerCase()}-code`} inputMode="numeric" autoComplete="one-time-code" maxLength={6} pattern="[0-9]{6}" value={code} onChange={e => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))} placeholder="000000" required /><button type="submit" className="primary-button" disabled={busy !== null || code.length !== 6}>{busy === item.type ? 'Verifying…' : `Verify ${item.label.toLowerCase()}`}</button></div><small>The code expires after 10 minutes. Never share it with anyone.</small></form>}
      </> : <button type="button" className="secondary-button" disabled={busy !== null || status === 'Pending'} onClick={() => void requestVerification(item.type)}>{busy === item.type ? 'Requesting…' : status === 'Pending' ? 'Under review' : 'Request verification'}</button>}
    </article>})}</div>
    <p className="verification-note">A verification badge appears only after verification is successfully completed. OTP delivery is fail-closed in production until a real SMS/email provider is configured.</p>
  </section>
}
