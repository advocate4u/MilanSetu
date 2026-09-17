import { useEffect, useRef, useState } from 'react'
import { deleteProfilePhoto, getProfilePhotos, loadProfilePhoto, reorderProfilePhotos, uploadProfilePhoto, type ProfilePhoto } from './profilePhotosApi'

type LoadedPhoto = ProfilePhoto & { preview?: string }

const MAX_FILE_SIZE = 5 * 1024 * 1024
const ALLOWED_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp'])

export default function ProfilePhotoManager() {
  const [photos, setPhotos] = useState<LoadedPhoto[]>([])
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const previews = useRef<string[]>([])

  useEffect(() => {
    let active = true
    const load = async () => {
      if (!sessionStorage.getItem('milansetu_access_token')) return
      try {
        const items = await getProfilePhotos()
        const hydrated = await Promise.all(items.map(async photo => ({ ...photo, preview: await loadProfilePhoto(photo) })))
        if (!active) { hydrated.forEach(photo => photo.preview && URL.revokeObjectURL(photo.preview)); return }
        previews.current.forEach(url => URL.revokeObjectURL(url))
        previews.current = hydrated.flatMap(photo => photo.preview ? [photo.preview] : [])
        setPhotos(hydrated)
      } catch (e) { if (active) setError(e instanceof Error ? e.message : 'Unable to load photos.') }
    }
    void load()
    return () => { active = false; previews.current.forEach(url => URL.revokeObjectURL(url)); previews.current = [] }
  }, [])

  const upload = async (file: File) => {
    setError(''); setSuccess('')
    if (!ALLOWED_TYPES.has(file.type)) return setError('Only JPEG, PNG, and WebP photos are supported.')
    if (file.size > MAX_FILE_SIZE) return setError('Photo must be 5 MB or smaller.')
    if (photos.length >= 6) return setError('You can add up to 6 profile photos.')
    setBusy(true)
    try {
      const photo = await uploadProfilePhoto(file)
      const preview = await loadProfilePhoto(photo)
      previews.current.push(preview)
      setPhotos(current => [...current, { ...photo, preview }])
      setSuccess(photos.length === 0 ? 'Photo added and set as primary.' : 'Photo added successfully.')
    } catch (e) { setError(e instanceof Error ? e.message : 'Unable to upload photo.') }
    finally { setBusy(false) }
  }

  const persistOrder = async (nextPhotos: LoadedPhoto[], message: string) => {
    setBusy(true); setError(''); setSuccess('')
    try {
      await reorderProfilePhotos(nextPhotos.map(photo => photo.fileName))
      setPhotos(nextPhotos)
      setSuccess(message)
    } catch (e) { setError(e instanceof Error ? e.message : 'Unable to update photo order.') }
    finally { setBusy(false) }
  }

  const move = (index: number, direction: -1 | 1) => {
    const target = index + direction
    if (target < 0 || target >= photos.length || busy) return
    const next = [...photos]
    ;[next[index], next[target]] = [next[target], next[index]]
    void persistOrder(next, target === 0 ? 'Primary photo updated.' : 'Photo order updated.')
  }

  const remove = async (photo: LoadedPhoto) => {
    setBusy(true); setError(''); setSuccess('')
    try {
      await deleteProfilePhoto(photo.fileName)
      if (photo.preview) { URL.revokeObjectURL(photo.preview); previews.current = previews.current.filter(url => url !== photo.preview) }
      setPhotos(current => current.filter(x => x.fileName !== photo.fileName))
      setSuccess('Photo removed. The first remaining photo is now primary.')
    } catch (e) { setError(e instanceof Error ? e.message : 'Unable to remove photo.') }
    finally { setBusy(false) }
  }

  if (!sessionStorage.getItem('milansetu_access_token')) return null
  return <section className="photo-manager" aria-label="Profile photos">
    <div className="photo-manager-head">
      <div><p className="eyebrow">PROFILE PHOTOS</p><h2>Show the real you</h2><p>Photos are private by default and available only through authenticated access. Up to 6 photos, 5 MB each.</p></div>
      <label className="primary-button photo-upload">{busy ? 'Working…' : 'Add photo'}<input type="file" accept="image/jpeg,image/png,image/webp" disabled={busy || photos.length >= 6} onChange={e => { const file = e.target.files?.[0]; if (file) void upload(file); e.currentTarget.value = '' }} /></label>
    </div>
    {error && <p className="profile-error" role="alert">{error}</p>}
    {success && <p className="profile-success" role="status">{success}</p>}
    {photos.length === 0 ? <div className="photo-empty">Add a clear, recent photo. Avoid phone numbers, addresses, documents, or other private information in images.</div> : <div className="photo-grid">{photos.map((photo, index) => <article className="photo-card" key={photo.fileName}>
      {photo.preview && <img src={photo.preview} alt={`Profile photo ${index + 1}${index === 0 ? ', primary' : ''}`} />}
      {index === 0 && <span className="photo-primary">Primary</span>}
      <div className="photo-actions">
        <button type="button" className="text-button" disabled={busy || index === 0} onClick={() => move(index, -1)} aria-label={`Move photo ${index + 1} left`}>←</button>
        <button type="button" className="text-button" disabled={busy || index === photos.length - 1} onClick={() => move(index, 1)} aria-label={`Move photo ${index + 1} right`}>→</button>
        <button type="button" className="text-button danger-link" disabled={busy} onClick={() => void remove(photo)}>Remove</button>
      </div>
    </article>)}</div>}
  </section>
}
