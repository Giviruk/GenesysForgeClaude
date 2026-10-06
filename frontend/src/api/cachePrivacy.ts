/** Clean legacy SW data before an API call, including while an old worker is updating. */
export async function clearLegacyReferenceCaches(): Promise<void> {
  if (typeof caches === 'undefined') return
  try {
    const keys = await caches.keys()
    await Promise.all(keys.filter(key => key.startsWith('genesysforge-reference-'))
      .map(key => caches.delete(key)))
  } catch {
    // CacheStorage can be disabled by browser policy; network access still works.
  }
}
