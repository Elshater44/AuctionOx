import React, { createContext, useContext, useState, useEffect, useRef, useCallback } from 'react'
import * as signalR from '@microsoft/signalr'
import { useAuth } from './AuthContext'
import { useToast } from './ToastContext'
import { notificationApi } from '@/api/notificationApi'

const NotificationContext = createContext(null)

const HUB_URL = (import.meta.env.VITE_API_URL || 'http://localhost:5050/api').replace(/\/api\/?$/, '') + '/hubs/notifications'

export function NotificationProvider({ children }) {
  const { token, isAuthenticated } = useAuth()
  const toast = useToast()

  const [notifications, setNotifications] = useState([])
  const [unreadCount, setUnreadCount] = useState(0)
  const [isLoading, setIsLoading] = useState(false)
  const connectionRef = useRef(null)

  const fetchUnreadCount = useCallback(async () => {
    if (!isAuthenticated) return
    try {
      const count = await notificationApi.getUnreadCount()
      setUnreadCount(count)
    } catch (err) {
      console.error('Failed to fetch notification unread count:', err)
    }
  }, [isAuthenticated])

  const fetchNotifications = useCallback(async ({ pageNumber = 1, pageSize = 15, unreadOnly = null } = {}) => {
    if (!isAuthenticated) return
    setIsLoading(true)
    try {
      const data = await notificationApi.getNotifications({ pageNumber, pageSize, unreadOnly })
      setNotifications(data.items || [])
    } catch (err) {
      console.error('Failed to fetch notifications:', err)
    } finally {
      setIsLoading(false)
    }
  }, [isAuthenticated])

  const markAsRead = useCallback(async (id) => {
    try {
      await notificationApi.markAsRead(id)
      setNotifications((prev) =>
        prev.map((n) => (n.id === id ? { ...n, isRead: true } : n))
      )
      setUnreadCount((prev) => Math.max(0, prev - 1))
    } catch (err) {
      console.error('Failed to mark notification as read:', err)
    }
  }, [])

  const markAllAsRead = useCallback(async () => {
    try {
      await notificationApi.markAllAsRead()
      setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })))
      setUnreadCount(0)
    } catch (err) {
      console.error('Failed to mark all notifications as read:', err)
    }
  }, [])

  const deleteNotification = useCallback(async (id) => {
    try {
      await notificationApi.deleteNotification(id)
      setNotifications((prev) => {
        const target = prev.find((n) => n.id === id)
        if (target && !target.isRead) {
          setUnreadCount((c) => Math.max(0, c - 1))
        }
        return prev.filter((n) => n.id !== id)
      })
    } catch (err) {
      console.error('Failed to delete notification:', err)
    }
  }, [])

  // Initial fetch on authentication
  useEffect(() => {
    if (isAuthenticated) {
      fetchUnreadCount()
      fetchNotifications()
    } else {
      setNotifications([])
      setUnreadCount(0)
    }
  }, [isAuthenticated, fetchUnreadCount, fetchNotifications])

  // Setup SignalR connection
  useEffect(() => {
    if (!isAuthenticated || !token) {
      if (connectionRef.current) {
        connectionRef.current.stop().catch(() => {})
        connectionRef.current = null
      }
      return
    }

    let isMounted = true

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => localStorage.getItem('auctionox_token') || token,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connectionRef.current = connection

    connection.on('ReceiveNotification', (newNotif) => {
      if (!isMounted) return

      setNotifications((prev) => {
        const filtered = prev.filter((n) => n.id !== newNotif.id)
        return [newNotif, ...filtered]
      })

      // Trigger toast alert based on notification type
      // NotificationType: 1 = Outbid, 2 = AuctionWon, 3 = AuctionEnded, 4 = NewBid, 5 = EndingSoon, 6 = System
      if (newNotif.type === 1) {
        toast.warning(newNotif.title, newNotif.message)
      } else if (newNotif.type === 2) {
        toast.success(newNotif.title, newNotif.message)
      } else {
        toast.info(newNotif.title, newNotif.message)
      }
    })

    connection.on('UpdateUnreadCount', (count) => {
      if (isMounted) {
        setUnreadCount(count)
      }
    })

    async function startHub() {
      try {
        await connection.start()
      } catch (err) {
        console.error('SignalR notification hub connection failed:', err)
      }
    }

    startHub()

    return () => {
      isMounted = false
      if (connectionRef.current) {
        connectionRef.current.stop().catch(() => {})
        connectionRef.current = null
      }
    }
  }, [isAuthenticated, token, toast])

  return (
    <NotificationContext.Provider
      value={{
        notifications,
        unreadCount,
        isLoading,
        fetchNotifications,
        fetchUnreadCount,
        markAsRead,
        markAllAsRead,
        deleteNotification,
      }}
    >
      {children}
    </NotificationContext.Provider>
  )
}

export function useNotifications() {
  const context = useContext(NotificationContext)
  if (!context) {
    throw new Error('useNotifications must be used within a NotificationProvider')
  }
  return context
}
