import { axiosClient } from './axiosClient'

export const adminApi = {
  // --- Category Management (Admin Protected) ---
  getCategories: async () => {
    const response = await axiosClient.get('/categories')
    return response.data
  },

  createCategory: async (categoryData) => {
    const response = await axiosClient.post('/categories', categoryData)
    return response.data
  },

  updateCategory: async (id, categoryData) => {
    const response = await axiosClient.put(`/categories/${id}`, categoryData)
    return response.data
  },

  deleteCategory: async (id) => {
    const response = await axiosClient.delete(`/categories/${id}`)
    return response.data
  },

  // --- Auction Moderation (Admin Protected) ---
  getAllAuctions: async ({ categoryId, status, search, sortBy, pageNumber = 1, pageSize = 20 } = {}) => {
    const params = { pageNumber, pageSize }
    if (categoryId) params.categoryId = categoryId
    if (status) params.status = status
    if (search) params.search = search
    if (sortBy) params.sortBy = sortBy

    const response = await axiosClient.get('/auctions', { params })
    return response.data
  },

  suspendAuction: async (id) => {
    const response = await axiosClient.put(`/auctions/admin/${id}/suspend`)
    return response.data
  },

  resumeAuction: async (id) => {
    const response = await axiosClient.put(`/auctions/admin/${id}/resume`)
    return response.data
  },

  deleteAuction: async (id) => {
    const response = await axiosClient.delete(`/auctions/admin/${id}`)
    return response.data
  },

  // --- Administrator Management (Admin Protected) ---
  getAdmins: async () => {
    const response = await axiosClient.get('/auth/admins')
    return response.data
  },

  createAdmin: async (adminData) => {
    const response = await axiosClient.post('/auth/create-admin', adminData)
    return response.data
  },

  demoteAdmin: async (id) => {
    const response = await axiosClient.put(`/auth/admins/${id}/role`, { role: 'User' })
    return response.data
  },

  promoteToAdmin: async (id) => {
    const response = await axiosClient.put(`/auth/users/${id}/role`, { role: 'Admin' })
    return response.data
  },

  updateUserRole: async (id, role) => {
    const response = await axiosClient.put(`/auth/users/${id}/role`, { role })
    return response.data
  },

  deleteAdmin: async (id) => {
    const response = await axiosClient.delete(`/auth/admins/${id}`)
    return response.data
  },

  // --- User & Account Moderation (Admin Protected) ---
  getAllUsers: async ({ role, search } = {}) => {
    const params = {}
    if (role && role !== 'all') params.role = role
    if (search) params.search = search
    const response = await axiosClient.get('/auth/users', { params })
    return response.data
  },

  suspendUser: async (id, { days = 7, reason } = {}) => {
    const response = await axiosClient.put(`/auth/users/${id}/suspend`, { days, reason })
    return response.data
  },

  banUser: async (id, { reason } = {}) => {
    const response = await axiosClient.put(`/auth/users/${id}/ban`, { reason })
    return response.data
  },

  reactivateUser: async (id) => {
    const response = await axiosClient.put(`/auth/users/${id}/reactivate`)
    return response.data
  },

  deleteUserAccount: async (id) => {
    const response = await axiosClient.delete(`/auth/users/${id}`)
    return response.data
  },
}
