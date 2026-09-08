import { FormEvent, useState } from 'react'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7001'

type AuthMode = 'login' | 'register'

async function submitAuth(mode: AuthMode, email: string, password: string) {
  const response = await fetch(`${apiBaseUrl}/api/auth/${mode}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ email, password }),
  })

  const data = await response.json().catch(() => ({}))
  if (!response.ok) throw new Error(data.message ?? 'Unable to complete the request.')
  return data
}

function App() {
  const [mode, setMode] = useState<AuthMode>('register')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [message, setMessage] = useState('')
  const [busy, setBusy] = useState(false)

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setMessage('')
    try {
      const result = await submitAuth(mode, email, password)
      setMessage(mode === 'register' ? 'Account created. You can now log in.' : 'Signed in successfully.')
      if (mode === 'login') sessionStorage.setItem('milansetu_access_token', result.accessToken)
      if (mode === 'register') setMode('login')
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Something went wrong.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <a className="brand" href="/" aria-label="MilanSetu home">
          <span className="brand-mark">♥</span>
          <span>MilanSetu</span>
        </a>
        <nav className="nav-links" aria-label="Main navigation">
          <a href="#how-it-works">How it works</a>
          <a href="#safety">Safety</a>
          <a href="#about">About</a>
          <button className="login-button" onClick={() => setMode('login')}>Log in</button>
          <button className="primary-button small" onClick={() => setMode('register')}>Create profile</button>
        </nav>
      </header>

      <main>
        <section className="hero">
          <div className="hero-copy">
            <p className="eyebrow">FREE • PRIVATE • MEANINGFUL</p>
            <h1>Find someone to build a life with.</h1>
            <p className="hero-text">
              MilanSetu is a free, privacy-first matrimonial platform built around
              compatibility, trust and meaningful connections.
            </p>
            <div className="hero-actions">
              <button className="primary-button" onClick={() => setMode('register')}>Create your free profile</button>
              <button className="secondary-button" onClick={() => document.getElementById('how-it-works')?.scrollIntoView({ behavior: 'smooth' })}>Explore how it works</button>
            </div>
            <p className="no-paywall">No subscription • No premium profile • No paid messaging</p>
          </div>

          <div className="hero-card" aria-label="MilanSetu values">
            <div className="heart-orbit">♥</div>
            <h2>A better way to meet.</h2>
            <div className="value-row"><span>✓</span><div><strong>Compatibility first</strong><small>Preferences you control.</small></div></div>
            <div className="value-row"><span>✓</span><div><strong>Privacy by design</strong><small>Your contact details stay private.</small></div></div>
            <div className="value-row"><span>✓</span><div><strong>Safety built in</strong><small>Report, block and moderation tools.</small></div></div>
          </div>
        </section>

        <section className="auth-panel" aria-label="Account access">
          <div className="auth-copy">
            <p className="eyebrow">YOUR ACCOUNT</p>
            <h2>{mode === 'register' ? 'Create a free account' : 'Welcome back'}</h2>
            <p>Your refresh session stays in a protected HttpOnly cookie. Your password is never stored in the browser.</p>
          </div>
          <form onSubmit={onSubmit} className="auth-form">
            <label>Email<input type="email" value={email} onChange={e => setEmail(e.target.value)} autoComplete="email" required maxLength={320} /></label>
            <label>Password<input type="password" value={password} onChange={e => setPassword(e.target.value)} autoComplete={mode === 'register' ? 'new-password' : 'current-password'} required minLength={12} /></label>
            <button className="primary-button" type="submit" disabled={busy}>{busy ? 'Please wait…' : mode === 'register' ? 'Create account' : 'Log in'}</button>
            {message && <p className="auth-message" role="status">{message}</p>}
            <button type="button" className="text-button" onClick={() => { setMode(mode === 'register' ? 'login' : 'register'); setMessage('') }}>
              {mode === 'register' ? 'Already have an account? Log in' : 'Need an account? Create one'}
            </button>
          </form>
        </section>

        <section className="principles" id="how-it-works">
          <div><span>01</span><h3>Tell us about you</h3><p>Build a profile around your values, family, lifestyle and marriage expectations.</p></div>
          <div><span>02</span><h3>Set your preferences</h3><p>Choose what matters to you — from city and language to community and lifestyle.</p></div>
          <div><span>03</span><h3>Connect by mutual choice</h3><p>Express interest, accept, and start a conversation without exposing your phone number.</p></div>
        </section>

        <section className="safety-banner" id="safety">
          <div><p className="eyebrow">SAFETY FIRST</p><h2>Respectful connections, protected users.</h2></div>
          <p>Abuse detection, reporting, blocking, scam signals and human moderation will be part of the platform from the beginning.</p>
        </section>
      </main>

      <footer id="about"><span>♥ MilanSetu</span><span>Built for meaningful connections.</span></footer>
    </div>
  )
}

export default App
