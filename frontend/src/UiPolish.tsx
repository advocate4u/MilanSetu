import { useEffect, useState, type ReactNode } from 'react'

export function AppShell({ children }: { children: ReactNode }) {
  const [online, setOnline] = useState(navigator.onLine)

  useEffect(() => {
    const on = () => setOnline(true)
    const off = () => setOnline(false)
    window.addEventListener('online', on)
    window.addEventListener('offline', off)
    return () => {
      window.removeEventListener('online', on)
      window.removeEventListener('offline', off)
    }
  }, [])

  return (
    <div className="app-shell">
      {!online && <div className="network-banner" role="status">You are offline. Changes will not be sent until your connection returns.</div>}
      {children}
    </div>
  )
}

export function SectionCard({ title, description, children }: { title: string; description?: string; children: ReactNode }) {
  return (
    <section className="section-card" aria-labelledby={`section-${title}`}>
      <div className="section-card-heading">
        <div>
          <h2 id={`section-${title}`}>{title}</h2>
          {description && <p>{description}</p>}
        </div>
      </div>
      {children}
    </section>
  )
}

export function LoadingState({ label = 'Loading…' }: { label?: string }) {
  return <div className="loading-state" role="status" aria-live="polite"><span className="spinner" aria-hidden="true" />{label}</div>
}

export function EmptyState({ title, description }: { title: string; description?: string }) {
  return <div className="empty-state"><strong>{title}</strong>{description && <span>{description}</span>}</div>
}
