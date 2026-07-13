// Тесты утверждают русские строки интерфейса — фиксируем язык до импорта модулей
// приложения (в jsdom navigator.language = 'en-US', иначе словари соберутся на английском).
localStorage.setItem('genesysforge.lang', 'ru')
