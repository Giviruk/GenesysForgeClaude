/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Базовый URL «Ариадны»; пусто — аналитика выключена. */
  readonly VITE_ARIADNE_ENDPOINT?: string
  /** Публичный ключ проекта в «Ариадне» (pub_…), безопасен в бандле. */
  readonly VITE_ARIADNE_PROJECT_KEY?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
