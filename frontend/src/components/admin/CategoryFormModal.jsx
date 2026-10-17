import React, { useState, useEffect } from 'react'
import { Card, CardHeader, CardTitle, CardContent, CardFooter } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Tag, X } from 'lucide-react'

export function CategoryFormModal({
  isOpen,
  category, // null if creating, object if editing
  onSave,
  onCancel,
  isSubmitting,
}) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [error, setError] = useState(null)

  useEffect(() => {
    if (category) {
      setName(category.name || '')
      setDescription(category.description || '')
    } else {
      setName('')
      setDescription('')
    }
    setError(null)
  }, [category, isOpen])

  if (!isOpen) return null

  const isEditing = Boolean(category)

  const handleSubmit = (e) => {
    e.preventDefault()
    if (!name.trim()) {
      setError('Category name is required')
      return
    }

    onSave({
      id: category?.id,
      name: name.trim(),
      description: description.trim() || undefined,
    })
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/60 backdrop-blur-sm animate-in fade-in duration-200">
      <Card className="max-w-md w-full shadow-2xl border-slate-200 bg-white rounded-2xl overflow-hidden animate-in zoom-in-95 duration-200">
        
        {/* Header */}
        <CardHeader className="bg-slate-50 border-b border-slate-100 pb-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2.5">
              <div className="p-2 bg-purple-100 text-purple-700 rounded-lg">
                <Tag className="w-5 h-5" />
              </div>
              <div>
                <CardTitle className="text-base font-bold text-slate-900">
                  {isEditing ? 'Edit Category' : 'Create New Category'}
                </CardTitle>
                <p className="text-xs text-slate-500">
                  {isEditing ? `Update details for "${category.name}"` : 'Add a new category to the marketplace'}
                </p>
              </div>
            </div>

            <button
              onClick={onCancel}
              disabled={isSubmitting}
              className="text-slate-400 hover:text-slate-600 p-1 rounded-md transition-colors"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        </CardHeader>

        <form onSubmit={handleSubmit}>
          <CardContent className="p-6 space-y-4">
            {error && (
              <div className="p-2.5 text-xs text-red-700 bg-red-50 border border-red-200 rounded-lg">
                {error}
              </div>
            )}

            <div className="space-y-1.5">
              <label className="text-xs font-bold text-slate-700">Category Name *</label>
              <Input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="e.g., Luxury Watches, Rare Coins..."
                maxLength={100}
                required
                className="text-xs font-medium"
              />
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-bold text-slate-700">Description</label>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Brief summary of items belonging to this category..."
                rows={3}
                maxLength={500}
                className="w-full text-xs p-3 rounded-md border border-slate-200 focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent transition-all"
              />
            </div>
          </CardContent>

          <CardFooter className="bg-slate-50 p-4 border-t border-slate-100 flex items-center justify-end gap-2.5">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={onCancel}
              disabled={isSubmitting}
              className="text-xs"
            >
              Cancel
            </Button>

            <Button
              type="submit"
              size="sm"
              disabled={isSubmitting}
              className="text-xs bg-purple-600 hover:bg-purple-700 text-white font-bold"
            >
              {isSubmitting ? 'Saving...' : isEditing ? 'Save Changes' : 'Create Category'}
            </Button>
          </CardFooter>
        </form>
      </Card>
    </div>
  )
}
