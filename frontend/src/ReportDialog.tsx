import { FormEvent, useState } from 'react'
import { submitReport, type ReportReason } from './safetyApi'

const reasons: Array<{ value: ReportReason; label: string }> = [
  { value: 'Abuse', label: 'Abusive language' },
  { value: 'Harassment', label: 'Harassment' },
  { value: 'Scam', label: 'Scam or asking for money' },
  { value: 'Impersonation', label: 'Fake or impersonated profile' },
  { value: 'InappropriateContent', label: 'Inappropriate content' },
  { value: 'Other', label: 'Other' },
]

export default function ReportDialog({ reportedUserId, onClose }: { reportedUserId: string; onClose: () => void }) {
  const [reason, setReason] = useState<ReportReason>('Abuse')
  const [details, setDetails] = useState('')
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState('')

  const submit = async (event: FormEvent) => {
    event.preventDefault(); setBusy(true); setMessage('')
    try { await submitReport(reportedUserId, reason, details); setMessage('Report submitted for review.'); setTimeout(onClose, 700) }
    catch (error) { setMessage(error instanceof Error ? error.message : 'Unable to submit the report.') }
    finally { setBusy(false) }
  }

  return <div className="ms-report-backdrop" role="presentation">
    <form className="ms-report-dialog" onSubmit={submit} role="dialog" aria-modal="true" aria-labelledby="report-title">
      <div className="ms-report-head"><div><p className="eyebrow">SAFETY</p><h2 id="report-title">Report this profile</h2></div><button type="button" onClick={onClose} disabled={busy}>Close</button></div>
      <p className="ms-report-note">Choose the closest reason. Only the information needed to review the report is collected.</p>
      <label>Reason<select value={reason} onChange={e => setReason(e.target.value as ReportReason)} disabled={busy}>{reasons.map(x => <option key={x.value} value={x.value}>{x.label}</option>)}</select></label>
      <label>Details <span className="ms-report-counter">{details.length}/2000</span><textarea value={details} onChange={e => setDetails(e.target.value)} maxLength={2000} rows={5} placeholder="Tell us what happened, if useful." disabled={busy}/></label>
      {message && <p className="ms-report-message" role="status">{message}</p>}
      <div className="ms-report-actions"><button type="button" onClick={onClose} disabled={busy}>Cancel</button><button className="primary-button" type="submit" disabled={busy}>{busy ? 'Submitting…' : 'Submit report'}</button></div>
    </form>
    <style>{`.ms-report-backdrop{position:fixed;inset:0;z-index:50;display:grid;place-items:center;padding:18px;background:rgba(30,20,18,.35)}.ms-report-dialog{width:min(520px,100%);padding:22px;border:1px solid #eadfdb;border-radius:20px;background:#fff;box-shadow:0 20px 60px rgba(0,0,0,.18)}.ms-report-head{display:flex;justify-content:space-between;gap:12px;align-items:flex-start}.ms-report-dialog h2{margin:0 0 8px}.ms-report-dialog label{display:block;margin-top:14px;font-size:13px;font-weight:600}.ms-report-dialog select,.ms-report-dialog textarea{display:block;width:100%;box-sizing:border-box;margin-top:6px;padding:11px 12px;border:1px solid #d8cbc6;border-radius:10px;background:#fff;font:inherit}.ms-report-dialog textarea{resize:vertical}.ms-report-note{font-size:13px;opacity:.75}.ms-report-counter{float:right;font-weight:400;opacity:.6}.ms-report-message{padding:10px;border-radius:10px;background:#f5f0ed}.ms-report-actions{display:flex;justify-content:flex-end;gap:8px;margin-top:18px}`}</style>
  </div>
}
