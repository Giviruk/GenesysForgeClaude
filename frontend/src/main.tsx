import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import './pwa'
import App from './App.tsx'
import { initAriadne, readAnalyticsConsent } from './analytics/ariadne'

if (readAnalyticsConsent() === 'granted') initAriadne()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
