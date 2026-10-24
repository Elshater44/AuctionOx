import { axiosClient } from './axiosClient'

export const auctionApi = {
  // GET /api/auctions with pagination, search, category filter, sorting
  getAuctions: async ({ categoryId, status, search, sortBy, pageNumber = 1, pageSize = 12 } = {}) => {
    const params = { pageNumber, pageSize }
    if (categoryId) params.categoryId = categoryId
    if (status) params.status = status
    if (search) params.search = search
    if (sortBy) params.sortBy = sortBy

    const response = await axiosClient.get('/auctions', { params })
    return response.data // PagedResult<AuctionItemDto>
  },

  // GET /api/auctions/{id}
  getAuctionDetails: async (id) => {
    const response = await axiosClient.get(`/auctions/${id}`)
    return response.data // AuctionDetailDto
  },

  // POST /api/images/upload (Protected, multipart/form-data)
  uploadImage: async (file) => {
    const formData = new FormData()
    formData.append('file', file)
    const response = await axiosClient.post('/images/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    })
    return response.data // { fileId, url }
  },

  // POST /api/auctions (Protected)
  createAuction: async (auctionData) => {
    const response = await axiosClient.post('/auctions', auctionData)
    return response.data
  },

  // POST /api/auctions/{id}/bids (Protected)
  placeBid: async (auctionId, bidAmount) => {
    const response = await axiosClient.post(`/auctions/${auctionId}/bids`, { bidAmount: Number(bidAmount) })
    return response.data
  },

  // POST /api/auctions/{id}/buy-now (Protected)
  buyItNow: async (auctionId) => {
    const response = await axiosClient.post(`/auctions/${auctionId}/buy-now`)
    return response.data
  },

  // GET /api/categories
  getCategories: async () => {
    const response = await axiosClient.get('/categories')
    return response.data
  },

  // GET /api/bids/my-bids (Protected)
  getMyBids: async () => {
    const response = await axiosClient.get('/bids/my-bids')
    return response.data // BidDto[]
  },

  // GET /api/auctions/my-auctions (Protected)
  getMyAuctions: async ({ pageNumber = 1, pageSize = 10 } = {}) => {
    const response = await axiosClient.get('/auctions/my-auctions', {
      params: { pageNumber, pageSize },
    })
    return response.data // PagedResult<AuctionItemDto>
  },

  // DELETE /api/auctions/{id} (Protected - Seller cancel/delete)
  deleteAuction: async (id) => {
    const response = await axiosClient.delete(`/auctions/${id}`)
    return response.data
  },

  // PUT /api/auctions/{id} (Protected - Seller update)
  updateAuction: async (id, updateData) => {
    const response = await axiosClient.put(`/auctions/${id}`, updateData)
    return response.data
  },
}
