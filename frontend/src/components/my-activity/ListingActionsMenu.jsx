import React, { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Trash2, ExternalLink, Lock, AlertCircle, Loader2 } from 'lucide-react'

export function ListingActionsMenu({ auction, onSelectAuction, onDeleteAuction }) {
  const [isDeleting, setIsDeleting] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)

  const hasBids = (auction.bidCount || 0) > 0
  const isCompleted = auction.status === 2 || auction.status === 'Completed'
  const isCancelled = auction.status === 3 || auction.status === 'Cancelled'
  const isSuspended = auction.status === 4 || auction.status === 'Suspended'
  const isExpired = auction.endTime ? new Date(auction.endTime) <= new Date() : false
  const isClosed = isCompleted || isCancelled || isSuspended || isExpired

  const handleDelete = async () => {
    setIsDeleting(true)
    try {
      await onDeleteAuction(auction.id)
      setShowConfirm(false)
    } finally {
      setIsDeleting(false)
    }
  }

  return (
    <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-2">
      <Button
        variant="outline"
        size="sm"
        onClick={() => onSelectAuction(auction.id)}
        className="flex items-center justify-center gap-1.5 text-xs font-semibold text-slate-700 hover:text-blue-600 hover:border-blue-300"
      >
        <ExternalLink className="w-3.5 h-3.5" />
        View Room
      </Button>

      {/* If bids exist on active item, locked by anti-sniping */}
      {!isClosed && hasBids && (
        <div 
          className="flex items-center gap-1.5 px-2.5 py-1.5 text-xs text-amber-700 bg-amber-50 rounded-md border border-amber-200 select-none"
          title="Once bids have been placed, lots cannot be deleted or cancelled to protect bidder commitments."
        >
          <Lock className="w-3 h-3 text-amber-600 flex-shrink-0" />
          <span className="font-medium truncate">Locked by Anti-Sniping</span>
        </div>
      )}

      {/* If no bids on active item, seller can cancel/delete */}
      {!isClosed && !hasBids && (
        <>
          {!showConfirm ? (
            <Button
              variant="outline"
              size="sm"
              onClick={() => setShowConfirm(true)}
              className="flex items-center justify-center gap-1.5 text-xs font-medium text-red-600 hover:text-red-700 hover:bg-red-50 border-red-200"
            >
              <Trash2 className="w-3.5 h-3.5" />
              Cancel Lot
            </Button>
          ) : (
            <div className="flex items-center gap-1.5 bg-red-50 p-1 rounded-md border border-red-200">
              <span className="text-[11px] font-semibold text-red-700 px-1">Confirm cancel?</span>
              <Button
                variant="destructive"
                size="sm"
                disabled={isDeleting}
                onClick={handleDelete}
                className="h-7 px-2 text-xs bg-red-600 hover:bg-red-700"
              >
                {isDeleting ? <Loader2 className="w-3 h-3 animate-spin" /> : 'Yes, Delete'}
              </Button>
              <Button
                variant="ghost"
                size="sm"
                disabled={isDeleting}
                onClick={() => setShowConfirm(false)}
                className="h-7 px-2 text-xs text-slate-600 hover:bg-slate-200"
              >
                No
              </Button>
            </div>
          )}
        </>
      )}
    </div>
  )
}
