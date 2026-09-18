import { useEffect, useState } from 'react'
import { getPrivacy, savePrivacy, type PrivacyVisibility } from './privacyApi'

export default function PrivacyPanel() {
  const [visibility, setVisibility] = useState<PrivacyVisibility>('MembersOnly')
  const [blocked, setBlocked] = useState(0)
  const [message, setMessage] = useState('')
  const [busy, setBusy] = useState(false)
  const load = async () => { try { const p = await getPrivacy(); setVisibility(p.visibility); setBlocked(p.blockedProfiles); setMessage('') } catch (e) { setMessage(e instanceof Error ? e.message : 'Unable to load privacy settings.') } }
  useEffect(() => { void load() }, [])
  const save = async () => { setBusy(true); setMessage(''); try { await savePrivacy(visibility); setMessage('Privacy settings saved.') } catch (e) { setMessage(e instanceof Error ? e.message : 'Unable to save privacy settings.') } finally { setBusy(false) } }
  return <section className="profile-editor" aria-label="Privacy settings"><div className="profile-editor-head"><div><p className="eyebrow">PRIVACY</p><h2>Control who can discover you</h2><p>Contact details and verification documents are never public.</p></div></div><div className="profile-fields"><label>Profile visibility<select value={visibility} onChange={e => setVisibility(e.target.value as PrivacyVisibility)}><option value="MembersOnly">Members only</option><option value="Public">Public</option><option value="Hidden">Hidden from discovery</option></select></label></div><p>Blocked profiles: <strong>{blocked}</strong></p>{message && <p role="status">{message}</p>}<button className="primary-button" onClick={() => void save()} disabled={busy}>{busy ? 'Saving…' : 'Save privacy settings'}</button></section>
}