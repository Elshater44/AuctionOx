import React, { useState, useRef, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useNotifications } from '@/context/NotificationContext'
import {
  Bell,
  Check,
  CheckCheck,
  Trash2,
  Gavel,
  Trophy,
  AlertTriangle,
  Clock,
  Info,
  X,
  ExternalLink
} from 'lucide-react'

// Helper for relative timestamps
function formatRelativeTime(dateString) {
  try {
    const date = new Date(dateString)
    const now = new Date()
    const diffSec = Math.floor((now - date) / 1000)

    if (diffSec < 60) return 'Just now'
    const diffMin = Math.floor(diffSec / 60)
    if (diffMin < 60) return `${diffMin}m ago`
    const diffHours = Math.floor(diffMin / 60)
    if (diffHours < 24) return `${diffHours}h ago`
    const diffDays = Math.floor(diffHours / 24)
    if (diffDays < 7) return `${diffDays}d ago`
    return date.toLocaleDateString()
  } catch {
    return ''
  }
}

// Icon and color mapping per notification type
// NotificationType: 1 = Outbid, 2 = AuctionWon, 3 = AuctionEnded, 4 = NewBid, 5 = EndingSoon, 6 = System
function getNotificationVisuals(type) {
  switch (type) {
    case 1:
      return {
        icon: AlertTriangle,
        bg: 'bg-amber-100 text-amber-700 border-amber-200',
        badge: 'Outbid',
      }
    case 2:
      return {
        icon: Trophy,
        bg: 'bg-emerald-100 text-emerald-700 border-emerald-200',
        badge: 'Won',
      }
    case 3:
      return {
        icon: Clock,
        bg: 'bg-purple-100 text-purple-700 border-purple-200',
        badge: 'Ended',
      }
    case 4:
      return {
        icon: Gavel,
        bg: 'bg-blue-100 text-blue-700 border-blue-200',
        badge: 'Bid',
      }
    default:
      return {
        icon: Info,
        bg: 'bg-slate-100 text-slate-700 border-slate-200',
        badge: 'Alert',
      }
  }
}

