<template>
  <div>
    <el-card>
      <template #header>
        <div class="card-header">
          <span>用户管理</span>
          <el-button type="primary" @click="$router.push('/admin/users/create')">
            <el-icon><Plus /></el-icon>新 增
          </el-button>
        </div>
      </template>
      <div class="toolbar">
        <el-input v-model="keyword" placeholder="搜索用户名/姓名" clearable style="width: 250px" @keyup.enter="search" />
        <el-select v-model="role" placeholder="全部角色" clearable style="width: 140px; margin-left: 12px" @change="search">
          <el-option label="管理员" value="Admin" />
          <el-option label="读者" value="Reader" />
        </el-select>
        <el-button type="primary" @click="search" style="margin-left: 12px">搜索</el-button>
      </div>
      <el-table :data="users" v-loading="loading" stripe style="margin-top: 16px">
        <el-table-column prop="username" label="用户名" width="140" />
        <el-table-column prop="name" label="姓名" width="120" />
        <el-table-column prop="phone" label="电话" width="140" />
        <el-table-column prop="email" label="邮箱" min-width="160" />
        <el-table-column label="角色" width="90">
          <template #default="scope">
            <el-tag :type="scope.row.role === 'Admin' ? 'danger' : 'primary'" size="small">
              {{ scope.row.role === 'Admin' ? '管理员' : '读者' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="80">
          <template #default="scope">
            <el-tag :type="scope.row.isActive ? 'success' : 'info'" size="small">
              {{ scope.row.isActive ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="scope">
            <el-button size="small" @click="$router.push(`/admin/users/${scope.row.id}/edit`)">编辑</el-button>
            <el-button size="small" type="danger" @click="handleDelete(scope.row)">删除</el-button>
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
import { getUsers, deleteUser } from '../../api/users'

const users = ref([])
const loading = ref(false)
const keyword = ref('')
const role = ref('')
const page = ref(1)
const pageSize = ref(10)
const total = ref(0)

async function loadData() {
  loading.value = true
  try {
    const res = await getUsers({ page: page.value, pageSize: pageSize.value, keyword: keyword.value, role: role.value })
    users.value = res.data.items
    total.value = res.data.total
  } finally {
    loading.value = false
  }
}

function search() { page.value = 1; loadData() }

async function handleDelete(row) {
  try {
    await ElMessageBox.confirm(`确认删除用户「${row.username}」？`, '警告', { type: 'warning' })
  } catch { return }

  try {
    await deleteUser(row.id)
    ElMessage.success('删除成功')
    loadData()
  } catch { /* handled */ }
}

onMounted(loadData)
</script>

<style scoped>
.card-header { display: flex; justify-content: space-between; align-items: center; }
.toolbar { display: flex; align-items: center; }
.pagination { margin-top: 16px; display: flex; justify-content: flex-end; }
</style>
