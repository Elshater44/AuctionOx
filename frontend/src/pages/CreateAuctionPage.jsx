import React, { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { auctionApi } from '@/api/auctionApi'
import {
  PlusCircle,
  AlertCircle,
  ArrowLeft,
  Image as ImageIcon,
  CheckCircle2,
  AlertTriangle,
  Layers,
  Upload,
  Loader2
} from 'lucide-react'

export function CreateAuctionPage({ onBack, onSuccess }) {
  const navigate = useNavigate()
  const handleBack = onBack || (() => navigate('/'))
  const handleSuccess = onSuccess || (() => navigate('/'))

  const [categories, setCategories] = useState([])
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [startingPrice, setStartingPrice] = useState(50)
  const [buyItNowPrice, setBuyItNowPrice] = useState('')
  const [durationHours, setDurationHours] = useState(24)
  const [categoryId, setCategoryId] = useState('')
  const [imageUrl, setImageUrl] = useState('')
  const [isUploading, setIsUploading] = useState(false)
  const fileInputRef = React.useRef(null)
  const [antiSnipingMinutes, setAntiSnipingMinutes] = useState(5)
  const [imageStatus, setImageStatus] = useState('idle') // 'idle' | 'loading' | 'loaded' | 'error'
  const [error, setError] = useState(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    async function loadCategories() {
      try {
        const data = await auctionApi.getCategories()
        setCategories(data || [])
        if (data?.length > 0) {
          setCategoryId(data[0].id)
        }
      } catch (err) {
        console.error('Failed to load categories', err)
      }
    }
    loadCategories()
  }, [])

  // Update image status when imageUrl changes
  useEffect(() => {
    if (!imageUrl || !imageUrl.trim()) {
      setImageStatus('idle')
    } else {
      setImageStatus('loading')
    }
  }, [imageUrl])

  const handleFileSelect = async (e) => {
    const file = e.target.files?.[0]
    if (!file) return

    try {
      setIsUploading(true)
      setError(null)
      const data = await auctionApi.uploadImage(file)
      setImageUrl(data.url)
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to upload image.')
    } finally {
      setIsUploading(false)
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      const now = new Date()
      const startTime = now.toISOString()
      const endTime = new Date(now.getTime() + durationHours * 60 * 60 * 1000).toISOString()

      const payload = {
        title: title.trim(),
        description: description.trim(),
        startingPrice: Number(startingPrice),
        buyItNowPrice: buyItNowPrice ? Number(buyItNowPrice) : null,
        startTime,
        endTime,
        antiSnipingMinutes: Number(antiSnipingMinutes),
        categoryId: Number(categoryId),
        imageUrl: imageUrl.trim() || undefined,
      }

      await auctionApi.createAuction(payload)
      handleSuccess()
    } catch (err) {
      const message = err.response?.data?.message || (err.response?.data?.errors 
        ? Object.values(err.response?.data?.errors || {}).flat().join(', ')
        : 'Failed to create auction. Please check inputs.')
      setError(message)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="max-w-2xl mx-auto py-4">
      <Button variant="ghost" size="sm" onClick={handleBack} className="gap-1.5 text-xs mb-4">
        <ArrowLeft className="w-3.5 h-3.5" /> Back to Marketplace
      </Button>

      <Card className="shadow-md border-slate-200">
        <CardHeader>
          <div className="flex items-center gap-2">
            <div className="p-2 bg-blue-100 text-blue-600 rounded-lg">
              <PlusCircle className="w-5 h-5" />
            </div>
            <div>
              <CardTitle className="text-xl font-bold">Create New Auction</CardTitle>
              <CardDescription>List an item for competitive live bidding</CardDescription>
            </div>
          </div>
        </CardHeader>

        <form onSubmit={handleSubmit}>
          <CardContent className="space-y-4">
            {error && (
              <div className="flex items-center gap-2 p-3 text-sm text-red-700 bg-red-50 border border-red-200 rounded-md">
                <AlertCircle className="w-4 h-4 shrink-0" />
                <span>{error}</span>
              </div>
            )}

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700">Item Title</label>
              <Input
                type="text"
                placeholder="e.g. Sony Alpha A7 IV Mirrorless Camera"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                required
              />
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700">Description</label>
              <textarea
                rows={3}
                className="flex w-full rounded-md border border-gray-300 bg-transparent px-3 py-2 text-sm shadow-sm placeholder:text-gray-400 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-blue-600"
                placeholder="Detailed condition, specs, accessories included..."
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                required
              />
            </div>

            {/* Category */}
            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700">Category</label>
              <select
                value={categoryId}
                onChange={(e) => setCategoryId(e.target.value)}
                className="w-full h-9 text-sm bg-white border border-gray-300 rounded-md px-3 py-1 font-medium text-slate-700 focus:outline-none focus:ring-1 focus:ring-blue-600"
                required
              >
                {categories.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
            </div>

            {/* Image URL & Live Preview */}
            <div className="space-y-2 pt-2 border-t border-slate-100">
              <div className="flex items-center justify-between">
                <label className="text-xs font-semibold text-slate-700 flex items-center gap-1.5">
                  <ImageIcon className="w-3.5 h-3.5 text-blue-600" />
                  <span>Item Image Link (URL)</span>
                </label>

                {imageStatus === 'loaded' && (
                  <span className="text-[11px] font-medium text-emerald-600 flex items-center gap-1">
                    <CheckCircle2 className="w-3.5 h-3.5" /> Image verified
                  </span>
                )}
                {imageStatus === 'error' && (
                  <span className="text-[11px] font-medium text-red-600 flex items-center gap-1">
                    <AlertTriangle className="w-3.5 h-3.5" /> Unable to load image
                  </span>
                )}
              </div>

              {/* Upload or URL Row */}
              <input
                type="file"
                ref={fileInputRef}
                onChange={handleFileSelect}
                accept="image/png,image/jpeg,image/webp"
                className="hidden"
              />

              <div className="flex gap-2">
                <Input
                  type="url"
                  placeholder="Paste URL or click upload to store image..."
                  value={imageUrl}
                  onChange={(e) => setImageUrl(e.target.value)}
                  className="flex-1"
                />
                <Button
                  type="button"
                  variant="outline"
                  disabled={isUploading}
                  onClick={() => fileInputRef.current?.click()}
                  className="shrink-0 flex items-center gap-1.5 border-slate-300 hover:bg-slate-100"
                >
                  {isUploading ? (
                    <>
                      <Loader2 className="w-3.5 h-3.5 animate-spin text-blue-600" />
                      <span className="text-xs">Uploading...</span>
                    </>
                  ) : (
                    <>
                      <Upload className="w-3.5 h-3.5 text-blue-600" />
                      <span className="text-xs">Upload Image</span>
                    </>
                  )}
                </Button>
              </div>

              {/* LIVE PREVIEW CONTAINER */}
              <div className="mt-2 rounded-xl border border-slate-200 bg-slate-50/70 p-3 overflow-hidden">
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
                      alt="Auction preview"
                      onLoad={() => setImageStatus('loaded')}
                      onError={() => setImageStatus('error')}
                      className={`max-h-[210px] w-auto max-w-full object-contain p-1 transition-opacity duration-200 ${
                        imageStatus === 'loaded' ? 'opacity-100' : 'opacity-30'
                      }`}
                    />

                    {imageStatus === 'loading' && (
                      <div className="absolute inset-0 flex items-center justify-center bg-white/60 text-xs text-slate-500">
                        Loading image preview...
                      </div>
                    )}

                    {imageStatus === 'error' && (
                      <div className="absolute inset-0 flex flex-col items-center justify-center p-4 text-center bg-red-50/90 text-red-700">
                        <AlertTriangle className="w-6 h-6 mb-1 text-red-500" />
                        <p className="font-bold text-xs">Failed to load preview</p>
                        <p className="text-[11px] text-red-600 max-w-xs mt-0.5">
                          Please verify the URL points directly to an image file (e.g. JPEG, PNG, WebP).
                        </p>
                      </div>
                    )}
                  </div>
                ) : (
                  <div className="rounded-lg border-2 border-dashed border-slate-200 p-6 text-center text-slate-400">
                    <ImageIcon className="w-8 h-8 mx-auto text-slate-300 mb-1" />
                    <p className="text-xs font-medium text-slate-600">No image link provided</p>
                    <p className="text-[11px] text-slate-400">Paste an image link above to preview how your auction will look.</p>
                  </div>
                )}
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4 pt-2 border-t border-slate-100">
              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-slate-700">Starting Price ($)</label>
                <Input
                  type="number"
                  min="1"
                  step="0.01"
                  value={startingPrice}
                  onChange={(e) => setStartingPrice(e.target.value)}
                  required
                />
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-slate-700">Buy It Now Price ($) (Optional)</label>
                <Input
                  type="number"
                  min={Number(startingPrice) + 1}
                  step="0.01"
                  placeholder="Instant purchase price"
                  value={buyItNowPrice}
                  onChange={(e) => setBuyItNowPrice(e.target.value)}
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-slate-700">Duration (Hours)</label>
                <select
                  value={durationHours}
                  onChange={(e) => setDurationHours(Number(e.target.value))}
                  className="w-full h-9 text-sm bg-white border border-gray-300 rounded-md px-3 py-1 font-medium text-slate-700 focus:outline-none focus:ring-1 focus:ring-blue-600"
                >
                  <option value={1}>1 Hour (Quick Test)</option>
                  <option value={6}>6 Hours</option>
                  <option value={24}>24 Hours (1 Day)</option>
                  <option value={72}>72 Hours (3 Days)</option>
                  <option value={168}>7 Days</option>
                </select>
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-slate-700">Anti-Sniping Buffer (Minutes)</label>
                <Input
                  type="number"
                  min="0"
                  max="30"
                  value={antiSnipingMinutes}
                  onChange={(e) => setAntiSnipingMinutes(e.target.value)}
                />
              </div>
            </div>

          </CardContent>

          <CardFooter className="flex justify-end gap-3 pt-2">
            <Button type="button" variant="outline" onClick={handleBack}>
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Publishing...' : 'Publish Auction'}
            </Button>
          </CardFooter>
        </form>
      </Card>
    </div>
  )
}
