import React, { useState, useEffect, useCallback } from 'react'
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell
} from '@/components/ui/table'
import { CategoryFormModal } from './CategoryFormModal'
import { AdminConfirmModal } from './AdminConfirmModal'
import { adminApi } from '@/api/adminApi'
import { useToast } from '@/context/ToastContext'
import { PlusCircle, Edit2, Trash2, Tag, RefreshCw } from 'lucide-react'

export function CategoryManagementTab() {
  const toast = useToast()
  const [categories, setCategories] = useState([])
  const [isLoading, setIsLoading] = useState(true)

  // Modals state
  const [formModalOpen, setFormModalOpen] = useState(false)
  const [editingCategory, setEditingCategory] = useState(null)
  const [isSaving, setIsSaving] = useState(false)

  const [deleteModalOpen, setDeleteModalOpen] = useState(false)
  const [deletingCategory, setDeletingCategory] = useState(null)
  const [isDeleting, setIsDeleting] = useState(false)

  const loadCategories = useCallback(async () => {
    setIsLoading(true)
    try {
      const data = await adminApi.getCategories()
      setCategories(data || [])
    } catch (err) {
      console.error('Failed to load categories:', err)
      toast.error('Error', 'Unable to fetch categories from the server.')
    } finally {
      setIsLoading(false)
    }
  }, [toast])

  useEffect(() => {
    loadCategories()
  }, [loadCategories])

  // Open Create Modal
  const handleOpenCreate = () => {
    setEditingCategory(null)
    setFormModalOpen(true)
  }

  // Open Edit Modal
  const handleOpenEdit = (category) => {
    setEditingCategory(category)
    setFormModalOpen(true)
  }

  // Save Category (Create or Edit)
  const handleSaveCategory = async (formData) => {
    setIsSaving(true)
    try {
      if (formData.id) {
        await adminApi.updateCategory(formData.id, {
          name: formData.name,
          description: formData.description,
        })
        toast.success('Category Updated', `"${formData.name}" has been updated successfully.`)
      } else {
        await adminApi.createCategory({
          name: formData.name,
          description: formData.description,
        })
        toast.success('Category Created', `"${formData.name}" has been added to the marketplace.`)
      }
      setFormModalOpen(false)
      loadCategories()
    } catch (err) {
      const msg = err.response?.data?.message || err.response?.data?.detail || 'Failed to save category.'
      toast.error('Operation Failed', msg)
    } finally {
      setIsSaving(false)
    }
  }

  // Open Delete Modal
  const handleOpenDelete = (category) => {
    setDeletingCategory(category)
    setDeleteModalOpen(true)
  }

  // Confirm Delete
  const handleConfirmDelete = async () => {
    if (!deletingCategory) return
    setIsDeleting(true)
    try {
      await adminApi.deleteCategory(deletingCategory.id)
      toast.success('Category Deleted', `"${deletingCategory.name}" was removed.`)
      setDeleteModalOpen(false)
      loadCategories()
    } catch (err) {
      const msg = err.response?.data?.message || err.response?.data?.detail || 'Failed to delete category.'
      toast.error('Deletion Failed', msg)
    } finally {
      setIsDeleting(false)
    }
  }

  return (
    <div className="space-y-4">
      {/* Action Header */}
      <div className="flex flex-wrap items-center justify-between gap-3 bg-white p-4 rounded-xl border border-slate-200 shadow-sm">
        <div>
          <h3 className="text-base font-bold text-slate-900 flex items-center gap-2">
            <Tag className="w-4 h-4 text-purple-600" />
            Marketplace Categories
          </h3>
          <p className="text-xs text-slate-500">
            Define, update, and manage categories available for sellers to list items under.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={loadCategories}
            disabled={isLoading}
            className="text-xs gap-1.5"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isLoading ? 'animate-spin' : ''}`} /> Refresh
          </Button>

          <Button
            size="sm"
            onClick={handleOpenCreate}
            className="text-xs font-bold bg-purple-600 hover:bg-purple-700 text-white gap-1.5 shadow-sm"
          >
            <PlusCircle className="w-4 h-4" /> Add Category
          </Button>
        </div>
      </div>

      {/* Table Card */}
      <Card className="border-slate-200 shadow-sm overflow-hidden">
        <CardContent className="p-0">
          {isLoading ? (
            <div className="p-8 text-center text-xs text-slate-400 space-y-2">
              <RefreshCw className="w-6 h-6 animate-spin mx-auto text-purple-600" />
              <span>Loading categories...</span>
            </div>
          ) : categories.length === 0 ? (
            <div className="p-12 text-center text-slate-400 space-y-2">
              <Tag className="w-8 h-8 mx-auto stroke-1 text-slate-300" />
              <p className="text-xs font-semibold text-slate-600">No categories found</p>
              <p className="text-[11px]">Click "Add Category" above to create your first category.</p>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-16">ID</TableHead>
                  <TableHead className="w-48">Name</TableHead>
                  <TableHead>Description</TableHead>
                  <TableHead className="text-right w-32">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {categories.map((cat) => (
                  <TableRow key={cat.id}>
                    <TableCell className="font-mono text-xs font-bold text-slate-400">
                      #{cat.id}
                    </TableCell>
                    <TableCell className="font-bold text-slate-900 text-xs">
                      {cat.name}
                    </TableCell>
                    <TableCell className="text-xs text-slate-500 max-w-md truncate">
                      {cat.description || <span className="italic text-slate-300">No description provided</span>}
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end gap-1.5">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleOpenEdit(cat)}
                          className="h-8 px-2 text-slate-600 hover:text-purple-600 hover:bg-purple-50 text-xs gap-1"
                        >
                          <Edit2 className="w-3.5 h-3.5" /> Edit
                        </Button>

                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleOpenDelete(cat)}
                          className="h-8 px-2 text-slate-400 hover:text-red-600 hover:bg-red-50 text-xs"
                        >
                          <Trash2 className="w-3.5 h-3.5" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Category Create/Edit Modal */}
      <CategoryFormModal
        isOpen={formModalOpen}
        category={editingCategory}
        onSave={handleSaveCategory}
        onCancel={() => setFormModalOpen(false)}
        isSubmitting={isSaving}
      />

      {/* Category Delete Confirm Modal */}
      <AdminConfirmModal
        isOpen={deleteModalOpen}
        title="Delete Category"
        description={`Are you sure you want to delete "${deletingCategory?.name}"? Items assigned to this category will remain, but sellers will no longer be able to select it.`}
        confirmText="Yes, Delete Category"
        variant="destructive"
        onConfirm={handleConfirmDelete}
        onCancel={() => setDeleteModalOpen(false)}
        isSubmitting={isDeleting}
      />
    </div>
  )
}
