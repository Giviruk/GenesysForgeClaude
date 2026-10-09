import { Footer } from '../components/Footer'
import { MarkdownContent } from '../components/MarkdownContent'
import { navigate } from '../router'
import { t } from '../i18n'
import privacy from '../content/legal/privacy.md?raw'
import terms from '../content/legal/terms.md?raw'

export function LegalPage({ document, loggedIn }: { document: 'privacy' | 'terms'; loggedIn: boolean }) {
  return (
    <div className="page legal-page">
      <article className="panel">
        <button type="button" className="small" onClick={() => navigate(loggedIn ? '/characters' : '/login')}>
          {t('← Назад', '← Back')}
        </button>
        <MarkdownContent markdown={document === 'privacy' ? privacy : terms} />
      </article>
      <Footer />
    </div>
  )
}
