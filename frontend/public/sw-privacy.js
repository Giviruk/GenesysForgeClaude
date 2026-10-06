// Remove reference caches created by older workers. They could contain user-owned content.
self.addEventListener('activate', event => {
  event.waitUntil(caches.keys().then(keys => Promise.all(
    keys.filter(key => key.startsWith('genesysforge-reference-')).map(key => caches.delete(key)),
  )))
})
