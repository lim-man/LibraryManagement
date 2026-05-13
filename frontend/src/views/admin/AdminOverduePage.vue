<template>
  <div>
    <el-card>
      <template #header>
        <div class="card-header">
          <span>逾期管理</span>
          <el-tag type="danger" size="large">{{ overdueCount }} 条逾期记录</el-tag>
        </div>
      </template>
      <el-table :data="records" v-loading="loading" stripe>
        <el-table-column prop="userName" label="读者" width="100" />
        <el-table-column prop="bookTitle" label="图书" min-width="180" />
        <el-table-column prop="bookISBN" label="ISBN" width="180" />
        <el-table-column label="借书日期" width="160">
          <template #default="scope">{{ formatDate(scope.row.borrowDate) }}</template>
        </el-table-column>
        <el-table-column label="应还日期" width="160">
          <template #default="scope">
            <span style="color: #F56C6C; font-weight: bold">{{ formatDate(scope.row.dueDate) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="逾期天数" width="100">
          <template #default="scope">
            <el-tag type="danger">{{ getOverdueDays(scope.row.dueDate) }} 天</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="80" fixed="right">
          <template #default="scope">
            <el-button size="small" type="primary" @click="handleReturn(scope.row)">归还</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="pagination">
        <el-pagination
          v-model:current-page="page"
          v-model:page-size="pageSize"
          :total="total"
          layout="total, prev, pager, next"
          @change="loadData"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getOverdue, returnBook } from '../../api/borrows'

const records = ref([])
const loading = ref(false)
const page = ref(1)
const pageSize = ref(10)
const overdueCount = ref(0)

async function loadData() {
  loading.value = true
  try {
    const res = await getOverdue({ page: page.value, pageSize: pageSize.value })
    records.value = res.data
    overdueCount.value = res.data.length
  } finally {
    loading.value = false
  }
}

async function handleReturn(row) {
  try {
    await returnBook(row.id)
    ElMessage.success('归还成功')
    loadData()
  } catch { /* handled */ }
}

function getOverdueDays(dueDate) {
  return Math.max(0, Math.floor((new Date() - new Date(dueDate)) / (1000 * 60 * 60 * 24)))
}

function formatDate(date) {
  return date ? new Date(date).toLocaleString('zh-CN') : '-'
}

onMounted(loadData)
</script>

<style scoped>
.card-header { display: flex; justify-content: space-between; align-items: center; }
.pagination { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>
