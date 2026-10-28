import React, { useState, useMemo } from 'react'
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { adminApi } from '@/api/adminApi'
import { useToast } from '@/context/ToastContext'
import { ShieldPlus, Search, User, X, Loader2, CheckCircle2, AlertTriangle, ShieldCheck } from 'lucide-react'

export function PromoteUserModal({
  isOpen,
  onClose,
  users = [],
  onSuccess,
}) {
  const toast = useToast()
  const [searchTerm, setSearchTerm] = useState('')
  const [selectedUser, setSelectedUser] = useState(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  // Filter candidates: active non-admin users
  const candidates = useMemo(() => {
    return users.filter((u) => {
      const isAdmin = u.roles?.includes('Admin') || u.email === 'admin@auctionox.com'
      if (isAdmin) return false

      if (!searchTerm.trim()) return true
      const q = searchTerm.toLowerCase()
      const fullName = `${u.firstName || ''} ${u.lastName || ''}`.toLowerCase()
      const email = (u.email || '').toLowerCase()
      return fullName.includes(q) || email.includes(q)
    })
  }, [users, searchTerm])

  if (!isOpen) return null

  const handleSelectUser = (u) => {
    setSelectedUser(u)
  }

  const handlePromote = async () => {
    if (!selectedUser) return
    setIsSubmitting(true)
    try {
      await adminApi.promoteToAdmin(selectedUser.id)
      toast.success(
        'Admin Privileges Granted',
        `${selectedUser.firstName} ${selectedUser.lastName} (${selectedUser.email}) has been elevated to Administrator.`
      )
      onSuccess?.(selectedUser)
      handleClose()
    } catch (err) {
      console.error('Failed to promote user:', err)
      toast.error('Promotion Failed', err.response?.data?.message || 'Unable to grant administrator privileges.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleClose = () => {
    setSearchTerm('')
    setSelectedUser(null)
    onClose()
  }

  const getInitials = (firstName, lastName, email) => {
    if (firstName && lastName) return `${firstName[0]}${lastName[0]}`.toUpperCase()
    if (email) return email.substring(0, 2).toUpperCase()
    return 'US'
  }

  const isUserLocked = selectedUser?.isLockedOut

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/60 backdrop-blur-sm animate-in fade-in duration-200">
      <Card className="max-w-lg w-full shadow-2xl border-slate-200 bg-white rounded-2xl overflow-hidden animate-in zoom-in-95 duration-200 flex flex-col max-h-[90vh]">
        {/* Header */}
        <CardHeader className="bg-purple-50/80 border-b border-purple-100 pb-4 shrink-0">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2.5">
              <div className="p-2 rounded-lg bg-purple-100 text-purple-700">
                <ShieldPlus className="w-5 h-5" />
              </div>
              <div>
                <CardTitle className="text-base font-bold text-purple-950">
                  Promote User to Administrator
                </CardTitle>
                <CardDescription className="text-xs text-purple-800/80 mt-0.5">
                  Select an existing registered user and elevate their role to Admin.
                </CardDescription>
              </div>
            </div>

            <button
              onClick={handleClose}
              disabled={isSubmitting}
              className="text-slate-400 hover:text-slate-600 p-1 rounded-md transition-colors"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        </CardHeader>

        <CardContent className="p-5 space-y-4 overflow-y-auto">
          {/* Search Box */}
          <div className="relative">
            <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <Input
              type="text"
              placeholder="Search existing users by name or email..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pl-9 text-xs"
              autoFocus
            />
          </div>

          {/* User Selection List */}
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-slate-700">
              Select User ({candidates.length} candidate{candidates.length === 1 ? '' : 's'})
            </label>

            <div className="border border-slate-200 rounded-lg max-h-48 overflow-y-auto divide-y divide-slate-100 bg-slate-50/50">
              {candidates.length === 0 ? (
                <div className="p-6 text-center text-slate-400 text-xs">
                  <User className="w-6 h-6 mx-auto mb-1 text-slate-300" />
                  {searchTerm ? 'No matching users found.' : 'No standard users available to promote.'}
                </div>
              ) : (
                candidates.map((u) => {
                  const isSelected = selectedUser?.id === u.id
                  return (
                    <button
                      key={u.id}
                      type="button"
                      onClick={() => handleSelectUser(u)}
                      className={`w-full text-left p-2.5 flex items-center justify-between transition-colors ${
                        isSelected
                          ? 'bg-purple-100/70 border-l-4 border-purple-600'
                          : 'hover:bg-slate-100 bg-white'
                      }`}
                    >
                      <div className="flex items-center gap-2.5 min-w-0">
                        <div className="w-7 h-7 rounded-full bg-slate-700 text-white font-bold text-[11px] flex items-center justify-center shrink-0">
                          {getInitials(u.firstName, u.lastName, u.email)}
                        </div>
                        <div className="min-w-0">
                          <p className="text-xs font-semibold text-slate-900 truncate">
                            {u.firstName} {u.lastName}
                          </p>
                          <p className="text-[11px] font-mono text-slate-500 truncate">
                            {u.email}
                          </p>
                        </div>
                      </div>

                      <div className="shrink-0 flex items-center gap-1.5 ml-2">
                        {u.isLockedOut ? (
                          <Badge variant="outline" className="text-[10px] text-amber-700 border-amber-300 bg-amber-50">
                            Suspended
                          </Badge>
                        ) : (
                          <Badge variant="outline" className="text-[10px] text-emerald-700 border-emerald-300 bg-emerald-50">
                            Active
                          </Badge>
                        )}
                        {isSelected && (
                          <CheckCircle2 className="w-4 h-4 text-purple-600 ml-1" />
                        )}
                      </div>
                    </button>
                  )
                })
              )}
            </div>
          </div>

          {/* Selected User Summary & Warning */}
          {selectedUser && (
            <div className="p-3.5 bg-purple-50/60 border border-purple-200 rounded-xl space-y-2">
              <div className="flex items-center justify-between">
                <span className="text-xs font-bold text-purple-900 flex items-center gap-1.5">
                  <ShieldCheck className="w-4 h-4 text-purple-600" />
                  Target User Selected
                </span>
                <span className="text-[11px] font-mono text-purple-700">
                  {selectedUser.email}
                </span>
              </div>

              <p className="text-xs text-slate-600 leading-relaxed">
                Elevating <strong className="text-slate-900">{selectedUser.firstName} {selectedUser.lastName}</strong> will grant them unrestricted access to the marketplace administrator dashboard, including category taxonomy control, auction suspension/deletion, and user moderation.
              </p>

              {isUserLocked && (
                <div className="p-2 bg-amber-100/80 border border-amber-300 rounded-lg flex items-center gap-2 text-xs text-amber-900">
                  <AlertTriangle className="w-4 h-4 text-amber-700 shrink-0" />
                  <span>This account is currently locked out. Please reactivate the user before promoting them.</span>
                </div>
              )}
            </div>
          )}
        </CardContent>

        <CardFooter className="bg-slate-50 p-4 border-t border-slate-100 flex items-center justify-end gap-2.5 shrink-0">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleClose}
            disabled={isSubmitting}
            className="text-xs"
          >
            Cancel
          </Button>

          <Button
            type="button"
            size="sm"
            onClick={handlePromote}
            disabled={!selectedUser || isUserLocked || isSubmitting}
            className="text-xs font-bold bg-purple-600 hover:bg-purple-700 text-white flex items-center gap-1.5 shadow-sm"
          >
            {isSubmitting ? (
              <>
                <Loader2 className="w-3.5 h-3.5 animate-spin" /> Granting Access...
              </>
            ) : (
              <>
                <ShieldPlus className="w-3.5 h-3.5" /> Grant Admin Privileges
              </>
            )}
          </Button>
        </CardFooter>
      </Card>
    </div>
  )
}