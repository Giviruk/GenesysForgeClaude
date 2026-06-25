// Нарративный dice roller Genesys: бросок пула кубов и нетто-итог символами.
// Результат считается на клиенте (v1); за столом он логируется через REST (см. client.ts).

export type DieKind = 'ability' | 'proficiency' | 'difficulty' | 'challenge' | 'boost' | 'setback'
export type DieSymbol = 'success' | 'failure' | 'advantage' | 'threat' | 'triumph' | 'despair'

/** Состав пула: сколько кубов каждого типа. */
export type RollPool = Record<DieKind, number>

/** Счётчики символов. */
export type RollSymbols = Record<DieSymbol, number>

export interface DieResult {
  kind: DieKind
  /** Выпавшие символы грани (пустая грань — []). */
  symbols: DieSymbol[]
}

export interface RollOutcome {
  /** Сырые суммы символов до взаимного гашения. */
  raw: RollSymbols
  /** Итог после гашения успехов/провалов и преимуществ/угроз; триумф/отчаяние не гасятся. */
  net: RollSymbols
  faces: DieResult[]
}

export const DIE_KINDS: DieKind[] = ['ability', 'proficiency', 'difficulty', 'challenge', 'boost', 'setback']
export const SYMBOL_ORDER: DieSymbol[] = ['success', 'failure', 'advantage', 'threat', 'triumph', 'despair']

const S = 'success', F = 'failure', A = 'advantage', T = 'threat' as const

// Грани кубов Genesys (каноничные). Каждая грань — список символов; [] — пустая грань.
const FACES: Record<DieKind, DieSymbol[][]> = {
  // Boost (синий d6)
  boost: [[], [], [S], [S, A], [A, A], [A]],
  // Setback (чёрный d6)
  setback: [[], [], [F], [F], [T], [T]],
  // Ability (зелёный d8)
  ability: [[], [S], [S], [S, S], [A], [A], [S, A], [A, A]],
  // Difficulty (фиолетовый d8)
  difficulty: [[], [F], [F, F], [T], [T], [T], [T, T], [F, T]],
  // Proficiency (жёлтый d12)
  proficiency: [[], [S], [S], [S, S], [S, S], [A], [S, A], [S, A], [S, A], [A, A], [A, A], ['triumph']],
  // Challenge (красный d12)
  challenge: [[], [F], [F], [F, F], [F, F], [T], [T], [F, T], [F, T], [T, T], [T, T], ['despair']],
}

export function emptyPool(): RollPool {
  return { ability: 0, proficiency: 0, difficulty: 0, challenge: 0, boost: 0, setback: 0 }
}

function emptySymbols(): RollSymbols {
  return { success: 0, failure: 0, advantage: 0, threat: 0, triumph: 0, despair: 0 }
}

/** Суммарное число кубов в пуле. */
export function poolSize(pool: RollPool): number {
  return DIE_KINDS.reduce((n, k) => n + Math.max(0, pool[k] | 0), 0)
}

/**
 * Бросает пул. `rng` — функция 0..1 (по умолчанию Math.random); в тестах передаётся seedable.
 */
export function rollPool(pool: RollPool, rng: () => number = Math.random): RollOutcome {
  const faces: DieResult[] = []
  const raw = emptySymbols()

  for (const kind of DIE_KINDS) {
    const count = Math.max(0, pool[kind] | 0)
    const table = FACES[kind]
    for (let i = 0; i < count; i++) {
      const face = table[Math.min(table.length - 1, Math.floor(rng() * table.length))]
      faces.push({ kind, symbols: face })
      for (const sym of face) raw[sym]++
    }
  }

  return { raw, net: netSymbols(raw), faces }
}

/**
 * Нетто-итог. Триумф также считается успехом, отчаяние — провалом (для гашения),
 * но сами триумф/отчаяние не гасятся и всегда показываются.
 */
export function netSymbols(raw: RollSymbols): RollSymbols {
  const net = emptySymbols()
  const successes = raw.success + raw.triumph - raw.failure - raw.despair
  const advantages = raw.advantage - raw.threat

  if (successes >= 0) net.success = successes
  else net.failure = -successes
  if (advantages >= 0) net.advantage = advantages
  else net.threat = -advantages

  net.triumph = raw.triumph
  net.despair = raw.despair
  return net
}

const SYMBOL_LABELS: Record<DieSymbol, [one: string, many: string]> = {
  success: ['успех', 'успеха'],
  failure: ['провал', 'провала'],
  advantage: ['преимущество', 'преимущества'],
  threat: ['угроза', 'угрозы'],
  triumph: ['триумф', 'триумфа'],
  despair: ['отчаяние', 'отчаяния'],
}

function plural(n: number, sym: DieSymbol): string {
  const [one, many] = SYMBOL_LABELS[sym]
  return `${n} ${n === 1 ? one : many}`
}

/** Краткий человекочитаемый итог броска по нетто-символам. */
export function summarize(net: RollSymbols): string {
  const parts: string[] = []
  if (net.success > 0) parts.push(plural(net.success, 'success'))
  else if (net.failure > 0) parts.push(plural(net.failure, 'failure'))
  else parts.push('ничья')

  if (net.advantage > 0) parts.push(plural(net.advantage, 'advantage'))
  else if (net.threat > 0) parts.push(plural(net.threat, 'threat'))

  if (net.triumph > 0) parts.push(plural(net.triumph, 'triumph'))
  if (net.despair > 0) parts.push(plural(net.despair, 'despair'))

  return parts.join(', ')
}
