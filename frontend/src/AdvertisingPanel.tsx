import { useEffect, useState } from 'react'

type AdMode = 'none' | 'sponsor' | 'adsense'

const mode = (import.meta.env.VITE_AD_MODE?.trim().toLowerCase() || 'none') as AdMode
const sponsorText = import.meta.env.VITE_AD_TEXT?.trim() || 'Help keep MilanSetu free for everyone.'
const sponsorLabel = import.meta.env.VITE_AD_LABEL?.trim() || 'Advertisement'
const sponsorUrl = import.meta.env.VITE_AD_URL?.trim() || ''
const sponsorImageUrl = import.meta.env.VITE_AD_IMAGE_URL?.trim() || ''
const adsenseClient = import.meta.env.VITE_ADSENSE_CLIENT?.trim() || ''
const adsenseSlot = import.meta.env.VITE_ADSENSE_SLOT?.trim() || ''

function AdPlaceholder() {
  return <div className="minimal-ad-empty">Advertisement space • optional and configurable</div>
}

function GoogleAd() {
  const [ready, setReady] = useState(false)

  useEffect(() => {
    if (!adsenseClient || !adsenseSlot) return
    const existing = document.querySelector('script[data-milansetu-adsense]')
    if (existing) { setReady(true); return }
    const script = document.createElement('script')
    script.async = true
    script.crossOrigin = 'anonymous'
    script.src = \`https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=\${encodeURIComponent(adsenseClient)}\`
    script.dataset.milansetuAdsense = 'true'
    script.onload = () => setReady(true)
    document.head.appendChild(script)
  }, [])

  if (!adsenseClient || !adsenseSlot || !ready) return <AdPlaceholder />
  return <ins className="adsbygoogle" style={{ display: 'block', minHeight: 90 }} data-ad-client={adsenseClient} data-ad-slot={adsenseSlot} data-ad-format="auto" data-full-width-responsive="true" />
}

export default function AdvertisingPanel() {
  if (mode === 'none') return null

  return <section className="minimal-ad-panel" aria-label="Advertisement">
    <style>{\`\
      .minimal-ad-panel{width:min(100% - 24px,1180px);margin:18px auto;padding:8px 12px;border:1px solid #eee3df;border-radius:12px;background:#fff;box-sizing:border-box;overflow:hidden}
      .minimal-ad-label{display:block;margin-bottom:5px;font-size:10px;letter-spacing:.08em;text-transform:uppercase;opacity:.5}
      .minimal-ad-sponsor{display:flex;align-items:center;gap:14px;min-height:64px}
      .minimal-ad-sponsor img{width:88px;height:52px;object-fit:cover;border-radius:8px;border:1px solid #eee3df}
      .minimal-ad-copy{flex:1;min-width:0;font-size:13px;line-height:1.4}
      .minimal-ad-copy strong{display:block;margin-bottom:2px}
      .minimal-ad-link{color:inherit;text-decoration:none}
      .minimal-ad-link:hover{text-decoration:underline}
      .minimal-ad-empty{min-height:72px;display:grid;place-items:center;font-size:12px;opacity:.5}
      @media(max-width:600px){.minimal-ad-panel{margin:14px 12px}.minimal-ad-sponsor{gap:10px}.minimal-ad-sponsor img{width:64px;height:46px}}
    \`}</style>
    <span className="minimal-ad-label">{sponsorLabel}</span>
    {mode === 'adsense' ? <GoogleAd /> : sponsorUrl ? <a className="minimal-ad-link" href={sponsorUrl} target="_blank" rel="noopener noreferrer sponsored">
      <div className="minimal-ad-sponsor">{sponsorImageUrl && <img src={sponsorImageUrl} alt="" /> }<div className="minimal-ad-copy"><strong>{sponsorText}</strong><span>Sponsored support helps keep MilanSetu free.</span></div></div>
    </a> : <div className="minimal-ad-sponsor">{sponsorImageUrl && <img src={sponsorImageUrl} alt="" />}<div className="minimal-ad-copy"><strong>{sponsorText}</strong><span>Sponsored support helps keep MilanSetu free.</span></div></div>}
  </section>
}