export function NotificationDropdown() {
  const [isOpen, setIsOpen] = useState(false)
  const [filterUnread, setFilterUnread] = useState(false)
  const dropdownRef = useRef(null)
  const navigate = useNavigate()

  const {
    notifications,
    unreadCount,
    isLoading,
    markAsRead,
    markAllAsRead,
    deleteNotification,
  } = useNotifications()

  // Close on outside click
  useEffect(() => {
    function handleClickOutside(event) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setIsOpen(false)
      }
    }
    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside)
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [isOpen])

  const filteredNotifications = filterUnread
    ? notifications.filter((n) => !n.isRead)
    : notifications

  const handleNotificationClick = async (notif) => {
    if (!notif.isRead) {
      await markAsRead(notif.id)
    }
    if (notif.auctionItemId) {
      setIsOpen(false)
      navigate(`/auctions/${notif.auctionItemId}`)
    }
  }

  return (
    <div className="relative inline-block" ref={dropdownRef}>
      {/* Bell Trigger Button */}
      <button
        onClick={() => setIsOpen((prev) => !prev)}
        className={`relative p-2 rounded-full transition-all focus:outline-none focus:ring-2 focus:ring-blue-500/20 ${
          isOpen
            ? 'bg-blue-100/80 text-blue-700'
            : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
        }`}
        aria-label="Notifications"
        title="View Notifications"
      >
        <Bell className="w-4 h-4" />

        {unreadCount > 0 && (
          <span className="absolute -top-0.5 -right-0.5 flex items-center justify-center min-w-[18px] h-[18px] px-1 text-[10px] font-extrabold text-white bg-red-600 rounded-full shadow-sm ring-2 ring-white animate-pulse">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {/* Dropdown Menu */}
      {isOpen && (
        <div className="absolute right-0 mt-2 w-80 sm:w-96 bg-white border border-slate-200/90 rounded-2xl shadow-2xl z-50 overflow-hidden flex flex-col max-h-[520px] animate-in fade-in slide-in-from-top-2 duration-150">
          {/* Header */}
          <div className="p-3.5 border-b border-slate-100 flex items-center justify-between bg-slate-50/80 backdrop-blur-sm">
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-bold text-slate-800">Notifications</h3>
              {unreadCount > 0 && (
                <span className="px-2 py-0.5 text-[11px] font-semibold bg-blue-100 text-blue-700 rounded-full">
                  {unreadCount} new
                </span>
              )}
            </div>

            <div className="flex items-center gap-1">
              {unreadCount > 0 && (
                <button
                  onClick={markAllAsRead}
                  className="flex items-center gap-1 text-[11px] font-semibold text-blue-600 hover:text-blue-800 px-2 py-1 rounded-md hover:bg-blue-50 transition-colors"
                  title="Mark all as read"
                >
                  <CheckCheck className="w-3.5 h-3.5" />
                  <span>Mark all read</span>
                </button>
              )}
              <button
                onClick={() => setIsOpen(false)}
                className="text-slate-400 hover:text-slate-600 p-1 rounded-md"
              >
                <X className="w-3.5 h-3.5" />
              </button>
            </div>
          </div>

          {/* Filter Pills */}
          <div className="px-3.5 py-2 border-b border-slate-100 flex items-center gap-2 bg-white text-xs">
            <button
              onClick={() => setFilterUnread(false)}
              className={`px-2.5 py-1 rounded-lg font-medium transition-colors ${
                !filterUnread
                  ? 'bg-slate-900 text-white shadow-xs'
                  : 'text-slate-600 hover:bg-slate-100'
              }`}
            >
              All ({notifications.length})
            </button>
            <button
              onClick={() => setFilterUnread(true)}
              className={`px-2.5 py-1 rounded-lg font-medium transition-colors ${
                filterUnread
                  ? 'bg-slate-900 text-white shadow-xs'
                  : 'text-slate-600 hover:bg-slate-100'
              }`}
            >
              Unread ({unreadCount})
            </button>
          </div>

          {/* Notification List */}
          <div className="flex-1 overflow-y-auto divide-y divide-slate-100">
            {isLoading && notifications.length === 0 ? (
              <div className="p-8 text-center text-xs text-slate-400">
                Loading notifications...
              </div>
            ) : filteredNotifications.length === 0 ? (
              <div className="p-8 text-center flex flex-col items-center justify-center space-y-2">
                <div className="w-10 h-10 rounded-full bg-slate-100 flex items-center justify-center text-slate-400">
                  <Bell className="w-5 h-5" />
                </div>
                <p className="text-xs font-semibold text-slate-700">All caught up!</p>
                <p className="text-[11px] text-slate-400">
                  {filterUnread ? 'No unread notifications' : 'No notifications to display'}
                </p>
              </div>
            ) : (
              filteredNotifications.map((n) => {
                const { icon: VisualIcon, bg: visualBg, badge: visualBadge } = getNotificationVisuals(n.type)
                return (
                  <div
                    key={n.id}
                    onClick={() => handleNotificationClick(n)}
                    className={`group relative p-3 sm:p-3.5 flex items-start gap-3 transition-colors cursor-pointer ${
                      n.isRead
                        ? 'bg-white hover:bg-slate-50/80'
                        : 'bg-blue-50/30 hover:bg-blue-50/60'
                    }`}
                  >
                    {/* Unread indicator bar/dot */}
                    {!n.isRead && (
                      <span className="absolute left-1.5 top-4 w-1.5 h-1.5 rounded-full bg-blue-600 ring-2 ring-blue-100" />
                    )}

                    {/* Icon */}
                    <div className={`shrink-0 w-8 h-8 rounded-xl flex items-center justify-center border shadow-2xs ${visualBg}`}>
                      <VisualIcon className="w-4 h-4" />
                    </div>

                    {/* Content */}
                    <div className="flex-1 min-w-0 pr-1">
                      <div className="flex items-center justify-between gap-1 mb-0.5">
                        <span className="text-xs font-bold text-slate-900 truncate">
                          {n.title}
                        </span>
                        <span className="text-[10px] text-slate-400 shrink-0">
                          {formatRelativeTime(n.createdAt)}
                        </span>
                      </div>

                      <p className="text-xs text-slate-600 leading-snug line-clamp-2">
                        {n.message}
                      </p>

                      <div className="mt-1.5 flex items-center gap-2">
                        <span className={`text-[10px] px-1.5 py-0.2 rounded font-medium border ${visualBg}`}>
                          {visualBadge}
                        </span>
                        {n.auctionItemId && (
                          <span className="text-[10px] text-blue-600 flex items-center gap-0.5 hover:underline">
                            View Lot <ExternalLink className="w-2.5 h-2.5" />
                          </span>
                        )}
                      </div>
                    </div>

                    {/* Actions on hover */}
                    <div
                      className="shrink-0 flex items-center gap-1 opacity-80 sm:opacity-0 sm:group-hover:opacity-100 transition-opacity"
                      onClick={(e) => e.stopPropagation()}
                    >
                      {!n.isRead && (
                        <button
                          onClick={() => markAsRead(n.id)}
                          className="p-1 rounded text-slate-400 hover:text-emerald-600 hover:bg-slate-100 transition-colors"
                          title="Mark as read"
                        >
                          <Check className="w-3.5 h-3.5" />
                        </button>
                      )}
                      <button
                        onClick={() => deleteNotification(n.id)}
                        className="p-1 rounded text-slate-400 hover:text-red-600 hover:bg-slate-100 transition-colors"
                        title="Delete notification"
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  </div>
                )
              })
            )}
          </div>

          {/* Footer */}
          {notifications.length > 0 && (
            <div className="p-2.5 bg-slate-50 border-t border-slate-100 text-center">
              <span className="text-[11px] text-slate-500">
                Real-time alerts powered by SignalR
              </span>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
