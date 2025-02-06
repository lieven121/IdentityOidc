import { UserDto, UsersClient } from '@/resources/api-clients/identity-api-client'
import { defineStore } from 'pinia'

export const useUserStore = defineStore('user', () => {
  const usersClient = new UsersClient()
  const user: Ref<UserDto | null> = ref(null)

  const router = useRouter()

  async function loadUser() {
    user.value = await usersClient.me()
  }

  async function logout() {
    router.push('/logout')
  }

  const states = {
    isAuthenticated: computed(() => user.value?.email != null),
    user: readonly(user),
  }

  const functions = { loadUser, logout }

  return { ...states, ...functions }
})
