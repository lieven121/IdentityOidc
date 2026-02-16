import { UserDto, UsersClient } from '@/resources/api-clients/identity-api-client'
import { RouteNames } from '@/router/routes'
import { defineStore } from 'pinia'

export const useUserStore = defineStore('user', () => {
  const usersClient = new UsersClient()
  const user: Ref<UserDto | null> = ref(null)
  const roles = ref<string[] | null>(null)

  const router = useRouter()

  const isLoading = computed(() => {
    if (user.value === null) return true
    if (roles.value === null) return true
    if (isUpdating.value) return true
    return false
  })

  async function loadUser() {
    user.value = await usersClient.currentUser()
    if (states.isAuthenticated.value) {
      roles.value = (await usersClient.getCurrentUserRoles()) ?? []
    }
  }

  const isAdmin = computed(() => {
    if (roles.value === null) return false
    return roles.value?.includes('Admin') ?? false
  })

  async function logout() {
    router.push({ name: RouteNames.Logout })
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
    roles: readonly(roles),
    isAdmin,
    isLoading,
  }

  const functions = { loadUser, logout, updateUser }

  return { ...states, ...functions }
})
