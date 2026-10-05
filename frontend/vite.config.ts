/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: ['favicon.svg', 'pwa-icon.svg'],
      manifest: {
        name: 'GenesysForge',
        short_name: 'GenesysForge',
        description: 'Character sheets, reference tools and GM aids for Genesys and Realms of Terrinoth.',
        theme_color: '#16131c',
        background_color: '#16131c',
        display: 'standalone',
        orientation: 'any',
        scope: '/',
        start_url: '/',
        categories: ['games', 'utilities'],
        icons: [
          {
            src: '/pwa-icon.svg',
            sizes: '512x512',
            type: 'image/svg+xml',
            purpose: 'any maskable',
          },
        ],
      },
      workbox: {
        importScripts: ['/sw-privacy.js'],
        cleanupOutdatedCaches: true,
        navigateFallback: '/index.html',
        globPatterns: ['**/*.{js,css,html,svg,png,ico,woff2}'],
        runtimeCaching: [
          {
            urlPattern: /\/api(?:\/|$)/,
            handler: 'NetworkOnly',
          },
        ],
      },
    }),
  ],
  server: {
    port: Number(process.env.PORT) || 5173,
    proxy: {
      '/api': 'http://localhost:5080',
      '/openapi': 'http://localhost:5080',
      // SignalR-хаб: проксируем с поддержкой WebSocket.
      '/hubs': { target: 'http://localhost:5080', ws: true },
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    // Фиксирует язык интерфейса ru до импорта модулей — тесты утверждают русские строки.
    setupFiles: ['./src/test-setup.ts'],
    exclude: ['e2e/**', 'node_modules/**', 'dist/**'],
  },
})
