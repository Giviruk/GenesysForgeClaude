import { AuthProvider } from './auth'
import { useAuth } from './auth-context'
import { AuthPage } from './pages/AuthPage'
import { CharactersPage } from './pages/CharactersPage'
import { CampaignsPage } from './pages/CampaignsPage'
import { NpcsPage } from './pages/NpcsPage'
import { SheetPage } from './pages/SheetPage'
import { MagicPage } from './pages/MagicPage'
import { ReferencePage } from './pages/ReferencePage'
import { AboutPage } from './pages/AboutPage'
import { HelpPage } from './pages/HelpPage'
import { ProfilePage } from './pages/ProfilePage'
import { SharedSheetPage } from './pages/SharedSheetPage'
import { Footer } from './components/Footer'
import { navigate, parseRoute, usePath, type AppArea } from './router'
import type { CampaignView } from './pages/CampaignsPage'

const campaignView = (sub: string | null): CampaignView =>
  sub === 'table' || sub === 'handbook' || sub === 'encounters' ? sub : 'overview'

function Shell() {
  const { token, logout } = useAuth()
  const path = usePath()

  const route = parseRoute(path)

  // «О проекте» доступна публично — до проверки токена (виден дисклеймер до входа).
  if (route.area === 'about') return <AboutPage loggedIn={!!token} />
  // Публичный read-only лист по share-token не требует логина.
  if (route.area === 'share') {
    return route.unknown || !route.id
      ? <NotFound />
      : <SharedSheetPage token={route.id} loggedIn={!!token} />
  }
  // Справка доступна публично, чтобы новый пользователь мог разобраться до регистрации.
  if (route.area === 'help' && !token) return <HelpPage loggedIn={false} />

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
          <button className={route.area === 'reference' ? 'tab active' : 'tab'} onClick={() => go('reference')}>Справочник</button>
          <button className={route.area === 'help' ? 'tab active' : 'tab'} onClick={() => go('help')}>Справка</button>
        </nav>
        <button className={route.area === 'account' ? 'tab active' : 'tab'} onClick={() => go('account')}>Профиль</button>
        <button className="small" onClick={() => { logout(); navigate('/login') }}>Выйти</button>
      </header>
      {route.unknown
        ? <NotFound />
        : route.area === 'characters'
          ? (route.id
              ? <SheetPage characterId={route.id}
                  printing={route.sub === 'print'}
                  onOpenPrint={() => navigate(`/characters/${route.id}/print`)}
                  onClosePrint={() => navigate(`/characters/${route.id}`)}
                  onBack={() => navigate('/characters')} />
              : <CharactersPage onOpen={id => navigate(`/characters/${id}`)} />)
          : route.area === 'campaigns'
            ? <CampaignsPage openId={route.id}
                view={campaignView(route.sub)} openEncounterId={route.subId}
                onOpen={id => navigate(`/campaigns/${id}`)} onBack={() => navigate('/campaigns')}
                onView={view => navigate(view === 'overview' ? `/campaigns/${route.id}` : `/campaigns/${route.id}/${view}`)}
                onOpenEncounter={eid => navigate(`/campaigns/${route.id}/encounters/${eid}`)}
                onCloseEncounter={() => navigate(`/campaigns/${route.id}/encounters`)} />
            : route.area === 'npcs'
              ? <NpcsPage openId={route.id}
                  onOpen={id => navigate(`/npcs/${id}`)} onBack={() => navigate('/npcs')} />
              : route.area === 'magic'
                ? <MagicPage />
              : route.area === 'account'
                ? <ProfilePage onBack={() => navigate('/characters')} />
                : route.area === 'help'
                  ? <HelpPage loggedIn showFooter={false} />
                  : <ReferencePage onNavigate={navigate} />}
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
