import React, { createContext, useContext, useState, useEffect } from 'react'
import { jwtDecode } from 'jwt-decode'
import { authApi } from '@/api/authApi'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [token, setToken] = useState(() => localStorage.getItem('auctionox_token'))
  const [user, setUser] = useState(() => {
    const saved = localStorage.getItem('auctionox_user')
    return saved ? JSON.parse(saved) : null
  })
  const [isLoading, setIsLoading] = useState(true)

  // Helper to extract role & email claims from the JWT
  const parseTokenUser = (jwtToken) => {
    try {
      const decoded = jwtDecode(jwtToken)
      // .NET claims format or standard claim names:
      const roleClaim = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || decoded.role || 'User'
      const email = decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || decoded.email || ''
      const sub = decoded.sub || decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || ''
      
      const roles = Array.isArray(roleClaim) ? roleClaim : [roleClaim]
      return {
        id: sub,
        email,
        roles,
        isAdmin: roles.includes('Admin'),
      }
    } catch {
      return null
    }
  }

  // Sync token changes to localStorage and user state
  const handleAuthSuccess = async (authResponse) => {
    const { token: newToken, refreshToken: newRefreshToken } = authResponse
    localStorage.setItem('auctionox_token', newToken)
    localStorage.setItem('auctionox_refresh_token', newRefreshToken)
    setToken(newToken)

    const tokenUser = parseTokenUser(newToken)
    try {
      // Fetch full profile (firstName, lastName, etc.)
      const profile = await authApi.getProfile()
      const mergedUser = { ...tokenUser, ...profile }
      setUser(mergedUser)
      localStorage.setItem('auctionox_user', JSON.stringify(mergedUser))
      return mergedUser
    } catch {
      setUser(tokenUser)
      localStorage.setItem('auctionox_user', JSON.stringify(tokenUser))
      return tokenUser
    }
  }

  const login = async (email, password) => {
    const data = await authApi.login(email, password)
    return await handleAuthSuccess(data)
  }

  const register = async (firstName, lastName, email, password) => {
    const data = await authApi.register(firstName, lastName, email, password)
    return await handleAuthSuccess(data)
  }

  const logout = () => {
    localStorage.removeItem('auctionox_token')
    localStorage.removeItem('auctionox_refresh_token')
    localStorage.removeItem('auctionox_user')
    setToken(null)
    setUser(null)
  }

  // Listen for 401 unauthorized events emitted from Axios interceptor
  useEffect(() => {
    const handleUnauthorized = () => logout()
    window.addEventListener('auth:unauthorized', handleUnauthorized)
    setIsLoading(false)
    return () => window.removeEventListener('auth:unauthorized', handleUnauthorized)
  }, [])

  return (
    <AuthContext.Provider
      value={{
        token,
        user,
        isAuthenticated: !!token && !!user,
        isAdmin: user?.isAdmin ?? false,
        isLoading,
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
