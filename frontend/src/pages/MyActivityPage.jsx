import React, { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { MyBidsTab } from '@/components/my-activity/MyBidsTab'
import { MyListingsTab } from '@/components/my-activity/MyListingsTab'
import { auctionApi } from '@/api/auctionApi'
import { useAuth } from '@/context/AuthContext'
import { useToast } from '@/context/ToastContext'
import { 
  Gavel, 
  Package, 
  User as UserIcon, 
  Shield, 
  ArrowLeft, 
  AlertTriangle, 
  Zap, 
  Mail, 
  Calendar,
  CheckCircle2,
  RefreshCw
} from 'lucide-react'

export function MyActivityPage({ onSelectAuction, onNavigateCreate, onNavigateHome }) {
  const navigate = useNavigate()
  const handleSelectAuction = onSelectAuction || ((id) => navigate(`/auctions/${id}`))
  const handleNavigateCreate = onNavigateCreate || (() => navigate('/create-auction'))
  const handleNavigateHome = onNavigateHome || (() => navigate('/'))

  const { user } = useAuth()
  const toast = useToast()

  const [activeTab, setActiveTab] = useState('bids')
  
  // Bids state
  const [bids, setBids] = useState([])
  const [isLoadingBids, setIsLoadingBids] = useState(true)

  // Listings state
  const [listings, setListings] = useState([])
  const [totalListingsCount, setTotalListingsCount] = useState(0)
  const [listingsPage, setListingsPage] = useState(1)
  const [isLoadingListings, setIsLoadingListings] = useState(true)

  // Fetch Bids
  const fetchBids = useCallback(async () => {
    setIsLoadingBids(true)
    try {
      const data = await auctionApi.getMyBids()
      setBids(data || [])
    } catch (err) {
      console.error('Failed to load user bids:', err)
      const isNetworkError = !err.response || err.code === 'ERR_NETWORK'
      toast.error(
        isNetworkError ? 'Server Offline' : 'Failed to load bids',
        isNetworkError ? 'Could not reach backend API. Please start the server.' : 'Please try again later.'
      )
    } finally {
      setIsLoadingBids(false)
    }
  }, [toast])

  // Fetch Listings
  const fetchListings = useCallback(async (page = 1) => {
    setIsLoadingListings(true)
    try {
      const result = await auctionApi.getMyAuctions({ pageNumber: page, pageSize: 10 })
      setListings(result?.items || [])
      setTotalListingsCount(result?.totalCount || 0)
      setListingsPage(result?.pageNumber || page)
    } catch (err) {
      console.error('Failed to load user listings:', err)
      const isNetworkError = !err.response || err.code === 'ERR_NETWORK'
      // If it's a network error, fetchBids already informed the user
      if (!isNetworkError) {
        toast.error('Failed to load listings', 'Please try again later.')
      }
    } finally {
      setIsLoadingListings(false)
    }
  }, [toast])

  useEffect(() => {
    fetchBids()
    fetchListings(1)
  }, [fetchBids, fetchListings])

  // Handle Auction Lot Cancellation / Deletion
  const handleDeleteAuction = async (auctionId) => {
    try {
      await auctionApi.deleteAuction(auctionId)
      toast.success('Listing Cancelled', 'The auction lot has been successfully deleted.')
      await fetchListings(listingsPage)
    } catch (err) {
      const msg = err.response?.data?.message || 'Could not cancel auction lot.'
      toast.error('Cancellation Failed', msg)
    }
  }

  // Count active outbid lots to display banner if needed
  const outbidCount = bids.reduce((acc, bid) => {
    const isCompleted = bid.auctionStatus === 2 || bid.auctionStatus === 'Completed'
    const isExpired = bid.endTime ? new Date(bid.endTime) <= new Date() : false
    if (!isCompleted && !isExpired && !bid.isWinningBid) {
      return acc + 1
    }
    return acc
  }, 0)

  // Unique auction lots the user bid on
  const uniqueBidLotsCount = new Set(bids.map((b) => b.auctionItemId)).size

  return (
    <div className="space-y-6">
      {/* Back button and page header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <button
            onClick={handleNavigateHome}
            className="inline-flex items-center gap-1.5 text-xs font-semibold text-slate-500 hover:text-slate-900 transition-colors mb-2"
          >
            <ArrowLeft className="w-3.5 h-3.5" /> Back to Marketplace
          </button>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-slate-900 tracking-tight">
            My Activity Dashboard
          </h1>
          <p className="text-xs sm:text-sm text-slate-500 mt-1">
            Track your live bids, manage your listed lots, and view won auctions.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => {
              fetchBids()
              fetchListings(listingsPage)
            }}
            className="flex items-center gap-1.5 text-xs text-slate-700"
          >
            <RefreshCw className="w-3.5 h-3.5" /> Refresh Data
          </Button>
          <Button
            size="sm"
            onClick={handleNavigateCreate}
            className="text-xs font-semibold bg-blue-600 hover:bg-blue-700"
          >
            <Package className="w-3.5 h-3.5 mr-1" /> Create Listing
          </Button>
        </div>
      </div>

      {/* Outbid Alert Banner */}
      {outbidCount > 0 && (
        <div className="flex items-center justify-between p-3.5 sm:p-4 rounded-xl bg-amber-500/10 border border-amber-300 text-amber-900 shadow-sm animate-in fade-in">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-amber-500 text-white rounded-lg shadow-sm shrink-0">
              <AlertTriangle className="w-5 h-5" />
            </div>
            <div>
              <h4 className="text-xs sm:text-sm font-bold text-amber-900">
                You have been outbid on {outbidCount} {outbidCount === 1 ? 'auction' : 'auctions'}!
              </h4>
              <p className="text-[11px] sm:text-xs text-amber-700">
                Other bidders have placed higher offers. Review your bids below and reclaim your winning position.
              </p>
            </div>
          </div>
          <Button
            size="sm"
            onClick={() => setActiveTab('bids')}
            className="bg-amber-600 hover:bg-amber-700 text-white font-bold text-xs shrink-0 ml-2"
          >
            <Zap className="w-3.5 h-3.5 mr-1 fill-current" /> View Outbid Lots
          </Button>
        </div>
      )}

      {/* Main Tabs Navigation */}
      <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
        <TabsList className="grid w-full grid-cols-3 max-w-md h-11 bg-slate-100 p-1 rounded-xl">
          <TabsTrigger value="bids" className="flex items-center gap-1.5 text-xs sm:text-sm">
            <Gavel className="w-4 h-4" />
            <span>My Bids</span>
            {uniqueBidLotsCount > 0 && (
              <span className="ml-1 px-1.5 py-0.2 bg-blue-100 text-blue-700 rounded-full text-[10px] font-bold">
                {uniqueBidLotsCount}
              </span>
            )}
          </TabsTrigger>

          <TabsTrigger value="listings" className="flex items-center gap-1.5 text-xs sm:text-sm">
            <Package className="w-4 h-4" />
            <span>My Listings</span>
            {totalListingsCount > 0 && (
              <span className="ml-1 px-1.5 py-0.2 bg-emerald-100 text-emerald-700 rounded-full text-[10px] font-bold">
                {totalListingsCount}
              </span>
            )}
          </TabsTrigger>

          <TabsTrigger value="profile" className="flex items-center gap-1.5 text-xs sm:text-sm">
            <UserIcon className="w-4 h-4" />
            <span>Profile</span>
          </TabsTrigger>
        </TabsList>

        {/* Tab 1: My Bids */}
        <TabsContent value="bids" className="mt-6">
          <MyBidsTab
            bids={bids}
            isLoading={isLoadingBids}
            onRefresh={fetchBids}
            onSelectAuction={handleSelectAuction}
            onNavigateHome={handleNavigateHome}
          />
        </TabsContent>

        {/* Tab 2: My Listings */}
        <TabsContent value="listings" className="mt-6">
          <MyListingsTab
            listings={listings}
            totalCount={totalListingsCount}
            pageNumber={listingsPage}
            pageSize={10}
            onPageChange={(page) => fetchListings(page)}
            isLoading={isLoadingListings}
            onRefresh={() => fetchListings(listingsPage)}
            onSelectAuction={handleSelectAuction}
            onNavigateCreate={handleNavigateCreate}
            onDeleteAuction={handleDeleteAuction}
          />
        </TabsContent>

        {/* Tab 3: Account Profile */}
        <TabsContent value="profile" className="mt-6">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <Card className="md:col-span-2 shadow-sm bg-white">
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <div className="w-12 h-12 rounded-full bg-blue-600 text-white flex items-center justify-center font-bold text-lg shadow-sm">
                      {user?.firstName?.[0]?.toUpperCase() || user?.email?.[0]?.toUpperCase() || 'U'}
                    </div>
                    <div>
                      <CardTitle className="text-lg">
                        {user?.firstName ? `${user.firstName} ${user.lastName || ''}`.trim() : 'Account User'}
                      </CardTitle>
                      <CardDescription className="text-xs">
                        Member of the AuctionOx real-time bidding network
                      </CardDescription>
                    </div>
                  </div>
                  {user?.isAdmin && (
                    <Badge className="bg-purple-600 text-white flex items-center gap-1 text-xs">
                      <Shield className="w-3 h-3" /> Administrator
                    </Badge>
                  )}
                </div>
              </CardHeader>

              <CardContent className="space-y-4 pt-2">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div className="p-3 bg-slate-50 rounded-lg border border-slate-100">
                    <div className="flex items-center gap-2 text-xs text-slate-500 font-medium">
                      <Mail className="w-3.5 h-3.5 text-slate-400" /> Email Address
                    </div>
                    <p className="font-semibold text-slate-800 text-sm mt-1">{user?.email || 'N/A'}</p>
                  </div>

                  <div className="p-3 bg-slate-50 rounded-lg border border-slate-100">
                    <div className="flex items-center gap-2 text-xs text-slate-500 font-medium">
                      <Shield className="w-3.5 h-3.5 text-slate-400" /> Account Roles
                    </div>
                    <p className="font-semibold text-slate-800 text-sm mt-1">
                      {user?.roles?.join(', ') || 'User'}
                    </p>
                  </div>

                  <div className="p-3 bg-slate-50 rounded-lg border border-slate-100">
                    <div className="flex items-center gap-2 text-xs text-slate-500 font-medium">
                      <Gavel className="w-3.5 h-3.5 text-slate-400" /> Bidding Status
                    </div>
                    <div className="flex items-center gap-1.5 mt-1">
                      <CheckCircle2 className="w-4 h-4 text-emerald-600" />
                      <span className="font-semibold text-emerald-700 text-sm">Verified Bidder</span>
                    </div>
                  </div>

                  <div className="p-3 bg-slate-50 rounded-lg border border-slate-100">
                    <div className="flex items-center gap-2 text-xs text-slate-500 font-medium">
                      <Package className="w-3.5 h-3.5 text-slate-400" /> Seller Capability
                    </div>
                    <div className="flex items-center gap-1.5 mt-1">
                      <CheckCircle2 className="w-4 h-4 text-blue-600" />
                      <span className="font-semibold text-blue-700 text-sm">Authorized Seller</span>
                    </div>
                  </div>
                </div>

                <div className="border-t pt-4">
                  <h4 className="text-xs font-semibold text-slate-700 uppercase tracking-wider mb-2">
                    Security & Protection Rules
                  </h4>
                  <ul className="text-xs text-slate-600 space-y-1.5 list-disc list-inside">
                    <li>Dynamic Anti-Sniping is automatically activated on all active lots.</li>
                    <li>Sellers cannot bid on their own listings to protect auction fairness.</li>
                    <li>Lots with active bids are locked against premature cancellation.</li>
                  </ul>
                </div>
              </CardContent>
            </Card>

            {/* Quick summary stats card */}
            <Card className="shadow-sm bg-slate-900 text-white border-0">
              <CardHeader>
                <CardTitle className="text-base text-white">Your Lifetime Stats</CardTitle>
                <CardDescription className="text-slate-400 text-xs">
                  Summary of your engagement
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center justify-between pb-3 border-b border-slate-800">
                  <span className="text-xs text-slate-400">Total Bids Placed:</span>
                  <span className="font-mono font-bold text-white text-sm">{bids.length}</span>
                </div>
                <div className="flex items-center justify-between pb-3 border-b border-slate-800">
                  <span className="text-xs text-slate-400">Auction Lots Created:</span>
                  <span className="font-mono font-bold text-white text-sm">{totalListingsCount}</span>
                </div>
                <div className="flex items-center justify-between pb-3 border-b border-slate-800">
                  <span className="text-xs text-slate-400">Unique Lots Bid On:</span>
                  <span className="font-mono font-bold text-white text-sm">{uniqueBidLotsCount}</span>
                </div>
                <div className="pt-2">
                  <Button
                    onClick={handleNavigateHome}
                    className="w-full bg-blue-600 hover:bg-blue-700 text-white text-xs font-semibold"
                  >
                    Browse More Auctions
                  </Button>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>
      </Tabs>
    </div>
  )
}
