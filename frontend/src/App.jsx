import React from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider } from '@/context/AuthContext'
import { ToastProvider } from '@/context/ToastContext'
import { NotificationProvider } from '@/context/NotificationContext'
import { Navbar } from '@/components/Navbar'
import { HomePage } from '@/pages/HomePage'
import { LoginPage } from '@/pages/LoginPage'
import { RegisterPage } from '@/pages/RegisterPage'
import { CreateAuctionPage } from '@/pages/CreateAuctionPage'
import { AuctionDetailPage } from '@/pages/AuctionDetailPage'
import { AdminDashboardPage } from '@/pages/AdminDashboardPage'
import { MyActivityPage } from '@/pages/MyActivityPage'

function AppLayout() {
  return (
    <div className="min-h-screen bg-slate-50 text-slate-900 flex flex-col">
      <Navbar />

      <main className="flex-1 max-w-6xl w-full mx-auto p-4 md:p-6">
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/create-auction" element={<CreateAuctionPage />} />
          <Route path="/auctions/:id" element={<AuctionDetailPage />} />
          <Route path="/admin" element={<AdminDashboardPage />} />
          <Route path="/my-activity" element={<MyActivityPage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </div>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <ToastProvider>
        <AuthProvider>
          <NotificationProvider>
            <AppLayout />
          </NotificationProvider>
        </AuthProvider>
      </ToastProvider>
    </BrowserRouter>
  )
}
