import React, { useState, useMemo } from 'react'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { CountdownTimer } from '@/components/CountdownTimer'
import { ListingActionsMenu } from '@/components/my-activity/ListingActionsMenu'
import { 
  Package, 
  DollarSign, 
  CheckCircle, 
  PlusCircle, 
  Search, 
  RefreshCw,
  Users,
  ChevronLeft,
  ChevronRight
} from 'lucide-react'

export function MyListingsTab({ 
  listings = [], 
  totalCount = 0,
  pageNumber = 1,
  pageSize = 10,
  onPageChange,
  isLoading, 
  onRefresh, 
  onSelectAuction, 
  onNavigateCreate,
  onDeleteAuction 
}) {
  const [filter, setFilter] = useState('all') // 'all', 'active', 'completed', 'unbid'
  const [searchQuery, setSearchQuery] = useState('')

  // Compute seller metrics across loaded listings
  const metrics = useMemo(() => {
    let activeCount = 0
    let completedCount = 0
    let totalBidValue = 0
    let totalBidsCount = 0

    for (const item of listings) {
      const isCompleted = item.status === 2 || item.status === 'Completed'
      const isExpired = item.endTime ? new Date(item.endTime) <= new Date() : false
      const isClosed = isCompleted || isExpired

      totalBidsCount += item.bidCount || 0

      if (isClosed) {
        completedCount++
      } else {
        activeCount++
        if ((item.bidCount || 0) > 0) {
          totalBidValue += item.currentPrice || 0
        }
      }
    }

    return { activeCount, completedCount, totalBidValue, totalBidsCount }
  }, [listings])

  // Filter listings
  const filteredListings = useMemo(() => {
    return listings.filter((item) => {
      const isCompleted = item.status === 2 || item.status === 'Completed'
      const isExpired = item.endTime ? new Date(item.endTime) <= new Date() : false
      const isClosed = isCompleted || isExpired
      const hasBids = (item.bidCount || 0) > 0

      if (filter === 'active' && isClosed) return false
      if (filter === 'completed' && !isClosed) return false
      if (filter === 'unbid' && (isClosed || hasBids)) return false

      if (searchQuery.trim()) {
        const q = searchQuery.toLowerCase()
        return (item.title || '').toLowerCase().includes(q) || (item.categoryName || '').toLowerCase().includes(q)
      }

      return true
    })
  }, [listings, filter, searchQuery])

  const totalPages = Math.ceil(totalCount / pageSize) || 1

  if (isLoading) {
    return (
      <div className="py-16 flex flex-col items-center justify-center space-y-3">
        <RefreshCw className="w-8 h-8 text-blue-600 animate-spin" />
        <p className="text-sm text-slate-500 font-medium">Loading your listed lots...</p>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Top Metrics Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {/* Active Lots */}
        <Card className="border-blue-100 bg-gradient-to-br from-white to-blue-50/50 shadow-sm">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <p className="text-xs font-semibold text-blue-700 uppercase tracking-wider">Active Lots</p>
              <h3 className="text-2xl font-black text-slate-900 mt-1">{metrics.activeCount}</h3>
              <p className="text-[11px] text-blue-600 mt-0.5 font-medium">Currently on marketplace</p>
            </div>
            <div className="p-3 bg-blue-100 text-blue-700 rounded-xl">
              <Package className="w-5 h-5" />
            </div>
          </CardContent>
        </Card>

        {/* Current Bid Value */}
        <Card className="border-emerald-100 bg-gradient-to-br from-white to-emerald-50/50 shadow-sm">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <p className="text-xs font-semibold text-emerald-700 uppercase tracking-wider">Bid Value</p>
              <h3 className="text-2xl font-black text-slate-900 mt-1">
                ${metrics.totalBidValue.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
              </h3>
              <p className="text-[11px] text-emerald-600 mt-0.5 font-medium">On active lots</p>
            </div>
            <div className="p-3 bg-emerald-100 text-emerald-700 rounded-xl">
              <DollarSign className="w-5 h-5" />
            </div>
          </CardContent>
        </Card>

        {/* Completed / Sold */}
        <Card className="border-purple-100 bg-gradient-to-br from-white to-purple-50/50 shadow-sm">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <p className="text-xs font-semibold text-purple-700 uppercase tracking-wider">Lots Completed</p>
              <h3 className="text-2xl font-black text-slate-900 mt-1">{metrics.completedCount}</h3>
              <p className="text-[11px] text-purple-600 mt-0.5 font-medium">Finished auctions</p>
            </div>
            <div className="p-3 bg-purple-100 text-purple-700 rounded-xl">
              <CheckCircle className="w-5 h-5" />
            </div>
          </CardContent>
        </Card>

        {/* Total Bids Received */}
        <Card className="border-indigo-100 bg-gradient-to-br from-white to-indigo-50/50 shadow-sm">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <p className="text-xs font-semibold text-indigo-700 uppercase tracking-wider">Bids Received</p>
              <h3 className="text-2xl font-black text-slate-900 mt-1">{metrics.totalBidsCount}</h3>
              <p className="text-[11px] text-indigo-600 mt-0.5 font-medium">Total buyer engagement</p>
            </div>
            <div className="p-3 bg-indigo-100 text-indigo-700 rounded-xl">
              <Users className="w-5 h-5" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Action / Filter Bar */}
      <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 bg-white p-3 rounded-xl border shadow-sm">
        <div className="flex items-center gap-1.5 overflow-x-auto pb-1 sm:pb-0">
          <Button
            size="sm"
            variant={filter === 'all' ? 'default' : 'ghost'}
            onClick={() => setFilter('all')}
            className="text-xs font-medium"
          >
            All Lots ({listings.length})
          </Button>
          <Button
            size="sm"
            variant={filter === 'active' ? 'default' : 'ghost'}
            onClick={() => setFilter('active')}
            className="text-xs font-medium"
          >
            Active ({metrics.activeCount})
          </Button>
          <Button
            size="sm"
            variant={filter === 'completed' ? 'default' : 'ghost'}
            onClick={() => setFilter('completed')}
            className="text-xs font-medium"
          >
            Completed ({metrics.completedCount})
          </Button>
          <Button
            size="sm"
            variant={filter === 'unbid' ? 'default' : 'ghost'}
            onClick={() => setFilter('unbid')}
            className="text-xs font-medium"
          >
            No Bids (Editable)
          </Button>
        </div>

        <div className="flex items-center gap-2">
          <div className="relative w-full sm:w-56">
            <Search className="w-3.5 h-3.5 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
            <Input
              type="text"
              placeholder="Filter listings..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-8 text-xs h-8"
            />
          </div>

          <Button
            size="sm"
            onClick={onNavigateCreate}
            className="h-8 text-xs font-semibold bg-blue-600 hover:bg-blue-700 flex items-center gap-1.5 whitespace-nowrap"
          >
            <PlusCircle className="w-3.5 h-3.5" />
            Create Lot
          </Button>
          
          {onRefresh && (
            <Button
              variant="outline"
              size="sm"
              onClick={onRefresh}
              title="Refresh listings"
              className="h-8 px-2.5 text-slate-600"
            >
              <RefreshCw className="w-3.5 h-3.5" />
            </Button>
          )}
        </div>
      </div>

      {/* Listing Cards List */}
      {filteredListings.length === 0 ? (
        <Card className="border-dashed p-10 text-center bg-white shadow-sm">
          <div className="w-12 h-12 bg-slate-100 text-slate-400 rounded-full flex items-center justify-center mx-auto mb-3">
            <Package className="w-6 h-6" />
          </div>
          <h3 className="font-semibold text-slate-800 text-base">No listings found</h3>
          <p className="text-xs text-slate-500 mt-1 max-w-sm mx-auto">
            {listings.length === 0
              ? "You haven't listed any auctions yet. Start selling items to reaching thousands of bidders!"
              : "No listings match your selected filter criteria."}
          </p>
          {listings.length === 0 && (
            <Button
              onClick={onNavigateCreate}
              className="mt-4 inline-flex items-center gap-1.5 text-xs font-semibold"
            >
              <PlusCircle className="w-3.5 h-3.5" /> List Your First Item
            </Button>
          )}
        </Card>
      ) : (
        <div className="space-y-3">
          {filteredListings.map((auction) => {
            const isCompleted = auction.status === 2 || auction.status === 'Completed'
            const isCancelled = auction.status === 3 || auction.status === 'Cancelled'
            const isSuspended = auction.status === 4 || auction.status === 'Suspended'
            const isExpired = auction.endTime ? new Date(auction.endTime) <= new Date() : false
            const isClosed = isCompleted || isCancelled || isSuspended || isExpired

            return (
              <Card 
                key={auction.id} 
                className="transition-all hover:shadow-md bg-white border border-slate-200"
              >
                <CardContent className="p-4 flex flex-col md:flex-row md:items-center justify-between gap-4">
                  {/* Left: Thumbnail & Info */}
                  <div className="flex items-start gap-4 flex-1 min-w-0">
                    <div 
                      onClick={() => onSelectAuction(auction.id)}
                      className="w-16 h-16 sm:w-20 sm:h-20 rounded-lg overflow-hidden bg-slate-100 flex-shrink-0 cursor-pointer border relative group"
                    >
                      {auction.imageUrl ? (
                        <img
                          src={auction.imageUrl}
                          alt={auction.title}
                          className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-200"
                        />
                      ) : (
                        <div className="w-full h-full flex items-center justify-center text-slate-400">
                          <Package className="w-6 h-6" />
                        </div>
                      )}
                    </div>

                    <div className="space-y-1.5 min-w-0 flex-1">
                      <div className="flex items-center gap-2 flex-wrap">
                        {auction.categoryName && (
                          <span className="text-[10px] font-semibold text-slate-500 uppercase tracking-wider bg-slate-100 px-2 py-0.5 rounded">
                            {auction.categoryName}
                          </span>
                        )}

                        {isClosed ? (
                          <Badge variant="secondary" className="text-xs">
                            {isCancelled ? 'Cancelled' : isSuspended ? 'Suspended' : 'Completed'}
                          </Badge>
                        ) : (
                          <Badge variant="success" className="text-xs bg-emerald-100 text-emerald-800 border-emerald-200">
                            Active
                          </Badge>
                        )}

                        {auction.endTime && !isClosed && (
                          <CountdownTimer endTime={auction.endTime} />
                        )}
                      </div>

                      <h4 
                        onClick={() => onSelectAuction(auction.id)}
                        className="font-bold text-slate-900 text-sm sm:text-base hover:text-blue-600 cursor-pointer truncate"
                      >
                        {auction.title}
                      </h4>

                      <div className="flex items-center gap-4 text-xs flex-wrap">
                        <div>
                          <span className="text-slate-500">Starting: </span>
                          <span className="font-semibold text-slate-700">
                            ${auction.startingPrice?.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                          </span>
                        </div>

                        <div>
                          <span className="text-slate-500">Current Price: </span>
                          <span className="font-bold text-emerald-700">
                            ${auction.currentPrice?.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                          </span>
                        </div>

                        <div className="flex items-center gap-1 font-medium text-slate-600 bg-slate-100 px-2 py-0.5 rounded text-[11px]">
                          <Users className="w-3 h-3 text-slate-500" />
                          <span>{auction.bidCount || 0} bids</span>
                        </div>
                      </div>
                    </div>
                  </div>

                  {/* Right: Seller Actions */}
                  <div className="self-end md:self-center flex-shrink-0">
                    <ListingActionsMenu
                      auction={auction}
                      onSelectAuction={onSelectAuction}
                      onDeleteAuction={onDeleteAuction}
                    />
                  </div>
                </CardContent>
              </Card>
            )
          })}
        </div>
      )}

      {/* Pagination Controls */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between border-t border-slate-200 pt-4">
          <p className="text-xs text-slate-500">
            Showing Page <span className="font-bold text-slate-800">{pageNumber}</span> of <span className="font-bold text-slate-800">{totalPages}</span> ({totalCount} total items)
          </p>

          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={pageNumber <= 1}
              onClick={() => onPageChange(pageNumber - 1)}
              className="text-xs h-8"
            >
              <ChevronLeft className="w-3.5 h-3.5 mr-1" /> Previous
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={pageNumber >= totalPages}
              onClick={() => onPageChange(pageNumber + 1)}
              className="text-xs h-8"
            >
              Next <ChevronRight className="w-3.5 h-3.5 ml-1" />
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
