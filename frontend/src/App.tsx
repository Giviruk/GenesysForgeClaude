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
import { t, useLang } from './i18n'
import { useSeo } from './seo'
import type { CampaignView } from './pages/CampaignsPage'
import { DiceRollerProvider } from './dice-roller-context'

const campaignView = (sub: string | null): CampaignView =>
  sub === 'table' || sub === 'handbook' || sub === 'encounters' ? sub : 'overview'

const NAV_ITEMS: Array<{ area: AppArea; label: string; path: string; icon: IconName }> = [
  { area: 'characters', label: t('Персонажи', 'Characters'), path: '/characters', icon: 'users' },
  { area: 'npcs', label: t('Бестиарий', 'Bestiary'), path: '/npcs', icon: 'skull' },
  { area: 'campaigns', label: t('Кампании', 'Campaigns'), path: '/campaigns', icon: 'map' },
  { area: 'magic', label: t('Магия', 'Magic'), path: '/magic', icon: 'flame' },
]

const FOOTER_NAV_ITEMS: Array<{ area: AppArea; label: string; path: string; icon: IconName }> = [
  { area: 'reference', label: t('Справочник', 'Reference'), path: '/reference', icon: 'book' },
  { area: 'account', label: t('Профиль', 'Profile'), path: '/account', icon: 'user' },
  { area: 'help', label: t('Справка', 'Help'), path: '/help', icon: 'help' },
]

function Shell() {
  const { token, logout } = useAuth()
  const [lang, setLang] = useLang()
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
      <aside className="app-sidebar no-print" aria-label={t('Основная навигация', 'Main navigation')}>
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
          <button type="button" className="side-nav-item"
            onClick={() => setLang(lang === 'ru' ? 'en' : 'ru')}>
            <Icon name="globe" className="side-nav-icon" />
            {lang === 'ru' ? 'English' : 'Русский'}
          </button>
          <button type="button" className="side-nav-item" onClick={() => { logout(); navigate('/login') }}>
            <Icon name="logout" className="side-nav-icon" />
            {t('Выйти', 'Sign out')}
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
        <h2>{t('Страница не найдена', 'Page not found')}</h2>
        <p className="muted">{t('Такого адреса нет. Вернитесь к списку персонажей.', 'This address does not exist. Return to the character list.')}</p>
        <button className="primary" onClick={() => navigate('/characters')}>{t('← К персонажам', '← Back to characters')}</button>
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
