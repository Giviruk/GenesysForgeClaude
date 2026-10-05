import { afterEach, describe, expect, it, vi } from 'vitest'
import { clearLegacyReferenceCaches } from './cachePrivacy'

afterEach(() => vi.unstubAllGlobals())

describe('legacy reference cache cleanup', () => {
  it('removes every old reference cache without deleting static assets or other apps', async () => {
    const remove = vi.fn().mockResolvedValue(true)
    vi.stubGlobal('caches', {
      keys: vi.fn().mockResolvedValue(['genesysforge-reference-v1', 'genesysforge-reference-v2', 'workbox-precache', 'another-app']),
      delete: remove,
    })
    await clearLegacyReferenceCaches()
    expect(remove.mock.calls).toEqual([['genesysforge-reference-v1'], ['genesysforge-reference-v2']])
  })

  it('works in environments without CacheStorage', async () => {
    vi.stubGlobal('caches', undefined)
    await expect(clearLegacyReferenceCaches()).resolves.toBeUndefined()
  })

  it('does not break network requests when the browser disables CacheStorage', async () => {
    vi.stubGlobal('caches', { keys: vi.fn().mockRejectedValue(new Error('SecurityError')) })
    await expect(clearLegacyReferenceCaches()).resolves.toBeUndefined()
  })
})
