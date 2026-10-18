import React from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { useAuth } from '@/context/AuthContext'
import { NotificationDropdown } from '@/components/NotificationDropdown'
import { Gavel, LogOut, User as UserIcon, Shield, LayoutDashboard, Store } from 'lucide-react'

export function Navbar({ 
  onNavigateHome, 
  onNavigateLogin, 
  onNavigateRegister, 
  onNavigateAdmin,
  onNavigateActivity,
  currentPage
}) {
  const navigate = useNavigate()
  const location = useLocation()
  const currentPath = location.pathname
  const { user, isAuthenticated, isAdmin, logout } = useAuth()

  const handleHome = onNavigateHome || (() => navigate('/'))
  const handleLogin = onNavigateLogin || (() => navigate('/login'))
  const handleRegister = onNavigateRegister || (() => navigate('/register'))
  const handleAdmin = onNavigateAdmin || (() => navigate('/admin'))
  const handleActivity = onNavigateActivity || (() => navigate(isAuthenticated ? '/my-activity' : '/login'))

  const isHomeActive = currentPage === 'home' || currentPath === '/'
  const isActivityActive = currentPage === 'activity' || currentPath === '/my-activity'
  const isAdminActive = currentPage === 'admin' || currentPath === '/admin'

  return (
    <header className="sticky top-0 z-50 w-full border-b bg-white/95 backdrop-blur supports-[backdrop-filter]:bg-white/60">
      <div className="max-w-6xl mx-auto px-4 h-16 flex items-center justify-between">
        
        {/* Brand & Left Navigation */}
        <div className="flex items-center gap-6">
          <div 
            onClick={handleHome}
            className="flex items-center gap-2.5 cursor-pointer select-none"
          >
            <div className="p-2 bg-blue-600 text-white rounded-lg shadow-sm">
              <Gavel className="w-5 h-5" />
            </div>
            <div>
              <span className="font-extrabold text-lg tracking-tight text-slate-900">
                Auction<span className="text-blue-600">Ox</span>
              </span>
            </div>
          </div>

          {/* Navigation Links for Authenticated Users */}
          {isAuthenticated && (
            <nav className="hidden sm:flex items-center gap-1">
              <Button
                variant={isHomeActive ? 'secondary' : 'ghost'}
                size="sm"
                onClick={handleHome}
                className="text-xs font-semibold flex items-center gap-1.5 text-slate-700"
              >
                <Store className="w-3.5 h-3.5 text-slate-500" />
                Marketplace
              </Button>

              <Button
                variant={isActivityActive ? 'secondary' : 'ghost'}
                size="sm"
                onClick={handleActivity}
                className="text-xs font-semibold flex items-center gap-1.5 text-slate-700"
              >
                <LayoutDashboard className="w-3.5 h-3.5 text-blue-600" />
                My Activity
              </Button>
            </nav>
          )}
        </div>

        {/* User / Auth Actions */}
        <div className="flex items-center gap-2.5">
          {isAuthenticated ? (
            <div className="flex items-center gap-2.5">
              {isAdmin && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleAdmin}
                  className={`flex items-center gap-1.5 text-xs font-bold border-purple-200 shadow-sm transition-all ${
                    isAdminActive 
                      ? 'bg-purple-600 text-white hover:bg-purple-700 border-transparent' 
                      : 'text-purple-700 bg-purple-50 hover:bg-purple-100'
                  }`}
                >
                  <Shield className="w-3.5 h-3.5" />
                  Admin Portal
                </Button>
              )}

              {/* Mobile button for My Activity */}
              <Button
                variant="outline"
                size="sm"
                onClick={handleActivity}
                className="sm:hidden flex items-center gap-1 text-xs text-blue-600 border-blue-200"
              >
                <LayoutDashboard className="w-3.5 h-3.5" />
                Activity
              </Button>

              {/* Notifications Dropdown */}
              <NotificationDropdown />

              <div 
                onClick={handleActivity}
                className="flex items-center gap-2 px-3 py-1.5 bg-slate-100 hover:bg-slate-200/80 cursor-pointer rounded-full text-xs font-medium text-slate-700 transition-colors"
                title="View My Activity"
              >
                <UserIcon className="w-3.5 h-3.5 text-slate-500" />
                <span>{user?.firstName ? `${user.firstName} ${user.lastName || ''}`.trim() : user?.email}</span>
                {isAdmin && (
                  <Badge variant="default" className="text-[10px] px-1.5 py-0 bg-purple-600 hover:bg-purple-700">
                    <Shield className="w-2.5 h-2.5 mr-0.5" /> Admin
                  </Badge>
                )}
              </div>

              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  logout()
                  navigate('/')
                }}
                className="flex items-center gap-1.5 text-xs text-slate-600 hover:text-red-600"
              >
                <LogOut className="w-3.5 h-3.5" /> Logout
              </Button>
            </div>
          ) : (
            <div className="flex items-center gap-2">
              <Button
                variant="ghost"
                size="sm"
                onClick={handleLogin}
                className="text-xs font-semibold"
              >
                Sign In
              </Button>
              <Button
                variant="default"
                size="sm"
                onClick={handleRegister}
                className="text-xs font-semibold"
              >
                Get Started
              </Button>
            </div>
          )}
        </div>

      </div>
    </header>
  )
}
