import { act, fireEvent, render, screen } from '@testing-library/react'
import { useState } from 'react'
import { afterEach, expect, it, vi } from 'vitest'
import { Select } from './Select'

const options = [{ value: '', label: 'Choose' }, { value: 'a', label: 'Alpha', group: 'Letters' },
  { value: 'b', label: 'Beta', disabled: true, group: 'Letters' }, { value: 'g', label: 'Gamma', group: 'Letters' }]
function Form({ disabled = false, required = false }) {
  const [value, setValue] = useState('')
  return <form aria-label="Form"><label htmlFor="choice">Choice</label><Select id="choice" name="choice" value={value}
    onChange={setValue} disabled={disabled} required={required}>{options.map(option => <option key={option.value} value={option.value} disabled={option.disabled}>{option.label}</option>)}</Select><button>Submit</button></form>
}
afterEach(() => vi.useRealTimers())

it('opens a portal, selects with a pointer and synchronizes FormData', () => {
  render(<Form />)
  const field = screen.getByRole('combobox', { name: 'Choice' })
  fireEvent.click(field)
  expect(screen.getByRole('listbox').parentElement).toBe(document.body)
  fireEvent.click(screen.getByRole('option', { name: 'Gamma' }))
  expect(field.textContent).toBe('Gamma')
  expect(field.title).toBe('Gamma')
  expect(screen.queryByRole('listbox')).toBeNull()
  expect(new FormData(screen.getByRole('form') as HTMLFormElement).get('choice')).toBe('g')
  expect(document.activeElement).toBe(field)
})
it('navigates with arrows, skips disabled options and commits only with Enter', () => {
  render(<Form />)
  const field = screen.getByRole('combobox')
  fireEvent.keyDown(field, { key: 'ArrowDown', altKey: true })
  fireEvent.keyDown(field, { key: 'ArrowDown' })
  fireEvent.keyDown(field, { key: 'ArrowDown' })
  expect(field.getAttribute('aria-activedescendant')).toBe(screen.getByRole('option', { name: 'Gamma' }).id)
  expect(new FormData(screen.getByRole('form') as HTMLFormElement).get('choice')).toBe('')
  fireEvent.keyDown(field, { key: 'Enter' })
  expect(field.textContent).toBe('Gamma')
})
it('supports Home, End and PageUp/PageDown, Space and Escape', () => {
  render(<Form />)
  const field = screen.getByRole('combobox')
  fireEvent.keyDown(field, { key: 'End' })
  fireEvent.keyDown(field, { key: 'PageUp' })
  fireEvent.keyDown(field, { key: 'PageDown' })
  fireEvent.keyDown(field, { key: 'Escape' })
  expect(field.textContent).toBe('Choose')
  fireEvent.keyDown(field, { key: 'Home' })
  fireEvent.keyDown(field, { key: 'ArrowDown' })
  fireEvent.keyDown(field, { key: ' ' })
  expect(field.textContent).toBe('Alpha')
})
it('uses typeahead while closed and resets its prefix after 500 ms', () => {
  vi.useFakeTimers()
  render(<Form />)
  const field = screen.getByRole('combobox')
  fireEvent.keyDown(field, { key: 'g' })
  expect(field.textContent).toBe('Gamma')
  vi.advanceTimersByTime(501)
  fireEvent.keyDown(field, { key: 'a' })
  expect(field.textContent).toBe('Alpha')
})
it('closes on Tab and outside pointer without committing the highlighted option', () => {
  render(<Form />)
  const field = screen.getByRole('combobox')
  fireEvent.keyDown(field, { key: 'End' })
  fireEvent.keyDown(field, { key: 'Tab' })
  expect(screen.queryByRole('listbox')).toBeNull()
  fireEvent.click(field)
  fireEvent.pointerDown(document.body)
  expect(screen.queryByRole('listbox')).toBeNull()
  expect(field.textContent).toBe('Choose')
})
it('does not open a disabled field', () => {
  render(<Form disabled />)
  fireEvent.click(screen.getByRole('combobox'))
  expect(screen.queryByRole('listbox')).toBeNull()
})
it('consumes Escape only while its popup is open, preserving the enclosing modal', () => {
  const parentKey = vi.fn()
  render(<div onKeyDown={parentKey}><Form /></div>)
  const field = screen.getByRole('combobox')
  fireEvent.click(field)
  fireEvent.keyDown(field, { key: 'Escape' })
  expect(parentKey).not.toHaveBeenCalled()
  fireEvent.keyDown(field, { key: 'Escape' })
  expect(parentKey).toHaveBeenCalledOnce()
})
it('keeps a dialog popup inside the modal subtree', () => {
  render(<dialog open aria-label="Picker"><Form /></dialog>)
  fireEvent.click(screen.getByRole('combobox'))
  // jsdom has no native top layer; browser coverage checks visibility and pointer selection.
  const popup = screen.getByRole('listbox', { hidden: true })
  expect(popup.parentElement).toBe(screen.getByRole('dialog'))
  expect(popup.getAttribute('popover')).toBe('manual')
  fireEvent.click(popup.querySelector('[data-value=g]')!)
  expect(screen.getByRole('combobox').textContent).toBe('Gamma')
})
it('preserves native required validation and focuses the visible field', () => {
  render(<Form required />)
  const form = screen.getByRole('form') as HTMLFormElement
  act(() => { expect(form.checkValidity()).toBe(false) })
  expect(document.activeElement).toBe(screen.getByRole('combobox'))
  expect(screen.getByRole('combobox').getAttribute('aria-invalid')).toBe('true')
  fireEvent.click(screen.getByRole('option', { name: 'Alpha' }))
  expect(form.checkValidity()).toBe(true)
  expect(screen.getByRole('combobox').hasAttribute('aria-invalid')).toBe(false)
})
it('reads option children and disabled optgroups', () => {
  const changed = vi.fn()
  render(<Select aria-label="Grouped" value="a" onChange={changed}><optgroup label="Disabled" disabled>
    <option value="a">Alpha</option></optgroup><option value="g">Gamma</option></Select>)
  fireEvent.click(screen.getByRole('combobox'))
  fireEvent.click(screen.getByRole('option', { name: 'Alpha' }))
  expect(changed).not.toHaveBeenCalled()
  fireEvent.click(screen.getByRole('option', { name: 'Gamma' }))
  expect(changed).toHaveBeenCalledWith('g')
})

