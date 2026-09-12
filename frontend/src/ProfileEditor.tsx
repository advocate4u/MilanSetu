import { FormEvent, useEffect, useState } from 'react'
import { getMyProfile, saveBasicProfile, saveExtendedProfile, type ProfileData } from './profileApi'

const initial = {
  displayName: '', dateOfBirth: '', gender: 'Other', accountType: 'Individual', maritalStatus: '', motherTongue: '', bio: '', visibility: 'MembersOnly',
  minPartnerAge: '', maxPartnerAge: '', relocationOpen: '', preferenceImportance: 'Flexible',
  highestQualification: '', fieldOfStudy: '', institution: '', profession: '', industry: '', employmentType: '', workLocation: '',
  parentsStatus: '', siblingsSummary: '', familyLocation: '', familyStructure: '', foodPreference: '', smoking: '', alcohol: '', exercise: '', interests: '', travel: '', pets: '',
}

type FormState = typeof initial

function fromProfile(p: ProfileData): FormState {
  const pref = p.preferences?.[0]
  return {
    ...initial,
    displayName: p.displayName ?? '', dateOfBirth: p.dateOfBirth ?? '', gender: p.gender ?? 'Other', accountType: p.accountType ?? 'Individual', maritalStatus: p.maritalStatus ?? '', motherTongue: p.motherTongue ?? '', bio: p.bio ?? '', visibility: p.visibility ?? 'MembersOnly',
    minPartnerAge: pref?.minAge == null ? '' : String(pref.minAge), maxPartnerAge: pref?.maxAge == null ? '' : String(pref.maxAge), relocationOpen: pref?.relocationOpen == null ? '' : String(pref.relocationOpen), preferenceImportance: pref?.importance ?? 'Flexible',
    highestQualification: p.education?.highestQualification ?? '', fieldOfStudy: p.education?.fieldOfStudy ?? '', institution: p.education?.institution ?? '', profession: p.employment?.profession ?? '', industry: p.employment?.industry ?? '', employmentType: p.employment?.employmentType ?? '', workLocation: p.employment?.workLocation ?? '',
    parentsStatus: p.family?.parentsStatus ?? '', siblingsSummary: p.family?.siblingsSummary ?? '', familyLocation: p.family?.familyLocation ?? '', familyStructure: p.family?.familyStructure ?? '', foodPreference: p.lifestyle?.foodPreference ?? '', smoking: p.lifestyle?.smoking ?? '', alcohol: p.lifestyle?.alcohol ?? '', exercise: p.lifestyle?.exercise ?? '', interests: p.lifestyle?.interests ?? '', travel: p.lifestyle?.travel ?? '', pets: p.lifestyle?.pets ?? '',
  }
}

