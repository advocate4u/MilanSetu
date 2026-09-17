import { describe, expect, it } from 'vitest'
import { calculateProfileAge, validatePartnerAgeRange } from './profileValidation'

describe('calculateProfileAge', () => {
  const today = new Date(2026, 8, 17)

  it('calculates age before a birthday correctly', () => {
    expect(calculateProfileAge('2000-10-01', today)).toBe(25)
  })

  it('calculates age after a birthday correctly', () => {
    expect(calculateProfileAge('2000-08-01', today)).toBe(26)
  })

  it('returns null for a missing or invalid date', () => {
    expect(calculateProfileAge('')).toBeNull()
    expect(calculateProfileAge('not-a-date')).toBeNull()
  })
})

describe('validatePartnerAgeRange', () => {
  it('accepts an empty preference range', () => {
    expect(validatePartnerAgeRange('', '')).toBe('')
  })

  it('rejects ages below 18', () => {
    expect(validatePartnerAgeRange('17', '30')).toContain('18')
  })

  it('rejects a reversed range', () => {
    expect(validatePartnerAgeRange('40', '30')).toContain('greater')
  })

  it('accepts a valid range', () => {
    expect(validatePartnerAgeRange('25', '40')).toBe('')
  })
})
