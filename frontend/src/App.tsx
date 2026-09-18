import { FormEvent, useState } from 'react'

const configuredApiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim()
const apiBaseUrl = configuredApiBaseUrl ? configuredApiBaseUrl.replace(/\/$/, '') : ''

type AuthMode = 'login' | 'register'

async function submitAuth(
  mode: AuthMode,
  email: string,
  password: string,
  legalAccepted: {
    terms: boolean
    privacy: boolean
    personalInformation: boolean
    verification: boolean
  }
) {
  if (!apiBaseUrl) {
    throw new Error('MilanSetu API is not configured for this deployment. Please configure VITE_API_BASE_URL.')
  }

  const response = await fetch(`${apiBaseUrl}/api/auth/${mode}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({
      email,
      password,
      acceptTerms: legalAccepted.terms,
      acceptPrivacy: legalAccepted.privacy,
      acceptPersonalInformation: legalAccepted.personalInformation,
      acceptVerification: legalAccepted.verification,
    }),
  })

  const data = await response.json().catch(() => ({}))
  if (!response.ok) {
    throw new Error(data.message ?? data.error ?? 'Unable to complete the request.')
  }

  return data
}

function App() {
  const [mode, setMode] = useState<AuthMode>('login')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [legalAccepted, setLegalAccepted] = useState({
    terms: false,
    privacy: false,
    personalInformation: false,
    verification: false,
  })
  const [message, setMessage] = useState('')
  const [busy, setBusy] = useState(false)

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setMessage('')

    if (
      mode === 'register' &&
      (!legalAccepted.terms ||
        !legalAccepted.privacy ||
        !legalAccepted.personalInformation ||
        !legalAccepted.verification)
    ) {
      setMessage('Please accept all required legal acknowledgements before creating your account.')
      setBusy(false)
      return
    }

    try {
      const result = await submitAuth(mode, email, password, legalAccepted)

      if (mode === 'login') {
        if (!result.accessToken) {
          throw new Error('Login succeeded but no access token was returned.')
        }

        sessionStorage.setItem('milansetu_access_token', result.accessToken)
        window.dispatchEvent(new Event('milansetu:auth-changed'))
      }

      setMessage(mode === 'register' ? 'Account created. You can now log in.' : 'Signed in successfully.')

      if (mode === 'register') {
        setMode('login')
        setPassword('')
      }
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Something went wrong.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="app-shell auth-page">
      <style>{`
        .auth-page {
          min-height: 100vh;
          display: flex;
          flex-direction: column;
        }

        .auth-page main {
          flex: 1;
          display: grid;
          place-items: center;
          padding: 32px 16px;
        }

        .auth-panel {
          width: min(100%, 520px);
          margin: 0 auto;
          box-sizing: border-box;
        }

        .auth-copy,
        .auth-form {
          box-sizing: border-box;
        }

        .auth-free-note {
          margin: 8px 0 0;
          line-height: 1.45;
        }

        .auth-free-note strong {
          font-weight: 800;
        }

        .legal-consents {
          display: grid;
          gap: 10px;
          margin: 14px 0;
          padding: 14px;
          border: 1px solid #eadfdb;
          border-radius: 12px;
          background: #fcfaf9;
          font-size: 13px;
        }

        .legal-consents label {
          display: flex;
          gap: 9px;
          align-items: flex-start;
          line-height: 1.4;
        }

        .legal-consents input {
          margin-top: 3px;
        }

        .legal-consents p {
          margin: 2px 0 0;
          font-size: 12px;
          opacity: .72;
          line-height: 1.45;
        }

        .auth-free-badge {
          text-align: center;
          margin: 12px 0 0;
          font-size: 12px;
          opacity: .72;
        }
      `}</style>

      <header className="topbar">
        <a className="brand" href="/" aria-label="MilanSetu home">
          <span className="brand-mark">♥</span>
          <span>MilanSetu</span>
        </a>
      </header>

      <main>
        <section className="auth-panel" aria-label="Account access">
          <div className="auth-copy">
            <p className="eyebrow">YOUR ACCOUNT</p>
            <h2>{mode === 'register' ? 'Create your profile' : 'Log in'}</h2>
            <p className="auth-free-note">
              <strong>Completely free.</strong> No subscription, no premium account, no paid messaging.
            </p>
          </div>

          <form onSubmit={onSubmit} className="auth-form">
            <label>
              Email
              <input
                type="email"
                value={email}
                onChange={event => setEmail(event.target.value)}
                autoComplete="email"
                required
                maxLength={320}
              />
            </label>

            <label>
              Password
              <input
                type="password"
                value={password}
                onChange={event => setPassword(event.target.value)}
                autoComplete={mode === 'register' ? 'new-password' : 'current-password'}
                required
                minLength={12}
              />
            </label>

            {mode === 'register' && (
              <div className="legal-consents" aria-label="Required legal acknowledgements">
                <label>
                  <input
                    type="checkbox"
                    checked={legalAccepted.terms}
                    onChange={event => setLegalAccepted(value => ({ ...value, terms: event.target.checked }))}
                  />
                  <span>I agree to the Terms of Use.</span>
                </label>

                <label>
                  <input
                    type="checkbox"
                    checked={legalAccepted.privacy}
                    onChange={event => setLegalAccepted(value => ({ ...value, privacy: event.target.checked }))}
                  />
                  <span>I acknowledge the Privacy Policy.</span>
                </label>

                <label>
                  <input
                    type="checkbox"
                    checked={legalAccepted.personalInformation}
                    onChange={event =>
                      setLegalAccepted(value => ({ ...value, personalInformation: event.target.checked }))
                    }
                  />
                  <span>
                    I am responsible for the personal and profile information I provide and choose to share.
                  </span>
                </label>

                <label>
                  <input
                    type="checkbox"
                    checked={legalAccepted.verification}
                    onChange={event =>
                      setLegalAccepted(value => ({ ...value, verification: event.target.checked }))
                    }
                  />
                  <span>
                    I will independently verify another user's identity, background and other important details
                    before making significant decisions.
                  </span>
                </label>

                <p>
                  MilanSetu provides a platform to help people connect and potentially find a partner. Users are
                  responsible for their own information, verification and decisions.
                </p>
              </div>
            )}

            <button className="primary-button" type="submit" disabled={busy}>
              {busy ? 'Please wait…' : mode === 'register' ? 'Create profile' : 'Log in'}
            </button>

            {message && (
              <p className="auth-message" role="status">
                {message}
              </p>
            )}

            <button
              type="button"
              className="text-button"
              onClick={() => {
                setMode(mode === 'register' ? 'login' : 'register')
                setMessage('')
              }}
            >
              {mode === 'register' ? 'Already have an account? Log in' : 'Create your profile'}
            </button>
          </form>

          <p className="auth-free-badge">MilanSetu is completely free for users.</p>
        </section>
      </main>
    </div>
  )
}

export default App
