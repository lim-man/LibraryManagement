<template>
  <div>
    <el-card>
      <template #header>
        <div class="card-header">
          <span>图书管理</span>
          <el-button type="primary" @click="$router.push('/admin/books/create')">
            <el-icon><Plus /></el-icon>新 增
          </el-button>
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
        <el-table-column prop="title" label="书名" min-width="160" />
        <el-table-column prop="author" label="作者" width="120" />
        <el-table-column prop="category" label="分类" width="100" />
        <el-table-column prop="totalCopies" label="总数" width="70" />
        <el-table-column prop="availableCopies" label="可借" width="70" />
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="scope">
            <el-button size="small" @click="$router.push(`/admin/books/${scope.row.id}/edit`)">编辑</el-button>
            <el-button size="small" type="danger" @click="handleDelete(scope.row)">删除</el-button>
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
import { ElMessage, ElMessageBox } from 'element-plus'
import { getBooks, getCategories, deleteBook } from '../../api/books'

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

function search() { page.value = 1; loadData() }

async function handleDelete(row) {
  try {
    await ElMessageBox.confirm(`确认删除《${row.title}》？`, '警告', { type: 'warning' })
  } catch { return }

  try {
    await deleteBook(row.id)
    ElMessage.success('删除成功')
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
