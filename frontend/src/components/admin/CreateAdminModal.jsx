import React, { useState } from 'react'
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { adminApi } from '@/api/adminApi'
import { useToast } from '@/context/ToastContext'
import { ShieldCheck, X, Loader2, AlertCircle, Eye, EyeOff, UserPlus } from 'lucide-react'

export function CreateAdminModal({ isOpen, onClose, onSuccess }) {
  const toast = useToast()

  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)

  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState(null)

  if (!isOpen) return null

  const resetForm = () => {
    setFirstName('')
    setLastName('')
    setEmail('')
    setPassword('')
    setConfirmPassword('')
    setError(null)
    setShowPassword(false)
  }

  const handleClose = () => {
    resetForm()
    onClose()
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)

    // Client-side validations
    if (!firstName.trim() || !lastName.trim()) {
      setError('First name and last name are required.')
      return
    }

    if (!email.trim() || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      setError('Please provide a valid email address.')
      return
    }

    if (!password || password.length < 6) {
      setError('Password must be at least 6 characters long.')
      return
    }

    if (password !== confirmPassword) {
      setError('Passwords do not match.')
      return
    }

    setIsSubmitting(true)
    try {
      const newAdmin = await adminApi.createAdmin({
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        email: email.trim().toLowerCase(),
        password: password,
      })

      toast.success(
        'Admin Provisioned',
        `Administrator account for ${newAdmin.email} was created successfully.`
      )
      resetForm()
      onSuccess(newAdmin)
      onClose()
    } catch (err) {
      console.error('Failed to create admin:', err)
      const message =
        err.response?.data?.message ||
        err.response?.data?.detail ||
        'Failed to provision new administrator account. Please try again.'
      setError(message)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-in fade-in duration-200">
      <Card className="w-full max-w-md bg-white shadow-2xl border-slate-200 overflow-hidden">
        {/* Header */}
        <CardHeader className="bg-purple-50/80 border-b border-purple-100/80 pb-4 pt-5">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2 text-purple-700">
              <div className="p-2 bg-purple-100 rounded-lg">
                <ShieldCheck className="w-5 h-5" />
              </div>
              <div>
                <CardTitle className="text-base font-bold text-slate-900">
                  Provision New Administrator
                </CardTitle>
                <CardDescription className="text-xs text-slate-500 mt-0.5">
                  Grant full administrative privileges across AuctionOx.
                </CardDescription>
              </div>
            </div>
            <button
              onClick={handleClose}
              disabled={isSubmitting}
              className="text-slate-400 hover:text-slate-600 p-1 rounded-md transition-colors"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </CardHeader>

        {/* Form Body */}
        <form onSubmit={handleSubmit}>
          <CardContent className="p-6 space-y-4">
            {error && (
              <div className="p-3 bg-red-50 border border-red-200 rounded-lg flex items-start gap-2.5 text-xs text-red-700 animate-in fade-in duration-200">
                <AlertCircle className="w-4 h-4 text-red-500 shrink-0 mt-0.5" />
                <span className="leading-relaxed">{error}</span>
              </div>
            )}

            {/* Name Fields */}
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-slate-700">First Name *</label>
                <Input
                  type="text"
                  placeholder="e.g. Sarah"
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  disabled={isSubmitting}
                  className="text-xs"
                  required
                />
              </div>
              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-slate-700">Last Name *</label>
                <Input
                  type="text"
                  placeholder="e.g. Connor"
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  disabled={isSubmitting}
                  className="text-xs"
                  required
                />
              </div>
            </div>

            {/* Email Address */}
            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700">Corporate Email Address *</label>
              <Input
                type="email"
                placeholder="admin@auctionox.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                disabled={isSubmitting}
                className="text-xs font-mono"
                required
              />
            </div>

            {/* Password */}
            <div className="space-y-1.5">
              <div className="flex items-center justify-between">
                <label className="text-xs font-semibold text-slate-700">Password *</label>
                <span className="text-[11px] text-slate-400">Min. 6 characters</span>
              </div>
              <div className="relative">
                <Input
                  type={showPassword ? 'text' : 'password'}
                  placeholder="••••••••••••"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  disabled={isSubmitting}
                  className="text-xs pr-10"
                  required
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
                >
                  {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                </button>
              </div>
            </div>

            {/* Confirm Password */}
            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700">Confirm Password *</label>
              <Input
                type={showPassword ? 'text' : 'password'}
                placeholder="••••••••••••"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                disabled={isSubmitting}
                className="text-xs"
                required
              />
            </div>

            <div className="p-3 bg-slate-50 border border-slate-200/80 rounded-lg text-[11px] text-slate-500 leading-relaxed">
              💡 The user will be created with the <strong className="text-purple-700">Admin</strong> role and will immediately have full access to auction moderation, category editing, and admin provisioning.
            </div>
          </CardContent>

          {/* Footer */}
          <CardFooter className="bg-slate-50 border-t border-slate-100 p-4 flex justify-end gap-2.5">
            <Button
              type="button"
              variant="outline"
              onClick={handleClose}
              disabled={isSubmitting}
              className="text-xs"
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={isSubmitting}
              className="text-xs font-bold bg-purple-600 hover:bg-purple-700 text-white flex items-center gap-1.5"
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="w-3.5 h-3.5 animate-spin" /> Provisioning...
                </>
              ) : (
                <>
                  <UserPlus className="w-3.5 h-3.5" /> Create Administrator
                </>
              )}
            </Button>
          </CardFooter>
        </form>
      </Card>
    </div>
  )
}
