import type { SVGProps } from 'react'

export type IconName =
  | 'alert'
  | 'arrow-left'
  | 'book'
  | 'copy'
  | 'dice'
  | 'file-import'
  | 'flame'
  | 'help'
  | 'logout'
  | 'magic'
  | 'map'
  | 'plus'
  | 'printer'
  | 'share'
  | 'skull'
  | 'trash'
  | 'user-plus'
  | 'user'
  | 'users'

const paths: Record<IconName, string[]> = {
  alert: ['M12 9v4', 'M12 17h.01', 'M10.3 3.9 2.6 17.2A2 2 0 0 0 4.3 20h15.4a2 2 0 0 0 1.7-2.8L13.7 3.9a2 2 0 0 0-3.4 0Z'],
  'arrow-left': ['M5 12h14', 'm11 6-6 6 6 6'],
  book: ['M4 19.5A2.5 2.5 0 0 1 6.5 17H20', 'M4 4.5A2.5 2.5 0 0 1 6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15Z'],
  copy: ['M8 8h10v10H8z', 'M6 16H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1'],
  dice: ['M4 4h16v16H4z', 'M8 8h.01', 'M16 8h.01', 'M12 12h.01', 'M8 16h.01', 'M16 16h.01'],
  'file-import': ['M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z', 'M14 2v6h6', 'M12 17v-6', 'm9 14 3 3 3-3'],
  flame: ['M12 22c4 0 7-3 7-7 0-3-2-5-4-7 .2 2-1 3-2 4 0-4-2-7-5-10 1 5-3 7-3 11 0 4 3 6 7 6Z'],
  help: ['M12 18h.01', 'M9.1 9a3 3 0 1 1 5.2 2c-.9.6-1.3 1.1-1.3 2', 'M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z'],
  logout: ['M10 17l5-5-5-5', 'M15 12H3', 'M21 19V5a2 2 0 0 0-2-2h-6'],
  magic: ['M15 4V2', 'M15 16v-2', 'M8 9H6', 'M18 9h-2', 'M10.2 4.2 8.8 2.8', 'M15.2 15.2l-1.4-1.4', 'M10.2 13.8l-1.4 1.4', 'M15.2 2.8l-1.4 1.4', 'M3 21l8-8', 'm7 17 4-4'],
  map: ['M9 18 3 20V6L9 4l6 2 6-2v14l-6 2-6-2Z', 'M9 4v14', 'M15 6v14'],
  plus: ['M12 5v14', 'M5 12h14'],
  printer: ['M6 9V2h12v7', 'M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2', 'M6 14h12v8H6z'],
  share: ['M18 8a3 3 0 1 0-2.8-4', 'M6 14a3 3 0 1 0 2.8 4', 'M18 16a3 3 0 1 0-2.8 4', 'M8.6 13.5l6.8 3', 'M15.4 7.5l-6.8 3'],
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
