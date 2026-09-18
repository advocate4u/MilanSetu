import { useEffect, useState } from 'react'
import './advertising.css'

type Ad={enabled:boolean;mode:'Disabled'|'Sponsor'|'AdSense';label:string;text:string;targetUrl?:string;imageUrl?:string;adSenseClient?:string;adSenseSlot?:string}
const base=(import.meta.env.VITE_API_BASE_URL??'').replace(/\/$/,'')
export default function AdvertisingPanel(){
  const [ad,setAd]=useState<Ad|null>(null)
  useEffect(()=>{fetch(base+'/api/advertising').then(r=>r.ok?r.json():null).then(setAd).catch(()=>setAd(null))},[])
  if(!ad?.enabled||ad.mode==='Disabled') return null
  return <section className="advertising-panel" aria-label="Advertisement">
    <div className="advertising-panel-label"><span className="advertising-dot" aria-hidden="true"/>{ad.label||'Advertisement'}</div>
    {ad.mode==='Sponsor'
      ? <div className="advertising-sponsor">
          {ad.imageUrl&&<div className="advertising-image-wrap"><img src={ad.imageUrl} alt="" loading="lazy"/></div>}
          <div className="advertising-copy"><strong>{ad.text}</strong><span>Sponsored support helps keep MilanSetu free for everyone.</span></div>
          {ad.targetUrl&&<a className="advertising-action" href={ad.targetUrl} target="_blank" rel="noopener noreferrer sponsored">Visit</a>}
        </div>
      : <AdSense client={ad.adSenseClient} slot={ad.adSenseSlot}/>}
  </section>
}
function AdSense({client,slot}:{client?:string;slot?:string}){
  useEffect(()=>{if(!client||!slot)return;const s=document.createElement('script');s.async=true;s.src='https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client='+encodeURIComponent(client);s.crossOrigin='anonymous';document.head.appendChild(s);s.onload=()=>{try{const w=window as typeof window & {adsbygoogle?:unknown[]};w.adsbygoogle=w.adsbygoogle||[];w.adsbygoogle.push({})}catch{}};return()=>{if(s.parentNode)s.parentNode.removeChild(s)}},[client,slot])
  if(!client||!slot)return null
  return <div className="advertising-adsense"><ins className="adsbygoogle" style={{display:'block',minHeight:90}} data-ad-client={client} data-ad-slot={slot} data-ad-format="auto" data-full-width-responsive="true"/></div>
}