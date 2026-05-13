<template>
  <div>
    <el-card>
      <template #header><span class="card-title">我的借阅记录</span></template>
      <el-table :data="records" v-loading="loading" stripe>
        <el-table-column prop="bookTitle" label="书名" min-width="180" />
        <el-table-column prop="bookISBN" label="ISBN" width="180" />
        <el-table-column label="借书日期" width="160">
          <template #default="scope">{{ formatDate(scope.row.borrowDate) }}</template>
        </el-table-column>
        <el-table-column label="应还日期" width="160">
          <template #default="scope">{{ formatDate(scope.row.dueDate) }}</template>
        </el-table-column>
        <el-table-column label="状态" width="120">
          <template #default="scope">
            <el-tag :type="scope.row.status === 'Borrowed' ? 'warning' : 'success'">
              {{ scope.row.status === 'Borrowed' ? '借阅中' : '已归还' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="100" fixed="right">
          <template #default="scope">
            <el-button v-if="scope.row.status === 'Borrowed'" type="primary" size="small" @click="handleReturn(scope.row)">
              归还
            </el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="pagination">
        <el-pagination
          v-model:current-page="page"
          v-model:page-size="pageSize"
          :total="total"
          :page-sizes="[10, 20]"
          layout="total, sizes, prev, pager, next"
          @change="loadData"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getBorrows, returnBook } from '../api/borrows'

const records = ref([])
const loading = ref(false)
const page = ref(1)
const pageSize = ref(10)
const total = ref(0)

async function loadData() {
  loading.value = true
  try {
    const res = await getBorrows({ page: page.value, pageSize: pageSize.value })
    records.value = res.data.items
    total.value = res.data.total
  } finally {
    loading.value = false
  }
}

async function handleReturn(record) {
  try {
    await ElMessageBox.confirm('确认归还该图书？', '提示', { type: 'info' })
  } catch { return }

  try {
    await returnBook(record.id)
    ElMessage.success('归还成功')
    loadData()
  } catch { /* handled */ }
}

function formatDate(date) {
  return date ? new Date(date).toLocaleString('zh-CN') : '-'
}

onMounted(loadData)
</script>

<style scoped>
.card-title { font-weight: bold; }
.pagination { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>
