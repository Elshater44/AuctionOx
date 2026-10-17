import React, { useState, useMemo } from 'react'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { BidStatusBadge } from '@/components/my-activity/BidStatusBadge'
import { CountdownTimer } from '@/components/CountdownTimer'
import { 
  Trophy, 
  AlertTriangle, 
  CheckCircle2, 
  DollarSign, 
  Zap, 
  ExternalLink, 
  Search, 
  Gavel, 
  ArrowRight,
  RefreshCw
} from 'lucide-react'

export function MyBidsTab({ bids = [], isLoading, onRefresh, onSelectAuction, onNavigateHome }) {
  const [filter, setFilter] = useState('all') // 'all', 'winning', 'outbid', 'won'
  const [searchQuery, setSearchQuery] = useState('')

  // Aggregate user bids by auction lot (using the user's highest bid per lot)
  const aggregatedLots = useMemo(() => {
    if (!bids || bids.length === 0) return []

    const lotMap = new Map()

    for (const bid of bids) {
      const lotId = bid.auctionItemId
      if (!lotMap.has(lotId)) {
        lotMap.set(lotId, {
          auctionItemId: lotId,
          auctionTitle: bid.auctionTitle || `Auction #${lotId}`,
          currentPrice: bid.currentAuctionPrice || bid.bidAmount,
          endTime: bid.endTime,
          auctionStatus: bid.auctionStatus,
          imageUrl: bid.imageUrl,
          userHighestBid: bid.bidAmount,
          isWinningBid: bid.isWinningBid,
          bidCount: 1,
          latestBidTime: bid.bidTime,
          allBids: [bid],
        })
      } else {
        const existing = lotMap.get(lotId)
        existing.bidCount += 1
        existing.allBids.push(bid)
        if (bid.bidAmount > existing.userHighestBid) {
          existing.userHighestBid = bid.bidAmount
          existing.isWinningBid = bid.isWinningBid
        }
        if (new Date(bid.bidTime) > new Date(existing.latestBidTime)) {
          existing.latestBidTime = bid.bidTime
        }
      }
    }

    return Array.from(lotMap.values()).sort(
      (a, b) => new Date(b.latestBidTime) - new Date(a.latestBidTime)
    )
  }, [bids])

  // Derive metrics
  const metrics = useMemo(() => {
    let winningCount = 0
    let outbidCount = 0
    let wonCount = 0
    let totalCommitted = 0

    for (const lot of aggregatedLots) {
      const isCompleted = lot.auctionStatus === 2 || lot.auctionStatus === 'Completed'
      const isExpired = lot.endTime ? new Date(lot.endTime) <= new Date() : false
      const isClosed = isCompleted || isExpired

      if (isClosed) {
        if (lot.isWinningBid) {
          wonCount++
        }
      } else {
        // Active auction
        if (lot.isWinningBid) {
          winningCount++
          totalCommitted += lot.userHighestBid
        } else {
          outbidCount++
        }
      }
    }

    return { winningCount, outbidCount, wonCount, totalCommitted }
  }, [aggregatedLots])

  // Filtered lots
  const filteredLots = useMemo(() => {
    return aggregatedLots.filter((lot) => {
      const isCompleted = lot.auctionStatus === 2 || lot.auctionStatus === 'Completed'
      const isExpired = lot.endTime ? new Date(lot.endTime) <= new Date() : false
      const isClosed = isCompleted || isExpired

      if (filter === 'winning' && (isClosed || !lot.isWinningBid)) return false
      if (filter === 'outbid' && (isClosed || lot.isWinningBid)) return false
      if (filter === 'won' && (!isClosed || !lot.isWinningBid)) return false

      if (searchQuery.trim()) {
        const q = searchQuery.toLowerCase()
        return (lot.auctionTitle || '').toLowerCase().includes(q)
      }

      return true
    })
  }, [aggregatedLots, filter, searchQuery])

  if (isLoading) {
    return (
      <div className="py-16 flex flex-col items-center justify-center space-y-3">
        <RefreshCw className="w-8 h-8 text-blue-600 animate-spin" />
        <p className="text-sm text-slate-500 font-medium">Loading your bidding activity...</p>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Top Metrics Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {/* Winning Bids */}
        <Card className="border-emerald-100 bg-gradient-to-br from-white to-emerald-50/50 shadow-sm">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <p className="text-xs font-semibold text-emerald-700 uppercase tracking-wider">Winning</p>
              <h3 className="text-2xl font-black text-slate-900 mt-1">{metrics.winningCount}</h3>
              <p className="text-[11px] text-emerald-600 mt-0.5 font-medium">Currently highest</p>
            </div>
            <div className="p-3 bg-emerald-100 text-emerald-700 rounded-xl">
              <CheckCircle2 className="w-5 h-5" />
            </div>
          </CardContent>
        </Card>

        {/* Outbid (Action Needed) */}
        <Card className={`border-amber-100 bg-gradient-to-br from-white to-amber-50/50 shadow-sm ${metrics.outbidCount > 0 ? 'ring-2 ring-amber-400/40' : ''}`}>
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <p className="text-xs font-semibold text-amber-700 uppercase tracking-wider">Outbid</p>
              <h3 className="text-2xl font-black text-slate-900 mt-1">{metrics.outbidCount}</h3>
              <p className="text-[11px] text-amber-600 mt-0.5 font-medium">Action needed</p>
            </div>
            <div className="p-3 bg-amber-100 text-amber-700 rounded-xl">
              <AlertTriangle className="w-5 h-5" />
            </div>
          </CardContent>
        </Card>

        {/* Auctions Won */}
        <Card className="border-purple-100 bg-gradient-to-br from-white to-purple-50/50 shadow-sm">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <p className="text-xs font-semibold text-purple-700 uppercase tracking-wider">Lots Won</p>
              <h3 className="text-2xl font-black text-slate-900 mt-1">{metrics.wonCount}</h3>
              <p className="text-[11px] text-purple-600 mt-0.5 font-medium">Auctions secured</p>
            </div>
            <div className="p-3 bg-purple-100 text-purple-700 rounded-xl">
              <Trophy className="w-5 h-5" />
            </div>
          </CardContent>
        </Card>

        {/* Total Committed */}
        <Card className="border-blue-100 bg-gradient-to-br from-white to-blue-50/50 shadow-sm">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <p className="text-xs font-semibold text-blue-700 uppercase tracking-wider">Active Committed</p>
              <h3 className="text-2xl font-black text-slate-900 mt-1">
                ${metrics.totalCommitted.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
              </h3>
              <p className="text-[11px] text-blue-600 mt-0.5 font-medium">In winning bids</p>
            </div>
            <div className="p-3 bg-blue-100 text-blue-700 rounded-xl">
              <DollarSign className="w-5 h-5" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Filter and Search Bar */}
      <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 bg-white p-3 rounded-xl border shadow-sm">
        <div className="flex items-center gap-1.5 overflow-x-auto pb-1 sm:pb-0">
          <Button
            size="sm"
            variant={filter === 'all' ? 'default' : 'ghost'}
            onClick={() => setFilter('all')}
            className="text-xs font-medium"
          >
            All Bids ({aggregatedLots.length})
          </Button>
          <Button
            size="sm"
            variant={filter === 'winning' ? 'default' : 'ghost'}
            onClick={() => setFilter('winning')}
            className={`text-xs font-medium ${filter === 'winning' ? 'bg-emerald-600 hover:bg-emerald-700' : 'text-emerald-700'}`}
          >
            Winning ({metrics.winningCount})
          </Button>
          <Button
            size="sm"
            variant={filter === 'outbid' ? 'default' : 'ghost'}
            onClick={() => setFilter('outbid')}
            className={`text-xs font-medium ${filter === 'outbid' ? 'bg-amber-600 hover:bg-amber-700' : 'text-amber-700'}`}
          >
            Outbid ({metrics.outbidCount})
          </Button>
          <Button
            size="sm"
            variant={filter === 'won' ? 'default' : 'ghost'}
            onClick={() => setFilter('won')}
            className={`text-xs font-medium ${filter === 'won' ? 'bg-purple-600 hover:bg-purple-700' : 'text-purple-700'}`}
          >
            Won ({metrics.wonCount})
          </Button>
        </div>

        <div className="flex items-center gap-2">
          <div className="relative w-full sm:w-56">
            <Search className="w-3.5 h-3.5 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
            <Input
              type="text"
              placeholder="Filter by title..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-8 text-xs h-8"
            />
          </div>
          {onRefresh && (
            <Button
              variant="outline"
              size="sm"
              onClick={onRefresh}
              title="Refresh bids"
              className="h-8 px-2.5 text-slate-600"
            >
              <RefreshCw className="w-3.5 h-3.5" />
            </Button>
          )}
        </div>
      </div>

      {/* Bid Lots List */}
      {filteredLots.length === 0 ? (
        <Card className="border-dashed p-10 text-center bg-white shadow-sm">
          <div className="w-12 h-12 bg-slate-100 text-slate-400 rounded-full flex items-center justify-center mx-auto mb-3">
            <Gavel className="w-6 h-6" />
          </div>
          <h3 className="font-semibold text-slate-800 text-base">No bids found</h3>
          <p className="text-xs text-slate-500 mt-1 max-w-sm mx-auto">
            {aggregatedLots.length === 0
              ? "You haven't placed any bids yet. Explore our live auctions and place your first bid!"
              : "No bids match your selected filter or search keyword."}
          </p>
          {aggregatedLots.length === 0 && (
            <Button
              onClick={onNavigateHome}
              className="mt-4 inline-flex items-center gap-1.5 text-xs font-semibold"
            >
              Explore Live Auctions <ArrowRight className="w-3.5 h-3.5" />
            </Button>
          )}
        </Card>
      ) : (
        <div className="space-y-3">
          {filteredLots.map((lot) => {
            const isCompleted = lot.auctionStatus === 2 || lot.auctionStatus === 'Completed'
            const isExpired = lot.endTime ? new Date(lot.endTime) <= new Date() : false
            const isClosed = isCompleted || isExpired
            const isOutbid = !isClosed && !lot.isWinningBid
            const isWinning = !isClosed && lot.isWinningBid
            const isWon = isClosed && lot.isWinningBid
            const deficit = (lot.currentPrice || 0) - (lot.userHighestBid || 0)

            return (
              <Card 
                key={lot.auctionItemId} 
                className={`transition-all hover:shadow-md bg-white border ${
                  isOutbid ? 'border-amber-300 ring-1 ring-amber-200' : 'border-slate-200'
                }`}
              >
                <CardContent className="p-4 flex flex-col md:flex-row md:items-center justify-between gap-4">
                  
                  {/* Left: Thumbnail & Main Info */}
                  <div className="flex items-start gap-4 flex-1 min-w-0">
                    <div 
                      onClick={() => onSelectAuction(lot.auctionItemId)}
                      className="w-16 h-16 sm:w-20 sm:h-20 rounded-lg overflow-hidden bg-slate-100 flex-shrink-0 cursor-pointer border relative group"
                    >
                      {lot.imageUrl ? (
                        <img
                          src={lot.imageUrl}
                          alt={lot.auctionTitle}
                          className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-200"
                        />
                      ) : (
                        <div className="w-full h-full flex items-center justify-center text-slate-400">
                          <Gavel className="w-6 h-6" />
                        </div>
                      )}
                    </div>

                    <div className="space-y-1.5 min-w-0 flex-1">
                      <div className="flex items-center gap-2 flex-wrap">
                        <BidStatusBadge bid={lot} />
                        {lot.endTime && !isClosed && (
                          <CountdownTimer endTime={lot.endTime} />
                        )}
                        {lot.bidCount > 1 && (
                          <span className="text-[10px] bg-slate-100 text-slate-600 px-1.5 py-0.5 rounded font-medium">
                            {lot.bidCount} of your bids
                          </span>
                        )}
                      </div>

                      <h4 
                        onClick={() => onSelectAuction(lot.auctionItemId)}
                        className="font-bold text-slate-900 text-sm sm:text-base hover:text-blue-600 cursor-pointer truncate"
                      >
                        {lot.auctionTitle}
                      </h4>

                      {/* Pricing comparison */}
                      <div className="flex items-center gap-4 text-xs">
                        <div>
                          <span className="text-slate-500">Your Bid: </span>
                          <span className="font-bold text-slate-800">
                            ${lot.userHighestBid.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                          </span>
                        </div>

                        <div>
                          <span className="text-slate-500">Current Price: </span>
                          <span className="font-bold text-blue-600">
                            ${lot.currentPrice.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                          </span>
                        </div>

                        {isOutbid && deficit > 0 && (
                          <div className="hidden sm:inline-flex text-amber-700 bg-amber-50 px-2 py-0.5 rounded font-medium border border-amber-200 text-[11px]">
                            Behind by ${deficit.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                          </div>
                        )}
                      </div>
                    </div>
                  </div>

                  {/* Right: Dynamic Action Buttons */}
                  <div className="flex items-center gap-2 self-end md:self-center flex-shrink-0">
                    {isOutbid && (
                      <Button
                        size="sm"
                        onClick={() => onSelectAuction(lot.auctionItemId)}
                        className="bg-amber-600 hover:bg-amber-700 text-white font-bold text-xs flex items-center gap-1.5 shadow-sm shadow-amber-200 animate-bounce"
                      >
                        <Zap className="w-3.5 h-3.5 fill-current" />
                        ⚡ Re-Bid Now
                      </Button>
                    )}

                    {isWinning && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => onSelectAuction(lot.auctionItemId)}
                        className="text-xs font-semibold text-emerald-700 border-emerald-300 hover:bg-emerald-50 flex items-center gap-1.5"
                      >
                        <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600" />
                        View Live Room
                      </Button>
                    )}

                    {isWon && (
                      <Button
                        size="sm"
                        onClick={() => onSelectAuction(lot.auctionItemId)}
                        className="bg-purple-600 hover:bg-purple-700 text-white font-semibold text-xs flex items-center gap-1.5 shadow-sm"
                      >
                        <Trophy className="w-3.5 h-3.5" />
                        View Won Lot
                      </Button>
                    )}

                    {isClosed && !isWon && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => onSelectAuction(lot.auctionItemId)}
                        className="text-xs text-slate-600 flex items-center gap-1.5"
                      >
                        <ExternalLink className="w-3.5 h-3.5" />
                        View Result
                      </Button>
                    )}
                  </div>

                </CardContent>
              </Card>
            )
          })}
        </div>
      )}
    </div>
  )
}
