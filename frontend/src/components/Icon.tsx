import type { SVGProps } from 'react'

export type IconName =
  | 'adjustments'
  | 'hammer'
  | 'sword'
  | 'lock'
  | 'coin'
  | 'check'
  | 'photo-plus'
  | 'target'
  | 'ghost-2'
  | 'shield-check'
  | 'heart-broken'
  | 'flask'
  | 'sparkles'
  | 'crown'
  | 'scroll'
  | 'hand-finger'
  | 'horse'
  | 'horse-toy'
  | 'feather'
  | 'link'
  | 'arrow-back-up'
  | 'arrow-bar-to-down'
  | 'chevron-right'
  | 'chevron-down'
  | 'point-filled'
  | 'circle-check'
  | 'circle-x'
  | 'info-circle'
  | 'alert-triangle'
  | 'dice-5'
  | 'truck'
  | 'alert'
  | 'arrow-left'
  | 'book'
  | 'copy'
  | 'dice'
  | 'file-import'
  | 'flame'
  | 'globe'
  | 'heart'
  | 'bolt'
  | 'shield'
  | 'package'
  | 'help'
  | 'logout'
  | 'menu'
  | 'close'
  | 'magic'
  | 'map'
  | 'plus'
  | 'printer'
  | 'share'
  | 'shop'
  | 'skull'
  | 'trash'
  | 'user-plus'
  | 'user'
  | 'users'

