import React, { useState } from 'react'
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { AlertOctagon, X, Loader2 } from 'lucide-react'

export function SuspendUserModal({
  isOpen,
  user,
  onConfirm,
  onCancel,
  isSubmitting,
}) {
  const [days, setDays] = useState(7)
  const [reason, setReason] = useState('')

  if (!isOpen || !user) return null

  const handleSubmit = (e) => {
    e.preventDefault()
    onConfirm({ days: Number(days), reason: reason.trim() || undefined })
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/60 backdrop-blur-sm animate-in fade-in duration-200">
      <Card className="max-w-md w-full shadow-2xl border-slate-200 bg-white rounded-2xl overflow-hidden animate-in zoom-in-95 duration-200">
        <CardHeader className="bg-amber-50/80 border-b border-amber-100 pb-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2.5">
              <div className="p-2 rounded-lg bg-amber-100 text-amber-700">
                <AlertOctagon className="w-5 h-5" />
              </div>
              <div>
                <CardTitle className="text-base font-bold text-amber-950">
                  Suspend User Account
                </CardTitle>
                <CardDescription className="text-xs text-amber-800/80 mt-0.5">
                  Temporarily lock out this account and revoke active sessions.
                </CardDescription>
              </div>
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

        <form onSubmit={handleSubmit}>
          <CardContent className="p-6 space-y-4">
            {/* Target User Info Card */}
            <div className="p-3 bg-slate-50 border border-slate-200 rounded-lg">
              <p className="text-xs font-semibold text-slate-800">
                {user.firstName} {user.lastName}
              </p>
              <p className="text-[11px] font-mono text-slate-500 mt-0.5">
                {user.email}
              </p>
            </div>

            {/* Duration Selector */}
            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700">
                Suspension Duration *
              </label>
              <div className="grid grid-cols-4 gap-2">
                {[
                  { label: '3 Days', value: 3 },
                  { label: '7 Days', value: 7 },
                  { label: '14 Days', value: 14 },
                  { label: '30 Days', value: 30 },
                ].map((option) => (
                  <button
                    key={option.value}
                    type="button"
                    onClick={() => setDays(option.value)}
                    className={`py-1.5 text-xs font-medium rounded-lg border transition-all ${
                      days === option.value
                        ? 'bg-amber-600 text-white border-amber-600 shadow-sm'
                        : 'bg-white text-slate-600 border-slate-200 hover:border-slate-300'
                    }`}
                  >
                    {option.label}
                  </button>
                ))}
              </div>
              <div className="pt-1 flex items-center gap-2">
                <span className="text-[11px] text-slate-500">Or custom days:</span>
                <Input
                  type="number"
                  min="1"
                  max="365"
                  value={days}
                  onChange={(e) => setDays(Math.max(1, parseInt(e.target.value) || 1))}
                  className="w-24 text-xs h-7"
                />
              </div>
            </div>

            {/* Optional Reason */}
            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700">
                Moderation Reason (Optional)
              </label>
              <Input
                type="text"
                placeholder="e.g. Inappropriate auction comments or shill bidding"
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                disabled={isSubmitting}
                className="text-xs"
              />
            </div>

            <p className="text-[11px] text-slate-400 leading-relaxed">
              ⚠️ The user will be immediately logged out and unable to sign in or place bids until the suspension period ends.
            </p>
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
              type="submit"
              size="sm"
              disabled={isSubmitting}
              className="text-xs font-bold bg-amber-600 hover:bg-amber-700 text-white flex items-center gap-1.5"
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="w-3.5 h-3.5 animate-spin" /> Suspending...
                </>
              ) : (
                'Confirm Suspension'
              )}
            </Button>
          </CardFooter>
        </form>
      </Card>
    </div>
  )
}
