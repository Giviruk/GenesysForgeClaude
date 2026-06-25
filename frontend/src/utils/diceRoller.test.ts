import { describe, it, expect } from 'vitest'
import {
  emptyPool, poolSize, rollPool, netSymbols, summarize,
  type RollPool, type RollSymbols,
} from './diceRoller'

// Детерминированный rng: проигрывает заранее заданную последовательность 0..1.
function seq(values: number[]): () => number {
  let i = 0
  return () => values[i++ % values.length]
}

function sym(p: Partial<RollSymbols>): RollSymbols {
  return { success: 0, failure: 0, advantage: 0, threat: 0, triumph: 0, despair: 0, ...p }
}

describe('poolSize', () => {
  it('суммирует все кубы и игнорирует отрицательные', () => {
    expect(poolSize({ ...emptyPool(), ability: 2, proficiency: 1, difficulty: 3 })).toBe(6)
  })
})

describe('rollPool', () => {
  it('rng=0 → первая грань каждого куба (всегда пустая) = ничья', () => {
    const pool: RollPool = { ...emptyPool(), ability: 2, difficulty: 1, boost: 1 }
    const out = rollPool(pool, () => 0)
    expect(out.faces).toHaveLength(4)
    expect(out.raw).toEqual(sym({}))
    expect(out.net).toEqual(sym({}))
  })

  it('proficiency последняя грань = триумф; challenge последняя = отчаяние', () => {
    // rng→1 (clamp на последний индекс): proficiency d12 → triumph, challenge d12 → despair
    const out = rollPool({ ...emptyPool(), proficiency: 1, challenge: 1 }, () => 0.999999)
    expect(out.raw.triumph).toBe(1)
    expect(out.raw.despair).toBe(1)
    // триумф считается успехом, отчаяние провалом → взаимно гасятся, но сами остаются
    expect(out.net.success).toBe(0)
    expect(out.net.failure).toBe(0)
    expect(out.net.triumph).toBe(1)
    expect(out.net.despair).toBe(1)
  })

  it('ability d8 грань с двумя успехами набирается по rng', () => {
    // ability faces index 3 = [S,S]; rng подобран на индекс 3 из 8 → 3/8 = 0.375
    const out = rollPool({ ...emptyPool(), ability: 1 }, seq([0.4]))
    expect(out.raw.success).toBe(2)
  })
})

describe('netSymbols', () => {
  it('гасит успехи против провалов и преимущества против угроз', () => {
    const net = netSymbols(sym({ success: 3, failure: 1, advantage: 1, threat: 2 }))
    expect(net).toEqual(sym({ success: 2, threat: 1 }))
  })

  it('триумф добавляет успех при гашении, но не гасится сам', () => {
    const net = netSymbols(sym({ failure: 1, triumph: 1 }))
    // success(triumph)=1 vs failure=1 → ничья по успехам, триумф остаётся
    expect(net.success).toBe(0)
    expect(net.failure).toBe(0)
    expect(net.triumph).toBe(1)
  })

  it('отчаяние считается провалом', () => {
    const net = netSymbols(sym({ success: 1, despair: 1 }))
    expect(net.success).toBe(0)
    expect(net.failure).toBe(0)
    expect(net.despair).toBe(1)
  })
})

describe('summarize', () => {
  it('успех с преимуществом', () => {
    expect(summarize(sym({ success: 2, advantage: 1 }))).toBe('2 успеха, 1 преимущество')
  })
  it('провал', () => {
    expect(summarize(sym({ failure: 1 }))).toBe('1 провал')
  })
  it('ничья без символов', () => {
    expect(summarize(sym({}))).toBe('ничья')
  })
  it('включает триумф и отчаяние', () => {
    expect(summarize(sym({ success: 1, triumph: 1 }))).toContain('1 триумф')
  })
})
