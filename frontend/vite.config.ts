/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: Number(process.env.PORT) || 5173,
    proxy: {
      '/api': 'http://localhost:5080',
      // SignalR-хаб: проксируем с поддержкой WebSocket.
      '/hubs': { target: 'http://localhost:5080', ws: true },
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
  },
})
