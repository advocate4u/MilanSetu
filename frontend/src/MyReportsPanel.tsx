import { useCallback, useEffect, useState } from 'react'
import { getMyReports, type MyReport } from './safetyApi'

const labels: Record<string, string> = {
  Abuse: 'Abusive language',
  Harassment: 'Harassment',
  Scam: 'Scam or asking for money',
  Impersonation: 'Fake or impersonated profile',
  InappropriateContent: 'Inappropriate content',
  Other: 'Other',
}

export default function MyReportsPanel() {
  const [reports, setReports] = useState<MyReport[]>([])
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    setBusy(true); setError('')
    try { setReports(await getMyReports()) }
    catch (e) { setError(e instanceof Error ? e.message : 'Unable to load your reports.') }
    finally { setLoading(false); setBusy(false) }
  }, [])

  useEffect(() => {
    void load()
    const refresh = () => void load()
    window.addEventListener('milansetu:notifications-changed', refresh)
    return () => window.removeEventListener('milansetu:notifications-changed', refresh)
  }, [load])

  return <section className="safety-reports-panel" aria-labelledby="my-reports-title">
    <div className="safety-reports-head">
      <div><p className="eyebrow">SAFETY</p><h2 id="my-reports-title">My reports</h2><p>Track the safety reports you have submitted. Review outcomes are shown here when available.</p></div>
      <button className="secondary-button" type="button" onClick={() => void load()} disabled={busy}>{busy ? 'Refreshing…' : 'Refresh'}</button>
    </div>
    {loading ? <p className="profile-empty">Loading your reports…</p> :
      error ? <p className="profile-error" role="alert">{error}</p> :
      reports.length === 0 ? <p className="profile-empty">You have not submitted any reports.</p> :
      <div className="safety-reports-list">{reports.map(report => <article className="safety-report-card" key={report.id}>
        <div><strong>{labels[report.reason] ?? report.reason}</strong><span className={'safety-report-status safety-report-status-' + report.status.toLowerCase()}>{report.status}</span></div>
        <p>Submitted {new Date(report.createdAt).toLocaleString()}</p>
        {report.resolvedAt && <small>Updated {new Date(report.resolvedAt).toLocaleString()}</small>}
      </article>)}</div>}
    <style>{'.safety-reports-panel{max-width:1180px;margin:30px auto 70px;padding:30px;border:1px solid #eaded7;border-radius:24px;background:#fffdfa;box-shadow:0 20px 60px rgba(60,42,32,.06)}.safety-reports-head{display:flex;justify-content:space-between;gap:24px;align-items:flex-end;margin-bottom:22px}.safety-reports-head h2{margin:0 0 8px;font-size:clamp(28px,4vw,42px);letter-spacing:-.04em}.safety-reports-head p:not(.eyebrow){margin:0;color:#716965;line-height:1.6;max-width:760px}.safety-reports-list{display:grid;gap:10px}.safety-report-card{padding:16px 18px;border:1px solid #eee3df;border-radius:14px;background:white}.safety-report-card>div{display:flex;justify-content:space-between;gap:12px;align-items:center}.safety-report-card p,.safety-report-card small{display:block;margin:7px 0 0;color:#716965;font-size:13px}.safety-report-status{padding:4px 9px;border-radius:999px;background:#f1e8e2;font-size:12px;font-weight:800;text-transform:capitalize}.safety-report-status-resolved{background:#edf8ef;color:#28633a}.safety-report-status-dismissed{background:#f3eeee;color:#7a6565}@media(max-width:800px){.safety-reports-panel{margin:20px 12px 60px;padding:18px}.safety-reports-head{display:block}.safety-reports-head>button{margin-top:14px}.safety-report-card>div{align-items:flex-start}}'}</style>
  </section>
}
