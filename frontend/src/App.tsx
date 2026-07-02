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
import { DiceRollerProvider } from './dice-roller-context'
import { useDiceRoller } from './dice-roller-store'

const campaignView = (sub: string | null): CampaignView =>
  sub === 'table' || sub === 'handbook' || sub === 'encounters' ? sub : 'overview'

const NAV_ITEMS: Array<{ area: AppArea; label: string; path: string }> = [
  { area: 'characters', label: 'Персонажи', path: '/characters' },
  { area: 'campaigns', label: 'Кампании', path: '/campaigns' },
  { area: 'npcs', label: 'Бестиарий', path: '/npcs' },
  { area: 'magic', label: 'Магия', path: '/magic' },
  { area: 'reference', label: 'Справочник', path: '/reference' },
  { area: 'help', label: 'Справка', path: '/help' },
]

const AREA_TITLES: Partial<Record<AppArea, string>> = {
  characters: 'Персонажи',
  campaigns: 'Кампании',
  npcs: 'Бестиарий',
  magic: 'Магия',
  reference: 'Справочник',
  help: 'Справка',
  account: 'Профиль',
}

const areaSubtitle = (area: AppArea, sub: string | null): string => {
  if (area === 'campaigns' && campaignView(sub) === 'table') return 'Игровой стол мастера'
  if (area === 'campaigns' && campaignView(sub) === 'encounters') return 'Рабочая область мастера'
  return 'Рабочая область'
}

function Shell() {
  const { token, logout } = useAuth()
  const { openRoller } = useDiceRoller()
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

  const currentTitle = AREA_TITLES[route.area] ?? 'Genesys Forge'
  const currentSubtitle = areaSubtitle(route.area, route.sub)

  return (
    <div className="app-shell">
      <aside className="app-sidebar no-print" aria-label="Основная навигация">
        <button type="button" className="app-brand" onClick={() => navigate('/characters')}>Genesys Forge</button>
        <nav className="side-nav">
          {NAV_ITEMS.map(item => (
            <button key={item.area} type="button"
              className={route.area === item.area ? 'side-nav-item active' : 'side-nav-item'}
              onClick={() => navigate(item.path)}>
              {item.label}
            </button>
          ))}
        </nav>
        <div className="side-nav-footer">
          <button type="button" className={route.area === 'account' ? 'side-nav-item active' : 'side-nav-item'}
            onClick={() => navigate('/account')}>Профиль</button>
          <button type="button" className="side-nav-item" onClick={() => { logout(); navigate('/login') }}>Выйти</button>
        </div>
      </aside>

      <div className="shell">
        <header className="topbar app-topbar">
          <div>
            <h1>{currentTitle}</h1>
            <div className="page-sub">{currentSubtitle}</div>
          </div>
          <button type="button" className="primary" onClick={() => openRoller()}>Дайсроллер</button>
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
