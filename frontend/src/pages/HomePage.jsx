import React, { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { auctionApi } from '@/api/auctionApi'
import { AuctionCard } from '@/components/AuctionCard'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Search, SlidersHorizontal, RefreshCw, AlertCircle, PlusCircle } from 'lucide-react'
import { useAuth } from '@/context/AuthContext'

export function HomePage({ onSelectAuction, onNavigateCreate }) {
  const navigate = useNavigate()
  const { isAuthenticated } = useAuth()
  const handleSelectAuction = onSelectAuction || ((id) => navigate(`/auctions/${id}`))
  const handleNavigateCreate = onNavigateCreate || (() => navigate(isAuthenticated ? '/create-auction' : '/login'))
  const [auctions, setAuctions] = useState([])
  const [categories, setCategories] = useState([])
  const [selectedCategory, setSelectedCategory] = useState(null)
  const [search, setSearch] = useState('')
  const [sortBy, setSortBy] = useState('created_desc')
  const [page, setPage] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState(null)

  const pageSize = 8

  // Load Categories on mount
  useEffect(() => {
    async function fetchCategories() {
      try {
        const data = await auctionApi.getCategories()
        setCategories(data || [])
      } catch (err) {
        console.error('Failed to load categories', err)
      }
    }
    fetchCategories()
  }, [])

  // Load Auctions when filters change
  const fetchAuctions = async () => {
    setIsLoading(true)
    setError(null)
    try {
      const data = await auctionApi.getAuctions({
        categoryId: selectedCategory,
        search: search.trim() || undefined,
        sortBy,
        pageNumber: page,
        pageSize,
      })
      setAuctions(data.items || [])
      setTotalCount(data.totalCount || 0)
    } catch (err) {
      console.error('Failed to load auctions', err)
      setError('Unable to reach the AuctionOx backend. Ensure Docker or API is running.')
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    fetchAuctions()
  }, [selectedCategory, sortBy, page])

  const handleSearchSubmit = (e) => {
    e.preventDefault()
    setPage(1)
    fetchAuctions()
  }

  const totalPages = Math.ceil(totalCount / pageSize) || 1

  return (
    <div className="space-y-6">
      
      {/* Search & Filter Header Bar */}
      <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm space-y-4">
        <div className="flex flex-col md:flex-row gap-3 justify-between items-center">
          
          {/* Search input form */}
          <form onSubmit={handleSearchSubmit} className="relative w-full md:w-96">
            <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
            <Input
              type="text"
              placeholder="Search items by title or keyword..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-9 text-xs"
            />
          </form>

          {/* Action buttons & sorting */}
          <div className="flex items-center gap-2 w-full md:w-auto justify-between md:justify-end">
            <select
              value={sortBy}
              onChange={(e) => { setSortBy(e.target.value); setPage(1); }}
              className="text-xs bg-slate-50 border border-slate-300 rounded-md px-3 py-1.5 font-medium text-slate-700 cursor-pointer focus:outline-none focus:ring-1 focus:ring-blue-600"
            >
              <option value="created_desc">Newest First</option>
              <option value="endtime">Ending Soonest</option>
              <option value="price">Price: Low to High</option>
              <option value="price_desc">Price: High to Low</option>
            </select>

            {isAuthenticated && (
              <Button 
                onClick={handleNavigateCreate} 
                size="sm" 
                className="gap-1.5 text-xs font-semibold"
              >
                <PlusCircle className="w-3.5 h-3.5" /> List Item
              </Button>
            )}
          </div>

        </div>

        {/* Category Pills */}
        <div className="flex items-center gap-2 overflow-x-auto pb-1 text-xs">
          <button
            onClick={() => { setSelectedCategory(null); setPage(1); }}
            className={`px-3 py-1 rounded-full font-medium transition-colors cursor-pointer ${
              selectedCategory === null 
                ? 'bg-blue-600 text-white shadow-sm' 
                : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
            }`}
          >
            All Categories
          </button>

          {categories.map((cat) => (
            <button
              key={cat.id}
              onClick={() => { setSelectedCategory(cat.id); setPage(1); }}
              className={`px-3 py-1 rounded-full font-medium whitespace-nowrap transition-colors cursor-pointer ${
                selectedCategory === cat.id 
                  ? 'bg-blue-600 text-white shadow-sm' 
                  : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
              }`}
            >
              {cat.name}
            </button>
          ))}
        </div>
      </div>

      {/* Error state */}
      {error && (
        <div className="p-4 bg-red-50 border border-red-200 text-red-700 rounded-xl flex items-center justify-between">
          <div className="flex items-center gap-2 text-sm">
            <AlertCircle className="w-5 h-5 shrink-0" />
            <span>{error}</span>
          </div>
          <Button variant="outline" size="sm" onClick={fetchAuctions} className="gap-1 text-xs">
            <RefreshCw className="w-3.5 h-3.5" /> Retry
          </Button>
        </div>
      )}

      {/* Loading state skeleton grid */}
      {isLoading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-5">
          {[...Array(pageSize)].map((_, i) => (
            <div key={i} className="h-80 bg-white border border-slate-200 rounded-xl animate-pulse p-4 space-y-3">
              <div className="w-full h-44 bg-slate-200 rounded-lg" />
              <div className="w-3/4 h-4 bg-slate-200 rounded" />
              <div className="w-1/2 h-3 bg-slate-200 rounded" />
            </div>
          ))}
        </div>
      ) : auctions.length === 0 ? (
        <div className="text-center py-16 bg-white rounded-xl border border-slate-200 p-8 space-y-3">
          <SlidersHorizontal className="w-10 h-10 text-slate-300 mx-auto" />
          <h3 className="text-lg font-bold text-slate-800">No auctions found</h3>
          <p className="text-xs text-slate-500 max-w-sm mx-auto">
            Try adjusting your search query, selecting another category, or list a new auction!
          </p>
        </div>
      ) : (
        /* Auction Grid */
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-5">
          {auctions.map((auction) => (
            <AuctionCard
              key={auction.id}
              auction={auction}
              onSelect={handleSelectAuction}
            />
          ))}
        </div>
      )}

      {/* Pagination Controls */}
      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-3 pt-6 border-t border-slate-200">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page === 1}
            className="text-xs"
          >
            Previous
          </Button>
          <span className="text-xs font-medium text-slate-600">
            Page {page} of {totalPages} ({totalCount} items)
          </span>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            disabled={page === totalPages}
            className="text-xs"
          >
            Next
          </Button>
        </div>
      )}

    </div>
  )
}
