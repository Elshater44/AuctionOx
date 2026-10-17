import React, { useState, useEffect, useCallback, useRef } from 'react'
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell
} from '@/components/ui/table'
import { CountdownTimer } from '@/components/CountdownTimer'
import { useParams, useNavigate } from 'react-router-dom'
import { ConfirmBidModal } from '@/components/ConfirmBidModal'
import { BuyItNowModal } from '@/components/BuyItNowModal'
import { EditAuctionModal } from '@/components/EditAuctionModal'
import { auctionApi } from '@/api/auctionApi'
import { useAuth } from '@/context/AuthContext'
import { useToast } from '@/context/ToastContext'
import { useBiddingHub } from '@/hooks/useBiddingHub'
import {
  ArrowLeft,
  Gavel,
  Zap,
  Shield,
  Clock,
  AlertCircle,
  AlertTriangle,
  TrendingUp,
  User as UserIcon,
  Radio,
  RefreshCw,
  Sparkles,
  Crown,
  Edit3
} from 'lucide-react'

export function AuctionDetailPage({ auctionId: propAuctionId, onBack, onNavigateLogin }) {
  const { id: paramAuctionId } = useParams()
  const navigate = useNavigate()
  const auctionId = propAuctionId || paramAuctionId
  const handleBack = onBack || (() => navigate('/'))
  const handleNavigateLogin = onNavigateLogin || (() => navigate('/login'))

  const { user, isAuthenticated, isAdmin } = useAuth()
  const toast = useToast()

  const [auction, setAuction] = useState(null)
  const [recentBids, setRecentBids] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [bidAmount, setBidAmount] = useState('')
  const [isPlacingBid, setIsPlacingBid] = useState(false)
  const [isBuyingNow, setIsBuyingNow] = useState(false)
  const [priceFlash, setPriceFlash] = useState(false)
  const [showConfirmModal, setShowConfirmModal] = useState(false)
  const [showBuyNowModal, setShowBuyNowModal] = useState(false)
  const [showEditModal, setShowEditModal] = useState(false)

  const prevHighestBidderIdRef = useRef(null)

  // 1. Fetch initial Auction Details
  const loadAuctionData = useCallback(async () => {
    if (!auctionId) return
    setIsLoading(true)
    try {
      const data = await auctionApi.getAuctionDetails(auctionId)
      setAuction(data)
      setRecentBids(data.recentBids || [])
      
      if (data.recentBids && data.recentBids.length > 0) {
        prevHighestBidderIdRef.current = data.recentBids[0].bidderId
      }

      // Default bid input to minimum required bid (current + 5)
      const nextMin = (data.currentPrice || data.startingPrice) + 5
      setBidAmount(nextMin.toFixed(2))
    } catch (err) {
      console.error('Failed to load auction details:', err)
      toast.error('Failed to load auction', 'Unable to reach backend. Ensure Docker or API is running.')
    } finally {
      setIsLoading(false)
    }
  }, [auctionId, toast])

  useEffect(() => {
    loadAuctionData()
  }, [loadAuctionData])

  // 2. Real-Time SignalR Live Broadcast Handler
  const handleLiveBidsReceived = useCallback((liveBids) => {
    if (!liveBids || liveBids.length === 0) return

    const newHighest = liveBids[0]
    const previousHighestId = prevHighestBidderIdRef.current

    // Alert if user was previously leading and was just outbid!
    if (user && previousHighestId === user.id && newHighest.bidderId !== user.id) {
      toast.warning(
        "You've been outbid!",
        `Another bidder placed a higher bid of $${newHighest.bidAmount.toFixed(2)}.`
      )
    }

    prevHighestBidderIdRef.current = newHighest.bidderId
    setRecentBids(liveBids)

    setAuction((prev) => {
      if (!prev) return prev
      const newPrice = newHighest.bidAmount
      return {
        ...prev,
        currentPrice: newPrice,
        bidCount: liveBids.length,
      }
    })

    // Suggest new minimum bid
    const nextSuggested = newHighest.bidAmount + 10
    setBidAmount(nextSuggested.toFixed(2))

    // Trigger visual pulse flash on the price card
    setPriceFlash(true)
    setTimeout(() => setPriceFlash(false), 1800)
  }, [user, toast])

  // Hook up SignalR
  const { connectionStatus } = useBiddingHub(auctionId, handleLiveBidsReceived)

  // Derived state: Is the current user the highest bidder?
  const highestBid = recentBids.length > 0 ? recentBids[0] : null
  const isHighestBidMine = Boolean(
    user && highestBid && (
      (highestBid.bidderId && highestBid.bidderId === user.id) ||
      (highestBid.bidderName && user.firstName && highestBid.bidderName.startsWith(user.firstName[0]))
    )
  )

  // Has current user placed any bid in this auction?
  const hasUserPlacedBid = Boolean(
    user && recentBids.some(b => b.bidderId === user.id || (user.firstName && b.bidderName?.startsWith(user.firstName[0])))
  )

  // 3. Bid Form Submission
  const handleBidSubmit = (e) => {
    e.preventDefault()
    if (!isAuthenticated) {
      handleNavigateLogin()
      return
    }

    const amount = Number(bidAmount)
    const currentPrice = auction.currentPrice || auction.startingPrice

    if (isNaN(amount) || amount <= currentPrice) {
      toast.error('Invalid Bid Amount', `Bid must be greater than current bid ($${currentPrice.toFixed(2)})`)
      return
    }

    // Step: "Are you sure?" confirmation if highest bid is already mine!
    if (isHighestBidMine) {
      setShowConfirmModal(true)
      return
    }

    executeBidSubmission(amount)
  }

  const executeBidSubmission = async (amountToBid) => {
    setShowConfirmModal(false)
    setIsPlacingBid(true)

    const amount = amountToBid || Number(bidAmount)

    try {
      await auctionApi.placeBid(auctionId, amount)
      toast.success(
        'Bid Placed Successfully!',
        `Your bid of $${amount.toFixed(2)} is now the winning bid.`
      )
    } catch (err) {
      const msg = err.response?.data?.message || err.response?.data?.detail || 'Failed to place bid. The price may have updated.'
      toast.error('Bid Failed', msg)
    } finally {
      setIsPlacingBid(false)
    }
  }

  // Quick bid increment helper
  const handleQuickIncrement = (increment) => {
    const base = auction.currentPrice || auction.startingPrice
    const calculated = (base + increment).toFixed(2)
    setBidAmount(calculated)
  }

  // 4. Buy It Now Actions
  const handleBuyNowClick = () => {
    if (!isAuthenticated) {
      handleNavigateLogin()
      return
    }
    setShowBuyNowModal(true)
  }

  const executeBuyNow = async () => {
    setIsBuyingNow(true)

    try {
      await auctionApi.buyItNow(auctionId)
      toast.success('Purchase Successful!', `You purchased "${auction.title}" via Buy-It-Now.`)
      setShowBuyNowModal(false)
      await loadAuctionData()
    } catch (err) {
      const msg = err.response?.data?.message || err.response?.data?.detail || 'Buy It Now transaction failed.'
      toast.error('Purchase Failed', msg)
    } finally {
      setIsBuyingNow(false)
    }
  }

  if (isLoading) {
    return (
      <div className="py-12 space-y-6">
        <Button variant="ghost" size="sm" onClick={handleBack} className="gap-1.5 text-xs">
          <ArrowLeft className="w-3.5 h-3.5" /> Back to Marketplace
        </Button>
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 animate-pulse">
          <div className="aspect-[4/3] bg-slate-200 rounded-2xl" />
          <div className="space-y-4">
            <div className="h-8 bg-slate-200 rounded w-3/4" />
            <div className="h-4 bg-slate-200 rounded w-1/2" />
            <div className="h-40 bg-slate-100 rounded-xl mt-6" />
          </div>
        </div>
      </div>
    )
  }

  if (!auction) {
    return (
      <div className="py-16 text-center space-y-4">
        <AlertCircle className="w-12 h-12 text-slate-400 mx-auto" />
        <h2 className="text-xl font-bold text-slate-800">Auction Not Found</h2>
        <p className="text-xs text-slate-500">The auction you are looking for does not exist or has been removed.</p>
        <Button onClick={handleBack} size="sm" className="gap-1.5 text-xs">
          <ArrowLeft className="w-3.5 h-3.5" /> Return to Marketplace
        </Button>
      </div>
    )
  }

  const isCompleted = auction.status === 2 || new Date(auction.endTime) <= new Date()
  const isSeller = user && (user.id === auction.sellerId || user.email === auction.sellerName)
  const currentPrice = auction.currentPrice || auction.startingPrice
  const fallbackImage = 'https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&auto=format&fit=crop&q=80'

  return (
    <div className="space-y-6 pb-12">
      
      {/* Top Breadcrumb & Live Connection Status Pill */}
      <div className="flex flex-wrap items-center justify-between gap-3 pt-2">
        <Button variant="ghost" size="sm" onClick={handleBack} className="gap-1.5 text-xs text-slate-600 hover:text-slate-900">
          <ArrowLeft className="w-3.5 h-3.5" /> Back to Marketplace
        </Button>

        {/* Top Actions: Edit Lot & Real-Time SignalR Connection Pill */}
        <div className="flex items-center gap-2.5">
          {(isSeller || isAdmin) && !isCompleted && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => setShowEditModal(true)}
              className="gap-1.5 text-xs font-semibold text-blue-700 bg-blue-50/80 hover:bg-blue-100 border-blue-200 shadow-sm transition-all"
            >
              <Edit3 className="w-3.5 h-3.5" /> Edit Auction
            </Button>
          )}

          {connectionStatus === 'connected' && (
            <div className="flex items-center gap-1.5 px-3 py-1 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded-full text-xs font-semibold">
              <span className="relative flex h-2 w-2">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
                <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
              </span>
              Live Room Connected
            </div>
          )}

          {connectionStatus === 'connecting' && (
            <div className="flex items-center gap-1.5 px-3 py-1 bg-blue-50 text-blue-700 border border-blue-200 rounded-full text-xs font-semibold">
              <RefreshCw className="w-3 h-3 animate-spin text-blue-500" />
              Connecting to Live Room...
            </div>
          )}

          {connectionStatus === 'reconnecting' && (
            <div className="flex items-center gap-1.5 px-3 py-1 bg-amber-50 text-amber-700 border border-amber-200 rounded-full text-xs font-semibold">
              <Radio className="w-3 h-3 text-amber-500 animate-pulse" />
              Reconnecting Live Feed...
            </div>
          )}

          {connectionStatus === 'disconnected' && (
            <div className="flex items-center gap-1.5 px-3 py-1 bg-slate-100 text-slate-600 border border-slate-200 rounded-full text-xs font-semibold">
              <span className="h-2 w-2 rounded-full bg-slate-400" />
              Offline
            </div>
          )}
        </div>
      </div>

      {/* Main 2-Column Product Layout */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
        
        {/* Left Column: Image, Details, Specs (7 Cols) */}
        <div className="lg:col-span-7 space-y-6">
          <div className="relative aspect-[4/3] w-full rounded-2xl overflow-hidden border border-slate-200 bg-slate-100 shadow-sm">
            <img
              src={auction.imageUrl || fallbackImage}
              alt={auction.title}
              className="w-full h-full object-cover"
              onError={(e) => { e.target.src = fallbackImage }}
            />
            <div className="absolute top-4 left-4 flex flex-col gap-1.5">
              {auction.categoryName && (
                <Badge variant="secondary" className="backdrop-blur-md bg-white/95 text-slate-800 font-bold text-xs shadow-sm">
                  {auction.categoryName}
                </Badge>
              )}
              {isCompleted ? (
                <Badge variant="outline" className="bg-slate-900/90 text-white border-0 font-bold text-xs">
                  Auction Closed
                </Badge>
              ) : (
                <Badge variant="success" className="text-xs font-bold flex items-center gap-1 shadow-sm">
                  <span className="relative flex h-1.5 w-1.5">
                    <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-200 opacity-75"></span>
                    <span className="relative inline-flex rounded-full h-1.5 w-1.5 bg-white"></span>
                  </span>
                  Live Auction Active
                </Badge>
              )}
            </div>
          </div>

          {/* Description & Specifications Card */}
          <Card className="border-slate-200 shadow-sm">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg font-bold text-slate-900">About this Lot</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4 text-sm text-slate-600 leading-relaxed">
              <p className="whitespace-pre-line">{auction.description}</p>

              <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 pt-4 border-t border-slate-100 text-xs">
                <div className="bg-slate-50 p-2.5 rounded-lg border border-slate-100">
                  <span className="text-slate-400 block font-medium">Seller</span>
                  <span className="font-semibold text-slate-800 flex items-center gap-1 mt-0.5">
                    <UserIcon className="w-3.5 h-3.5 text-slate-500" />
                    {auction.sellerName || 'Verified Seller'}
                  </span>
                </div>

                <div className="bg-slate-50 p-2.5 rounded-lg border border-slate-100">
                  <span className="text-slate-400 block font-medium">Anti-Sniping</span>
                  <span className="font-semibold text-slate-800 flex items-center gap-1 mt-0.5">
                    <Shield className="w-3.5 h-3.5 text-blue-500" />
                    +{auction.antiSnipingMinutes || 5} min extension
                  </span>
                </div>

                <div className="bg-slate-50 p-2.5 rounded-lg border border-slate-100 col-span-2 sm:col-span-1">
                  <span className="text-slate-400 block font-medium">Starting Bid</span>
                  <span className="font-semibold text-slate-800 mt-0.5 block">
                    ${auction.startingPrice?.toFixed(2)}
                  </span>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Right Column: Live Bidding & Action Control Center (5 Cols) */}
        <div className="lg:col-span-5 space-y-5">
          
          {/* Header Title & Timer Bar */}
          <div className="space-y-2">
            <div className="flex items-start justify-between gap-3">
              <h1 className="text-2xl sm:text-3xl font-extrabold text-slate-900 tracking-tight">
                {auction.title}
              </h1>
              {(isSeller || isAdmin) && !isCompleted && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setShowEditModal(true)}
                  className="gap-1 text-xs font-semibold text-blue-700 bg-blue-50/70 hover:bg-blue-100 border-blue-200 shrink-0"
                  title="Edit Auction Details"
                >
                  <Edit3 className="w-3.5 h-3.5" /> Edit
                </Button>
              )}
            </div>
            <div className="flex items-center justify-between text-xs text-slate-500 pt-1">
              <span>Lot #{auction.id}</span>
              <div className="flex items-center gap-1.5 font-medium">
                <Clock className="w-3.5 h-3.5 text-slate-400" />
                <span>Ends:</span>
                <CountdownTimer endTime={auction.endTime} />
              </div>
            </div>
          </div>

          {/* Pricing & Real-Time Bid Card */}
          <Card className={`border-2 transition-all duration-500 ${
            isHighestBidMine
              ? 'border-emerald-500 bg-emerald-50/20 shadow-md'
              : priceFlash 
                ? 'border-blue-500 bg-blue-50/30 shadow-md scale-[1.01]' 
                : 'border-slate-200 bg-white shadow-sm'
          }`}>
            <CardContent className="p-6 space-y-5">
              
              {/* Status Header: You are Winning / Outbid */}
              {isHighestBidMine && (
                <div className="bg-emerald-50 border-2 border-emerald-400 p-3.5 rounded-xl flex items-center justify-between text-xs text-emerald-950 shadow-sm animate-in fade-in">
                  <div className="flex items-center gap-2.5">
                    <div className="p-1.5 bg-emerald-100 rounded-lg text-emerald-700">
                      <Crown className="w-4 h-4 fill-emerald-600 text-emerald-600" />
                    </div>
                    <div>
                      <span className="font-extrabold block text-emerald-900">You are the highest bidder!</span>
                      <span className="text-[11px] text-emerald-700">Your bid is currently winning this lot</span>
                    </div>
                  </div>
                  <Badge variant="success" className="text-[10px] font-bold uppercase tracking-wider">
                    Winning
                  </Badge>
                </div>
              )}

              {hasUserPlacedBid && !isHighestBidMine && !isCompleted && (
                <div className="bg-amber-50 border border-amber-200 p-3 rounded-xl flex items-center gap-2 text-xs text-amber-900 animate-in fade-in">
                  <AlertTriangle className="w-4 h-4 text-amber-600 shrink-0" />
                  <span>You have been outbid on this item. Place a new bid to regain the lead!</span>
                </div>
              )}

              <div className="flex items-baseline justify-between border-b border-slate-100 pb-4">
                <div>
                  <span className="text-[11px] uppercase tracking-wider font-bold text-slate-400 block">
                    {isCompleted ? 'Final Sale Price' : 'Current Highest Bid'}
                  </span>
                  <div className="flex items-center gap-2 mt-1">
                    <span className="text-3xl sm:text-4xl font-extrabold text-slate-900 tracking-tight">
                      ${currentPrice?.toFixed(2)}
                    </span>
                    {priceFlash && (
                      <Badge variant="success" className="animate-bounce text-[10px] gap-0.5">
                        <Sparkles className="w-2.5 h-2.5" /> Updated Live
                      </Badge>
                    )}
                  </div>
                </div>

                <div className="text-right">
                  <span className="text-[11px] uppercase tracking-wider font-bold text-slate-400 block">
                    Total Bids
                  </span>
                  <span className="text-lg font-bold text-slate-700 flex items-center justify-end gap-1 mt-1">
                    <TrendingUp className="w-4 h-4 text-blue-500" />
                    {auction.bidCount || recentBids.length || 0}
                  </span>
                </div>
              </div>

              {/* Bidding Interaction Section */}
              {!isCompleted ? (
                isSeller ? (
                  <div className="p-4 bg-slate-50 border border-slate-200 rounded-xl text-center space-y-1">
                    <Shield className="w-5 h-5 text-slate-400 mx-auto" />
                    <p className="text-xs font-semibold text-slate-700">You are the seller of this auction</p>
                    <p className="text-[11px] text-slate-500">Sellers cannot place bids on their own lots.</p>
                  </div>
                ) : (
                  <form onSubmit={handleBidSubmit} className="space-y-4">
                    <div className="space-y-1.5">
                      <div className="flex items-center justify-between">
                        <label className="text-xs font-bold text-slate-700">Your Bid Amount ($)</label>
                        <span className="text-[11px] text-slate-400">
                          Min next bid: ${(currentPrice + 1).toFixed(2)}
                        </span>
                      </div>
                      <div className="relative">
                        <span className="absolute left-3 top-2.5 text-sm font-semibold text-slate-400">$</span>
                        <Input
                          type="number"
                          step="0.01"
                          min={(currentPrice + 0.01).toFixed(2)}
                          value={bidAmount}
                          onChange={(e) => setBidAmount(e.target.value)}
                          placeholder={(currentPrice + 10).toFixed(2)}
                          className="pl-7 font-bold text-base h-11 text-slate-900"
                          disabled={isPlacingBid}
                          required
                        />
                      </div>
                    </div>

                    {/* Quick increment buttons */}
                    <div className="flex items-center gap-2">
                      <span className="text-[10px] uppercase font-bold text-slate-400 tracking-wider">
                        Quick Add:
                      </span>
                      {[10, 25, 50, 100].map((inc) => (
                        <button
                          key={inc}
                          type="button"
                          onClick={() => handleQuickIncrement(inc)}
                          className="text-xs font-semibold px-2.5 py-1 bg-slate-100 hover:bg-blue-50 hover:text-blue-600 rounded-md transition-colors"
                        >
                          +${inc}
                        </button>
                      ))}
                    </div>

                    <Button
                      type="submit"
                      disabled={isPlacingBid}
                      className={`w-full h-11 text-sm font-bold text-white gap-2 shadow-sm transition-colors ${
                        isHighestBidMine 
                          ? 'bg-amber-600 hover:bg-amber-700' 
                          : 'bg-blue-600 hover:bg-blue-700'
                      }`}
                    >
                      <Gavel className="w-4 h-4" />
                      {isPlacingBid
                        ? 'Submitting Bid...'
                        : !isAuthenticated
                          ? 'Sign In to Bid'
                          : isHighestBidMine
                            ? 'Outbid Myself (Raise Winning Bid)'
                            : 'Place Live Bid'}
                    </Button>
                  </form>
                )
              ) : (
                <div className="p-4 bg-slate-100 rounded-xl text-center space-y-1">
                  <p className="text-xs font-bold text-slate-700">This auction has ended</p>
                  <p className="text-[11px] text-slate-500">No further bids can be accepted.</p>
                </div>
              )}

              {/* Buy It Now Option (If Available) */}
              {auction.buyItNowPrice && !isCompleted && !isSeller && (
                <div className="pt-4 border-t border-slate-100">
                  <div className="bg-gradient-to-r from-emerald-500/10 via-emerald-500/5 to-transparent p-4 rounded-xl border border-emerald-200/80 flex items-center justify-between gap-3">
                    <div>
                      <div className="flex items-center gap-1 text-emerald-800 text-xs font-bold">
                        <Zap className="w-3.5 h-3.5 fill-current" /> Buy It Now
                      </div>
                      <div className="text-xl font-extrabold text-emerald-700 mt-0.5">
                        ${auction.buyItNowPrice.toFixed(2)}
                      </div>
                      <span className="text-[10px] text-emerald-600">Skip bidding & purchase immediately</span>
                    </div>

                    <Button
                      type="button"
                      variant="success"
                      size="sm"
                      onClick={handleBuyNowClick}
                      disabled={isBuyingNow}
                      className="font-bold text-xs gap-1.5 shadow-sm"
                    >
                      <Zap className="w-3.5 h-3.5 fill-current" />
                      {isBuyingNow ? 'Processing...' : 'Buy Now'}
                    </Button>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Real-Time Live Bids Table Section */}
      <Card className="border-slate-200 shadow-sm mt-8">
        <CardHeader className="pb-3 border-b border-slate-100 flex flex-row items-center justify-between">
          <div>
            <CardTitle className="text-base font-bold text-slate-900 flex items-center gap-2">
              <TrendingUp className="w-4 h-4 text-blue-600" />
              Live Bid History
            </CardTitle>
            <CardDescription className="text-xs">
              Live updates broadcast via SignalR WebSocket room
            </CardDescription>
          </div>
          <Badge variant="outline" className="text-xs font-medium">
            {recentBids.length} {recentBids.length === 1 ? 'Bid' : 'Bids'} recorded
          </Badge>
        </CardHeader>

        <CardContent className="p-0">
          {recentBids.length === 0 ? (
            <div className="py-12 text-center text-slate-400 space-y-2">
              <Gavel className="w-8 h-8 mx-auto stroke-1" />
              <p className="text-xs font-medium">No bids have been placed on this item yet.</p>
              <p className="text-[11px] text-slate-400">Be the first to set the benchmark!</p>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-16">Rank</TableHead>
                  <TableHead>Bidder</TableHead>
                  <TableHead>Bid Amount</TableHead>
                  <TableHead>Time</TableHead>
                  <TableHead className="text-right">Status</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {recentBids.map((bid, index) => {
                  const isHighest = index === 0
                  const isMyBid = user && (
                    (bid.bidderId && bid.bidderId === user.id) ||
                    (user.firstName && bid.bidderName?.startsWith(user.firstName[0]))
                  )

                  return (
                    <TableRow 
                      key={bid.id || index}
                      className={
                        isHighest && isMyBid 
                          ? 'bg-emerald-100/40 border-l-4 border-l-emerald-500 hover:bg-emerald-100/60' 
                          : isHighest 
                            ? 'bg-emerald-50/30 hover:bg-emerald-50/50' 
                            : isMyBid 
                              ? 'bg-blue-50/40 hover:bg-blue-50/70' 
                              : ''
                      }
                    >
                      <TableCell className="font-mono text-xs font-bold text-slate-400">
                        #{index + 1}
                      </TableCell>
                      <TableCell className="font-semibold text-slate-800 text-xs">
                        <div className="flex items-center gap-1.5">
                          <span>{bid.bidderName || 'Anonymous'}</span>
                          {isMyBid && (
                            <Badge variant="secondary" className="text-[9px] px-1.5 py-0 bg-blue-100 text-blue-700 font-bold border-blue-200">
                              You
                            </Badge>
                          )}
                        </div>
                      </TableCell>
                      <TableCell className="text-sm font-extrabold text-slate-900">
                        ${bid.bidAmount?.toFixed(2)}
                      </TableCell>
                      <TableCell className="text-xs text-slate-500">
                        {new Date(bid.bidTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' })}
                      </TableCell>
                      <TableCell className="text-right">
                        {isHighest ? (
                          <Badge variant="success" className="text-[10px] font-bold">
                            {isMyBid ? 'Your Winning Bid' : 'Highest Bid'}
                          </Badge>
                        ) : isMyBid ? (
                          <Badge variant="outline" className="text-[10px] text-amber-700 border-amber-300 bg-amber-50 font-semibold">
                            Outbid
                          </Badge>
                        ) : (
                          <span className="text-[11px] text-slate-400 font-medium">Outbid</span>
                        )}
                      </TableCell>
                    </TableRow>
                  )
                })}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Confirmation Modal when outbidding yourself */}
      <ConfirmBidModal
        isOpen={showConfirmModal}
        currentPrice={currentPrice}
        newBidAmount={Number(bidAmount)}
        onConfirm={() => executeBidSubmission(Number(bidAmount))}
        onCancel={() => setShowConfirmModal(false)}
        isSubmitting={isPlacingBid}
      />

      {/* Buy It Now Confirmation Modal */}
      <BuyItNowModal
        isOpen={showBuyNowModal}
        auction={auction}
        onConfirm={executeBuyNow}
        onCancel={() => setShowBuyNowModal(false)}
        isSubmitting={isBuyingNow}
      />

      {/* Edit Auction Modal */}
      <EditAuctionModal
        isOpen={showEditModal}
        onClose={() => setShowEditModal(false)}
        auction={auction}
        onSuccess={(updated) => {
          setAuction(prev => ({ ...prev, ...updated }))
          loadAuctionData()
        }}
      />

    </div>
  )
}
