import React, { useState, useEffect, useCallback } from 'react'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell
} from '@/components/ui/table'
import { AdminConfirmModal } from './AdminConfirmModal'
import { EditAuctionModal } from '@/components/EditAuctionModal'
import { adminApi } from '@/api/adminApi'
import { useToast } from '@/context/ToastContext'
import {
  Gavel,
  Search,
  RefreshCw,
  Pause,
  Play,
  Trash2,
  Eye,
  AlertTriangle,
  ChevronLeft,
  ChevronRight,
  ShieldAlert,
  Edit3
} from 'lucide-react'

export function AuctionModerationTab({ onSelectAuction }) {
  const toast = useToast()
  const [auctions, setAuctions] = useState([])
  const [totalCount, setTotalCount] = useState(0)
  const [isLoading, setIsLoading] = useState(true)

  // Filters & Pagination
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [page, setPage] = useState(1)
  const pageSize = 10

  // Confirm Modal State
  const [confirmState, setConfirmState] = useState({
    isOpen: false,
    type: null, // 'suspend' | 'resume' | 'delete'
    auction: null,
  })
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [editingAuction, setEditingAuction] = useState(null)

  const loadAuctions = useCallback(async () => {
    setIsLoading(true)
    try {
      const data = await adminApi.getAllAuctions({
        search: search.trim() || undefined,
        status: statusFilter || undefined,
        pageNumber: page,
        pageSize,
      })
      setAuctions(data.items || [])
      setTotalCount(data.totalCount || 0)
    } catch (err) {
      console.error('Failed to load moderation auctions:', err)
      toast.error('Error', 'Unable to fetch auctions for moderation.')
    } finally {
      setIsLoading(false)
    }
  }, [search, statusFilter, page, toast])

  useEffect(() => {
    loadAuctions()
  }, [loadAuctions])

  // Reset to page 1 on filter changes
  const handleFilterChange = (newStatus) => {
    setStatusFilter(newStatus)
    setPage(1)
  }

  const handleSearchSubmit = (e) => {
    e.preventDefault()
    setPage(1)
    loadAuctions()
  }

  // Action Triggers
  const triggerSuspend = (auction) => {
    setConfirmState({
      isOpen: true,
      type: 'suspend',
      auction,
    })
  }

  const triggerResume = (auction) => {
    setConfirmState({
      isOpen: true,
      type: 'resume',
      auction,
    })
  }

  const triggerDelete = (auction) => {
    setConfirmState({
      isOpen: true,
      type: 'delete',
      auction,
    })
  }

  const handleConfirmAction = async () => {
    const { type, auction } = confirmState
    if (!type || !auction) return

    setIsSubmitting(true)
    try {
      if (type === 'suspend') {
        await adminApi.suspendAuction(auction.id)
        toast.warning(
          'Auction Suspended',
          `Lot #${auction.id} "${auction.title}" has been suspended. Bidding is now frozen.`
        )
      } else if (type === 'resume') {
        await adminApi.resumeAuction(auction.id)
        toast.success(
          'Auction Resumed',
          `Lot #${auction.id} "${auction.title}" is now active and bidding has resumed.`
        )
      } else if (type === 'delete') {
        await adminApi.deleteAuction(auction.id)
        toast.success(
          'Auction Deleted',
          `Lot #${auction.id} "${auction.title}" has been administratively removed.`
        )
      }
      setConfirmState({ isOpen: false, type: null, auction: null })
      loadAuctions()
    } catch (err) {
      const msg = err.response?.data?.message || err.response?.data?.detail || `Failed to ${type} auction.`
      toast.error('Action Failed', msg)
    } finally {
      setIsSubmitting(false)
    }
  }

  // Format Helper for Status
  const getStatusBadge = (status) => {
    // Status can be number (0: Draft, 1: Active, 2: Completed, 3: Cancelled, 4: Suspended) or string
    const s = typeof status === 'string' ? status.toLowerCase() : status

    if (s === 1 || s === 'active') {
      return (
        <Badge variant="success" className="text-[10px] uppercase tracking-wider font-bold">
          Active
        </Badge>
      )
    }
    if (s === 4 || s === 'suspended') {
      return (
        <Badge variant="warning" className="text-[10px] uppercase tracking-wider font-bold">
          Suspended
        </Badge>
      )
    }
    if (s === 2 || s === 'completed') {
      return (
        <Badge variant="secondary" className="text-[10px] uppercase tracking-wider font-bold bg-slate-200 text-slate-700">
          Completed
        </Badge>
      )
    }
    if (s === 3 || s === 'cancelled') {
      return (
        <Badge variant="destructive" className="text-[10px] uppercase tracking-wider font-bold">
          Cancelled
        </Badge>
      )
    }
    return (
      <Badge variant="outline" className="text-[10px] uppercase tracking-wider font-bold">
        Draft
      </Badge>
    )
  }

  const isAuctionActive = (status) => status === 1 || String(status).toLowerCase() === 'active'
  const isAuctionSuspended = (status) => status === 4 || String(status).toLowerCase() === 'suspended'

  const totalPages = Math.ceil(totalCount / pageSize) || 1

  return (
    <div className="space-y-4">
      {/* Action & Filter Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-4 rounded-xl border border-slate-200 shadow-sm">
        <div>
          <h3 className="text-base font-bold text-slate-900 flex items-center gap-2">
            <ShieldAlert className="w-4 h-4 text-purple-600" />
            Auction Moderation Queue
          </h3>
          <p className="text-xs text-slate-500">
            Oversee marketplace lots, suspend suspect bidding in real-time, or remove fraudulent listings.
          </p>
        </div>

        {/* Filter Controls */}
        <div className="flex flex-wrap items-center gap-2.5">
          <form onSubmit={handleSearchSubmit} className="relative min-w-[200px]">
            <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-slate-400" />
            <Input
              type="text"
              placeholder="Search by lot title..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-8 h-9 text-xs"
            />
          </form>

          <select
            value={statusFilter}
            onChange={(e) => handleFilterChange(e.target.value)}
            className="h-9 px-3 text-xs rounded-md border border-slate-200 bg-white font-medium text-slate-700 focus:outline-none focus:ring-2 focus:ring-purple-500 cursor-pointer"
          >
            <option value="">All Statuses</option>
            <option value="Active">Active</option>
            <option value="Suspended">Suspended</option>
            <option value="Completed">Completed</option>
            <option value="Cancelled">Cancelled</option>
          </select>

          <Button
            variant="outline"
            size="sm"
            onClick={loadAuctions}
            disabled={isLoading}
            className="text-xs gap-1.5 h-9"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isLoading ? 'animate-spin' : ''}`} /> Refresh
          </Button>
        </div>
      </div>

      {/* Table Card */}
      <Card className="border-slate-200 shadow-sm overflow-hidden">
        <CardContent className="p-0">
          {isLoading ? (
            <div className="p-12 text-center text-xs text-slate-400 space-y-2">
              <RefreshCw className="w-6 h-6 animate-spin mx-auto text-purple-600" />
              <span>Loading auctions for moderation...</span>
            </div>
          ) : auctions.length === 0 ? (
            <div className="p-12 text-center text-slate-400 space-y-2">
              <Gavel className="w-8 h-8 mx-auto stroke-1 text-slate-300" />
              <p className="text-xs font-semibold text-slate-600">No auctions found</p>
              <p className="text-[11px]">No auction lots matched your current filter criteria.</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow className="bg-slate-50/80 hover:bg-slate-50/80">
                    <TableHead className="w-[70px] text-xs font-bold text-slate-600">Lot #</TableHead>
                    <TableHead className="min-w-[240px] text-xs font-bold text-slate-600">Item</TableHead>
                    <TableHead className="text-xs font-bold text-slate-600">Category</TableHead>
                    <TableHead className="text-xs font-bold text-slate-600">Seller</TableHead>
                    <TableHead className="text-xs font-bold text-slate-600">Price</TableHead>
                    <TableHead className="text-xs font-bold text-slate-600">Bids</TableHead>
                    <TableHead className="text-xs font-bold text-slate-600">Status</TableHead>
                    <TableHead className="text-right text-xs font-bold text-slate-600">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {auctions.map((auction) => {
                    const active = isAuctionActive(auction.status)
                    const suspended = isAuctionSuspended(auction.status)

                    return (
                      <TableRow key={auction.id} className="hover:bg-slate-50/50">
                        {/* Lot ID */}
                        <TableCell className="font-mono text-xs font-bold text-slate-500">
                          #{auction.id}
                        </TableCell>

                        {/* Item Title & Thumbnail */}
                        <TableCell>
                          <div className="flex items-center gap-3">
                            <div className="w-10 h-10 rounded-lg bg-slate-100 overflow-hidden flex-shrink-0 border border-slate-200">
                              {auction.imageUrl ? (
                                <img
                                  src={auction.imageUrl}
                                  alt={auction.title}
                                  className="w-full h-full object-cover"
                                  onError={(e) => {
                                    e.target.style.display = 'none'
                                  }}
                                />
                              ) : (
                                <div className="w-full h-full flex items-center justify-center text-slate-400">
                                  <Gavel className="w-4 h-4" />
                                </div>
                              )}
                            </div>
                            <div className="min-w-0">
                              <p className="font-semibold text-xs text-slate-900 truncate max-w-xs">
                                {auction.title}
                              </p>
                              <p className="text-[10px] text-slate-400 truncate max-w-xs">
                                {auction.description || 'No description provided'}
                              </p>
                            </div>
                          </div>
                        </TableCell>

                        {/* Category */}
                        <TableCell className="text-xs text-slate-600">
                          {auction.categoryName || 'General'}
                        </TableCell>

                        {/* Seller */}
                        <TableCell className="text-xs text-slate-600">
                          {auction.sellerName || 'Anonymous'}
                        </TableCell>

                        {/* Price */}
                        <TableCell className="text-xs font-bold text-slate-900">
                          ${(auction.currentPrice ?? auction.startingPrice ?? 0).toLocaleString(undefined, {
                            minimumFractionDigits: 2,
                            maximumFractionDigits: 2,
                          })}
                        </TableCell>

                        {/* Bids */}
                        <TableCell className="text-xs text-slate-600">
                          <span className="font-semibold text-slate-800">{auction.bidCount ?? 0}</span>
                        </TableCell>

                        {/* Status */}
                        <TableCell>
                          {getStatusBadge(auction.status)}
                        </TableCell>

                        {/* Actions */}
                        <TableCell className="text-right">
                          <div className="flex items-center justify-end gap-1.5">
                            {/* Inspect Room */}
                            {onSelectAuction && (
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => onSelectAuction(auction.id)}
                                title="View Lot in Live Room"
                                className="h-8 w-8 p-0 text-slate-500 hover:text-blue-600 hover:bg-blue-50"
                              >
                                <Eye className="w-4 h-4" />
                              </Button>
                            )}

                            {/* Edit Lot */}
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => setEditingAuction(auction)}
                              title="Edit Auction Details"
                              className="h-8 w-8 p-0 text-slate-500 hover:text-blue-600 hover:bg-blue-50"
                            >
                              <Edit3 className="w-4 h-4" />
                            </Button>

                            {/* Suspend Toggle */}
                            {active && (
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => triggerSuspend(auction)}
                                title="Suspend live bidding"
                                className="h-8 px-2 text-[11px] font-semibold text-amber-700 border-amber-200 hover:bg-amber-50 hover:border-amber-300 gap-1"
                              >
                                <Pause className="w-3.5 h-3.5" /> Suspend
                              </Button>
                            )}

                            {/* Resume Toggle */}
                            {suspended && (
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => triggerResume(auction)}
                                title="Resume live bidding"
                                className="h-8 px-2 text-[11px] font-semibold text-emerald-700 border-emerald-200 hover:bg-emerald-50 hover:border-emerald-300 gap-1"
                              >
                                <Play className="w-3.5 h-3.5" /> Resume
                              </Button>
                            )}

                            {/* Delete Auction */}
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => triggerDelete(auction)}
                              title="Delete Lot"
                              className="h-8 w-8 p-0 text-slate-400 hover:text-red-600 hover:bg-red-50"
                            >
                              <Trash2 className="w-4 h-4" />
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    )
                  })}
                </TableBody>
              </Table>
            </div>
          )}

          {/* Pagination Footer */}
          {!isLoading && totalCount > 0 && (
            <div className="flex items-center justify-between px-4 py-3 border-t border-slate-100 bg-slate-50/50 text-xs text-slate-500">
              <div>
                Showing <span className="font-semibold text-slate-700">{(page - 1) * pageSize + 1}</span> to{' '}
                <span className="font-semibold text-slate-700">
                  {Math.min(page * pageSize, totalCount)}
                </span>{' '}
                of <span className="font-semibold text-slate-700">{totalCount}</span> auction lots
              </div>

              <div className="flex items-center gap-1.5">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={page <= 1}
                  className="h-7 px-2 text-xs gap-1"
                >
                  <ChevronLeft className="w-3.5 h-3.5" /> Prev
                </Button>

                <span className="text-xs px-2 font-medium">
                  {page} / {totalPages}
                </span>

                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                  disabled={page >= totalPages}
                  className="h-7 px-2 text-xs gap-1"
                >
                  Next <ChevronRight className="w-3.5 h-3.5" />
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Confirmation Dialog */}
      <AdminConfirmModal
        isOpen={confirmState.isOpen}
        title={
          confirmState.type === 'suspend'
            ? `Suspend Auction Lot #${confirmState.auction?.id}`
            : confirmState.type === 'resume'
            ? `Resume Auction Lot #${confirmState.auction?.id}`
            : `Delete Auction Lot #${confirmState.auction?.id}`
        }
        description={
          confirmState.type === 'suspend'
            ? `Are you sure you want to suspend "${confirmState.auction?.title}"? This will immediately freeze live bidding and prevent participants from placing any bids.`
            : confirmState.type === 'resume'
            ? `Are you sure you want to resume "${confirmState.auction?.title}"? This will unfreeze the lot and allow participants to continue bidding.`
            : `Are you sure you want to administratively delete "${confirmState.auction?.title}"? This will soft-delete the lot and remove it from marketplace search.`
        }
        confirmText={
          confirmState.type === 'suspend'
            ? 'Suspend Bidding'
            : confirmState.type === 'resume'
            ? 'Resume Bidding'
            : 'Delete Lot'
        }
        variant={confirmState.type === 'delete' ? 'destructive' : 'warning'}
        isSubmitting={isSubmitting}
        onConfirm={handleConfirmAction}
        onCancel={() => setConfirmState({ isOpen: false, type: null, auction: null })}
      />

      {/* Edit Auction Modal */}
      <EditAuctionModal
        isOpen={!!editingAuction}
        onClose={() => setEditingAuction(null)}
        auction={editingAuction}
        onSuccess={() => {
          loadAuctions()
        }}
      />
    </div>
  )
}
