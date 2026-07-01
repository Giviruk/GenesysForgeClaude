import { navigate } from '../router'
import { Footer, REPO_URL, CHANGELOG_URL } from '../components/Footer'

const LICENSE_URL = `${REPO_URL}/blob/master/LICENSE`
const NOTICE_URL = `${REPO_URL}/blob/master/NOTICE`

/** Публичная страница «О проекте»: описание, ссылки, лицензия и копирайт-дисклеймер. */
export function AboutPage({ loggedIn }: { loggedIn: boolean }) {
  return (
    <div className="page about-page">
      <div className="panel">
        <div className="page-head">
          <h2>О проекте</h2>
          <button className="small" onClick={() => navigate(loggedIn ? '/characters' : '/login')}>
            ← Назад
          </button>
        </div>

        <p>
          <strong>GenesysForge</strong> — некоммерческий веб-инструмент для создания и ведения
          листов персонажей для систем <em>Genesys Core</em> и <em>Realms of Terrinoth</em>:
          характеристики, навыки, таланты, героические способности, инвентарь и автоматический
          пересчёт производных характеристик, а также инструменты ведущего — кампании, бестиарий,
          энкаунтеры и игровой стол.
        </p>

        <h3>Ссылки</h3>
        <ul>
          <li><a href={REPO_URL} target="_blank" rel="noreferrer">Исходный код (GitHub)</a></li>
          <li><a href={CHANGELOG_URL} target="_blank" rel="noreferrer">История изменений (Changelog)</a></li>
          <li><a href={LICENSE_URL} target="_blank" rel="noreferrer">Лицензия кода (Apache-2.0)</a></li>
        </ul>

        <h3>Лицензия</h3>
        <p>
          Исходный код проекта распространяется под лицензией{' '}
          <a href={LICENSE_URL} target="_blank" rel="noreferrer">Apache License 2.0</a>.
          Лицензия кода не распространяется на игровой контент — см. дисклеймер ниже.
        </p>

        <h3>Правовая информация</h3>
        <p className="muted">
          GenesysForge — независимый фанатский проект. Он не аффилирован с Fantasy Flight Games,
          Edge Studio, Asmodee и не одобрен ими. «Genesys», «Realms of Terrinoth» и связанные
          названия и термины являются товарными знаками и/или объектами авторского права их
          правообладателей и используются здесь лишь для обозначения поддерживаемых систем.
        </p>
        <p className="muted">
          Проект не содержит оригинальных текстов из официальных книг. Встроенные данные
          воспроизводят только структуру и числовые параметры вместе с собственными краткими
          парафраз-описаниями. Подробнее — в файле{' '}
          <a href={NOTICE_URL} target="_blank" rel="noreferrer">NOTICE</a>.
        </p>
      </div>

      <Footer />
    </div>
  )
}
