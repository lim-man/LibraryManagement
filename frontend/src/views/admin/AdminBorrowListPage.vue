<template>
  <div>
    <el-card>
      <template #header><span class="card-title">借阅管理</span></template>
      <div class="toolbar">
        <el-select v-model="status" placeholder="全部状态" clearable style="width: 130px" @change="search">
          <el-option label="借阅中" value="Borrowed" />
          <el-option label="已归还" value="Returned" />
        </el-select>
        <el-button type="primary" @click="search" style="margin-left: 12px">搜索</el-button>
        <div style="margin-left: auto">
          <el-button type="success" @click="dialogVisible = true">
            <el-icon><Plus /></el-icon>借 书
          </el-button>
        </div>
      </div>
      <el-table :data="records" v-loading="loading" stripe style="margin-top: 16px">
        <el-table-column prop="id" label="编号" width="70" />
        <el-table-column prop="userName" label="读者" width="100" />
        <el-table-column prop="bookTitle" label="图书" min-width="160" />
        <el-table-column prop="bookISBN" label="ISBN" width="180" />
        <el-table-column label="借书日期" width="160">
          <template #default="scope">{{ formatDate(scope.row.borrowDate) }}</template>
        </el-table-column>
        <el-table-column label="应还日期" width="160">
          <template #default="scope">{{ formatDate(scope.row.dueDate) }}</template>
        </el-table-column>
        <el-table-column label="状态" width="90">
          <template #default="scope">
            <el-tag :type="scope.row.status === 'Borrowed' ? 'warning' : 'success'" size="small">
              {{ scope.row.status === 'Borrowed' ? '借阅中' : '已归还' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="80" fixed="right">
          <template #default="scope">
            <el-button v-if="scope.row.status === 'Borrowed'" size="small" type="primary" @click="handleReturn(scope.row)">归还</el-button>
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

    <el-dialog v-model="dialogVisible" title="借书" width="450px">
      <el-form ref="borrowFormRef" :model="borrowForm" :rules="borrowRules" label-width="80px">
        <el-form-item label="读者" prop="userId">
          <el-select v-model="borrowForm.userId" placeholder="请选择读者" filterable style="width: 100%">
            <el-option v-for="u in readers" :key="u.id" :label="`${u.name} (${u.username})`" :value="u.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="图书" prop="bookId">
          <el-select v-model="borrowForm.bookId" placeholder="请选择图书" filterable style="width: 100%">
            <el-option v-for="b in allBooks" :key="b.id" :label="`${b.title} - ${b.author} (可借:${b.availableCopies})`" :value="b.id" :disabled="b.availableCopies <= 0" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleBorrow" :loading="borrowing">确认借书</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getBorrows, borrowBook, returnBook } from '../../api/borrows'
import { getUsers } from '../../api/users'
import { getBooks } from '../../api/books'

const records = ref([])
const loading = ref(false)
const status = ref('')
const page = ref(1)
const pageSize = ref(10)
const total = ref(0)

const dialogVisible = ref(false)
const borrowing = ref(false)
const borrowFormRef = ref()
const readers = ref([])
const allBooks = ref([])
const borrowForm = ref({ userId: null, bookId: null })
const borrowRules = {
  userId: [{ required: true, message: '请选择读者', trigger: 'change' }],
  bookId: [{ required: true, message: '请选择图书', trigger: 'change' }]
}

async function loadData() {
  loading.value = true
  try {
    const res = await getBorrows({ page: page.value, pageSize: pageSize.value, status: status.value })
    records.value = res.data.items
    total.value = res.data.total
  } finally { loading.value = false }
}

function search() { page.value = 1; loadData() }

async function handleReturn(row) {
  try {
    await returnBook(row.id)
    ElMessage.success('归还成功')
    loadData()
  } catch { /* handled */ }
}

async function handleBorrow() {
  const valid = await borrowFormRef.value?.validate().catch(() => false)
  if (!valid) return

  borrowing.value = true
  try {
    await borrowBook({ userId: borrowForm.value.userId, bookId: borrowForm.value.bookId })
    ElMessage.success('借书成功')
    dialogVisible.value = false
    borrowForm.value = { userId: null, bookId: null }
    loadData()
  } catch { /* handled */ }
  finally { borrowing.value = false }
}

function formatDate(date) {
  return date ? new Date(date).toLocaleString('zh-CN') : '-'
}

onMounted(async () => {
  loadData()
  try {
    const [uRes, bRes] = await Promise.all([getUsers({ pageSize: 100, role: 'Reader' }), getBooks({ pageSize: 100 })])
    readers.value = uRes.data.items
    allBooks.value = bRes.data.items
  } catch { /* handled */ }
})
</script>

<style scoped>
.card-title { font-weight: bold; }
.toolbar { display: flex; align-items: center; }
.pagination { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>
