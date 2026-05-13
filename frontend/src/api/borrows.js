import request from './request'

export function borrowBook(data) {
  return request.post('/borrows/borrow', data)
}

export function returnBook(recordId) {
  return request.post(`/borrows/return/${recordId}`)
}

export function getBorrows(params) {
  return request.get('/borrows', { params })
}

export function getOverdue(params) {
  return request.get('/borrows/overdue', { params })
}

export function getDashboard() {
  return request.get('/stats/dashboard')
}