const paths: Record<IconName, string[]> = {
  'adjustments': ['M4 7h16', 'M4 17h16', 'M8 4v6', 'M16 14v6'],
  'hammer': ['m14 3 7 7-3 3-3-3L5 20l-3-3L12 7 9 4l5-1Z'],
  'sword': ['m4 20 13-13', 'M14 3h7v7L8 20l-4-4L14 3Z', 'm3 13 8 8'],
  'lock': ['M5 10h14v11H5z', 'M8 10V6a4 4 0 0 1 8 0v4', 'M12 14v3'],
  'coin': ['M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20Z', 'M15 7h-4a2 2 0 0 0 0 4h2a2 2 0 0 1 0 4H9', 'M12 5v12'],
  'check': ['m5 12 4 4L19 6'],
  'photo-plus': ['M3 4h14v16H3z', 'm3 15 5-5 5 5', 'M21 3v6', 'M18 6h6', 'M8 7h.01'],
  'target': ['M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20Z', 'M12 7a5 5 0 1 0 0 10 5 5 0 0 0 0-10Z', 'M12 12h.01'],
  'ghost-2': ['M4 21V10a8 8 0 0 1 16 0v11l-4-3-4 3-4-3-4 3Z', 'M9 10h.01', 'M15 10h.01'],
  'shield-check': ['M12 2 3 6v6c0 5 9 10 9 10s9-5 9-10V6l-9-4Z', 'm8 11 3 3 5-5'],
  'heart-broken': ['M12 5 9 2a6 6 0 0 0-7 7c0 4 10 12 10 12S22 13 22 9a6 6 0 0 0-7-7l-3 3Z', 'm12 5-2 5 4 3-2 4'],
  'flask': ['M9 3h6', 'M10 3v6L4 19a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2L14 9V3', 'M7 15h10'],
  'sparkles': ['m12 3 3 6 6 3-6 3-3 6-3-6-6-3 6-3 3-6Z', 'M21 1v4', 'M19 3h4'],
  'crown': ['m3 6 5 5 4-8 4 8 5-5-2 14H5L3 6Z'],
  'scroll': ['M7 4h12v14a3 3 0 0 1-6 0H4V7a3 3 0 0 1 6 0H4', 'M7 18v2a3 3 0 0 0 3 3h6', 'M11 9h5', 'M11 13h5'],
  'hand-finger': ['M9 12V4a2 2 0 0 1 4 0v8l4-2 4 3-2 8H9l-5-8a2 2 0 0 1 3-2l2 1Z'],
  'horse': ['M3 21v-8l4-3V4l3 2 3-2 8 9-3 3-4-3-1 8', 'M10 21v-5', 'M14 9h.01'],
  'horse-toy': ['M3 19c4 4 14 4 18 0', 'M6 18v-5l3-3V4l3 2 3-2 6 8-3 3-4-3-1 6'],
  'feather': ['m4 20 16-16', 'M6 18C0 8 15-2 21 3c5 6-5 21-15 15Z', 'M8 16h7', 'M12 12h7'],
  'link': ['m9 15 6-6', 'M7 14l-3 3a4 4 0 0 0 6 6l3-3', 'M11 4l3-3a4 4 0 0 1 6 6l-3 3'],
  'arrow-back-up': ['M5 9h9a6 6 0 0 1 0 12', 'm9 5-4 4 4 4'],
  'arrow-bar-to-down': ['M12 3v12', 'm7 10 5 5 5-5', 'M4 19h16'],
  'chevron-right': ['m9 5 7 7-7 7'],
  'chevron-down': ['m5 9 7 7 7-7'],
  'point-filled': ['M12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8Z'],
  'circle-check': ['M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20Z', 'm7 12 3 3 7-7'],
  'circle-x': ['M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20Z', 'm8 8 8 8', 'm16 8-8 8'],
  'info-circle': ['M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20Z', 'M12 11v6', 'M12 7h.01'],
  'alert-triangle': ['M12 8v5', 'M12 17h.01', 'M12 3 2 21h20L12 3Z'],
  'dice-5': ['M4 4h16v16H4z', 'M8 8h.01', 'M16 8h.01', 'M12 12h.01', 'M8 16h.01', 'M16 16h.01'],
  'truck': ['M2 5h12v13H2z', 'M14 9h4l4 5v4h-8', 'M6 18a2 2 0 1 0 0 4 2 2 0 0 0 0-4Z', 'M18 18a2 2 0 1 0 0 4 2 2 0 0 0 0-4Z'],

  alert: ['M12 9v4', 'M12 17h.01', 'M10.3 3.9 2.6 17.2A2 2 0 0 0 4.3 20h15.4a2 2 0 0 0 1.7-2.8L13.7 3.9a2 2 0 0 0-3.4 0Z'],
  'arrow-left': ['M5 12h14', 'm11 6-6 6 6 6'],
  book: ['M4 19.5A2.5 2.5 0 0 1 6.5 17H20', 'M4 4.5A2.5 2.5 0 0 1 6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15Z'],
  copy: ['M8 8h10v10H8z', 'M6 16H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1'],
  dice: ['M4 4h16v16H4z', 'M8 8h.01', 'M16 8h.01', 'M12 12h.01', 'M8 16h.01', 'M16 16h.01'],
  'file-import': ['M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z', 'M14 2v6h6', 'M12 17v-6', 'm9 14 3 3 3-3'],
  flame: ['M12 22c4 0 7-3 7-7 0-3-2-5-4-7 .2 2-1 3-2 4 0-4-2-7-5-10 1 5-3 7-3 11 0 4 3 6 7 6Z'],
  globe: ['M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20Z', 'M2 12h20', 'M12 2a15 15 0 0 1 4 10 15 15 0 0 1-4 10 15 15 0 0 1-4-10 15 15 0 0 1 4-10Z'],
  heart: ['M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1.1-1.1a5.5 5.5 0 0 0-7.8 7.8L12 21l8.8-8.6a5.5 5.5 0 0 0 0-7.8Z'],
  bolt: ['m13 2-9 12h7l-1 8 10-12h-7l1-8Z'],
  shield: ['M12 2 3 6v6c0 5 9 10 9 10s9-5 9-10V6l-9-4Z'],
  package: ['m12 3 9 5v9l-9 5-9-5V8l9-5Z', 'm3 8 9 5 9-5', 'M12 13v9', 'm7 5 10 5'],
  help: ['M12 18h.01', 'M9.1 9a3 3 0 1 1 5.2 2c-.9.6-1.3 1.1-1.3 2', 'M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z'],
  logout: ['M10 17l5-5-5-5', 'M15 12H3', 'M21 19V5a2 2 0 0 0-2-2h-6'],
  menu: ['M4 6h16', 'M4 12h16', 'M4 18h16'],
  close: ['M6 6l12 12', 'M18 6 6 18'],
  magic: ['M15 4V2', 'M15 16v-2', 'M8 9H6', 'M18 9h-2', 'M10.2 4.2 8.8 2.8', 'M15.2 15.2l-1.4-1.4', 'M10.2 13.8l-1.4 1.4', 'M15.2 2.8l-1.4 1.4', 'M3 21l8-8', 'm7 17 4-4'],
  map: ['M9 18 3 20V6L9 4l6 2 6-2v14l-6 2-6-2Z', 'M9 4v14', 'M15 6v14'],
  plus: ['M12 5v14', 'M5 12h14'],
  printer: ['M6 9V2h12v7', 'M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2', 'M6 14h12v8H6z'],
  share: ['M18 8a3 3 0 1 0-2.8-4', 'M6 14a3 3 0 1 0 2.8 4', 'M18 16a3 3 0 1 0-2.8 4', 'M8.6 13.5l6.8 3', 'M15.4 7.5l-6.8 3'],
  shop: ['M3 9h18', 'M5 9v11h14V9', 'M4 4h16l1 5H3l1-5Z', 'M9 20v-6h6v6'],
  skull: ['M12 2a8 8 0 0 0-8 8v3a4 4 0 0 0 4 4v3h8v-3a4 4 0 0 0 4-4v-3a8 8 0 0 0-8-8Z', 'M9 11h.01', 'M15 11h.01', 'M10 16v2', 'M14 16v2'],
  trash: ['M3 6h18', 'M8 6V4h8v2', 'M6 6l1 16h10l1-16', 'M10 11v6', 'M14 11v6'],
  'user-plus': ['M15 19a6 6 0 0 0-12 0', 'M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8Z', 'M19 8v6', 'M16 11h6'],
  user: ['M20 21a8 8 0 0 0-16 0', 'M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8Z'],
  users: ['M17 21a5 5 0 0 0-10 0', 'M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8Z', 'M22 21a4 4 0 0 0-4-4', 'M16 3.1a4 4 0 0 1 0 7.8'],
}

export function Icon({ name, ...props }: { name: IconName } & SVGProps<SVGSVGElement>) {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor"
      strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" {...props}>
      {paths[name].map((d, i) => <path key={i} d={d} />)}
    </svg>
  )
}
