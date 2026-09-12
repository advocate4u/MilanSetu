export type ProfileData = {
  id: string
  displayName: string
  dateOfBirth: string
  gender: string
  accountType: string
  maritalStatus: string | null
  motherTongue: string | null
  bio: string | null
  visibility: string
  locations: Array<{ locationId: number; isPrimary: boolean; countryCode: string; stateName: string; districtName: string; cityName: string }>
  preferences: Array<{ minAge: number | null; maxAge: number | null; relocationOpen: boolean | null; importance: string }>
  education: { highestQualification: string | null; fieldOfStudy: string | null; institution: string | null } | null
  employment: { profession: string | null; industry: string | null; employmentType: string | null; workLocation: string | null } | null
  family: { parentsStatus: string | null; siblingsSummary: string | null; familyLocation: string | null; familyStructure: string | null } | null
  lifestyle: { foodPreference: string | null; smoking: string | null; alcohol: string | null; exercise: string | null; interests: string | null; travel: string | null; pets: string | null } | null
}

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7001'

function headers(): Record<string, string> {
  const token = sessionStorage.getItem('milansetu_access_token')
  return token ? { Authorization: `Bearer ${token}` } : {}
}

async function request(path: string, init: RequestInit = {}) {
  const response = await fetch(`${apiBaseUrl}${path}`, { credentials: 'include', ...init, headers: { ...headers(), ...(init.headers ?? {}) } })
  const data = await response.json().catch(() => null)
  if (!response.ok) throw new Error(data?.message ?? 'Unable to save profile.')
  return data
}

export async function getMyProfile() { return request('/api/profile/me') as Promise<ProfileData> }

export async function saveBasicProfile(value: { displayName: string; dateOfBirth: string; gender: string; accountType: string; maritalStatus: string; motherTongue: string; bio: string; visibility: string; minPartnerAge: number | null; maxPartnerAge: number | null; relocationOpen: boolean | null; preferenceImportance: string }) {
  return request('/api/profile/me', { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(value) })
}

export async function saveExtendedProfile(value: Record<string, string>) {
  return request('/api/profile/me/extended', { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(value) })
}
