import { axiosClient } from './axiosClient'

export const notificationApi = {
  // GET /api/notifications
  getNotifications: async ({ pageNumber = 1, pageSize = 10, unreadOnly = null } = {}) => {
    const params = { pageNumber, pageSize }
    if (unreadOnly !== null && unreadOnly !== undefined) {
      params.unreadOnly = unreadOnly
    }
    const response = await axiosClient.get('/notifications', { params })
    return response.data
  },

  // GET /api/notifications/unread-count
  getUnreadCount: async () => {
    const response = await axiosClient.get('/notifications/unread-count')
    return response.data.unreadCount
  },

  // PUT /api/notifications/{id}/read
  markAsRead: async (id) => {
    const response = await axiosClient.put(`/notifications/${id}/read`)
    return response.data
  },

  // PUT /api/notifications/read-all
  markAllAsRead: async () => {
    const response = await axiosClient.put('/notifications/read-all')
    return response.data
  },

  // DELETE /api/notifications/{id}
  deleteNotification: async (id) => {
    const response = await axiosClient.delete(`/notifications/${id}`)
    return response.data
  }
}
