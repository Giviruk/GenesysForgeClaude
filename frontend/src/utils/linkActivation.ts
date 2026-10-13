import type { KeyboardEvent } from 'react'

/** Activate a card link without intercepting keyboard events from its child controls. */
export function activateLinkOnKey(event: KeyboardEvent<HTMLElement>, activate: () => void): void {
  if (event.target !== event.currentTarget || (event.key !== 'Enter' && event.key !== ' ')) return
  event.preventDefault()
  activate()
}
