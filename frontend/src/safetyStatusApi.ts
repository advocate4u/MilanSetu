const base=(import.meta.env.VITE_API_BASE_URL??'').trim().replace(/\/$/,'')
function auth(): Record<string, string> { const t=sessionStorage.getItem('milansetu_access_token'); return t ? { Authorization: `Bearer ${t}` } : {} }
export async function getSafety(){if(!base)throw new Error('MilanSetu API is not configured for this deployment.');const r=await fetch(base+'/api/safety/me',{headers:auth(),credentials:'include'});const d=await r.json().catch(()=>({}));if(!r.ok)throw new Error(d.message??'Unable to load safety status.');return d}
