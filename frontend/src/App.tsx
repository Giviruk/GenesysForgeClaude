import { useState } from 'react'
import { AuthProvider } from './auth'
import { useAuth } from './auth-context'
import { AuthPage } from './pages/AuthPage'
import { CharactersPage } from './pages/CharactersPage'
import { CampaignsPage } from './pages/CampaignsPage'
import { NpcsPage } from './pages/NpcsPage'
import { SheetPage } from './pages/SheetPage'

type Area = 'characters' | 'campaigns' | 'npcs'

function Shell() {
  const { token, logout } = useAuth()
  const [area, setArea] = useState<Area>('characters')
  const [characterId, setCharacterId] = useState<string | null>(null)

  if (!token) return <AuthPage />

  function go(next: Area) {
    setCharacterId(null)
    setArea(next)
  }

  return (
    <div className="shell">
      <header className="topbar">
        <span className="logo" onClick={() => go('characters')}>Genesys Forge</span>
        <nav className="topnav">
          <button className={area === 'characters' ? 'tab active' : 'tab'} onClick={() => go('characters')}>Персонажи</button>
          <button className={area === 'campaigns' ? 'tab active' : 'tab'} onClick={() => go('campaigns')}>Кампании</button>
          <button className={area === 'npcs' ? 'tab active' : 'tab'} onClick={() => go('npcs')}>Бестиарий</button>
        </nav>
        <button className="small" onClick={() => { setCharacterId(null); logout() }}>Выйти</button>
      </header>
      {area === 'characters'
        ? (characterId
            ? <SheetPage characterId={characterId} onBack={() => setCharacterId(null)} />
            : <CharactersPage onOpen={setCharacterId} />)
        : area === 'campaigns'
          ? <CampaignsPage />
          : <NpcsPage />}
    </div>
  )
}

export default function App() {
  return (
    <AuthProvider>
      <Shell />
    </AuthProvider>
  )
}
