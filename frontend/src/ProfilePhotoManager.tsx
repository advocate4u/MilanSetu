import { useEffect, useState } from 'react'
import { deleteProfilePhoto, getProfilePhotos, loadProfilePhoto, uploadProfilePhoto, type ProfilePhoto } from './profilePhotosApi'

export default function ProfilePhotoManager() {
  const [photos, setPhotos] = useState<(ProfilePhoto & { preview?: string })[]>([])
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  const load = async () => {
    if (!sessionStorage.getItem('milansetu_access_token')) return
    try {
      const items = await getProfilePhotos()
      const hydrated = await Promise.all(items.map(async photo => ({ ...photo, preview: await loadProfilePhoto(photo) })))
      setPhotos(current => {
        current.forEach(photo => { if (photo.preview) URL.revokeObjectURL(photo.preview) })
        return hydrated
      })
    } catch (e) { setError(e instanceof Error ? e.message : 'Unable to load photos.') }
  }

  useEffect(() => { void load(); return () => photos.forEach(photo => photo.preview && URL.revokeObjectURL(photo.preview)) }, [])

  const upload = async (file: File) => {
    setBusy(true); setError(''); setSuccess('')
    try {
      const photo = await uploadProfilePhoto(file)
      const preview = await loadProfilePhoto(photo)
      setPhotos(current => [...current, { ...photo, preview }])
      setSuccess('Photo added successfully.')
    } catch (e) { setError(e instanceof Error ? e.message : 'Unable to upload photo.') }
    finally { setBusy(false) }
  }

  const remove = async (photo: ProfilePhoto & { preview?: string }) => {
    setBusy(true); setError(''); setSuccess('')
    try {
      await deleteProfilePhoto(photo.fileName)
      if (photo.preview) URL.revokeObjectURL(photo.preview)
      setPhotos(current => current.filter(x => x.fileName !== photo.fileName))
      setSuccess('Photo removed.')
    } catch (e) { setError(e instanceof Error ? e.message : 'Unable to remove photo.') }
    finally { setBusy(false) }
  }

  if (!sessionStorage.getItem('milansetu_access_token')) return null

  return <section className="photo-manager" aria-label="Profile photos">
    <div className="photo-manager-head"><div><p className="eyebrow">PROFILE PHOTOS</p><h2>Show the real you</h2><p>Photos are private by default and are only available through authenticated access. Up to 6 photos, 5 MB each.</p></div><label className="primary-button photo-upload">{busy ? 'Working…' : 'Add photo'}<input type="file" accept="image/jpeg,image/png,image/webp" disabled={busy || photos.length >= 6} onChange={e => { const file = e.target.files?.[0]; if (file) void upload(file); e.currentTarget.value = '' }} /></label></div>
    {error && <p className="profile-error" role="alert">{error}</p>}{success && <p className="profile-success" role="status">{success}</p>}
    {photos.length === 0 ? <div className="photo-empty">Add a clear, recent photo. Avoid phone numbers, addresses, documents, or other private information in images.</div> : <div className="photo-grid">{photos.map((photo, index) => <article className="photo-card" key={photo.fileName}>{photo.preview && <img src={photo.preview} alt={`Profile photo ${index + 1}`} />}{index === 0 && <span className="photo-primary">Primary</span>}<button className="text-button danger-link" disabled={busy} onClick={() => void remove(photo)}>Remove</button></article>)}</div>}
  </section>
}
