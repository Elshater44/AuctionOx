import { axiosClient } from './axiosClient'

export const authApi = {
  // POST /api/auth/login -> returns { token, refreshToken, expiration }
  login: async (email, password) => {
    const response = await axiosClient.post('/auth/login', { email, password })
    return response.data
  },

  // POST /api/auth/register -> returns { token, refreshToken, expiration }
  register: async (firstName, lastName, email, password) => {
    const response = await axiosClient.post('/auth/register', {
      firstName,
      lastName,
      email,
      password,
    })
    return response.data
  },

  // GET /api/auth/profile -> returns UserProfileDto { id, email, firstName, lastName, createdAt }
  getProfile: async () => {
    const response = await axiosClient.get('/auth/profile')
    return response.data
  },

  // POST /api/auth/refresh -> returns new { token, refreshToken, expiration }
  refreshToken: async (token, refreshToken) => {
    const response = await axiosClient.post('/auth/refresh', { token, refreshToken })
    return response.data
  },
}
