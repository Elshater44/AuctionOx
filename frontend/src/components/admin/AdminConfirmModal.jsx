import React from 'react'
import { Card, CardHeader, CardTitle, CardContent, CardFooter } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { AlertTriangle, X } from 'lucide-react'

export function AdminConfirmModal({
  isOpen,
  title,
  description,
  confirmText = 'Confirm',
  variant = 'destructive', // 'destructive' | 'warning' | 'success'
  onConfirm,
  onCancel,
  isSubmitting,
}) {
  if (!isOpen) return null

  const isDestructive = variant === 'destructive'
  const isSuccess = variant === 'success'

  const headerBg = isDestructive
    ? 'bg-red-50/80 border-red-100'
    : isSuccess
    ? 'bg-emerald-50/80 border-emerald-100'
    : 'bg-amber-50/80 border-amber-100'

  const iconBg = isDestructive
    ? 'bg-red-100 text-red-600'
    : isSuccess
    ? 'bg-emerald-100 text-emerald-600'
    : 'bg-amber-100 text-amber-700'

  const titleColor = isDestructive
    ? 'text-red-950'
    : isSuccess
    ? 'text-emerald-950'
    : 'text-amber-950'

  const buttonColor = isDestructive
    ? 'bg-red-600 hover:bg-red-700'
    : isSuccess
    ? 'bg-emerald-600 hover:bg-emerald-700'
    : 'bg-amber-600 hover:bg-amber-700'

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/60 backdrop-blur-sm animate-in fade-in duration-200">
      <Card className="max-w-md w-full shadow-2xl border-slate-200 bg-white rounded-2xl overflow-hidden animate-in zoom-in-95 duration-200">
        
        <CardHeader className={`${headerBg} border-b pb-4`}>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2.5">
              <div className={`p-2 rounded-lg ${iconBg}`}>
                <AlertTriangle className="w-5 h-5" />
              </div>
              <CardTitle className={`text-base font-bold ${titleColor}`}>
                {title}
              </CardTitle>
            </div>

            <button
              onClick={onCancel}
              disabled={isSubmitting}
              className="text-slate-400 hover:text-slate-600 p-1 rounded-md transition-colors"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        </CardHeader>

        <CardContent className="p-6 text-xs text-slate-600 leading-relaxed">
          {description}
        </CardContent>

        <CardFooter className="bg-slate-50 p-4 border-t border-slate-100 flex items-center justify-end gap-2.5">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={onCancel}
            disabled={isSubmitting}
            className="text-xs"
          >
            Cancel
          </Button>

          <Button
            type="button"
            size="sm"
            onClick={onConfirm}
            disabled={isSubmitting}
            className={`text-xs text-white font-bold ${buttonColor}`}
          >
            {isSubmitting ? 'Processing...' : confirmText}
          </Button>
        </CardFooter>
      </Card>
    </div>
  )
}
