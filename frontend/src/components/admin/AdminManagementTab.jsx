import React, { useState, useEffect, useCallback } from 'react'
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
  TableCell,
} from '@/components/ui/table'
import { CreateAdminModal } from './CreateAdminModal'
import { PromoteUserModal } from './PromoteUserModal'
import { SuspendUserModal } from './SuspendUserModal'
import { AdminConfirmModal } from './AdminConfirmModal'
import { UserActionsMenu } from './UserActionsMenu'
import { adminApi } from '@/api/adminApi'
import { useAuth } from '@/context/AuthContext'
import { useToast } from '@/context/ToastContext'
import {
  ShieldCheck,
  ShieldPlus,
  UserPlus,
  RefreshCw,
  Search,
  Shield,
  CheckCircle2,
  User,
  UserMinus,
  Trash2,
  Ban,
  Clock,
  Unlock,
  AlertTriangle,
  Users,
} from 'lucide-react'

export function AdminManagementTab() {
  const { user: currentUser } = useAuth()
  const toast = useToast()

  const [users, setUsers] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [searchQuery, setSearchQuery] = useState('')
  const [roleFilter, setRoleFilter] = useState('all') // 'all' | 'Admin' | 'User'
  const [statusFilter, setStatusFilter] = useState('all') // 'all' | 'active' | 'suspended' | 'banned'

  // Modals
  const [createAdminOpen, setCreateAdminOpen] = useState(false)
  const [promoteModalOpen, setPromoteModalOpen] = useState(false)
  const [suspendModalOpen, setSuspendModalOpen] = useState(false)
  const [targetUser, setTargetUser] = useState(null)

  // Confirm Modal state (reusable for ban, reactivate, demote, delete)
  const [confirmConfig, setConfirmConfig] = useState({
    isOpen: false,
    title: '',
    description: '',
    confirmText: 'Confirm',
    variant: 'destructive',
    action: null,
    isSubmitting: false,
  })

  const loadUsers = useCallback(async () => {
    setIsLoading(true)
    try {
      const data = await adminApi.getAllUsers({
        role: roleFilter !== 'all' ? roleFilter : undefined,
        search: searchQuery.trim() || undefined,
      })
      setUsers(data || [])
    } catch (err) {
      console.error('Failed to load users:', err)
      toast.error('Error', 'Unable to fetch accounts from the server.')
    } finally {
      setIsLoading(false)
    }
  }, [roleFilter, searchQuery, toast])

  useEffect(() => {
    loadUsers()
  }, [loadUsers])

  const handleAdminCreated = (newAdmin) => {
    setUsers((prev) => [
      {
        ...newAdmin,
        roles: ['Admin'],
        isLockedOut: false,
        lockoutEnd: null,
      },
      ...prev.filter((u) => u.id !== newAdmin.id),
    ])
  }

  // --- Modal Triggers ---

  // 1. Suspend User
  const handleOpenSuspend = (user) => {
    setTargetUser(user)
    setSuspendModalOpen(true)
  }

  const handleConfirmSuspend = async ({ days, reason }) => {
    if (!targetUser) return
    setConfirmConfig((prev) => ({ ...prev, isSubmitting: true }))
    try {
      const res = await adminApi.suspendUser(targetUser.id, { days, reason })
      setUsers((prev) =>
        prev.map((u) =>
          u.id === targetUser.id
            ? { ...u, isLockedOut: true, lockoutEnd: res.lockoutEnd }
            : u
        )
      )
      toast.success(
        'Account Suspended',
        `${targetUser.firstName} ${targetUser.lastName} has been suspended for ${days} days.`
      )
      setSuspendModalOpen(false)
      setTargetUser(null)
    } catch (err) {
      console.error('Failed to suspend user:', err)
      toast.error('Suspension Failed', err.response?.data?.message || 'Unable to suspend user account.')
    } finally {
      setConfirmConfig((prev) => ({ ...prev, isSubmitting: false }))
    }
  }

  // 2. Ban User
  const handleOpenBan = (user) => {
    setConfirmConfig({
      isOpen: true,
      title: 'Permanently Ban User Account',
      description: `Are you sure you want to permanently ban ${user.firstName} ${user.lastName} (${user.email})? All active sessions and tokens will be immediately invalidated, and the user will be barred indefinitely from logging in or placing bids.`,
      confirmText: 'Permanently Ban',
      variant: 'destructive',
      action: async () => {
        const res = await adminApi.banUser(user.id)
        setUsers((prev) =>
          prev.map((u) =>
            u.id === user.id
              ? { ...u, isLockedOut: true, lockoutEnd: res.lockoutEnd }
              : u
          )
        )
        toast.success(
          'Account Banned',
          `${user.firstName} ${user.lastName} has been permanently banned from the platform.`
        )
      },
      isSubmitting: false,
    })
  }

  // 3. Reactivate User
  const handleOpenReactivate = (user) => {
    setConfirmConfig({
      isOpen: true,
      title: 'Reactivate Account',
      description: `Are you sure you want to lift the suspension/ban on ${user.firstName} ${user.lastName} (${user.email})? Their account status will return to Active immediately, restoring access to log in and participate in auctions.`,
      confirmText: 'Reactivate Account',
      variant: 'success',
      action: async () => {
        await adminApi.reactivateUser(user.id)
        setUsers((prev) =>
          prev.map((u) =>
            u.id === user.id
              ? { ...u, isLockedOut: false, lockoutEnd: null }
              : u
          )
        )
        toast.success(
          'Account Reactivated',
          `${user.firstName} ${user.lastName}'s account has been successfully reactivated.`
        )
      },
      isSubmitting: false,
    })
  }

  // 4. Demote Admin
  const handleOpenDemote = (admin) => {
    setConfirmConfig({
      isOpen: true,
      title: 'Demote Administrator',
      description: `Are you sure you want to demote ${admin.firstName} ${admin.lastName} (${admin.email}) to a standard User? Their administrator permissions will be revoked immediately and all active sessions invalidated.`,
      confirmText: 'Demote to User',
      variant: 'warning',
      action: async () => {
        await adminApi.demoteAdmin(admin.id)
        setUsers((prev) =>
          prev.map((u) =>
            u.id === admin.id
              ? { ...u, roles: ['User'] }
              : u
          )
        )
        toast.success(
          'Admin Demoted',
          `${admin.firstName} ${admin.lastName} has been demoted to a standard user.`
        )
      },
      isSubmitting: false,
    })
  }

  // 5. Promote User to Admin
  const handleOpenPromote = (user) => {
    setConfirmConfig({
      isOpen: true,
      title: 'Grant Administrator Privileges',
      description: `Are you sure you want to promote ${user.firstName} ${user.lastName} (${user.email}) to an Administrator? They will receive elevated permissions to moderate auctions, manage category taxonomies, and oversee platform accounts.`,
      confirmText: 'Grant Admin Access',
      variant: 'warning',
      action: async () => {
        await adminApi.promoteToAdmin(user.id)
        setUsers((prev) =>
          prev.map((u) =>
            u.id === user.id
              ? { ...u, roles: ['Admin', ...(u.roles?.filter((r) => r !== 'Admin') || [])] }
              : u
          )
        )
        toast.success(
          'Admin Privileges Granted',
          `${user.firstName} ${user.lastName} is now a System Administrator.`
        )
      },
      isSubmitting: false,
    })
  }

  const handleUserPromoted = (promotedUser) => {
    setUsers((prev) =>
      prev.map((u) =>
        u.id === promotedUser.id
          ? { ...u, roles: ['Admin', ...(u.roles?.filter((r) => r !== 'Admin') || [])] }
          : u
      )
    )
  }

  // 6. Delete Account
  const handleOpenDelete = (user) => {
    const isAdmin = user.roles?.includes('Admin')
    setConfirmConfig({
      isOpen: true,
      title: `Delete ${isAdmin ? 'Administrator' : 'User'} Account`,
      description: `Are you sure you want to permanently remove ${user.firstName} ${user.lastName} (${user.email})? This action cannot be undone and will revoke their platform access entirely.`,
      confirmText: 'Delete Account',
      variant: 'destructive',
      action: async () => {
        await adminApi.deleteUserAccount(user.id)
        setUsers((prev) => prev.filter((u) => u.id !== user.id))
        toast.success(
          'Account Deleted',
          `${user.firstName} ${user.lastName}'s account has been permanently removed.`
        )
      },
      isSubmitting: false,
    })
  }

  const handleExecuteConfirm = async () => {
    if (!confirmConfig.action) return
    setConfirmConfig((prev) => ({ ...prev, isSubmitting: true }))
    try {
      await confirmConfig.action()
      setConfirmConfig((prev) => ({ ...prev, isOpen: false, isSubmitting: false }))
    } catch (err) {
      console.error('Action failed:', err)
      toast.error('Action Failed', err.response?.data?.message || 'Unable to complete the requested action.')
      setConfirmConfig((prev) => ({ ...prev, isSubmitting: false }))
    }
  }

  // Client-side status filter
  const displayedUsers = users.filter((u) => {
    if (statusFilter === 'all') return true
    const isLocked = u.isLockedOut
    const isPermanent = isLocked && u.lockoutEnd && new Date(u.lockoutEnd).getFullYear() > new Date().getFullYear() + 50
    if (statusFilter === 'active') return !isLocked
    if (statusFilter === 'banned') return isLocked && isPermanent
    if (statusFilter === 'suspended') return isLocked && !isPermanent
    return true
  })

  const getInitials = (firstName, lastName, email) => {
    if (firstName && lastName) {
      return `${firstName[0]}${lastName[0]}`.toUpperCase()
    }
    if (email) {
      return email.substring(0, 2).toUpperCase()
    }
    return 'OX'
  }

  const formatDate = (dateString) => {
    if (!dateString) return '—'
    const date = new Date(dateString)
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    })
  }

  const renderStatusBadge = (user) => {
    if (!user.isLockedOut) {
      return (
        <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-full border border-emerald-200">
          <CheckCircle2 className="w-3 h-3 text-emerald-600" />
          Active
        </span>
      )
    }

    const isPermanent = user.lockoutEnd && new Date(user.lockoutEnd).getFullYear() > new Date().getFullYear() + 50
    if (isPermanent) {
      return (
        <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-red-700 bg-red-50 px-2 py-0.5 rounded-full border border-red-200">
          <Ban className="w-3 h-3 text-red-600" />
          Banned
        </span>
      )
    }

    // Temporary suspension
    const diffMs = new Date(user.lockoutEnd) - new Date()
    const daysLeft = Math.max(1, Math.ceil(diffMs / (1000 * 60 * 60 * 24)))
    return (
      <span
        className="inline-flex items-center gap-1 text-[11px] font-semibold text-amber-700 bg-amber-50 px-2 py-0.5 rounded-full border border-amber-200"
        title={`Suspended until ${formatDate(user.lockoutEnd)}`}
      >
        <Clock className="w-3 h-3 text-amber-600" />
        Suspended ({daysLeft}d left)
      </span>
    )
  }

  return (
    <div className="space-y-4">
      {/* Top Action & Filter Bar */}
      <div className="flex flex-col md:flex-row items-stretch md:items-center justify-between gap-3">
        {/* Search & Filters */}
        <div className="flex flex-wrap items-center gap-2 flex-1">
          {/* Search Input */}
          <div className="relative flex-1 min-w-[200px] max-w-sm">
            <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <Input
              type="text"
              placeholder="Search by name or email..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-9 text-xs bg-white"
            />
          </div>

          {/* Role Filter Tabs */}
          <div className="flex items-center bg-slate-100 p-0.5 rounded-lg border border-slate-200 text-xs">
            <button
              onClick={() => setRoleFilter('all')}
              className={`px-2.5 py-1 rounded-md font-medium transition-all ${
                roleFilter === 'all'
                  ? 'bg-white text-slate-900 shadow-xs font-semibold'
                  : 'text-slate-600 hover:text-slate-900'
              }`}
            >
              All Roles
            </button>
            <button
              onClick={() => setRoleFilter('Admin')}
              className={`px-2.5 py-1 rounded-md font-medium transition-all ${
                roleFilter === 'Admin'
                  ? 'bg-white text-purple-700 shadow-xs font-semibold'
                  : 'text-slate-600 hover:text-slate-900'
              }`}
            >
              Admins
            </button>
            <button
              onClick={() => setRoleFilter('User')}
              className={`px-2.5 py-1 rounded-md font-medium transition-all ${
                roleFilter === 'User'
                  ? 'bg-white text-slate-900 shadow-xs font-semibold'
                  : 'text-slate-600 hover:text-slate-900'
              }`}
            >
              Users
            </button>
          </div>

          {/* Status Filter */}
          <div className="flex items-center bg-slate-100 p-0.5 rounded-lg border border-slate-200 text-xs">
            <button
              onClick={() => setStatusFilter('all')}
              className={`px-2.5 py-1 rounded-md font-medium transition-all ${
                statusFilter === 'all'
                  ? 'bg-white text-slate-900 shadow-xs font-semibold'
                  : 'text-slate-600 hover:text-slate-900'
              }`}
            >
              All Status
            </button>
            <button
              onClick={() => setStatusFilter('active')}
              className={`px-2.5 py-1 rounded-md font-medium transition-all ${
                statusFilter === 'active'
                  ? 'bg-white text-emerald-700 shadow-xs font-semibold'
                  : 'text-slate-600 hover:text-slate-900'
              }`}
            >
              Active
            </button>
            <button
              onClick={() => setStatusFilter('suspended')}
              className={`px-2.5 py-1 rounded-md font-medium transition-all ${
                statusFilter === 'suspended'
                  ? 'bg-white text-amber-700 shadow-xs font-semibold'
                  : 'text-slate-600 hover:text-slate-900'
              }`}
            >
              Suspended
            </button>
            <button
              onClick={() => setStatusFilter('banned')}
              className={`px-2.5 py-1 rounded-md font-medium transition-all ${
                statusFilter === 'banned'
                  ? 'bg-white text-red-700 shadow-xs font-semibold'
                  : 'text-slate-600 hover:text-slate-900'
              }`}
            >
              Banned
            </button>
          </div>
        </div>

        {/* Action Buttons */}
        <div className="flex items-center gap-2 shrink-0">
          <Button
            variant="outline"
            size="sm"
            onClick={loadUsers}
            disabled={isLoading}
            className="text-xs text-slate-600 hover:text-slate-900 border-slate-200"
          >
            <RefreshCw className={`w-3.5 h-3.5 mr-1.5 ${isLoading ? 'animate-spin' : ''}`} />
            Refresh
          </Button>

          <Button
            variant="outline"
            size="sm"
            onClick={() => setPromoteModalOpen(true)}
            className="text-xs font-semibold text-purple-700 hover:text-purple-800 bg-purple-50 hover:bg-purple-100 border-purple-200 flex items-center gap-1.5"
          >
            <ShieldPlus className="w-3.5 h-3.5" />
            Promote User
          </Button>

          <Button
            size="sm"
            onClick={() => setCreateAdminOpen(true)}
            className="text-xs font-bold bg-purple-600 hover:bg-purple-700 text-white shadow-sm flex items-center gap-1.5"
          >
            <UserPlus className="w-3.5 h-3.5" />
            Create Admin
          </Button>
        </div>
      </div>

      {/* Main Table Card */}
      <Card className="border-slate-200 shadow-sm bg-white overflow-hidden">
        <CardHeader className="p-4 border-b border-slate-100 flex flex-row items-center justify-between">
          <div>
            <CardTitle className="text-sm font-bold text-slate-900 flex items-center gap-2">
              <Users className="w-4 h-4 text-purple-600" />
              Platform Accounts & Moderation
            </CardTitle>
            <CardDescription className="text-xs text-slate-500 mt-0.5">
              Full control over user accounts: suspend, permanently ban, reactivate, demote admins, or delete accounts.
            </CardDescription>
          </div>
          <Badge variant="secondary" className="text-xs font-semibold px-2 py-0.5 bg-purple-50 text-purple-700 border border-purple-200">
            {displayedUsers.length} {displayedUsers.length === 1 ? 'Account' : 'Accounts'}
          </Badge>
        </CardHeader>

        <CardContent className="p-0">
          {isLoading ? (
            <div className="py-16 text-center text-slate-400 space-y-3">
              <RefreshCw className="w-8 h-8 animate-spin mx-auto text-purple-500 opacity-60" />
              <p className="text-xs">Loading user accounts...</p>
            </div>
          ) : displayedUsers.length === 0 ? (
            <div className="py-16 text-center text-slate-400 space-y-2">
              <User className="w-10 h-10 mx-auto text-slate-300" />
              <p className="text-xs font-medium text-slate-600">No accounts match your criteria</p>
              <p className="text-[11px] text-slate-400 max-w-sm mx-auto">
                {searchQuery || roleFilter !== 'all' || statusFilter !== 'all'
                  ? 'Try adjusting your search query or filters.'
                  : 'No accounts registered yet.'}
              </p>
            </div>
          ) : (
            <div className="overflow-x-auto min-h-[320px]">
              <Table>
                <TableHeader>
                  <TableRow className="bg-slate-50 hover:bg-slate-50 border-slate-200">
                    <TableHead className="text-xs font-semibold text-slate-600 w-[260px]">User</TableHead>
                    <TableHead className="text-xs font-semibold text-slate-600">Role</TableHead>
                    <TableHead className="text-xs font-semibold text-slate-600">Joined</TableHead>
                    <TableHead className="text-xs font-semibold text-slate-600">Status</TableHead>
                    <TableHead className="text-xs font-semibold text-slate-600 text-right w-[170px]">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {displayedUsers.map((user, index) => {
                    const isSelf = currentUser && (currentUser.email === user.email || currentUser.id === user.id)
                    const isDefaultAdmin = user.email === 'admin@auctionox.com'
                    const isAdmin = user.roles?.includes('Admin')

                    return (
                      <TableRow key={user.id || user.email} className="hover:bg-slate-50/70 border-slate-100 transition-colors">
                        {/* Name & Email with Avatar */}
                        <TableCell className="py-3">
                          <div className="flex items-center gap-3">
                            <div
                              className={`w-9 h-9 rounded-full text-white font-bold text-xs flex items-center justify-center shrink-0 shadow-sm ${
                                isAdmin
                                  ? 'bg-gradient-to-tr from-purple-600 to-indigo-600'
                                  : 'bg-gradient-to-tr from-slate-600 to-slate-800'
                              }`}
                            >
                              {getInitials(user.firstName, user.lastName, user.email)}
                            </div>
                            <div className="min-w-0">
                              <div className="flex items-center gap-1.5">
                                <span className="font-semibold text-xs text-slate-900 truncate">
                                  {user.firstName} {user.lastName}
                                </span>
                                {isSelf && (
                                  <span className="text-[10px] font-bold bg-blue-50 text-blue-700 px-1.5 py-0.2 rounded border border-blue-200">
                                    You
                                  </span>
                                )}
                              </div>
                              <span className="text-[11px] font-mono text-slate-500 block truncate">
                                {user.email}
                              </span>
                            </div>
                          </div>
                        </TableCell>

                        {/* Role Badge */}
                        <TableCell className="py-3">
                          {isDefaultAdmin ? (
                            <Badge className="text-[10px] font-bold px-2 py-0.5 bg-amber-600 hover:bg-amber-700 text-white">
                              <Shield className="w-3 h-3 mr-1" />
                              Super Admin
                            </Badge>
                          ) : isAdmin ? (
                            <Badge className="text-[10px] font-bold px-2 py-0.5 bg-purple-600 hover:bg-purple-700 text-white">
                              <ShieldCheck className="w-3 h-3 mr-1" />
                              Admin
                            </Badge>
                          ) : (
                            <Badge variant="outline" className="text-[10px] font-medium px-2 py-0.5 text-slate-600 border-slate-300">
                              <User className="w-3 h-3 mr-1 text-slate-400" />
                              User
                            </Badge>
                          )}
                        </TableCell>

                        {/* Joined Date */}
                        <TableCell className="py-3 text-xs text-slate-500 font-medium">
                          {formatDate(user.createdAt)}
                        </TableCell>

                        {/* Status */}
                        <TableCell className="py-3">
                          {renderStatusBadge(user)}
                        </TableCell>

                        {/* Actions */}
                        <TableCell className="py-3 text-right">
                          <UserActionsMenu
                            user={user}
                            isSelf={isSelf}
                            isDefaultAdmin={isDefaultAdmin}
                            isAdmin={isAdmin}
                            onSuspend={handleOpenSuspend}
                            onBan={handleOpenBan}
                            onReactivate={handleOpenReactivate}
                            onPromote={handleOpenPromote}
                            onDemote={handleOpenDemote}
                            onDelete={handleOpenDelete}
                            isNearBottom={index >= displayedUsers.length - 2 && displayedUsers.length > 2}
                          />
                        </TableCell>
                      </TableRow>
                    )
                  })}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Create Admin Modal */}
      <CreateAdminModal
        isOpen={createAdminOpen}
        onClose={() => setCreateAdminOpen(false)}
        onSuccess={handleAdminCreated}
      />

      {/* Promote Existing User Modal */}
      <PromoteUserModal
        isOpen={promoteModalOpen}
        onClose={() => setPromoteModalOpen(false)}
        users={users}
        onSuccess={handleUserPromoted}
      />

      {/* Suspend User Modal */}
      <SuspendUserModal
        isOpen={suspendModalOpen}
        user={targetUser}
        onConfirm={handleConfirmSuspend}
        onCancel={() => {
          setSuspendModalOpen(false)
          setTargetUser(null)
        }}
        isSubmitting={confirmConfig.isSubmitting}
      />

      {/* Universal Action Confirm Modal */}
      <AdminConfirmModal
        isOpen={confirmConfig.isOpen}
        title={confirmConfig.title}
        description={confirmConfig.description}
        confirmText={confirmConfig.confirmText}
        variant={confirmConfig.variant}
        onConfirm={handleExecuteConfirm}
        onCancel={() => setConfirmConfig((prev) => ({ ...prev, isOpen: false }))}
        isSubmitting={confirmConfig.isSubmitting}
      />
    </div>
  )
}