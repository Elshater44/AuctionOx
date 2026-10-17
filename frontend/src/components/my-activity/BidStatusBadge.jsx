import React from 'react'
import { Badge } from '@/components/ui/badge'
import { Trophy, AlertTriangle, CheckCircle2, Clock, Ban } from 'lucide-react'

export function BidStatusBadge({ bid, className = '' }) {
  if (!bid) return null

  const isCompleted = bid.auctionStatus === 2 || bid.auctionStatus === 'Completed'
  const isCancelled = bid.auctionStatus === 3 || bid.auctionStatus === 'Cancelled'
  const isSuspended = bid.auctionStatus === 4 || bid.auctionStatus === 'Suspended'
  
  const isExpired = bid.endTime ? new Date(bid.endTime) <= new Date() : false
  const isEnded = isCompleted || isExpired

  if (isCancelled || isSuspended) {
    return (
      <Badge variant="destructive" className={`flex items-center gap-1.5 font-medium ${className}`}>
        <Ban className="w-3.5 h-3.5" />
        {isCancelled ? 'Cancelled' : 'Suspended'}
      </Badge>
    )
  }

  if (isEnded) {
    if (bid.isWinningBid) {
      return (
        <Badge variant="success" className={`flex items-center gap-1.5 font-medium bg-emerald-600 text-white shadow-sm ${className}`}>
          <Trophy className="w-3.5 h-3.5" />
          Auction Won
        </Badge>
      )
    }
    return (
      <Badge variant="secondary" className={`flex items-center gap-1.5 font-medium text-slate-600 bg-slate-200/80 ${className}`}>
        <Clock className="w-3.5 h-3.5" />
        Ended
      </Badge>
    )
  }

  // Active auction
  if (bid.isWinningBid) {
    return (
      <Badge variant="success" className={`flex items-center gap-1.5 font-medium bg-emerald-500/15 text-emerald-700 border border-emerald-300 ${className}`}>
        <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600" />
        Winning Bid
      </Badge>
    )
  }

  return (
    <Badge variant="warning" className={`flex items-center gap-1.5 font-semibold bg-amber-500/15 text-amber-800 border border-amber-300 animate-pulse ${className}`}>
      <AlertTriangle className="w-3.5 h-3.5 text-amber-600" />
      Outbid
    </Badge>
  )
}
