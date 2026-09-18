import { FormEvent, useMemo, useState } from 'react'

const configuredApiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim()
const apiBaseUrl = configuredApiBaseUrl ? configuredApiBaseUrl.replace(/\/$/, '') : ''
type AuthMode = 'login' | 'register'
type DemoProfile = { id: number; name: string; age: number; city: string; profession: string; education: string; bio: string; tags: string[] }
type DemoMessage = { id: number; sender: 'me' | 'them'; body: string; time: string }

const demoProfiles: DemoProfile[] = [
  { id: 1, name: 'Asha', age: 29, city: 'Delhi', profession: 'Software Engineer', education: 'B.Tech', bio: 'Enjoys reading, travel and building a calm family life.', tags: ['Vegetarian', 'Hindi', 'Travel'] },
  { id: 2, name: 'Rahul', age: 31, city: 'Gurugram', profession: 'Product Manager', education: 'MBA', bio: 'Values open communication, family time and a balanced career.', tags: ['Fitness', 'Hindi', 'Music'] },
  { id: 3, name: 'Neha', age: 28, city: 'Chandigarh', profession: 'Doctor', education: 'MBBS', bio: 'Warm, independent and looking for a respectful partnership.', tags: ['Punjabi', 'Reading', 'Pets'] },
  { id: 4, name: 'Arjun', age: 30, city: 'Noida', profession: 'Architect', education: 'B.Arch', bio: 'Creative professional who enjoys design, food and weekend trips.', tags: ['Travel', 'Hindi', 'Design'] },
]

const starterMessages: DemoMessage[] = [
  { id: 1, sender: 'them', body: 'Hi! Thanks for connecting. What kind of life are you hoping to build together?', time: '10:24 AM' },
  { id: 2, sender: 'me', body: 'A respectful partnership with good communication, family time and room for both careers.', time: '10:27 AM' },
]

