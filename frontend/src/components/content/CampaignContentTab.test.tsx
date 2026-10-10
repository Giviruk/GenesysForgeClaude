import {render,screen,fireEvent,waitFor} from '@testing-library/react'
import {beforeEach,describe,it,expect,vi} from 'vitest'
import {CampaignContentTab} from './CampaignContentTab'
const methods=vi.hoisted(()=>({base:vi.fn(),entries:vi.fn(),decision:vi.fn(),save:vi.fn()}))
const summary={systems:[{system:'genesysCore',isOpen:true,characters:[],categories:[{category:'skill',total:1,enabled:1}]},{system:'realmsOfTerrinoth',isOpen:true,characters:[],categories:[]}],packs:[{id:'p',name:'World',system:'genesysCore',isEnabled:true,isMine:true,status:'active',ownerName:'GM',ownerIsMember:true,entryCount:1,exclusionCount:0,updatePolicy:'manual',entries:[{entryType:'skill',entryId:'original-s',name:'Sailing',state:'pending'}]}],items:[{id:'connection',entryId:'original-i',entryType:'skill',name:'Navigation',system:'genesysCore',meta:'',status:'pending',ownerName:'Player',isEnabled:false}],alerts:[],availableCount:1,overrideCount:0}
vi.mock('../../api/client',()=>({api:{campaignContent:vi.fn(async()=>summary),campaignBase:vi.fn(async()=>[{category:'skill',key:'gc.skill.a',name:'Athletics',nameRu:'Атлетика',enabled:true,meta:'',usedBy:[]}]),setCampaignBase:methods.base,setCampaignPackEntries:methods.entries,decideCampaignContent:methods.decision,saveCampaignBaseAsPack:methods.save}}))
beforeEach(()=>{vi.clearAllMocks();window.history.replaceState(null,'','/campaigns/c/content');methods.base.mockResolvedValue(undefined);methods.entries.mockResolvedValue(undefined);methods.decision.mockResolvedValue(undefined);methods.save.mockResolvedValue({id:'saved'})})
describe('campaign content',()=>{
 it('changes book availability and preserves category in the URL',async()=>{
  render(<CampaignContentTab campaignId="c"/>);fireEvent.click(await screen.findByRole('button',{name:'Системы'}))
  fireEvent.click(await screen.findByRole('switch',{name:'Доступен: Атлетика'}))
  await waitFor(()=>expect(methods.base).toHaveBeenCalledWith('c','genesysCore',[{category:'skill',key:'gc.skill.a',enabled:false}]))
  expect(window.location.search).toContain('section=systems');expect(window.location.search).toContain('category=skill')
 })
 it('approves a pending pack entry and a proposal using their original IDs',async()=>{
  render(<CampaignContentTab campaignId="c"/>);fireEvent.click(await screen.findByRole('button',{name:'Наборы'}))
  fireEvent.click(screen.getByRole('checkbox',{name:/Sailing/}))
  await waitFor(()=>expect(methods.entries).toHaveBeenCalledWith('c','p',[{entryType:'skill',entryId:'original-s',enabled:true}]))
  fireEvent.click(screen.getByRole('button',{name:'Отдельные элементы'}));fireEvent.click(screen.getByRole('button',{name:'Одобрить'}))
  await waitFor(()=>expect(methods.decision).toHaveBeenCalledWith('c','item','original-i','approve'))
 })
 it('shows save-as-pack confirmation after the server preserves availability',async()=>{
  vi.spyOn(window,'prompt').mockReturnValue('Saved rules')
  render(<CampaignContentTab campaignId="c"/>);fireEvent.click(await screen.findByRole('button',{name:'Системы'}));fireEvent.click(screen.getByRole('button',{name:'Сохранить как набор'}))
  await waitFor(()=>expect(methods.save).toHaveBeenCalledWith('c','genesysCore','Saved rules'))
  expect((await screen.findByRole('status')).textContent).toContain('разрешающие правки сохранены')
  vi.restoreAllMocks()
 })
})
