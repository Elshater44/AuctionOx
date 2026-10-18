import React, { createContext, useContext, useState, useCallback, useMemo } from 'react'
import { CheckCircle2, AlertCircle, AlertTriangle, Info, X } from 'lucide-react'

const ToastContext = createContext(null)

export function ToastProvider({ children }) {
  const [toasts, setToasts] = useState([])

  const removeToast = useCallback((id) => {
    setToasts((prev) => prev.filter((t) => t.id !== id))
  }, [])

  const addToast = useCallback(({ title, description, variant = 'info', duration = 4000 }) => {
    const id = Date.now().toString() + Math.random().toString(36).substring(2, 5)
    const newToast = { id, title, description, variant }

    setToasts((prev) => {
      // Deduplicate: Don't show duplicate toast if already visible
      const isDuplicate = prev.some(
        (t) => t.title === title && t.description === description && t.variant === variant
      )
      if (isDuplicate) return prev

      // Keep at most 3 simultaneous toasts to prevent screen flooding
      return [...prev.slice(-2), newToast]
    })

    if (duration > 0) {
      setTimeout(() => {
        removeToast(id)
      }, duration)
    }

    return id
  }, [removeToast])

  const toast = useMemo(() => ({
    success: (title, description) => addToast({ title, description, variant: 'success' }),
    error: (title, description) => addToast({ title, description, variant: 'error' }),
    warning: (title, description) => addToast({ title, description, variant: 'warning' }),
    info: (title, description) => addToast({ title, description, variant: 'info' }),
    dismiss: removeToast,
  }), [addToast, removeToast])

  return (
    <ToastContext.Provider value={toast}>
      {children}
      <Toaster toasts={toasts} onDismiss={removeToast} />
    </ToastContext.Provider>
  )
}

export function useToast() {
  const context = useContext(ToastContext)
  if (!context) {
    throw new Error('useToast must be used within a ToastProvider')
  }
  return context
}

function Toaster({ toasts, onDismiss }) {
  if (toasts.length === 0) return null

  return (
    <div className="fixed top-4 right-4 z-50 flex flex-col gap-2.5 max-w-sm w-full pointer-events-none p-2 sm:p-0">
      {toasts.map((t) => {
        const isSuccess = t.variant === 'success'
        const isError = t.variant === 'error'
        const isWarning = t.variant === 'warning'
        const isInfo = t.variant === 'info'

        return (
          <div
            key={t.id}
            className={`pointer-events-auto flex items-start gap-3 p-3.5 rounded-xl shadow-lg border backdrop-blur-md transition-all duration-300 animate-in slide-in-from-top-2 fade-in ${
              isSuccess ? 'bg-white/95 border-emerald-200 text-slate-800' :
              isError ? 'bg-white/95 border-red-200 text-slate-800' :
              isWarning ? 'bg-white/95 border-amber-200 text-slate-800' :
              'bg-white/95 border-blue-200 text-slate-800'
            }`}
          >
            <div className="mt-0.5 shrink-0">
              {isSuccess && <CheckCircle2 className="w-4 h-4 text-emerald-600" />}
              {isError && <AlertCircle className="w-4 h-4 text-red-600" />}
              {isWarning && <AlertTriangle className="w-4 h-4 text-amber-600" />}
              {isInfo && <Info className="w-4 h-4 text-blue-600" />}
            </div>

            <div className="flex-1 space-y-0.5">
              <h4 className="text-xs font-bold leading-tight">{t.title}</h4>
              {t.description && (
                <p className="text-[11px] text-slate-500 leading-snug">{t.description}</p>
              )}
            </div>

            <button
              onClick={() => onDismiss(t.id)}
              className="text-slate-400 hover:text-slate-600 p-0.5 rounded transition-colors"
            >
              <X className="w-3.5 h-3.5" />
            </button>
          </div>
        )
      })}
    </div>
  )
}
