// Read-only, sequential baseline; never prints response bodies or the bearer token.
// GENESYS_BENCH_TOKEN=... node scripts/measure-api.mjs --base-url http://localhost:8080 --samples 20
const args = process.argv.slice(2)
const option = (name, fallback) => {
  const index = args.indexOf(name)
  return index < 0 ? fallback : args[index + 1]
}
const base = new URL(option('--base-url', 'http://localhost:8080'))
if (!['http:', 'https:'].includes(base.protocol) || base.username || base.password) throw new Error('Use an HTTP(S) base URL without credentials')
const samples = Number(option('--samples', '20'))
if (!Number.isInteger(samples) || samples < 5 || samples > 200) throw new Error('samples must be 5..200')
if (!process.env.GENESYS_BENCH_TOKEN) throw new Error('Set GENESYS_BENCH_TOKEN; response bodies and tokens are not logged')
const headers = { Authorization: `Bearer ${process.env.GENESYS_BENCH_TOKEN}` }
const paths = ['/api/v1/health', '/api/v1/reference/GenesysCore', '/api/v1/reference/RealmsOfTerrinoth', '/api/v1/characters/']
const character = option('--character-id', '')
if (character) {
  if (!/^[a-f0-9-]{36}$/i.test(character)) throw new Error('Invalid character id')
  paths.push(`/api/v1/characters/${character}`, `/api/v1/characters/${character}/crafting`)
}
const health = await fetch(new URL('/api/v1/health', base), { headers, cache: 'no-store', signal: AbortSignal.timeout(15000) })
const identity = health.ok ? await health.json() : { status: health.status }
const percentile = (values, p) => [...values].sort((a, b) => a - b)[Math.ceil(values.length * p) - 1]
const metrics = []
for (const path of paths) {
  const rows = []
  for (let index = -3; index < samples; index++) {
    const start = performance.now()
    const response = await fetch(new URL(path, base), { headers, cache: 'no-store', signal: AbortSignal.timeout(15000) })
    const ttfb = performance.now() - start
    const bytes = (await response.arrayBuffer()).byteLength
    if (index >= 0) rows.push({ status: response.status, ttfb, total: performance.now() - start, bytes })
  }
  metrics.push({ path, samples, failed: rows.filter(r => r.status !== 200).length,
    ttfbMedianMs: +percentile(rows.map(r => r.ttfb), .5).toFixed(2),
    totalP95Ms: +percentile(rows.map(r => r.total), .95).toFixed(2),
    decodedBytesMedian: percentile(rows.map(r => r.bytes), .5) })
}
console.log(JSON.stringify({ at: new Date().toISOString(), base: base.origin, identity, warmups: 3, metrics }, null, 2))
if (metrics.some(m => m.failed)) process.exitCode = 1