async function submitAuth(mode: AuthMode, email: string, password: string) {
  if (!apiBaseUrl) throw new Error('MilanSetu API is not configured for this deployment. Please configure VITE_API_BASE_URL.')
  const response = await fetch(`${apiBaseUrl}/api/auth/${mode}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, credentials: 'include', body: JSON.stringify({ email, password, acceptTerms: legalAccepted.terms, acceptPrivacy: legalAccepted.privacy, acceptPersonalInformation: legalAccepted.personalInformation, acceptVerification: legalAccepted.verification }) })
  const data = await response.json().catch(() => ({}))
  if (!response.ok) throw new Error(data.message ?? data.error ?? 'Unable to complete the request.')
  return data
}

function App() {
  const [mode, setMode] = useState<AuthMode>('register')
  const [email, setEmail] = useState(''); const [password, setPassword] = useState('')
  const [legalAccepted, setLegalAccepted] = useState({ terms: false, privacy: false, personalInformation: false, verification: false })
  const [message, setMessage] = useState(''); const [busy, setBusy] = useState(false)
  const [demo, setDemo] = useState(false); const [city, setCity] = useState('All cities'); const [minAge, setMinAge] = useState(18)
  const [selected, setSelected] = useState<DemoProfile | null>(null); const [interest, setInterest] = useState('')
  const [connected, setConnected] = useState<DemoProfile | null>(null); const [messages, setMessages] = useState<DemoMessage[]>(starterMessages); const [draft, setDraft] = useState('')

  const filtered = useMemo(() => demoProfiles.filter(p => (city === 'All cities' || p.city === city) && p.age >= minAge), [city, minAge])

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault(); setBusy(true); setMessage('')
    if (mode === 'register' && (!legalAccepted.terms || !legalAccepted.privacy || !legalAccepted.personalInformation || !legalAccepted.verification)) { setMessage('Please accept all required legal acknowledgements before creating your account.'); setBusy(false); return }
    try {
      const result = await submitAuth(mode, email, password)
      if (mode === 'login') {
        if (!result.accessToken) throw new Error('Login succeeded but no access token was returned.')
        sessionStorage.setItem('milansetu_access_token', result.accessToken)
        window.dispatchEvent(new Event('milansetu:auth-changed'))
      }
      setMessage(mode === 'register' ? 'Account created. You can now log in.' : 'Signed in successfully.')
      if (mode === 'register') setMode('login')
    } catch (error) { setMessage(error instanceof Error ? error.message : 'Something went wrong.') }
    finally { setBusy(false) }
  }

  const expressInterest = (profile: DemoProfile) => { setInterest(`Interest sent to ${profile.name} in demo mode.`); setConnected(profile); setMessages(starterMessages) }
  const sendDemoMessage = (event: FormEvent) => { event.preventDefault(); const body = draft.trim(); if (!body || !connected) return; setMessages(current => [...current, { id: Date.now(), sender: 'me', body, time: 'Now' }]); setDraft('') }

  return <div className="app-shell">
    <style>{`.auth-page{min-height:100vh;display:flex;flex-direction:column}.auth-page main{flex:1;display:grid;place-items:center;padding:32px 16px}.auth-panel{width:min(100%,520px);margin:0 auto}.auth-panel,.auth-copy,.auth-form{box-sizing:border-box}.auth-free-note{margin:8px 0 0;line-height:1.45}.auth-free-note strong{font-weight:800}.legal-consents{display:grid;gap:10px;margin:14px 0;padding:14px;border:1px solid #eadfdb;border-radius:12px;background:#fcfaf9;font-size:13px}.legal-consents label{display:flex;gap:9px;align-items:flex-start;line-height:1.4}.legal-consents input{margin-top:3px}.legal-consents p{margin:2px 0 0;font-size:12px;opacity:.72;line-height:1.45}.auth-free-badge{text-align:center;margin-top:12px;font-size:12px;opacity:.72}`}</style>
    <header className="topbar">
      <a className="brand" href="/" aria-label="MilanSetu home"><span className="brand-mark">♥</span><span>MilanSetu</span></a>
    </header>
    <main>
      <section className="auth-panel" aria-label="Account access">
        <div className="auth-copy">
          <p className="eyebrow">YOUR ACCOUNT</p>
          <h2>{mode === 'register' ? 'Create your profile' : 'Log in'}</h2>
          <p className="auth-free-note"><strong>Completely free.</strong> No subscription, no premium account, no paid messaging.</p>
        </div>
        <form onSubmit={onSubmit} className="auth-form">
          <label>Email<input type="email" value={email} onChange={e => setEmail(e.target.value)} autoComplete="email" required maxLength={320}/></label>
          <label>Password<input type="password" value={password} onChange={e => setPassword(e.target.value)} autoComplete={mode === 'register' ? 'new-password' : 'current-password'} required minLength={12}/></label>
          {mode === 'register' && <div className="legal-consents" aria-label="Required legal acknowledgements">
            <label><input type="checkbox" checked={legalAccepted.terms} onChange={e => setLegalAccepted(v => ({ ...v, terms: e.target.checked }))} /> I agree to the Terms of Use.</label>
            <label><input type="checkbox" checked={legalAccepted.privacy} onChange={e => setLegalAccepted(v => ({ ...v, privacy: e.target.checked }))} /> I acknowledge the Privacy Policy.</label>
            <label><input type="checkbox" checked={legalAccepted.personalInformation} onChange={e => setLegalAccepted(v => ({ ...v, personalInformation: e.target.checked }))} /> I am responsible for the personal and profile information I provide and choose to share.</label>
            <label><input type="checkbox" checked={legalAccepted.verification} onChange={e => setLegalAccepted(v => ({ ...v, verification: e.target.checked }))} /> I will independently verify another user's identity, background and other important details before making significant decisions.</label>
            <p>MilanSetu provides a platform to help people connect and potentially find a partner. Users are responsible for their own information, verification and decisions.</p>
          </div>}
          <button className="primary-button" type="submit" disabled={busy}>{busy ? 'Please wait…' : mode === 'register' ? 'Create profile' : 'Log in'}</button>
          {message && <p className="auth-message" role="status">{message}</p>}
          <button type="button" className="text-button" onClick={() => { setMode(mode === 'register' ? 'login' : 'register'); setMessage('') }}>{mode === 'register' ? 'Already have an account? Log in' : 'Need an account? Create your profile'}</button>
        </form>
        <p className="auth-free-badge">MilanSetu is completely free for users.</p>
      </section>
    </main>
  </div>
}
export default App
