import React from 'react'
import { Card, CardHeader, CardTitle, CardContent, CardFooter } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Zap, ShieldCheck, PackageCheck, X } from 'lucide-react'

export function BuyItNowModal({
  isOpen,
  auction,
  onConfirm,
  onCancel,
  isSubmitting,
}) {
  if (!isOpen || !auction) return null

  const fallbackImage = 'https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=600&auto=format&fit=crop&q=80'

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/60 backdrop-blur-sm animate-in fade-in duration-200">
      <Card className="max-w-lg w-full shadow-2xl border-emerald-200 bg-white rounded-2xl overflow-hidden animate-in zoom-in-95 duration-200">
        
        {/* Header with Emerald Gradient Banner */}
        <div className="bg-gradient-to-r from-emerald-600 to-teal-700 text-white p-6 relative overflow-hidden">
          <div className="absolute -right-6 -top-6 w-24 h-24 bg-white/10 rounded-full blur-xl pointer-events-none" />
          
          <div className="flex items-start justify-between relative z-10">
            <div className="flex items-center gap-3">
              <div className="p-2.5 bg-white/20 backdrop-blur-md rounded-xl text-white shadow-sm ring-1 ring-white/30">
                <Zap className="w-6 h-6 fill-amber-300 text-amber-300" />
              </div>
              <div>
                <CardTitle className="text-xl font-bold tracking-tight text-white flex items-center gap-1.5">
                  Instant Buyout Confirmation
                </CardTitle>
                <p className="text-xs text-emerald-100 font-medium mt-0.5">
                  Skip the bidding race and claim this lot immediately
                </p>
              </div>
            </div>

            <button
              onClick={onCancel}
              disabled={isSubmitting}
              className="text-white/70 hover:text-white p-1 rounded-lg hover:bg-white/10 transition-colors"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        <CardContent className="p-6 space-y-5">
          {/* Item Preview Card */}
          <div className="flex items-center gap-3.5 p-3.5 bg-slate-50 border border-slate-200/80 rounded-xl">
            <img
              src={auction.imageUrl || fallbackImage}
              alt={auction.title}
              className="w-16 h-16 rounded-lg object-cover border border-slate-200 shrink-0 bg-slate-100"
              onError={(e) => { e.target.src = fallbackImage }}
            />
            <div className="flex-1 min-w-0 space-y-1">
              <div className="flex items-center gap-1.5">
                {auction.categoryName && (
                  <Badge variant="secondary" className="text-[10px] px-1.5 py-0 font-bold bg-white border border-slate-200">
                    {auction.categoryName}
                  </Badge>
                )}
                <span className="text-[11px] text-slate-400">Lot #{auction.id}</span>
              </div>
              <h4 className="font-bold text-sm text-slate-900 truncate">
                {auction.title}
              </h4>
              <p className="text-[11px] text-slate-500 truncate">
                Offered by {auction.sellerName || 'Verified Seller'}
              </p>
            </div>
          </div>

          {/* Pricing Box */}
          <div className="p-4 bg-emerald-50/70 border border-emerald-200/90 rounded-xl space-y-2">
            <div className="flex items-baseline justify-between">
              <span className="text-xs font-semibold text-emerald-900 uppercase tracking-wider">
                Total Buyout Price
              </span>
              <span className="text-3xl font-black text-emerald-700 tracking-tight">
                ${Number(auction.buyItNowPrice || 0).toFixed(2)}
              </span>
            </div>
            <p className="text-[11px] text-emerald-700">
              This fixed price bypasses all active bids and immediately declares you the winning purchaser.
            </p>
          </div>

          {/* Guarantees List */}
          <div className="space-y-2.5 text-xs text-slate-600 border-t border-slate-100 pt-3">
            <div className="flex items-center gap-2">
              <ShieldCheck className="w-4 h-4 text-emerald-600 shrink-0" />
              <span><strong>Instant Lock:</strong> The auction closes immediately to all other participants.</span>
            </div>
            <div className="flex items-center gap-2">
              <PackageCheck className="w-4 h-4 text-emerald-600 shrink-0" />
              <span><strong>Direct Settlement:</strong> Notification and fulfillment details are sent to the seller.</span>
            </div>
          </div>
        </CardContent>

        {/* Footer Actions */}
        <CardFooter className="bg-slate-50 p-4 border-t border-slate-100 flex items-center justify-end gap-3">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={onCancel}
            disabled={isSubmitting}
            className="text-xs font-semibold px-4"
          >
            Cancel
          </Button>

          <Button
            type="button"
            size="sm"
            onClick={onConfirm}
            disabled={isSubmitting}
            className="text-xs font-bold bg-emerald-600 hover:bg-emerald-700 text-white gap-2 shadow-sm px-5 h-9"
          >
            <Zap className="w-3.5 h-3.5 fill-amber-300 text-amber-300" />
            {isSubmitting ? 'Confirming Purchase...' : 'Confirm & Buy Now'}
          </Button>
        </CardFooter>
      </Card>
    </div>
  )
}
