import { useEffect, useState } from 'react'

type Settings = { enabled: boolean; requireMutualMatch: boolean; sharePhone: boolean; shareEmail: boolean; shareWhatsApp: boolean }
type RequestItem = { id: string; requesterUserId?: string; recipientUserId?: string; status: string; createdAt?: string }
type Requests = { incoming: RequestItem[]; outgoing: RequestItem[] }

async function api(path: string, init?: RequestInit) {
  const token = sessionStorage.getItem('milansetu_access_token')
  const response = await fetch(path, {
    ...init,
    headers: { 'Content-Type': 'application/json', Authorization: 'Bearer ' + token, ...(init?.headers || {}) },
  })
  const data = await response.json().catch(() => ({}))
  if (!response.ok) throw new Error(data.message || 'Request failed.')
  return data
}

export default function ContactSharingPanel() {
  const [settings, setSettings] = useState<Settings | null>(null)
  const [requests, setRequests] = useState<Requests | null>(null)
  const [message, setMessage] = useState('')
  const [busyId, setBusyId] = useState<string | null>(null)

  const load = async () => {
    try {
      const [nextSettings, nextRequests] = await Promise.all([
        api('/api/contact-sharing/settings'),
        api('/api/contact-sharing/requests'),
      ])
      setSettings(nextSettings)
      setRequests(nextRequests)
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Unable to load contact-sharing requests.')
    }
  }

  useEffect(() => { void load() }, [])

  const respond = async (id: string, action: 'accept' | 'decline') => {
    if (busyId) return
    setBusyId(id)
    setMessage('')
    try {
      await api('/api/contact-sharing/requests/' + id + '/' + action, { method: 'POST' })
      setMessage(action === 'accept' ? 'Contact sharing accepted.' : 'Contact sharing declined.')
      await load()
      window.dispatchEvent(new Event('milansetu:connections-changed'))
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Request failed.')
    } finally {
      setBusyId(null)
    }
  }

  if (!settings?.enabled) return null
  const incoming = (requests?.incoming || []).filter(item => item.status === 'Pending')
  const outgoing = requests?.outgoing || []

  return <section className="panel">
    <h2>Contact sharing</h2>
    <p>Contact details stay private until the required consent is completed.</p>
    <h3>Requests received</h3>
    {incoming.length === 0 ? <p>No pending contact-share requests.</p> : incoming.map(item =>
      <div key={item.id} style={{ display: 'flex', justifyContent: 'space-between', gap: 12, alignItems: 'center', padding: '10px 0', borderBottom: '1px solid #eee' }}>
        <span>Someone you matched with requested your contact details.</span>
        <span>
          <button disabled={busyId !== null} onClick={() => void respond(item.id, 'accept')}>{busyId === item.id ? 'Please wait…' : 'Accept'}</button>
          <button disabled={busyId !== null} onClick={() => void respond(item.id, 'decline')}>Decline</button>
        </span>
      </div>
    )}
    <h3>Requests you sent</h3>
    {outgoing.length === 0 ? <p>You have not requested anyone's contact details.</p> : outgoing.map(item =>
      <div key={item.id} style={{ padding: '9px 0', borderBottom: '1px solid #eee' }}>
        <strong>{item.status === 'Pending' ? 'Awaiting response' : item.status === 'Accepted' ? 'Request accepted' : 'Request declined'}</strong>
        <p style={{ margin: '4px 0', fontSize: 13 }}>Sent {item.createdAt ? new Date(item.createdAt).toLocaleDateString() : 'recently'}</p>
      </div>
    )}
    {message && <small role="status" aria-live="polite">{message}</small>}
  </section>
}
