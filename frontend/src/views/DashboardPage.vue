<template>
  <div>
    <template v-if="userRole === 'Admin'">
      <el-row :gutter="20" style="margin-bottom: 20px">
        <el-col :span="6" v-for="card in statCards" :key="card.label">
          <el-card shadow="hover" :body-style="{ padding: '20px' }">
            <div class="stat-card">
              <el-icon :size="32" :color="card.color"><component :is="card.icon" /></el-icon>
              <div class="stat-info">
                <div class="stat-value">{{ card.value }}</div>
                <div class="stat-label">{{ card.label }}</div>
              </div>
            </div>
          </el-card>
        </el-col>
      </el-row>
      <el-row :gutter="20">
        <el-col :span="12">
          <el-card header="热门图书 TOP5">
            <el-table :data="dashboard?.popularBooks || []" size="small">
              <el-table-column prop="title" label="书名" />
              <el-table-column prop="count" label="借阅次数" width="100" />
            </el-table>
          </el-card>
        </el-col>
        <el-col :span="12">
          <el-card header="最近借阅记录">
            <el-table :data="dashboard?.recentBorrows || []" size="small">
              <el-table-column prop="userName" label="读者" width="80" />
              <el-table-column prop="bookTitle" label="图书" />
              <el-table-column prop="status" label="状态" width="80">
                <template #default="scope">
                  <el-tag :type="scope.row.status === 'Borrowed' ? 'warning' : 'success'" size="small">
                    {{ scope.row.status === 'Borrowed' ? '借阅中' : '已归还' }}
                  </el-tag>
                </template>
              </el-table-column>
            </el-table>
          </el-card>
        </el-col>
      </el-row>
    </template>
    <template v-else>
      <el-card>
        <template #header><span class="card-header-title">欢迎使用图书管理系统</span></template>
        <p style="margin-bottom: 16px; font-size: 16px; color: #606266;">
          你好，<strong>{{ userName }}</strong>！你可以浏览图书、借阅图书、查看借阅记录。
        </p>
        <el-button type="primary" @click="$router.push('/books')">浏览图书</el-button>
        <el-button @click="$router.push('/my-borrows')">我的借阅</el-button>
      </el-card>
    </template>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { getDashboard } from '../api/borrows'
import { getBooks } from '../api/books'

const userRole = ref(localStorage.getItem('userRole') || '')
const userName = ref(localStorage.getItem('userName') || '')
const dashboard = ref(null)
const readerBooks = ref(null)

const statCards = computed(() => {
  if (!dashboard.value) return []
  return [
    { label: '藏书总数', value: dashboard.value.totalBooks, icon: 'Reading', color: '#409EFF' },
    { label: '可借数量', value: dashboard.value.availableBooks, icon: 'Collection', color: '#67C23A' },
    { label: '当前借出', value: dashboard.value.currentBorrowed, icon: 'DocumentChecked', color: '#E6A23C' },
    { label: '逾期未还', value: dashboard.value.overdueCount, icon: 'WarningFilled', color: '#F56C6C' }
  ]
})

onMounted(async () => {
  if (userRole.value === 'Admin') {
    try {
      const res = await getDashboard()
      dashboard.value = res.data
    } catch { /* handled */ }
  }
})
</script>

<style scoped>
.stat-card { display: flex; align-items: center; gap: 16px; }
.stat-value { font-size: 24px; font-weight: bold; color: #303133; }
.stat-label { font-size: 13px; color: #909399; margin-top: 2px; }
.card-header-title { font-weight: bold; }
</style>
