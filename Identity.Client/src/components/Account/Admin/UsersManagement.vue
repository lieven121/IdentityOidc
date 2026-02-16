<script setup lang="ts">
import { ExternalClient, ResultPageOfUserDto, UserDto } from '@/resources/api-clients/identity-api-client'

const externalClient = new ExternalClient()
const users = ref<ResultPageOfUserDto | null>(null)
const isLoading = ref(false)
const selectedUser = ref<UserDto | null>(null)
const showUserDialog = ref(false)

const loadUsers = async () => {
  isLoading.value = true
  try {
    users.value = await externalClient.external_GetUsers()
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  loadUsers()
})

const viewUser = (user: UserDto) => {
  selectedUser.value = user
  showUserDialog.value = true
}
</script>

<template>
  <Card class="account-card users-card">
    <template #title>
      <div class="card-header">
        <div class="title-section">
          <i class="fa-duotone fa-users"></i>
          <h1>User Management</h1>
        </div>
      </div>
    </template>
    <template #content>
      <div class="users-content">
        <p class="section-description">
          Manage user accounts, permissions, and access across the platform.
        </p>

        <Divider />

        <div class="subsection-header">
          <div class="subsection-title">
            <h3>All Users</h3>
          </div>
          <Button
            label="Add User"
            icon="fa-duotone fa-plus"
            severity="success"
            size="small"
            @click="() => { }"
          />
        </div>

        <div class="search-bar">
          <IconField>
            <InputIcon class="fa-duotone fa-magnifying-glass" />
            <InputText
              placeholder="Search users by name or email..."
              style="width: 100%;"
            />
          </IconField>
        </div>

        <div
          v-if="isLoading"
          class="loading-state"
        >
          <ProgressSpinner />
          <p>Loading users...</p>
        </div>

        <DataTable
          v-else-if="users?.items"
          :value="users.items"
          paginator
          :rows="10"
          :rowsPerPageOptions="[5, 10, 20, 50]"
          stripedRows
          class="users-table"
          responsiveLayout="scroll"
        >
          <Column
            field="userName"
            header="Username"
            sortable
          >
            <template #body="{ data }">
              <div class="user-cell">
                <Avatar
                  :label="data.userName?.[0]?.toUpperCase()"
                  shape="circle"
                  style="background-color: var(--primary-color);"
                />
                <span class="username">{{ data.userName }}</span>
              </div>
            </template>
          </Column>
          <Column
            field="email"
            header="Email"
            sortable
          />
          <!-- Roles column removed as roles property is not available in UserDto -->
          <Column header="Actions">
            <template #body="{ data }">
              <div class="action-buttons">
                <Button
                  icon="fa-duotone fa-eye"
                  text
                  rounded
                  severity="info"
                  @click="viewUser(data)"
                  v-tooltip.top="'View Details'"
                />
                <Button
                  icon="fa-duotone fa-pen"
                  text
                  rounded
                  severity="warning"
                  @click="() => { }"
                  v-tooltip.top="'Edit User'"
                />
                <Button
                  icon="fa-duotone fa-trash"
                  text
                  rounded
                  severity="danger"
                  @click="() => { }"
                  v-tooltip.top="'Delete User'"
                />
              </div>
            </template>
          </Column>
        </DataTable>

        <div
          v-else
          class="empty-state"
        >
          <i class="fa-duotone fa-users"></i>
          <p>No users found</p>
        </div>
      </div>
    </template>
  </Card>

  <!-- User Details Dialog -->
  <Dialog
    v-model:visible="showUserDialog"
    :header="selectedUser?.userName"
    :modal="true"
    :style="{ width: '50rem' }"
    :breakpoints="{ '960px': '75vw', '640px': '90vw' }"
  >
    <div
      v-if="selectedUser"
      class="user-details"
    >
      <div class="user-details">
        <div class="detail-row">
          <label>Email:</label>
          <span>{{ selectedUser.email }}</span>
        </div>
        <!-- Roles and joined date removed as these properties are not available in UserDto -->
      </div>
    </div>
  </Dialog>
</template>

<style lang="scss" scoped>
@use '../account-shared.scss' as *;

.users-card {
  height: 100%;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.users-content {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.search-bar {
  width: 100%;
  position: sticky;
  top: 70px;
  background: var(--surface-card);
  z-index: 1;
  padding: 0.5rem 0;
}

.user-cell {
  display: flex;
  align-items: center;
  gap: 0.75rem;

  .username {
    font-weight: 600;
  }
}

.roles-cell {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

@media (max-width: 768px) {
  .card-header.with-actions {
    flex-direction: column;
    align-items: stretch;

    button {
      width: 100%;
    }
  }
}
</style>