it('focuses and opens only the first invalid select in a form', () => {
  render(<form aria-label="Two choices"><label htmlFor="first">First</label>
    <Select id="first" name="first" value="" onChange={() => {}} required><option value="">Choose</option><option value="a">Alpha</option></Select>
    <label htmlFor="second">Second</label><Select id="second" name="second" value="" onChange={() => {}} required><option value="">Choose</option><option value="b">Beta</option></Select>
  </form>)
  act(() => { expect((screen.getByRole('form') as HTMLFormElement).checkValidity()).toBe(false) })
  expect(document.activeElement).toBe(screen.getByRole('combobox', { name: 'First' }))
  expect(screen.getAllByRole('listbox')).toHaveLength(1)
  expect(screen.getByRole('listbox').getAttribute('aria-labelledby')).toBe('first')
})

it('leaves text input validation in charge and closes an already open menu', () => {
  render(<form aria-label="Text first"><input aria-label="Name" required />
    <Select aria-label="Choice" value="a" onChange={() => {}} required><option value="a">Alpha</option></Select>
  </form>)
  fireEvent.click(screen.getByRole('combobox'))
  act(() => { expect((screen.getByRole('form') as HTMLFormElement).checkValidity()).toBe(false) })
  expect(screen.queryByRole('listbox')).toBeNull()
  expect(screen.getByRole('combobox').getAttribute('aria-expanded')).toBe('false')
})

it('preserves option descriptions in both the popup and native mirror', () => {
  const { container } = render(<Select value="a" onChange={() => {}}><option value="a" title="Own quality description">Alpha</option></Select>)
  fireEvent.click(screen.getByRole('combobox'))
  expect(screen.getByRole('option', { name: 'Alpha' }).title).toBe('Own quality description')
  expect(container.querySelector('select option')?.getAttribute('title')).toBe('Own quality description')
})

it('does not submit the first option when a controlled value has no match', () => {
  render(<form aria-label="Missing value"><Select name="choice" value="retired" onChange={() => {}} required><option value="a">Alpha</option></Select></form>)
  const form = screen.getByRole('form') as HTMLFormElement
  expect(new FormData(form).get('choice')).not.toBe('a')
  act(() => { expect(form.checkValidity()).toBe(false) })
  expect(screen.getByRole('combobox').getAttribute('aria-invalid')).toBe('true')
})