export default function ProfileEditor() {
  const [form, setForm] = useState<FormState>(initial)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  useEffect(() => { getMyProfile().then(p => setForm(fromProfile(p))).catch(e => { if (!(e instanceof Error && e.message.includes('not been created'))) setError(e instanceof Error ? e.message : 'Unable to load profile.') }).finally(() => setLoading(false)) }, [])
  const set = (key: keyof FormState, value: string) => setForm(current => ({ ...current, [key]: value }))

  const submit = async (event: FormEvent) => {
    event.preventDefault(); setSaving(true); setMessage(''); setError('')
    try {
      await saveBasicProfile({ ...form, minPartnerAge: form.minPartnerAge ? Number(form.minPartnerAge) : null, maxPartnerAge: form.maxPartnerAge ? Number(form.maxPartnerAge) : null, relocationOpen: form.relocationOpen === '' ? null : form.relocationOpen === 'true' })
      await saveExtendedProfile({ highestQualification: form.highestQualification, fieldOfStudy: form.fieldOfStudy, institution: form.institution, profession: form.profession, industry: form.industry, employmentType: form.employmentType, workLocation: form.workLocation, parentsStatus: form.parentsStatus, siblingsSummary: form.siblingsSummary, familyLocation: form.familyLocation, familyStructure: form.familyStructure, foodPreference: form.foodPreference, smoking: form.smoking, alcohol: form.alcohol, exercise: form.exercise, interests: form.interests, travel: form.travel, pets: form.pets })
      setMessage('Profile saved successfully.')
    } catch (e) { setError(e instanceof Error ? e.message : 'Unable to save profile.') } finally { setSaving(false) }
  }

  if (loading) return <section className="profile-editor"><div className="profile-empty">Loading your profile…</div></section>
  if (!sessionStorage.getItem('milansetu_access_token')) return null

  return <section className="profile-editor" aria-label="Edit profile"><div className="profile-editor-head"><div><p className="eyebrow">MY PROFILE</p><h2>Build a profile that feels like you</h2><p>Share what helps someone understand you. Contact details and exact addresses are never collected here.</p></div></div><form onSubmit={submit}>
    <fieldset><legend>About you</legend><div className="profile-fields"><label>Display name<input required maxLength={120} value={form.displayName} onChange={e => set('displayName', e.target.value)}/></label><label>Date of birth<input required type="date" value={form.dateOfBirth} onChange={e => set('dateOfBirth', e.target.value)}/></label><label>Gender<select value={form.gender} onChange={e => set('gender', e.target.value)}><option>Male</option><option>Female</option><option>Other</option></select></label><label>Account type<select value={form.accountType} onChange={e => set('accountType', e.target.value)}><option>Individual</option><option>Family</option><option>Assisted</option></select></label><label>Marital status<input maxLength={40} value={form.maritalStatus} onChange={e => set('maritalStatus', e.target.value)} placeholder="e.g. Never married"/></label><label>Mother tongue<input maxLength={80} value={form.motherTongue} onChange={e => set('motherTongue', e.target.value)}/></label></div><label>About me<textarea maxLength={2000} rows={5} value={form.bio} onChange={e => set('bio', e.target.value)} placeholder="Tell people about your values, interests and the life you hope to build."/></label></fieldset>
    <fieldset><legend>Partner preferences</legend><div className="profile-fields"><label>Minimum age<select value={form.minPartnerAge} onChange={e => set('minPartnerAge', e.target.value)}><option value="">No preference</option>{[18,21,25,28,30,35,40,50].map(x => <option key={x}>{x}</option>)}</select></label><label>Maximum age<select value={form.maxPartnerAge} onChange={e => set('maxPartnerAge', e.target.value)}><option value="">No preference</option>{[25,30,35,40,45,50,60,70].map(x => <option key={x}>{x}</option>)}</select></label><label>Relocation<select value={form.relocationOpen} onChange={e => set('relocationOpen', e.target.value)}><option value="">Not specified</option><option value="true">Open to relocation</option><option value="false">Prefer not to relocate</option></select></label><label>Importance<select value={form.preferenceImportance} onChange={e => set('preferenceImportance', e.target.value)}><option>NoPreference</option><option>Flexible</option><option>Preferred</option><option>DealBreaker</option></select></label><label>Profile visibility<select value={form.visibility} onChange={e => set('visibility', e.target.value)}><option>MembersOnly</option><option>Public</option><option>Hidden</option></select></label></div></fieldset>
    <fieldset><legend>Education & career</legend><div className="profile-fields"><label>Highest qualification<input maxLength={160} value={form.highestQualification} onChange={e => set('highestQualification', e.target.value)}/></label><label>Field of study<input maxLength={160} value={form.fieldOfStudy} onChange={e => set('fieldOfStudy', e.target.value)}/></label><label>Institution<input maxLength={200} value={form.institution} onChange={e => set('institution', e.target.value)}/></label><label>Profession<input maxLength={160} value={form.profession} onChange={e => set('profession', e.target.value)}/></label><label>Industry<input maxLength={160} value={form.industry} onChange={e => set('industry', e.target.value)}/></label><label>Employment type<input maxLength={80} value={form.employmentType} onChange={e => set('employmentType', e.target.value)}/></label><label>Work location<input maxLength={160} value={form.workLocation} onChange={e => set('workLocation', e.target.value)}/></label></div></fieldset>
    <fieldset><legend>Family</legend><div className="profile-fields"><label>Parents status<input maxLength={120} value={form.parentsStatus} onChange={e => set('parentsStatus', e.target.value)}/></label><label>Family location<input maxLength={160} value={form.familyLocation} onChange={e => set('familyLocation', e.target.value)}/></label><label>Family structure<input maxLength={120} value={form.familyStructure} onChange={e => set('familyStructure', e.target.value)} placeholder="e.g. nuclear / joint"/></label></div><label>Siblings<textarea maxLength={500} rows={3} value={form.siblingsSummary} onChange={e => set('siblingsSummary', e.target.value)}/></label></fieldset>
    <fieldset><legend>Lifestyle</legend><div className="profile-fields"><label>Food preference<input maxLength={80} value={form.foodPreference} onChange={e => set('foodPreference', e.target.value)} placeholder="e.g. Vegetarian"/></label><label>Smoking<input maxLength={80} value={form.smoking} onChange={e => set('smoking', e.target.value)} placeholder="e.g. No"/></label><label>Alcohol<input maxLength={80} value={form.alcohol} onChange={e => set('alcohol', e.target.value)} placeholder="e.g. No"/></label><label>Exercise<input maxLength={80} value={form.exercise} onChange={e => set('exercise', e.target.value)}/></label><label>Travel<input maxLength={120} value={form.travel} onChange={e => set('travel', e.target.value)}/></label><label>Pets<input maxLength={120} value={form.pets} onChange={e => set('pets', e.target.value)}/></label></div><label>Interests<textarea maxLength={500} rows={3} value={form.interests} onChange={e => set('interests', e.target.value)} placeholder="Reading, music, travel, volunteering…"/></label></fieldset>
    {error && <p className="profile-error" role="alert">{error}</p>}{message && <p className="profile-success" role="status">{message}</p>}<button className="primary-button" type="submit" disabled={saving}>{saving ? 'Saving…' : 'Save profile'}</button>
  </form></section>
}
