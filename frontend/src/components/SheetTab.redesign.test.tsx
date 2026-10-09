import { render, screen, fireEvent } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { CharacterSheet } from '../api/types'
import { SheetTab } from './SheetTab'
import { VitalCard } from './content/VitalCard'
vi.mock('./CriticalInjuriesSection', () => ({ CriticalInjuriesSection: () => null }))
vi.mock('../dice-roller-store', () => ({useDiceRoller:()=>({openRoller:vi.fn()})}))
const sheet={id:'redesign',system:'genesysCore',isCreationPhase:true,availableXp:100,
  archetype:{brawn:2,agility:2,intellect:2,cunning:2,willpower:2,presence:2}, characteristics:{brawn:2,agility:2,intellect:2,cunning:2,willpower:2,presence:2},
  woundsCurrent:2,strainCurrent:0,derived:{woundThreshold:10,strainThreshold:10,soak:2,meleeDefense:0,rangedDefense:0,encumbranceLoad:0,encumbranceThreshold:7,encumbered:false},
  skills:[{skillDefId:'s',name:'Navigation',nameRu:'Навигация',characteristic:'agility',kind:'general',ranks:1,freeRanks:1,isCareer:true,nextRankCost:10,pool:{ability:1,proficiency:1},nextPool:{ability:0,proficiency:3},setbackDice:2,boostDice:1,difficultyDice:1,difficultyUpgrades:1}],
} as unknown as CharacterSheet
beforeEach(()=>localStorage.clear())
describe('redesigned sheet',()=>{
 it('persists progression per character and removes editing controls from a readonly sheet',()=>{
  const props={sheet,onError:vi.fn(),refresh:vi.fn()}
  const view=render(<SheetTab {...props}/>)
  fireEvent.click(screen.getByRole('button',{name:'Прокачка'}))
  expect(localStorage.getItem('genesysforge.skillProgress.redesign')).toBe('false')
  expect(view.container.querySelector('.rd-buy')).toBeNull()
  view.unmount();const next=render(<SheetTab {...props}/>)
  expect(next.container.querySelector('.rd-buy')).toBeNull()
  next.rerender(<SheetTab {...props} readOnly/>)
  expect(screen.queryByRole('button',{name:'Прокачка'})).toBeNull()
  expect(next.container.querySelector('.rd-vital button')).toBeNull()
 })
 it('previews the next rank without losing modifier dice or allowing a free rank refund',()=>{
  const view=render(<SheetTab sheet={sheet} onError={vi.fn()} refresh={vi.fn()}/>)
  const modifiers=()=>view.container.querySelectorAll('.die.boost,.die.setback,.die.difficulty,.die-upgrade').length
  expect(modifiers()).toBe(5)
  fireEvent.mouseEnter(view.container.querySelector('.rd-buy')!)
  expect(view.container.querySelectorAll('.rd-rank-diamonds i')).toHaveLength(5)
  expect(view.container.querySelector('.rd-rank-diamonds .preview')).toBeTruthy()
  expect(view.container.querySelectorAll('.die.proficiency')).toHaveLength(3)
  expect(modifiers()).toBe(5)
  fireEvent.mouseLeave(view.container.querySelector('.rd-buy')!)
  expect(view.container.querySelectorAll('.die.proficiency')).toHaveLength(1)
  expect(modifiers()).toBe(5)
  expect((view.container.querySelector('.rd-refund') as HTMLButtonElement).disabled).toBe(true)
 })
 it('keeps the current pool when an older response has no server preview',()=>{
  const legacy={...sheet,skills:[{...sheet.skills[0],nextPool:null}]}
  const view=render(<SheetTab sheet={legacy} onError={vi.fn()} refresh={vi.fn()}/>)
  fireEvent.mouseEnter(view.container.querySelector('.rd-buy')!)
  expect(view.container.querySelector('.rd-skill-preview')).toBeNull()
  expect(view.container.querySelectorAll('.die.proficiency')).toHaveLength(1)
 })
 it('shows overflow without clamping damage and prevents decrementing below zero',()=>{
  const onChange=vi.fn();const view=render(<VitalCard kind="wounds" current={12} threshold={10} onChange={onChange}/>)
  expect(screen.getByRole('meter').getAttribute('aria-valuetext')).toContain('12 / 10')
  expect(screen.getByText('Порог превышен')).toBeTruthy()
  fireEvent.click(screen.getByRole('button',{name:'Увеличить: Раны'}));expect(onChange).toHaveBeenCalledWith(13)
  view.rerender(<VitalCard kind="wounds" current={0} threshold={10} onChange={onChange}/>)
  expect((screen.getByRole('button',{name:'Уменьшить: Раны'}) as HTMLButtonElement).disabled).toBe(true)
 })
 it('blocks the third creation rank and its preview while retaining the roll action',()=>{
  const capped={...sheet,skills:[{...sheet.skills[0],ranks:2,pool:{ability:0,proficiency:2}}]}
  const view=render(<SheetTab sheet={capped} onError={vi.fn()} refresh={vi.fn()}/>)
  const buy=view.container.querySelector('.rd-buy') as HTMLButtonElement
  expect(buy.disabled).toBe(true)
  expect(buy.title).toContain('максимальный ранг — 2')
  fireEvent.mouseEnter(buy)
  expect(view.container.querySelector('.rd-rank-diamonds .preview')).toBeNull()
  expect((view.container.querySelector('.rd-roll') as HTMLButtonElement).disabled).toBe(false)
  view.rerender(<SheetTab sheet={{...capped,isCreationPhase:false}} onError={vi.fn()} refresh={vi.fn()}/>)
  expect((view.container.querySelector('.rd-buy') as HTMLButtonElement).disabled).toBe(false)
 })
})
