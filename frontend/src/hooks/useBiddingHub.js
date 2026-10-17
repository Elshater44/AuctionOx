import { useState, useEffect, useRef } from 'react'
import * as signalR from '@microsoft/signalr'

const HUB_URL = (import.meta.env.VITE_API_URL || 'http://localhost:5050/api').replace(/\/api\/?$/, '') + '/hubs/bidding'

export function useBiddingHub(auctionId, onLiveBidsReceived) {
  const [connectionStatus, setConnectionStatus] = useState('disconnected') // 'disconnected' | 'connecting' | 'connected' | 'reconnecting'
  const connectionRef = useRef(null)
  const onBidsRef = useRef(onLiveBidsReceived)

  useEffect(() => {
    onBidsRef.current = onLiveBidsReceived
  }, [onLiveBidsReceived])

  useEffect(() => {
    if (!auctionId) return

    let isMounted = true

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => localStorage.getItem('auctionox_token') || '',
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
      .configureLogging(signalR.LogLevel.Information)
      .build()

    connectionRef.current = connection

    connection.onreconnecting(() => {
      if (isMounted) setConnectionStatus('reconnecting')
    })

    connection.onreconnected(async () => {
      if (isMounted) {
        setConnectionStatus('connected')
        try {
          await connection.invoke('JoinAuctionRoom', Number(auctionId))
        } catch (err) {
          console.error('Failed to rejoin auction room after reconnect:', err)
        }
      }
    })

    connection.onclose(() => {
      if (isMounted) setConnectionStatus('disconnected')
    })

    connection.on('ReceiveLiveBidData', (liveBids) => {
      if (onBidsRef.current) {
        onBidsRef.current(liveBids)
      }
    })

    async function start() {
      try {
        setConnectionStatus('connecting')
        await connection.start()
        if (isMounted) {
          setConnectionStatus('connected')
          await connection.invoke('JoinAuctionRoom', Number(auctionId))
        }
      } catch (err) {
        console.error('SignalR connection failed:', err)
        if (isMounted) setConnectionStatus('disconnected')
      }
    }

    start()

    return () => {
      isMounted = false
      if (connection.state === signalR.HubConnectionState.Connected) {
        connection.invoke('LeaveAuctionRoom', Number(auctionId)).catch(() => {})
      }
      connection.stop().catch(() => {})
    }
  }, [auctionId])

  return { connectionStatus }
}
