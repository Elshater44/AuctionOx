import React, { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/context/AuthContext'
import { LogIn, AlertCircle } from 'lucide-react'

export function LoginPage({ onNavigateToRegister, onSuccess }) {
  const navigate = useNavigate()
  const handleSuccess = onSuccess || (() => navigate('/'))
  const handleNavigateToRegister = onNavigateToRegister || (() => navigate('/register'))

  const { login } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await login(email, password)
      handleSuccess()
    } catch (err) {
      const message = err.response?.data?.message || 'Invalid email or password.'
      setError(message)
    } finally {
      setIsSubmitting(false)
    }
  }

  // Pre-fill demo credentials
  const fillCredentials = (demoEmail, demoPassword) => {
    setEmail(demoEmail)
    setPassword(demoPassword)
  }

  return (
    <div className="flex items-center justify-center min-h-[calc(100vh-160px)] px-4">
      <Card className="w-full max-w-md shadow-md border-slate-200">
        <CardHeader className="space-y-1 text-center">
          <div className="flex justify-center mb-2">
            <div className="p-3 bg-blue-100 text-blue-600 rounded-full">
              <LogIn className="w-6 h-6" />
            </div>
          </div>
          <CardTitle className="text-2xl font-bold">Sign In</CardTitle>
          <CardDescription>Enter your email and password to access AuctionOx</CardDescription>
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
              <label className="text-xs font-semibold text-slate-600">Email Address</label>
              <Input
                type="email"
                placeholder="name@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-600">Password</label>
              <Input
                type="password"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
            </div>

            {/* Quick Demo Logins Helper */}
            <div className="pt-2 border-t">
              <span className="text-xs text-slate-400 block mb-1.5">Quick fill test credentials:</span>
              <div className="flex gap-2">
                <button
                  type="button"
                  onClick={() => fillCredentials('admin@auctionox.com', 'AdminPassword123!')}
                  className="text-xs px-2 py-1 bg-slate-100 hover:bg-slate-200 rounded text-slate-700 font-medium cursor-pointer"
                >
                  Admin User
                </button>
                <button
                  type="button"
                  onClick={() => fillCredentials('user@auctionox.com', 'UserPassword123!')}
                  className="text-xs px-2 py-1 bg-slate-100 hover:bg-slate-200 rounded text-slate-700 font-medium cursor-pointer"
                >
                  Regular User
                </button>
              </div>
            </div>
          </CardContent>

          <CardFooter className="flex flex-col gap-3">
            <Button type="submit" className="w-full font-semibold" disabled={isSubmitting}>
              {isSubmitting ? 'Signing in...' : 'Sign In'}
            </Button>
            <p className="text-xs text-center text-slate-500">
              Don't have an account?{' '}
              <button
                type="button"
                onClick={handleNavigateToRegister}
                className="text-blue-600 hover:underline font-semibold cursor-pointer"
              >
                Sign up
              </button>
            </p>
          </CardFooter>
        </form>
      </Card>
    </div>
  )
}
