import React, { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/context/AuthContext'
import { CategoryManagementTab } from '@/components/admin/CategoryManagementTab'
import { AuctionModerationTab } from '@/components/admin/AuctionModerationTab'
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Shield, ShieldAlert, Tag, ArrowLeft, Lock } from 'lucide-react'

export function AdminDashboardPage({ onBack, onSelectAuction }) {
  const navigate = useNavigate()
  const handleBack = onBack || (() => navigate('/'))
  const handleSelectAuction = onSelectAuction || ((id) => navigate(`/auctions/${id}`))

  const { isAdmin, isAuthenticated } = useAuth()
  const [activeTab, setActiveTab] = useState('moderation') // 'categories' | 'moderation'

  // Non-Admin / Unauthorized Fallback Guard
  if (!isAuthenticated || !isAdmin) {
    return (
      <div className="max-w-md mx-auto py-16 text-center space-y-4">
        <Card className="border-red-100 shadow-lg bg-white overflow-hidden">
          <CardHeader className="bg-red-50/80 border-b border-red-100 pb-6 pt-8">
            <div className="w-12 h-12 rounded-full bg-red-100 text-red-600 flex items-center justify-center mx-auto mb-3">
              <Lock className="w-6 h-6" />
            </div>
            <CardTitle className="text-lg font-bold text-red-950">
              Access Restricted
            </CardTitle>
            <CardDescription className="text-xs text-red-800/80">
              Administrator privileges required.
            </CardDescription>
          </CardHeader>
          <CardContent className="p-6 space-y-4">
            <p className="text-xs text-slate-600 leading-relaxed">
              You do not have administrative permissions to view or modify marketplace operations. Please sign in with an authorized administrator account (e.g. <span className="font-mono text-purple-700 bg-purple-50 px-1 py-0.5 rounded">admin@auctionox.com</span>).
            </p>
            <Button
              onClick={handleBack}
              className="w-full text-xs font-bold bg-slate-900 hover:bg-slate-800 text-white"
            >
              Return to Marketplace
            </Button>
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Top Breadcrumb & Header */}
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <button
            onClick={handleBack}
            className="flex items-center gap-1.5 text-xs text-slate-500 hover:text-slate-800 transition-colors"
          >
            <ArrowLeft className="w-4 h-4" /> Back to Marketplace
          </button>

          <Badge variant="default" className="text-[11px] px-2.5 py-0.5 bg-purple-600 hover:bg-purple-700">
            <Shield className="w-3 h-3 mr-1" /> Admin Control Center
          </Badge>
        </div>

        <div>
          <h1 className="text-2xl font-extrabold tracking-tight text-slate-900 flex items-center gap-2">
            Marketplace Administration
          </h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Manage category taxonomies, oversee platform auction lots, and enforce compliance in real-time.
          </p>
        </div>
      </div>

      {/* Admin Tab Navigation */}
      <div className="flex items-center gap-2 border-b border-slate-200 pb-1">
        <button
          onClick={() => setActiveTab('moderation')}
          className={`flex items-center gap-2 px-4 py-2.5 text-xs font-bold rounded-lg transition-all ${
            activeTab === 'moderation'
              ? 'bg-purple-600 text-white shadow-sm'
              : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
          }`}
        >
          <ShieldAlert className="w-4 h-4" />
          Auction Moderation Queue
        </button>

        <button
          onClick={() => setActiveTab('categories')}
          className={`flex items-center gap-2 px-4 py-2.5 text-xs font-bold rounded-lg transition-all ${
            activeTab === 'categories'
              ? 'bg-purple-600 text-white shadow-sm'
              : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
          }`}
        >
          <Tag className="w-4 h-4" />
          Category Management
        </button>
      </div>

      {/* Tab Content */}
      <div>
        {activeTab === 'moderation' && (
          <AuctionModerationTab onSelectAuction={handleSelectAuction} />
        )}
        {activeTab === 'categories' && (
          <CategoryManagementTab />
        )}
      </div>
    </div>
  )
}
