<template>
  <div>
    <el-card>
      <template #header>
        <div class="card-header">
          <span>图书列表</span>
        </div>
      </template>
      <div class="toolbar">
        <el-input v-model="keyword" placeholder="搜索书名/作者/ISBN" clearable style="width: 280px" @keyup.enter="search" />
        <el-select v-model="category" placeholder="全部分类" clearable style="width: 160px; margin-left: 12px" @change="search">
          <el-option v-for="c in categories" :key="c" :label="c" :value="c" />
        </el-select>
        <el-button type="primary" @click="search" style="margin-left: 12px">搜索</el-button>
      </div>
      <el-table :data="books" v-loading="loading" stripe style="margin-top: 16px">
        <el-table-column prop="isbn" label="ISBN" width="180" />
        <el-table-column prop="title" label="书名" min-width="180" />
        <el-table-column prop="author" label="作者" width="120" />
        <el-table-column prop="category" label="分类" width="100" />
        <el-table-column prop="publisher" label="出版社" width="140" />
        <el-table-column label="可借/总数" width="100">
          <template #default="scope">
            <span :style="{ color: scope.row.availableCopies > 0 ? '#67C23A' : '#F56C6C' }">
              {{ scope.row.availableCopies }}/{{ scope.row.totalCopies }}
            </span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="scope">
            <el-button type="primary" size="small" :disabled="scope.row.availableCopies <= 0" @click="handleBorrow(scope.row)">
              {{ scope.row.availableCopies > 0 ? '借阅' : '已借完' }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="pagination">
        <el-pagination
          v-model:current-page="page"
          v-model:page-size="pageSize"
          :total="total"
          :page-sizes="[10, 20, 50]"
          layout="total, sizes, prev, pager, next"
          @change="loadData"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getBooks, getCategories } from '../api/books'
import { borrowBook } from '../api/borrows'

const books = ref([])
const categories = ref([])
const loading = ref(false)
const keyword = ref('')
const category = ref('')
const page = ref(1)
const pageSize = ref(10)
const total = ref(0)

async function loadData() {
  loading.value = true
  try {
    const res = await getBooks({ page: page.value, pageSize: pageSize.value, keyword: keyword.value, category: category.value })
    books.value = res.data.items
    total.value = res.data.total
  } finally {
    loading.value = false
  }
}

function search() {
  page.value = 1
  loadData()
}

async function handleBorrow(book) {
  try {
    await borrowBook({ bookId: book.id })
    ElMessage.success('借阅成功')
    loadData()
  } catch { /* handled */ }
}

onMounted(async () => {
  loadData()
  try {
    const res = await getCategories()
    categories.value = res.data
  } catch { /* handled */ }
})
</script>

<style scoped>
.card-header { display: flex; justify-content: space-between; align-items: center; }
.toolbar { display: flex; align-items: center; }
.pagination { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>
