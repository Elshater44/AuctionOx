import React, { useState, useRef, useEffect } from 'react'
import { Button } from '@/components/ui/button'
import {
  ChevronDown,
  ShieldPlus,
  UserMinus,
  Clock,
  Ban,
  Unlock,
  Trash2,
} from 'lucide-react'

export function UserActionsMenu({
  user,
  isSelf,
  isDefaultAdmin,
  isAdmin,
  onSuspend,
  onBan,
  onReactivate,
  onPromote,
  onDemote,
  onDelete,
  isNearBottom = false,
}) {
  const [isOpen, setIsOpen] = useState(false)
  const menuRef = useRef(null)
  const buttonRef = useRef(null)

  // Close when clicking outside
  useEffect(() => {
    function handleClickOutside(event) {
      if (
        menuRef.current &&
        !menuRef.current.contains(event.target) &&
        buttonRef.current &&
        !buttonRef.current.contains(event.target)
      ) {
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

  // Root Super Admin Guard
  if (isDefaultAdmin) {
    return (
      <span className="text-[11px] text-slate-400 font-medium italic">
        Root System
      </span>
    )
  }

  // Self Session Guard
  if (isSelf) {
    return (
      <span className="text-[11px] text-blue-600 font-medium bg-blue-50 px-2 py-0.5 rounded border border-blue-100">
        Current Session
      </span>
    )
  }

  return (
    <div className="relative inline-flex items-center justify-end gap-1.5">
      {/* Quick 1-click Reactivate button if account is currently locked */}
      {user.isLockedOut && (
        <Button
          variant="outline"
          size="sm"
          onClick={() => onReactivate(user)}
          className="h-8 px-2.5 text-xs font-semibold text-emerald-700 bg-emerald-50/80 hover:bg-emerald-100 hover:text-emerald-800 border-emerald-200 flex items-center gap-1 shadow-2xs"
          title="Reactivate account immediately"
        >
          <Unlock className="w-3.5 h-3.5" />
          Reactivate
        </Button>
      )}

      {/* Main Actions Dropdown Button */}
      <Button
        ref={buttonRef}
        variant="outline"
        size="sm"
        onClick={() => setIsOpen((prev) => !prev)}
        className={`h-8 px-2.5 text-xs font-semibold rounded-lg border transition-all flex items-center gap-1.5 shadow-2xs ${
          isOpen
            ? 'bg-slate-100 text-slate-900 border-slate-300 ring-2 ring-purple-500/10'
            : 'bg-white text-slate-700 hover:text-slate-900 hover:bg-slate-50 border-slate-200'
        }`}
        aria-expanded={isOpen}
      >
        <span>Actions</span>
        <ChevronDown
          className={`w-3.5 h-3.5 text-slate-400 transition-transform duration-150 ${
            isOpen ? 'rotate-180' : ''
          }`}
        />
      </Button>

      {/* Dropdown Floating Popover */}
      {isOpen && (
        <div
          ref={menuRef}
          className={`absolute right-0 ${
            isNearBottom ? 'bottom-full mb-1.5' : 'top-full mt-1.5'
          } z-50 w-52 bg-white rounded-xl shadow-xl border border-slate-200 py-1.5 text-xs divide-y divide-slate-100 animate-in fade-in zoom-in-95 duration-150`}
        >
          {/* Section 1: Role & Permissions */}
          <div className="py-1">
            <div className="px-3 py-1 text-[10px] font-bold uppercase tracking-wider text-slate-400">
              Role & Access
            </div>

            {!isAdmin ? (
              <button
                type="button"
                onClick={() => {
                  setIsOpen(false)
                  onPromote(user)
                }}
                disabled={user.isLockedOut}
                className="w-full flex items-center gap-2.5 px-3 py-2 text-left font-medium text-slate-700 hover:bg-purple-50 hover:text-purple-700 transition-colors disabled:opacity-40 disabled:hover:bg-transparent disabled:cursor-not-allowed"
                title={user.isLockedOut ? 'Cannot promote a suspended account' : undefined}
              >
                <ShieldPlus className="w-4 h-4 text-purple-600 shrink-0" />
                <span>Make Administrator</span>
              </button>
            ) : (
              <button
                type="button"
                onClick={() => {
                  setIsOpen(false)
                  onDemote(user)
                }}
                className="w-full flex items-center gap-2.5 px-3 py-2 text-left font-medium text-slate-700 hover:bg-indigo-50 hover:text-indigo-700 transition-colors"
              >
                <UserMinus className="w-4 h-4 text-indigo-600 shrink-0" />
                <span>Demote to User</span>
              </button>
            )}
          </div>

          {/* Section 2: Account Moderation */}
          <div className="py-1">
            <div className="px-3 py-1 text-[10px] font-bold uppercase tracking-wider text-slate-400">
              Moderation
            </div>

            {user.isLockedOut ? (
              <button
                type="button"
                onClick={() => {
                  setIsOpen(false)
                  onReactivate(user)
                }}
                className="w-full flex items-center gap-2.5 px-3 py-2 text-left font-medium text-emerald-700 hover:bg-emerald-50 transition-colors"
              >
                <Unlock className="w-4 h-4 text-emerald-600 shrink-0" />
                <span>Reactivate Account</span>
              </button>
            ) : (
              <>
                <button
                  type="button"
                  onClick={() => {
                    setIsOpen(false)
                    onSuspend(user)
                  }}
                  className="w-full flex items-center gap-2.5 px-3 py-2 text-left font-medium text-slate-700 hover:bg-amber-50 hover:text-amber-800 transition-colors"
                >
                  <Clock className="w-4 h-4 text-amber-600 shrink-0" />
                  <span>Suspend Account...</span>
                </button>

                <button
                  type="button"
                  onClick={() => {
                    setIsOpen(false)
                    onBan(user)
                  }}
                  className="w-full flex items-center gap-2.5 px-3 py-2 text-left font-medium text-slate-700 hover:bg-rose-50 hover:text-rose-800 transition-colors"
                >
                  <Ban className="w-4 h-4 text-rose-600 shrink-0" />
                  <span>Permanently Ban...</span>
                </button>
              </>
            )}
          </div>

          {/* Section 3: Danger Zone */}
          <div className="py-1">
            <div className="px-3 py-1 text-[10px] font-bold uppercase tracking-wider text-red-400">
              Danger Zone
            </div>

            <button
              type="button"
              onClick={() => {
                setIsOpen(false)
                onDelete(user)
              }}
              className="w-full flex items-center gap-2.5 px-3 py-2 text-left font-medium text-red-600 hover:bg-red-50 hover:text-red-700 transition-colors"
            >
              <Trash2 className="w-4 h-4 text-red-500 shrink-0" />
              <span>Delete Account</span>
            </button>
          </div>
        </div>
      )}
    </div>
  )
}