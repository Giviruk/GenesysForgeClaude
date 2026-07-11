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
import { Icon, type IconName } from './components/Icon'
import { navigate, parseRoute, usePath, type AppArea } from './router'
import { useSeo } from './seo'
import type { CampaignView } from './pages/CampaignsPage'
import { DiceRollerProvider } from './dice-roller-context'

const campaignView = (sub: string | null): CampaignView =>
  sub === 'table' || sub === 'handbook' || sub === 'encounters' ? sub : 'overview'

const NAV_ITEMS: Array<{ area: AppArea; label: string; path: string; icon: IconName }> = [
  { area: 'characters', label: 'Персонажи', path: '/characters', icon: 'users' },
  { area: 'npcs', label: 'Бестиарий', path: '/npcs', icon: 'skull' },
  { area: 'campaigns', label: 'Кампании', path: '/campaigns', icon: 'map' },
  { area: 'magic', label: 'Магия', path: '/magic', icon: 'flame' },
]

const FOOTER_NAV_ITEMS: Array<{ area: AppArea; label: string; path: string; icon: IconName }> = [
  { area: 'reference', label: 'Справочник', path: '/reference', icon: 'book' },
  { area: 'account', label: 'Профиль', path: '/account', icon: 'user' },
  { area: 'help', label: 'Справка', path: '/help', icon: 'help' },
]

function Shell() {
  const { token, logout } = useAuth()
  const path = usePath()

  const route = parseRoute(path)
  useSeo(route, path)

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

  return (
    <div className="app-shell">
      <aside className="app-sidebar no-print" aria-label="Основная навигация">
        <button type="button" className="app-brand" onClick={() => navigate('/characters')}>GENESYSFORGE</button>
        <nav className="side-nav">
          {NAV_ITEMS.map(item => (
            <button key={item.area} type="button"
              className={route.area === item.area ? 'side-nav-item active' : 'side-nav-item'}
              onClick={() => navigate(item.path)}>
              <Icon name={item.icon} className="side-nav-icon" />
              {item.label}
            </button>
          ))}
        </nav>
        <div className="side-nav-footer">
          {FOOTER_NAV_ITEMS.map(item => (
            <button key={item.area} type="button"
              className={route.area === item.area ? 'side-nav-item active' : 'side-nav-item'}
              onClick={() => navigate(item.path)}>
              <Icon name={item.icon} className="side-nav-icon" />
              {item.label}
            </button>
          ))}
          <button type="button" className="side-nav-item" onClick={() => { logout(); navigate('/login') }}>
            <Icon name="logout" className="side-nav-icon" />
            Выйти
          </button>
        </div>
      </aside>

      <div className="shell">
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
      <DiceRollerProvider>
        <Shell />
      </DiceRollerProvider>
    </AuthProvider>
  )
}
