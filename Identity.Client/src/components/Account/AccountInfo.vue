<script setup lang="ts">
import { UserDto } from '@/resources/api-clients/identity-api-client'

const userStore = useUserStore()

const user = ref<UserDto>()
watch(() => userStore.user, (newUser) => {
  if (newUser) {
    user.value = new UserDto({ ...newUser })
  }
}, { immediate: true })

const handleUpdate = async () => {
  if (user.value) {
    await userStore.updateUser(user.value)
  }
}
</script>

<template>
  <Card
    v-if="user"
    class="account-card account-info-card"
  >
    <template #title>
      <h1>Account Information</h1>
    </template>
    <template #content>
      <div class="info-section">
        <p class="section-description">
          Manage your personal information and account preferences.
        </p>

        <div class="fields">
          <div class="field">
            <label for="email">Email Address</label>
            <InputText
              id="email"
              :value="user.email"
              readonly
              disabled
            />
            <small class="field-hint">Your email address cannot be changed.</small>
          </div>

          <div class="field">
            <label for="roles">Roles</label>

            <div class="flex">
              <Tag
                v-for="role in userStore.roles"
                :key="role"
                :value="role"
              />
            </div>
          </div>

          <div class="field">
            <label for="username">Username</label>
            <InputText
              id="username"
              v-model="user.userName"
            />
            <small class="field-hint">This is how others will see you on the platform.</small>
          </div>

          <!-- Joined date field removed as it's not available in UserDto -->
        </div>

        <div class="actions">
          <Button
            label="Save Changes"
            icon="fa-duotone fa-check"
            severity="success"
            :loading="userStore.isLoading"
            @click="handleUpdate"
          />
        </div>
      </div>
    </template>
  </Card>

  <Card
    v-else
    class="account-info-card"
  >
    <template #content>
      <div class="loading-state">
        <ProgressSpinner />
        <p>Loading account information...</p>
      </div>
    </template>
  </Card>
</template>

<style lang="scss" scoped>
@use './account-shared.scss' as *;



.info-section {
  gap: 2rem;
}
</style>
