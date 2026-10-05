import { useEffect, useRef } from 'react'
import * as signalR from '@microsoft/signalr'
import { tokenStorage } from './api/client'

export type CampaignHubStatus = 'connecting' | 'connected' | 'disconnected'

interface Handlers {
  onGameTableChanged?: () => void
  onCampaignChanged?: () => void
  onRollAdded?: () => void
  onStatus?: (status: CampaignHubStatus) => void
}

/**
 * Realtime-подключение к кампании на время открытой карточки. Подключается с тем же JWT
 * (через access_token), подписывается на события и по ним просит перечитать REST-ресурс.
 * REST остаётся источником истины; здесь только инвалидация. Отключается при размонтировании
 * и смене кампании.
 */
export function useCampaignHub(campaignId: string | null, handlers: Handlers): void {
  // Держим актуальные колбэки в ref, чтобы не пересоздавать соединение при их смене.
  const handlersRef = useRef(handlers)
  useEffect(() => { handlersRef.current = handlers })

  useEffect(() => {
    if (!campaignId) return
    let stopped = false
    const h = () => handlersRef.current

    h().onStatus?.('connecting')
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/campaign', { accessTokenFactory: () => tokenStorage.get() ?? '' })
      .withAutomaticReconnect()
      .build()

    connection.on('GameTableChanged', () => h().onGameTableChanged?.())
    connection.on('CampaignChanged', () => h().onCampaignChanged?.())
    connection.on('RollAdded', () => h().onRollAdded?.())
    connection.onreconnecting(() => h().onStatus?.('connecting'))
    const subscribeAndRefresh = async () => {
      if (stopped) return
      await connection.invoke('SubscribeCampaign', campaignId)
      if (stopped) return
      // Events missed before subscription / during a disconnect cannot be replayed.
      // Ask every REST snapshot consumer to catch up after the group subscription succeeds.
      h().onGameTableChanged?.()
      h().onCampaignChanged?.()
      h().onRollAdded?.()
      h().onStatus?.('connected')
    }
    connection.onreconnected(() => {
      void subscribeAndRefresh().catch(() => { if (!stopped) h().onStatus?.('disconnected') })
    })
    connection.onclose(() => h().onStatus?.('disconnected'))

    const started = connection.start()
      .then(subscribeAndRefresh)
      .catch(() => { if (!stopped) h().onStatus?.('disconnected') })

    return () => {
      stopped = true
      // Останавливаем только после завершения start(), чтобы не оборвать negotiation
      // (иначе StrictMode-перемонтаж в dev сыплет «stopped during negotiation»).
      void started.finally(() => connection.stop())
    }
  }, [campaignId])
}
