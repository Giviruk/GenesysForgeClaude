# Локальные шрифты v5

Golos Text 400/500/600/700 — интерфейс; Fira Sans Extra Condensed
400/500/600/700 — заголовки, кнопки, числа; Jost 300 (latin) — логотип.
WOFF2 с официального Google Fonts CSS API, получены 13 октября 2026 года.
Подмножества интерфейса: latin, latin-ext, cyrillic, cyrillic-ext.
`font-display: swap`, обслуживание с собственного домена, внешних CDN нет.

Источники и SIL OFL 1.1:
- https://github.com/google/fonts/tree/main/ofl/golostext — golostext-OFL.txt
- https://github.com/google/fonts/tree/main/ofl/firasansextracondensed — firasansextracondensed-OFL.txt
- https://github.com/google/fonts/tree/main/ofl/jost — jost-OFL.txt

Genesys Glyphs & Dice: см. [GENESYS-README.md](GENESYS-README.md).
Использование разрешено владельцем продукта 13.10.2026.
Старые шрифты и неиспользуемые файлы удалены в GEN-V5-12.
Fira: отдельные text subsets latin (U+0020–00FF, U+0300–0308 и
U+2010–2027), latin-ext U+0100–01FF, cyrillic U+0400–045F и cyrillic-ext
U+0460–052F / U+1C80–1C8A на каждом весе. Для остальных знаков заголовки
используют Golos как fallback. Golos — variable 400–700, четыре unicode subset.
Jost содержит только символы логотипа GENESYSFORGE и пробел (его единственная роль).
Суммарный вес UI WOFF2: 410396 байт (~401 КБ); glyph-шрифт ещё 4268 байт.
