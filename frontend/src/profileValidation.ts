export function calculateProfileAge(dateOfBirth: string, today = new Date()): number | null {
  if (!dateOfBirth) return null
  const birth = new Date(`${dateOfBirth}T00:00:00`)
  if (Number.isNaN(birth.getTime())) return null

  let age = today.getFullYear() - birth.getFullYear()
  const beforeBirthday = today.getMonth() < birth.getMonth() ||
    (today.getMonth() === birth.getMonth() && today.getDate() < birth.getDate())
  if (beforeBirthday) age -= 1
  return age
}

export function validatePartnerAgeRange(minAge: string, maxAge: string): string {
  if (minAge && (Number(minAge) < 18 || Number(minAge) > 120)) return 'Minimum partner age must be between 18 and 120.'
  if (maxAge && (Number(maxAge) < 18 || Number(maxAge) > 120)) return 'Maximum partner age must be between 18 and 120.'
  if (minAge && maxAge && Number(minAge) > Number(maxAge)) return 'Minimum partner age cannot be greater than maximum partner age.'
  return ''
}
