import { navigate } from '../router'
import { Footer, REPO_URL, CHANGELOG_URL } from '../components/Footer'
import { t } from '../i18n'

const LICENSE_URL = `${REPO_URL}/blob/master/LICENSE`
const NOTICE_URL = `${REPO_URL}/blob/master/NOTICE`

/** Публичная страница «О проекте»: описание, ссылки, лицензия и копирайт-дисклеймер. */
export function AboutPage({ loggedIn }: { loggedIn: boolean }) {
  return (
    <div className="page about-page">
      <div className="panel">
        <div className="page-head">
          <h2>{t('О проекте', 'About')}</h2>
          <button className="small" onClick={() => navigate(loggedIn ? '/characters' : '/login')}>
            {t('← Назад', '← Back')}
          </button>
        </div>

        <p>
          <strong>GenesysForge</strong>{t(
            ' — некоммерческий веб-инструмент для создания и ведения листов персонажей для систем ',
            ' is a non-commercial web tool for creating and managing character sheets for the ',
          )}<em>Genesys Core</em>{t(' и ', ' and ')}<em>Realms of Terrinoth</em>{t(
            ': характеристики, навыки, таланты, героические способности, инвентарь и автоматический ' +
            'пересчёт производных характеристик, а также инструменты ведущего — кампании, бестиарий, ' +
            'энкаунтеры и игровой стол.',
            ' systems: characteristics, skills, talents, heroic abilities, inventory and automatic ' +
            'recalculation of derived characteristics, plus GM tools — campaigns, a bestiary, ' +
            'encounters and a game table.',
          )}
        </p>

        <h3>{t('Ссылки', 'Links')}</h3>
        <ul>
          <li><a href={REPO_URL} target="_blank" rel="noreferrer">{t('Исходный код (GitHub)', 'Source code (GitHub)')}</a></li>
          <li><a href={CHANGELOG_URL} target="_blank" rel="noreferrer">{t('История изменений (Changelog)', 'Changelog')}</a></li>
          <li><a href={LICENSE_URL} target="_blank" rel="noreferrer">{t('Лицензия кода (Apache-2.0)', 'Code license (Apache-2.0)')}</a></li>
        </ul>

        <h3>{t('Лицензия', 'License')}</h3>
        <p>
          {t('Исходный код проекта распространяется под лицензией', 'The project source code is distributed under the')}{' '}
          <a href={LICENSE_URL} target="_blank" rel="noreferrer">Apache License 2.0</a>.
          {' '}{t('Лицензия кода не распространяется на игровой контент — см. дисклеймер ниже.', 'The code license does not cover game content — see the disclaimer below.')}
        </p>

        <h3>{t('Правовая информация', 'Legal')}</h3>
        <p className="muted">
          {t(
            'GenesysForge — независимый фанатский проект. Он не аффилирован с Fantasy Flight Games, ' +
            'Edge Studio, Asmodee и не одобрен ими. «Genesys», «Realms of Terrinoth» и связанные ' +
            'названия и термины являются товарными знаками и/или объектами авторского права их ' +
            'правообладателей и используются здесь лишь для обозначения поддерживаемых систем.',
            'GenesysForge is an independent fan project. It is not affiliated with or endorsed by ' +
            'Fantasy Flight Games, Edge Studio or Asmodee. "Genesys", "Realms of Terrinoth" and related ' +
            'names and terms are trademarks and/or copyrighted works of their respective owners and are ' +
            'used here only to identify the supported systems.',
          )}
        </p>
        <p className="muted">
          {t(
            'Проект не содержит оригинальных текстов из официальных книг. Встроенные данные ' +
            'воспроизводят только структуру и числовые параметры вместе с собственными краткими ' +
            'парафраз-описаниями. Подробнее — в файле',
            'The project contains no original text from the official books. The bundled data reproduces ' +
            'only structure and numeric parameters together with short original paraphrase descriptions. ' +
            'See the',
          )}{' '}
          <a href={NOTICE_URL} target="_blank" rel="noreferrer">NOTICE</a>{t('.', ' file for details.')}
        </p>
      </div>

      <Footer />
    </div>
  )
}
