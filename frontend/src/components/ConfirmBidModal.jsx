import React from 'react'
import { Card, CardHeader, CardTitle, CardContent, CardFooter } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { AlertTriangle, Crown, X } from 'lucide-react'

export function ConfirmBidModal({
  isOpen,
  currentPrice,
  newBidAmount,
  onConfirm,
  onCancel,
  isSubmitting,
}) {
  if (!isOpen) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-sm animate-in fade-in duration-200">
      <Card className="max-w-md w-full shadow-2xl border-amber-200 bg-white overflow-hidden animate-in zoom-in-95 duration-200">
        <CardHeader className="bg-amber-50/80 border-b border-amber-100 pb-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2.5">
              <div className="p-2 bg-amber-100 text-amber-700 rounded-lg">
                <AlertTriangle className="w-5 h-5" />
              </div>
              <div>
                <CardTitle className="text-base font-bold text-amber-950">
                  You are Already the Highest Bidder!
                </CardTitle>
                <span className="text-xs text-amber-700 font-medium flex items-center gap-1 mt-0.5">
                  <Crown className="w-3.5 h-3.5 fill-amber-500 text-amber-500" />
                  Your current bid is winning this auction
                </span>
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

        <CardContent className="p-6 space-y-4 text-xs text-slate-600">
          <p>
            You currently hold the top position for this lot. Placing another bid will increase the price and outbid yourself.
          </p>

          <div className="grid grid-cols-2 gap-3 p-3.5 bg-slate-50 border border-slate-200 rounded-xl text-center">
            <div>
              <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
                Current Winning Bid
              </span>
              <span className="text-base font-extrabold text-emerald-600 mt-0.5 block">
                ${Number(currentPrice).toFixed(2)}
              </span>
            </div>

            <div className="border-l border-slate-200 pl-3">
              <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">
                New Intended Bid
              </span>
              <span className="text-base font-extrabold text-blue-600 mt-0.5 block">
                ${Number(newBidAmount).toFixed(2)}
              </span>
            </div>
          </div>

          <p className="text-[11px] text-slate-500 italic">
            Are you sure you want to proceed with this self-outbid?
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
            type="button"
            size="sm"
            onClick={onConfirm}
            disabled={isSubmitting}
            className="text-xs bg-amber-600 hover:bg-amber-700 text-white font-bold"
          >
            {isSubmitting ? 'Submitting...' : 'Yes, Outbid Myself'}
          </Button>
        </CardFooter>
      </Card>
    </div>
  )
}
