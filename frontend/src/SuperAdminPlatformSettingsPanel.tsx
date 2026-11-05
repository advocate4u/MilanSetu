import { useEffect, useState } from 'react'

type PlatformSettings = { messagingEnabled: boolean; maxProfilePhotos: number }

async function api(path: string, init?: RequestInit) {
  const token = sessionStorage.getItem('milansetu_access_token')
  const response = await fetch(path, {
    ...init,
    headers: { 'Content-Type': 'application/json', Authorization: 'Bearer ' + token, ...(init?.headers || {}) },
  })
  if (!response.ok) {
    const detail = await response.json().catch(() => null)
    throw new Error(detail?.message || 'Request failed.')
  }
  return response.json()
}

export default function SuperAdminPlatformSettingsPanel() {
  const [role, setRole] = useState('')
  const [settings, setSettings] = useState<PlatformSettings | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [message, setMessage] = useState('')
  const [loadError, setLoadError] = useState('')

  useEffect(() => {
    let active = true
    void (async () => {
      try {
        const me = await api('/api/auth/me')
        if (!active) return
        setRole(me.role || '')
        if (me.role === 'SuperAdmin') {
          const data = await api('/api/admin/platform-settings')
          if (active) setSettings(data)
        }
      } catch (error) {
        if (active && role === 'SuperAdmin') {
          setLoadError(error instanceof Error ? error.message : 'Unable to load platform settings.')
        }
      } finally {
        if (active) setLoading(false)
      }
    })()
    return () => { active = false }
  }, [])

  if (role !== 'SuperAdmin') return null
  if (loading) return <section className="panel" aria-label="Platform controls"><p role="status">Loading platform settings…</p></section>
  if (!settings) return <section className="panel" aria-label="Platform controls"><h2>Platform controls</h2><p role="alert">{loadError || 'Platform settings could not be loaded.'}</p></section>

  const save = async () => {
    setSaving(true)
    setMessage('')
    try {
      await api('/api/admin/platform-settings', { method: 'PUT', body: JSON.stringify(settings) })
      setMessage('Platform settings saved.')
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Unable to save settings.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <section className="panel" aria-label="Platform controls">
      <h2>Platform controls</h2>
      <p>Control messaging and the maximum number of profile photos per user.</p>
      <label>
        <input
          type="checkbox"
          checked={settings.messagingEnabled}
          onChange={event => { setSettings({ ...settings, messagingEnabled: event.target.checked }); setMessage('') }}
        />
        Enable messaging
      </label>
      <label>
        Maximum profile photos per user
        <input
          type="number"
          min={1}
          max={20}
          step={1}
          value={settings.maxProfilePhotos}
          onChange={event => {
            const value = Number(event.target.value)
            if (Number.isInteger(value) && value >= 1 && value <= 20) {
              setSettings({ ...settings, maxProfilePhotos: value })
              setMessage('')
            }
          }}
        />
      </label>
      <button className="primary-button" type="button" disabled={saving} onClick={() => void save()}>
        {saving ? 'Saving…' : 'Save platform settings'}
      </button>
      {message && <p role="status" aria-live="polite">{message}</p>}
    </section>
  )
}
