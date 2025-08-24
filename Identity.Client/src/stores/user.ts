import { UserDto, UsersClient } from '@/resources/api-clients/identity-api-client'
import { defineStore } from 'pinia'

export const useUserStore = defineStore('user', () => {
  const usersClient = new UsersClient()
  const user: Ref<UserDto | null> = ref(null)

  const router = useRouter()

  async function loadUser() {
    user.value = await usersClient.currentUser()
  }

  async function logout() {
    router.push('/logout')
  }

  async function updateUser(updatedUser: UserDto) {
    console.log('Updating user:', updatedUser)
    user.value = await usersClient.updateUser(updatedUser)
  }

  const states = {
    isAuthenticated: computed(() => user.value?.email != null),
    user: readonly(user),
  }

  const functions = { loadUser, logout, updateUser }

  return { ...states, ...functions }
})
