import { useEffect, useState } from 'react'
import { getVerificationStatus, requestVerification, verifyVerificationCode, type VerificationItem, type VerificationType } from './verificationApi'
import './verification.css'

const labels: Array<{ type: VerificationType; label: string; description: string; otp: boolean }> = [
  { type: 'Mobile', label: 'Mobile number', description: 'Confirms that you control the mobile number on your account.', otp: true },
  { type: 'Email', label: 'Email address', description: 'Confirms that you control the email address on your account.', otp: true },
  { type: 'Identity', label: 'Identity', description: 'Secure review can establish an identity-verified badge without publishing documents.', otp: false },
  { type: 'Education', label: 'Education', description: 'A verified education record can help others assess profile information with more confidence.', otp: false },
  { type: 'Employment', label: 'Employment', description: 'A verified employment record can help distinguish stated career details from reviewed details.', otp: false },
]

type OtpType = 'Mobile' | 'Email'

export default function VerificationPanel() {
  const [items, setItems] = useState<VerificationItem[]>([])
  const [busy, setBusy] = useState<VerificationType | null>(null)
  const [otpType, setOtpType] = useState<OtpType | null>(null)
  const [codes, setCodes] = useState<Record<OtpType, string>>({ Mobile: '', Email: '' })
  const [cooldowns, setCooldowns] = useState<Record<OtpType, number>>({ Mobile: 0, Email: 0 })
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  useEffect(() => { if (sessionStorage.getItem('milansetu_access_token')) void getVerificationStatus().then(setItems).catch(e => setError(e instanceof Error ? e.message : 'Unable to load verification status.')) }, [])
  useEffect(() => { const timer = window.setInterval(() => setCooldowns(current => ({ Mobile: Math.max(0, current.Mobile - 1), Email: Math.max(0, current.Email - 1) })), 1000); return () => window.clearInterval(timer) }, [])
  if (!sessionStorage.getItem('milansetu_access_token')) return null
  const statusFor = (type: VerificationType) => items.find(x => x.type === type)?.status ?? 'NotStarted'

  const request = async (type: VerificationType) => {
    setBusy(type); setError(''); setSuccess('')
    try {
      const result = await requestVerification(type)
      setItems(current => current.some(item => item.type === type) ? current.map(item => item.type === type ? { ...item, status: 'Pending' } : item) : [...current, { type, status: 'Pending', requestedAt: new Date().toISOString(), verifiedAt: null }])
      if (type === 'Mobile' || type === 'Email') { setOtpType(type); if ('resendAfterSeconds' in result) setCooldowns(current => ({ ...current, [type]: result.resendAfterSeconds })) }
      setSuccess(result?.message ?? 'Verification request submitted.')
    } catch (e) { setError(e instanceof Error ? e.message : 'Unable to request verification.') } finally { setBusy(null) }
  }

  const verify = async (type: OtpType) => {
    setBusy(type); setError(''); setSuccess('')
    try { const result = await verifyVerificationCode(type, codes[type]); setItems(current => current.map(item => item.type === type ? { ...item, status: 'Verified', verifiedAt: result.verifiedAt } : item)); setCodes(current => ({ ...current, [type]: '' })); setOtpType(null); setSuccess(result.message) }
    catch (e) { setError(e instanceof Error ? e.message : 'Unable to verify the code.') } finally { setBusy(null) }
  }

  return <section className="verification-panel" aria-label="Profile verification"><div className="verification-head"><div><p className="eyebrow">TRUST & VERIFICATION</p><h2>Build confidence, privately</h2><p>Mobile and email ownership can now be verified with a one-time code. Verification documents are never published as part of your public profile.</p></div></div>{error && <p className="profile-error" role="alert">{error}</p>}{success && <p className="profile-success" role="status">{success}</p>}<div className="verification-grid">{labels.map(item => { const status = statusFor(item.type); const verified = status === 'Verified'; const otp = item.otp && (item.type === 'Mobile' || item.type === 'Email') ? item.type : null; return <article className="verification-card" key={item.type}><div className="verification-card-top"><div><h3>{item.label}</h3><p>{item.description}</p></div><span className={`verification-status verification-${status.toLowerCase()}`}>{status === 'NotStarted' ? 'Not started' : status}</span></div>{verified ? <strong className="verification-badge">✓ Verified</strong> : otp ? <><button type="button" className="secondary-button" disabled={busy !== null || cooldowns[otp] > 0} onClick={() => void request(otp)}>{busy === otp ? 'Sending…' : cooldowns[otp] > 0 ? `Resend in ${cooldowns[otp]}s` : status === 'Pending' ? 'Send code again' : 'Send verification code'}</button>{otpType === otp && <form className="verification-otp" onSubmit={e => { e.preventDefault(); void verify(otp) }}><label htmlFor={`verification-code-${otp}`}>6-digit code</label><div className="verification-otp-row"><input id={`verification-code-${otp}`} inputMode="numeric" autoComplete="one-time-code" maxLength={6} pattern="[0-9]{6}" value={codes[otp]} onChange={e => setCodes(current => ({ ...current, [otp]: e.target.value.replace(/\D/g, '').slice(0, 6) }))} placeholder="000000" required /><button type="submit" className="primary-button" disabled={busy !== null || codes[otp].length !== 6}>{busy === otp ? 'Verifying…' : 'Verify'}</button></div><small>The code expires after 10 minutes. Never share it with anyone.</small></form>}</> : <button type="button" className="secondary-button" disabled={busy !== null || status === 'Pending'} onClick={() => void request(item.type)}>{busy === item.type ? 'Requesting…' : status === 'Pending' ? 'Under review' : 'Request verification'}</button>}</article> })}</div><p className="verification-note">Verification badges only appear after a verification is actually completed. A request or uploaded document alone does not create a verified badge.</p></section>
}
