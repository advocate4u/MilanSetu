export type PrivacyVisibility = 'Public' | 'MembersOnly' | 'Hidden'
const base=(import.meta.env.VITE_API_BASE_URL??'').trim().replace(/\/$/,'')
function headers(): Record<string, string> { const t=sessionStorage.getItem('milansetu_access_token'); return t ? { Authorization: `Bearer ${t}` } : {} }
function requireBase(){if(!base)throw new Error('MilanSetu API is not configured for this deployment.')}
export async function getPrivacy(){requireBase();const r=await fetch(base+'/api/privacy/me',{headers:headers(),credentials:'include'});const d=await r.json().catch(()=>({}));if(!r.ok)throw new Error(d.message??'Unable to load privacy settings.');return d as {visibility:PrivacyVisibility;discoverable:boolean;blockedProfiles:number;contactDetailsPublic:boolean;verificationDocumentsPublic:boolean}}
export async function savePrivacy(visibility:PrivacyVisibility){requireBase();const r=await fetch(base+'/api/privacy/me',{method:'PUT',headers:{'Content-Type':'application/json',...headers()},credentials:'include',body:JSON.stringify({visibility})});const d=await r.json().catch(()=>({}));if(!r.ok)throw new Error(d.message??'Unable to save privacy settings.');return d}
