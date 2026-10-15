import { fireEvent, screen, type ByRoleOptions } from '@testing-library/react'

/** Choose through the visible listbox, as a pointer user does. */
export function chooseSelect(field: HTMLElement, value: string) {
  fireEvent.pointerDown(field)
  fireEvent.mouseDown(field)
  fireEvent.click(field)
  const list = document.getElementById(field.getAttribute('aria-controls')!)!
  const option = [...list.querySelectorAll<HTMLElement>('[role=option]')]
    .find(element => element.dataset.value === value)
  if (!option) throw new Error(`Select option not found: ${value}`)
  fireEvent.click(option)
}

export async function waitForSelectOption(field: HTMLElement, options: ByRoleOptions) {
  fireEvent.click(field)
  const option = await screen.findByRole('option', options)
  fireEvent.keyDown(field, { key: 'Escape' })
  return option
}
