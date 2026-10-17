import axios from 'axios'

// Backend running via Docker Compose (http://localhost:5050) or direct dotnet run (https://localhost:7256 / http://localhost:5218)
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5050/api'

export const axiosClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Request Interceptor: Inject JWT token into Authorization header if present
axiosClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('auctionox_token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

// Response Interceptor: Handle 401 Unauthorized gracefully
axiosClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Token expired or invalid: clear local storage
      localStorage.removeItem('auctionox_token')
      localStorage.removeItem('auctionox_refresh_token')
      localStorage.removeItem('auctionox_user')
      
      // Dispatch custom event so AuthContext updates state without full page reload
      window.dispatchEvent(new Event('auth:unauthorized'))
    }
    return Promise.reject(error)
  }
)
