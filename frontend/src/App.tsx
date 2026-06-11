import { useState } from 'react'
import { AuthProvider, useAuth } from './auth'
import { AuthPage } from './pages/AuthPage'
import { CharactersPage } from './pages/CharactersPage'
import { SheetPage } from './pages/SheetPage'

function Shell() {
  const { token, logout } = useAuth()
  const [characterId, setCharacterId] = useState<string | null>(null)

  if (!token) return <AuthPage />

  return (
    <div className="shell">
      <header className="topbar">
        <span className="logo" onClick={() => setCharacterId(null)}>Genesys Forge</span>
        <button className="small" onClick={() => { setCharacterId(null); logout() }}>Выйти</button>
      </header>
      {characterId
        ? <SheetPage characterId={characterId} onBack={() => setCharacterId(null)} />
        : <CharactersPage onOpen={setCharacterId} />}
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
