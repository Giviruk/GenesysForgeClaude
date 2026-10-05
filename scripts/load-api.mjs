// Concurrent load test for the GenesysForge API. Node 20+, no dependencies.
// Local stack (registers its own throwaway user):
//   node scripts/load-api.mjs --base-url http://localhost:8180 --concurrency 1,10,50 --seconds 20
// Any other host needs an explicit token and runs read-only scenarios only:
//   GENESYS_LOAD_TOKEN=... node scripts/load-api.mjs --base-url https://example --character-id <id> --concurrency 1,5
// Never prints response bodies or the token.
const args = process.argv.slice(2)
const option = (name, fallback) => (i => (i < 0 ? fallback : args[i + 1]))(args.indexOf(name))
const base = new URL(option('--base-url', 'http://localhost:8180'))
const local = ['localhost', '127.0.0.1', '[::1]'].includes(base.hostname)
const levels = option('--concurrency', '1,10,50').split(',').map(Number)
const seconds = Number(option('--seconds', '20'))
const only = option('--scenarios', '')
if (levels.some(n => !Number.isInteger(n) || n < 1 || n > (local ? 200 : 10))) throw new Error('concurrency out of range')
if (!(seconds >= 3 && seconds <= (local ? 300 : 30))) throw new Error('seconds out of range')

const json = { 'Content-Type': 'application/json' }
async function call(method, path, token, body) {
  const response = await fetch(new URL(path, base), {
    method, cache: 'no-store', signal: AbortSignal.timeout(30000),
    headers: { ...(body ? json : {}), ...(token ? { Authorization: `Bearer ${token}` } : {}), 'Accept-Encoding': 'gzip' },
    body: body ? JSON.stringify(body) : undefined,
  })
  return response
}
async function must(method, path, token, body) {
  const response = await call(method, path, token, body)
  if (!response.ok) throw new Error(`${method} ${path} -> ${response.status}`)
  return response.status === 204 ? null : response.json()
}

// ---- setup ----
let token = process.env.GENESYS_LOAD_TOKEN
let characters = option('--character-id', '') ? [option('--character-id', '')] : []
let skillIds = []
if (!token) {
  if (!local) throw new Error('Set GENESYS_LOAD_TOKEN for non-local hosts')
  const user = await must('POST', '/api/auth/register', null, {
    email: `load-${Date.now()}@example.test`, password: 'Passw0rd!', displayName: 'Load test' })
  token = user.token
}
if (local && characters.length === 0) {
  const reference = await must('GET', '/api/reference/RealmsOfTerrinoth', token)
  for (let i = 0; i < 20; i++) {
    const created = await must('POST', '/api/characters/', token, {
      name: `Load ${i}`, system: 'realmsOfTerrinoth', archetypeId: reference.archetypes[0].id,
      careerId: reference.careers[0].id, freeCareerSkillNames: [], archetypeSkillChoices: [], careerGearChoices: [] })
    characters.push(created.id)
  }
  const sheet = await must('GET', `/api/characters/${characters[0]}`, token)
  skillIds = sheet.skills.filter(s => s.ranks === 0).map(s => s.skillDefId)
}
if (characters.length === 0) throw new Error('Pass --character-id for remote hosts')

const scenarios = {
  health: () => call('GET', '/api/health'),
  reference: () => call('GET', '/api/reference/RealmsOfTerrinoth', token),
  list: () => call('GET', '/api/characters/', token),
  sheet: w => call('GET', `/api/characters/${characters[w % characters.length]}`, token),
  ...(local && skillIds.length ? {
    // Each worker owns one character, so buy+refund never races on XP.
    mutate: async w => {
      const id = characters[w % characters.length], skill = skillIds[w % skillIds.length]
      const bought = await call('POST', `/api/characters/${id}/skills/${skill}/buy-rank`, token)
      await bought.arrayBuffer()
      return call('POST', `/api/characters/${id}/skills/${skill}/refund-rank`, token)
    },
  } : {}),
}
const selected = Object.entries(scenarios).filter(([name]) => !only || only.split(',').includes(name))

const pct = (sorted, p) => sorted[Math.min(sorted.length - 1, Math.ceil(sorted.length * p) - 1)] ?? 0
async function run(name, fn, concurrency) {
  const until = performance.now() + seconds * 1000
  const rows = []
  const statuses = {}
  await Promise.all(Array.from({ length: Math.min(concurrency, name === 'mutate' ? characters.length : concurrency) }, async (_, w) => {
    while (performance.now() < until) {
      const start = performance.now()
      let status = 'error', bytes = 0, app = NaN, db = NaN
      try {
        const response = await fn(w)
        status = response.status
        bytes = (await response.arrayBuffer()).byteLength
        const timing = response.headers.get('server-timing') ?? ''
        app = Number(/app;dur=([\d.]+)/.exec(timing)?.[1])
        db = Number(/db;dur=([\d.]+)/.exec(timing)?.[1])
      } catch { /* counted as error */ }
      statuses[status] = (statuses[status] ?? 0) + 1
      rows.push({ ms: performance.now() - start, bytes, app, db })
    }
  }))
  const ms = rows.map(r => r.ms).sort((a, b) => a - b)
  const apps = rows.map(r => r.app).filter(Number.isFinite).sort((a, b) => a - b)
  const dbs = rows.map(r => r.db).filter(Number.isFinite).sort((a, b) => a - b)
  return {
    scenario: name, concurrency, requests: rows.length, rps: +(rows.length / seconds).toFixed(1),
    p50: +pct(ms, .5).toFixed(1), p95: +pct(ms, .95).toFixed(1), p99: +pct(ms, .99).toFixed(1),
    appP50: +pct(apps, .5).toFixed(1), dbP50: +pct(dbs, .5).toFixed(1),
    kb: +(pct(rows.map(r => r.bytes).sort((a, b) => a - b), .5) / 1024).toFixed(1),
    errors: rows.length - (statuses[200] ?? 0) - (statuses[204] ?? 0), statuses,
  }
}

const identity = await (await call('GET', '/api/health')).json().catch(() => ({}))
const results = []
for (const [name, fn] of selected)
  for (const concurrency of levels) results.push(await run(name, fn, concurrency))
console.table(results.map(({ statuses, ...r }) => r))
console.log(JSON.stringify({ at: new Date().toISOString(), base: base.origin, identity, seconds, results }))
