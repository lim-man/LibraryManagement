import { defineStore } from 'pinia'
import { ref } from 'vue'
import { login as loginApi, getCurrentUser } from '../api/auth'

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('token') || '')
  const user = ref(null)
  const userRole = ref(localStorage.getItem('userRole') || '')
  const userName = ref(localStorage.getItem('userName') || '')

  async function login(credentials) {
    const res = await loginApi(credentials)
    token.value = res.data.token
    user.value = res.data.user
    userRole.value = res.data.user.role
    userName.value = res.data.user.name

    localStorage.setItem('token', res.data.token)
    localStorage.setItem('userRole', res.data.user.role)
    localStorage.setItem('userName', res.data.user.name)

    return res
  }

  function logout() {
    token.value = ''
    user.value = null
    userRole.value = ''
    userName.value = ''
    localStorage.removeItem('token')
    localStorage.removeItem('userRole')
    localStorage.removeItem('userName')
  }

  return { token, user, userRole, userName, login, logout }
})
