import {render,screen,fireEvent,within,waitFor} from '@testing-library/react'
import {describe,it,expect,vi} from 'vitest'
import {PickerModal} from './ContentUi'
const rows=[{key:'skill:a',group:'skill',groupLabel:'Навыки',name:'Sailing'},{key:'talent:b',group:'talent',groupLabel:'Таланты',name:'Resolve'}]
describe('content picker',()=>{
 it('retains selected entries when search hides them and selects only visible rows',async()=>{
  const save=vi.fn().mockResolvedValue(undefined);const close=vi.fn()
  render(<PickerModal title="Выбор" rows={rows} onClose={close} onConfirm={save}/>)
  const dialog=within(screen.getByRole('dialog'))
  fireEvent.click(dialog.getByRole('checkbox',{name:'Sailing'}))
  fireEvent.change(dialog.getByRole('searchbox'),{target:{value:'Resolve'}})
  fireEvent.click(dialog.getByRole('checkbox',{name:'Выбрать видимые (1)'}))
  fireEvent.click(dialog.getByRole('button',{name:'Добавить (2)'}))
  await waitFor(()=>expect(save).toHaveBeenCalledWith(['skill:a','talent:b']))
  await waitFor(()=>expect(close).toHaveBeenCalledOnce())
 })
 it('preserves selection and dialog when saving fails',async()=>{
  const close=vi.fn();render(<PickerModal title="Выбор" rows={rows} onClose={close} onConfirm={vi.fn().mockRejectedValue(new Error('Отказ'))}/>)
  fireEvent.click(screen.getByRole('checkbox',{name:'Sailing'}));fireEvent.click(screen.getByRole('button',{name:'Добавить (1)'}))
  expect((await screen.findByRole('alert')).textContent).toBe('Отказ')
  expect((screen.getByRole('checkbox',{name:'Sailing'}) as HTMLInputElement).checked).toBe(true)
  expect(close).not.toHaveBeenCalled()
 })
})
