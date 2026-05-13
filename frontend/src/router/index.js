import { createRouter, createWebHistory } from 'vue-router'

const routes = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('../views/LoginPage.vue')
  },
  {
    path: '/',
    component: () => import('../components/Layout.vue'),
    children: [
      { path: '', redirect: '/dashboard' },
      { path: 'dashboard', name: 'Dashboard', component: () => import('../views/DashboardPage.vue') },
      { path: 'books', name: 'Books', component: () => import('../views/BookListPage.vue') },
      { path: 'admin/books', name: 'AdminBooks', component: () => import('../views/admin/AdminBookListPage.vue') },
      { path: 'admin/books/create', name: 'AdminBookCreate', component: () => import('../views/admin/AdminBookFormPage.vue') },
      { path: 'admin/books/:id/edit', name: 'AdminBookEdit', component: () => import('../views/admin/AdminBookFormPage.vue') },
      { path: 'admin/users', name: 'AdminUsers', component: () => import('../views/admin/AdminUserListPage.vue') },
      { path: 'admin/users/create', name: 'AdminUserCreate', component: () => import('../views/admin/AdminUserFormPage.vue') },
      { path: 'admin/users/:id/edit', name: 'AdminUserEdit', component: () => import('../views/admin/AdminUserFormPage.vue') },
      { path: 'admin/borrows', name: 'AdminBorrows', component: () => import('../views/admin/AdminBorrowListPage.vue') },
      { path: 'admin/overdue', name: 'AdminOverdue', component: () => import('../views/admin/AdminOverduePage.vue') },
      { path: 'my-borrows', name: 'MyBorrows', component: () => import('../views/MyBorrowsPage.vue') },
      { path: 'profile', name: 'Profile', component: () => import('../views/ProfilePage.vue') },
    ]
  },
  { path: '/:pathMatch(.*)*', name: 'NotFound', component: () => import('../views/NotFoundPage.vue') }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, from, next) => {
  const token = localStorage.getItem('token')
  const userRole = localStorage.getItem('userRole')

  if (to.path === '/login') {
    if (token) return next('/dashboard')
    return next()
  }

  if (!token) return next('/login')

  if (to.path.startsWith('/admin') && userRole !== 'Admin') {
    return next('/dashboard')
  }

  next()
})

export default router
