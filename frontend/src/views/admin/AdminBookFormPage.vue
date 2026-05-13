<template>
  <div>
    <el-card>
      <template #header>
        <span>{{ isEdit ? '编辑图书' : '新增图书' }}</span>
      </template>
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px" style="max-width: 700px">
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="ISBN" prop="isbn">
              <el-input v-model="form.isbn" placeholder="请输入ISBN号" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="书名" prop="title">
              <el-input v-model="form.title" placeholder="请输入书名" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="作者" prop="author">
              <el-input v-model="form.author" placeholder="请输入作者" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="出版社">
              <el-input v-model="form.publisher" placeholder="请输入出版社" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="分类">
              <el-input v-model="form.category" placeholder="如：计算机、文学" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="价格">
              <el-input-number v-model="form.price" :min="0" :precision="2" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="总册数" prop="totalCopies">
              <el-input-number v-model="form.totalCopies" :min="1" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="存放位置">
              <el-input v-model="form.location" placeholder="如：A区-1排-1层" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="封面图URL">
          <el-input v-model="form.coverImageUrl" placeholder="请输入封面图片URL（可选）" />
        </el-form-item>
        <el-form-item label="简介">
          <el-input v-model="form.description" type="textarea" :rows="3" placeholder="请输入图书简介" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSubmit" :loading="submitting">保 存</el-button>
          <el-button @click="$router.back()">取 消</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { getBook, createBook, updateBook } from '../../api/books'

const route = useRoute()
const router = useRouter()
const formRef = ref()
const submitting = ref(false)

const isEdit = computed(() => !!route.params.id)

const rules = {
  isbn: [{ required: true, message: '请输入ISBN', trigger: 'blur' }],
  title: [{ required: true, message: '请输入书名', trigger: 'blur' }],
  author: [{ required: true, message: '请输入作者', trigger: 'blur' }],
  totalCopies: [{ required: true, message: '请输入总册数', trigger: 'blur' }]
}

const form = reactive({
  isbn: '',
  title: '',
  author: '',
  publisher: '',
  category: '',
  price: null,
  totalCopies: 1,
  location: '',
  coverImageUrl: '',
  description: ''
})

onMounted(async () => {
  if (isEdit.value) {
    try {
      const res = await getBook(route.params.id)
      Object.assign(form, res.data)
    } catch { router.back() }
  }
})

async function handleSubmit() {
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  submitting.value = true
  try {
    if (isEdit.value) {
      await updateBook(route.params.id, form)
      ElMessage.success('更新成功')
    } else {
      await createBook(form)
      ElMessage.success('新增成功')
    }
    router.push('/admin/books')
  } catch { /* handled */ }
  finally { submitting.value = false }
}
</script>

