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
}
