import { AuthProvider } from './auth'
import { useAuth } from './auth-context'
import { AuthPage } from './pages/AuthPage'
import { CharactersPage } from './pages/CharactersPage'
import { CampaignsPage } from './pages/CampaignsPage'
import { NpcsPage } from './pages/NpcsPage'
import { SheetPage } from './pages/SheetPage'
import { MagicPage } from './pages/MagicPage'
import { AboutPage } from './pages/AboutPage'
import { Footer } from './components/Footer'
import { navigate, parseRoute, usePath, type AppArea } from './router'

function Shell() {
  const { token, logout } = useAuth()
  const path = usePath()

  const route = parseRoute(path)

  // «О проекте» доступна публично — до проверки токена (виден дисклеймер до входа).
  if (route.area === 'about') return <AboutPage loggedIn={!!token} />

  if (!token) return <AuthPage />

  function go(area: AppArea) {
    navigate(`/${area}`)
  }

  return (
    <div className="shell">
      <header className="topbar">
        <span className="logo" onClick={() => go('characters')}>Genesys Forge</span>
        <nav className="topnav">
          <button className={route.area === 'characters' ? 'tab active' : 'tab'} onClick={() => go('characters')}>Персонажи</button>
          <button className={route.area === 'campaigns' ? 'tab active' : 'tab'} onClick={() => go('campaigns')}>Кампании</button>
          <button className={route.area === 'npcs' ? 'tab active' : 'tab'} onClick={() => go('npcs')}>Бестиарий</button>
          <button className={route.area === 'magic' ? 'tab active' : 'tab'} onClick={() => go('magic')}>Магия</button>
        </nav>
        <button className="small" onClick={() => { logout(); navigate('/login') }}>Выйти</button>
      </header>
      {route.unknown
        ? <NotFound />
        : route.area === 'characters'
          ? (route.id
              ? <SheetPage characterId={route.id} onBack={() => navigate('/characters')} />
              : <CharactersPage onOpen={id => navigate(`/characters/${id}`)} />)
          : route.area === 'campaigns'
            ? <CampaignsPage openId={route.id}
                onOpen={id => navigate(`/campaigns/${id}`)} onBack={() => navigate('/campaigns')} />
            : route.area === 'npcs'
              ? <NpcsPage openId={route.id}
                  onOpen={id => navigate(`/npcs/${id}`)} onBack={() => navigate('/npcs')} />
              : <MagicPage />}
      <Footer />
    </div>
  )
}

function NotFound() {
  return (
    <div className="page">
      <div className="panel">
        <h2>Страница не найдена</h2>
        <p className="muted">Такого адреса нет. Вернитесь к списку персонажей.</p>
        <button className="primary" onClick={() => navigate('/characters')}>← К персонажам</button>
      </div>
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
