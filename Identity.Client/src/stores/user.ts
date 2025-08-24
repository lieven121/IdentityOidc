import { UserDto, UsersClient } from '@/resources/api-clients/identity-api-client'
import { defineStore } from 'pinia'

export const useUserStore = defineStore('user', () => {
  const usersClient = new UsersClient()
  const user: Ref<UserDto | null> = ref(null)

  const router = useRouter()

  const isLoading = computed(() => {
    if (user.value === null) return true
    if (isUpdating.value) return true
    return false
  })

  async function loadUser() {
    user.value = await usersClient.currentUser()
  }

  async function logout() {
    router.push('/logout')
  }

  const isUpdating = ref(false)
  async function updateUser(updatedUser: UserDto) {
    if (isUpdating.value) return
    isUpdating.value = true
    try {
      user.value = await usersClient.updateUser(updatedUser)
    } finally {
      isUpdating.value = false
    }
  }

  const states = {
    isAuthenticated: computed(() => user.value?.email != null),
    user: readonly(user),
    isLoading,
  }

  const functions = { loadUser, logout, updateUser }

  return { ...states, ...functions }
})
