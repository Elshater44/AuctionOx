import React, { useState, useEffect } from 'react'
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { auctionApi } from '@/api/auctionApi'
import { useToast } from '@/context/ToastContext'
import {
  Edit3,
  X,
  AlertCircle,
  Image as ImageIcon,
  CheckCircle2,
  AlertTriangle,
  Lock,
  Layers
} from 'lucide-react'

export function EditAuctionModal({ isOpen, onClose, auction, onSuccess }) {
  const toast = useToast()
  const [categories, setCategories] = useState([])
  
  // Form fields
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [imageUrl, setImageUrl] = useState('')
  const [buyItNowPrice, setBuyItNowPrice] = useState('')
  
  // Image preview state
  const [imageStatus, setImageStatus] = useState('idle') // 'idle' | 'loading' | 'loaded' | 'error'
  
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState(null)

  // Determine if bids exist
  const hasBids = (auction?.bidCount > 0) || (auction?.recentBids?.length > 0)

  // Load categories
  useEffect(() => {
    async function fetchCategories() {
      try {
        const data = await auctionApi.getCategories()
        setCategories(data || [])
      } catch (err) {
        console.error('Failed to load categories', err)
      }
    }
    if (isOpen) {
      fetchCategories()
    }
  }, [isOpen])

  // Prefill when auction prop changes
  useEffect(() => {
    if (auction && isOpen) {
      setTitle(auction.title || '')
      setDescription(auction.description || '')
      setCategoryId(auction.categoryId ? String(auction.categoryId) : '')
      setImageUrl(auction.imageUrl || '')
      setBuyItNowPrice(auction.buyItNowPrice ? String(auction.buyItNowPrice) : '')
      setError(null)
      setImageStatus(auction.imageUrl ? 'loading' : 'idle')
    }
  }, [auction, isOpen])

  // Track image url changes for preview
  useEffect(() => {
    if (!imageUrl || !imageUrl.trim()) {
      setImageStatus('idle')
    } else {
      setImageStatus('loading')
    }
  }, [imageUrl])

  if (!isOpen || !auction) return null

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      const payload = {
        title: title.trim(),
        description: description.trim(),
        categoryId: categoryId ? Number(categoryId) : auction.categoryId,
        imageUrl: imageUrl.trim() || null,
        buyItNowPrice: buyItNowPrice ? Number(buyItNowPrice) : null,
        startTime: auction.startTime,
        endTime: auction.endTime,
      }

      const updated = await auctionApi.updateAuction(auction.id, payload)
      toast.success('Auction Updated', `"${title}" has been updated successfully.`)
      if (onSuccess) onSuccess(updated)
      onClose()
    } catch (err) {
      const msg =
        err.response?.data?.message ||
        (err.response?.data?.errors
          ? Object.values(err.response?.data?.errors).flat().join(', ')
          : 'Failed to update auction. Please check your inputs.')
      setError(msg)
      toast.error('Update Failed', msg)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/60 backdrop-blur-sm animate-in fade-in duration-200">
      <Card className="max-w-2xl w-full max-h-[90vh] flex flex-col shadow-2xl border-slate-200 bg-white rounded-2xl overflow-hidden animate-in zoom-in-95 duration-200">
        
        {/* Header */}
        <CardHeader className="bg-slate-50/80 border-b border-slate-100 pb-4 shrink-0">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2.5">
              <div className="p-2 rounded-lg bg-blue-100 text-blue-600">
                <Edit3 className="w-5 h-5" />
              </div>
              <div>
                <CardTitle className="text-base font-bold text-slate-900">
                  Edit Auction #{auction.id}
                </CardTitle>
                <CardDescription className="text-xs text-slate-500">
                  Update auction details and preview listing media
                </CardDescription>
              </div>
            </div>

            <button
              onClick={onClose}
              disabled={isSubmitting}
              className="text-slate-400 hover:text-slate-600 p-1.5 rounded-md transition-colors"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </CardHeader>

        {/* Scrollable Form Body */}
        <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-6 space-y-4">
          {error && (
            <div className="flex items-center gap-2 p-3 text-xs text-red-700 bg-red-50 border border-red-200 rounded-lg">
              <AlertCircle className="w-4 h-4 shrink-0" />
              <span>{error}</span>
            </div>
          )}

          {hasBids && (
            <div className="flex items-start gap-2.5 p-3 text-xs text-amber-800 bg-amber-50 border border-amber-200 rounded-lg">
              <Lock className="w-4 h-4 shrink-0 text-amber-600 mt-0.5" />
              <div>
                <span className="font-bold">Bids already placed: </span>
                To protect active bidders, starting price and category terms are locked. You can still update the description and item photo.
              </div>
            </div>
          )}

          {/* Item Title */}
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-slate-700 flex items-center justify-between">
              <span>Item Title</span>
              {hasBids && <span className="text-[10px] text-amber-600 font-normal">Locked</span>}
            </label>
            <Input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              disabled={hasBids}
              className={hasBids ? 'bg-slate-50 text-slate-500 cursor-not-allowed' : ''}
              required
            />
          </div>

          {/* Category & Buy It Now */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700 flex items-center justify-between">
                <span>Category</span>
                {hasBids && <span className="text-[10px] text-amber-600 font-normal">Locked</span>}
              </label>
              <select
                value={categoryId}
                onChange={(e) => setCategoryId(e.target.value)}
                disabled={hasBids}
                className={`w-full h-9 text-xs bg-white border border-slate-300 rounded-md px-3 py-1 font-medium text-slate-700 focus:outline-none focus:ring-1 focus:ring-blue-600 ${
                  hasBids ? 'bg-slate-50 text-slate-400 cursor-not-allowed' : ''
                }`}
                required
              >
                <option value="">Select category...</option>
                {categories.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700 flex items-center justify-between">
                <span>Buy It Now Price ($)</span>
                {hasBids && <span className="text-[10px] text-amber-600 font-normal">Locked</span>}
              </label>
              <Input
                type="number"
                min={Number(auction.startingPrice || 0) + 1}
                step="0.01"
                placeholder="Optional instant buy price"
                value={buyItNowPrice}
                onChange={(e) => setBuyItNowPrice(e.target.value)}
                disabled={hasBids}
                className={hasBids ? 'bg-slate-50 text-slate-500 cursor-not-allowed' : ''}
              />
            </div>
          </div>

          {/* Description */}
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-slate-700">Description</label>
            <textarea
              rows={3}
              className="flex w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-xs shadow-sm placeholder:text-slate-400 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-blue-600"
              placeholder="Item details, specs, package contents..."
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              required
            />
          </div>

          {/* Image URL & Live Preview Section */}
          <div className="space-y-2 pt-2 border-t border-slate-100">
            <div className="flex items-center justify-between">
              <label className="text-xs font-semibold text-slate-700 flex items-center gap-1.5">
                <ImageIcon className="w-3.5 h-3.5 text-blue-600" />
                <span>Image Link (URL)</span>
              </label>

              {imageStatus === 'loaded' && (
                <span className="text-[11px] font-medium text-emerald-600 flex items-center gap-1">
                  <CheckCircle2 className="w-3.5 h-3.5" /> Image verified
                </span>
              )}
              {imageStatus === 'error' && (
                <span className="text-[11px] font-medium text-red-600 flex items-center gap-1">
                  <AlertTriangle className="w-3.5 h-3.5" /> Unable to load
                </span>
              )}
            </div>

            <Input
              type="url"
              placeholder="https://images.unsplash.com/photo-..."
              value={imageUrl}
              onChange={(e) => setImageUrl(e.target.value)}
              className="text-xs"
            />

            {/* LIVE IMAGE PREVIEW BOX */}
            <div className="mt-2 rounded-xl border border-slate-200 bg-slate-50/60 p-3 overflow-hidden">
              <div className="flex items-center justify-between mb-2">
                <span className="text-[11px] font-bold uppercase tracking-wider text-slate-500 flex items-center gap-1">
                  <Layers className="w-3 h-3" /> Live Image Preview
                </span>
                {imageUrl && (
                  <button
                    type="button"
                    onClick={() => setImageUrl('')}
                    className="text-[11px] text-slate-400 hover:text-red-500 transition-colors"
                  >
                    Clear URL
                  </button>
                )}
              </div>

              {imageUrl && imageUrl.trim() ? (
                <div className="relative rounded-lg bg-white border border-slate-200 overflow-hidden flex items-center justify-center min-h-[160px] max-h-[220px]">
                  <img
                    src={imageUrl.trim()}
                    alt="Preview"
                    onLoad={() => setImageStatus('loaded')}
                    onError={() => setImageStatus('error')}
                    className={`max-h-[210px] w-auto max-w-full object-contain p-1 transition-opacity duration-200 ${
                      imageStatus === 'loaded' ? 'opacity-100' : 'opacity-30'
                    }`}
                  />

                  {imageStatus === 'loading' && (
                    <div className="absolute inset-0 flex items-center justify-center bg-white/60 text-xs text-slate-500">
                      Loading preview...
                    </div>
                  )}

                  {imageStatus === 'error' && (
                    <div className="absolute inset-0 flex flex-col items-center justify-center p-4 text-center bg-red-50/90 text-red-700">
                      <AlertTriangle className="w-6 h-6 mb-1 text-red-500" />
                      <p className="font-bold text-xs">Failed to load preview</p>
                      <p className="text-[11px] text-red-600 max-w-xs mt-0.5">
                        Please check that the link points directly to a valid image file (JPEG, PNG, WebP).
                      </p>
                    </div>
                  )}
                </div>
              ) : (
                <div className="rounded-lg border-2 border-dashed border-slate-200 p-6 text-center text-slate-400">
                  <ImageIcon className="w-8 h-8 mx-auto text-slate-300 mb-1" />
                  <p className="text-xs font-medium text-slate-600">No image link provided</p>
                  <p className="text-[11px] text-slate-400">Paste an image URL above to see an instant preview.</p>
                </div>
              )}
            </div>
          </div>

          {/* Footer inside form to trigger on Enter */}
          <div className="pt-4 border-t border-slate-100 flex items-center justify-end gap-2.5">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={onClose}
              disabled={isSubmitting}
              className="text-xs"
            >
              Cancel
            </Button>

            <Button
              type="submit"
              size="sm"
              disabled={isSubmitting}
              className="text-xs font-bold bg-blue-600 hover:bg-blue-700 text-white shadow-sm"
            >
              {isSubmitting ? 'Saving Changes...' : 'Save Changes'}
            </Button>
          </div>
        </form>
      </Card>
    </div>
  )
}
