<script setup lang="ts">
import { AdminClient, ResultPageOfUserDto, UserDto, UsersFilter, CreateUserDto, ResetPasswordDto } from '@/resources/api-clients/identity-api-client'

const adminClient = new AdminClient()
const users = ref<ResultPageOfUserDto | null>(null)
const isLoading = ref(false)
const selectedUser = ref<UserDto | null>(null)
const showUserDialog = ref(false)
const userRoles = ref<string[]>([])
const searchFilter = ref<UsersFilter>(new UsersFilter())
const isSearching = ref(false)

const showCreateUserDialog = ref(false)
const newUserForm = ref({
  email: '',
  username: '',
  password: ''
})

const showResetPasswordDialog = ref(false)
const resetPasswordForm = ref({
  newPassword: '',
  confirmPassword: ''
})

const isCreatingUser = ref(false)
const isResettingPassword = ref(false)
// const isUpdatingUserRoles = ref(false)

const canEdit = true

const loadUsers = async () => {
  isLoading.value = true
  try {
    if (searchFilter.value.email || searchFilter.value.username) {
      users.value = await adminClient.admin_GetUsersSearch(searchFilter.value)
    } else {
      users.value = await adminClient.admin_GetUsers()
    }
  } catch (error) {
    console.error('Failed to load users:', error)
  } finally {
    isLoading.value = false
  }
}

const handleSearch = async () => {
  await loadUsers()
}

const clearSearch = async () => {
  searchFilter.value = new UsersFilter()
  await loadUsers()
}

onMounted(() => {
  loadUsers()
})

const viewUser = async (user: UserDto) => {
  selectedUser.value = new UserDto({ ...user })
  try {
    userRoles.value = (await adminClient.admin_GetUserRolesById(user.id!)) || []
  } catch (error) {
    console.error('Failed to load user roles:', error)
    userRoles.value = []
  }
  showUserDialog.value = true
}

const openCreateUserDialog = () => {
  newUserForm.value = {
    email: '',
    username: '',
    password: ''
  }
  showCreateUserDialog.value = true
}

const handleCreateUser = async () => {
  if (!newUserForm.value.email || !newUserForm.value.username || !newUserForm.value.password) {
    return
  }

  isCreatingUser.value = true
  try {
    const createDto = new CreateUserDto({
      email: newUserForm.value.email,
      username: newUserForm.value.username,
      password: newUserForm.value.password
    })
    await adminClient.admin_CreateUser(createDto)
    showCreateUserDialog.value = false
    await loadUsers()
    newUserForm.value = { email: '', username: '', password: '' }
  } catch (error) {
    console.error('Failed to create user:', error)
  } finally {
    isCreatingUser.value = false
  }
}

const openResetPasswordDialog = () => {
  if (!selectedUser.value) return
  resetPasswordForm.value = {
    newPassword: '',
    confirmPassword: ''
  }
  showResetPasswordDialog.value = true
}

const handleResetPassword = async () => {
  if (!selectedUser.value) return

  if (resetPasswordForm.value.newPassword !== resetPasswordForm.value.confirmPassword) {
    alert('Passwords do not match')
    return
  }

  isResettingPassword.value = true
  try {
    const resetDto = new ResetPasswordDto({
      newPassword: resetPasswordForm.value.newPassword
    })
    await adminClient.admin_ResetPassword(selectedUser.value.id!, resetDto)
    showResetPasswordDialog.value = false
    alert('Password reset successfully')
  } catch (error) {
    console.error('Failed to reset password:', error)
    alert('Failed to reset password')
  } finally {
    isResettingPassword.value = false
  }
}

// const addUserToRole = async (roleName: string) => {
//   if (!selectedUser.value) return

//   isUpdatingUserRoles.value = true
//   try {
//     await adminClient.admin_AddUserToRoleById(selectedUser.value.id!, roleName)
//     userRoles.value.push(roleName)
//   } catch (error) {
//     console.error('Failed to add role:', error)
//   } finally {
//     isUpdatingUserRoles.value = false
//   }
// }

// const removeUserFromRole = async (roleName: string) => {
//   if (!selectedUser.value) return

//   isUpdatingUserRoles.value = true
//   try {
//     await adminClient.admin_RemoveUserFromRoleById(selectedUser.value.id!, roleName)
//     userRoles.value = userRoles.value.filter(r => r !== roleName)
//   } catch (error) {
//     console.error('Failed to remove role:', error)
//   } finally {
//     isUpdatingUserRoles.value = false
//   }
// }
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
            <h3 class="title">All Users</h3>
          </div>
          <Button
            v-if="canEdit"
            label="Add User"
            icon="fa-duotone fa-plus"
            severity="success"
            size="small"
            @click="openCreateUserDialog"
          />
        </div>

        <div class="search-bar">
          <div class="search-fields">
            <InputGroup>
              <InputGroupAddon>
                <i class="fa-duotone fa-magnifying-glass"></i>
              </InputGroupAddon>
              <InputText
                v-model="searchFilter.email"
                placeholder="Search by email..."
                style="width: 100%;"
              />
            </InputGroup>
            <InputGroup>
              <InputGroupAddon>
                <i class="fa-duotone fa-at"></i>
              </InputGroupAddon>
              <InputText
                v-model="searchFilter.username"
                placeholder="Search by username..."
                style="width: 100%;"
              />
            </InputGroup>
            <Button
              label="Search"
              icon="fa-duotone fa-magnifying-glass"
              @click="handleSearch"
              :loading="isSearching"
            />
            <Button
              label="Clear"
              icon="fa-duotone fa-xmark"
              text
              @click="clearSearch"
            />
          </div>
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
            field="username"
            header="Username"
            sortable
          >
            <template #body="{ data }">
              <div class="user-cell">
                <Avatar
                  :label="data.username?.[0]?.toUpperCase()"
                  shape="circle"
                  style="background-color: var(--primary-color);"
                />
                <span class="username">{{ data.username }}</span>
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
    :header="selectedUser?.username"
    :modal="true"
    :style="{ width: '50rem' }"
    :breakpoints="{ '960px': '75vw', '640px': '90vw' }"
  >
    <div
      v-if="selectedUser"
      class="user-details"
    >
      <div class="detail-section">
        <h4>Account Information</h4>
        <div class="detail-row">
          <label>Email:</label>
          <span>{{ selectedUser.email }}</span>
        </div>
        <div class="detail-row">
          <label>Username:</label>
          <span>{{ selectedUser.username }}</span>
        </div>
      </div>

      <div class="detail-section">
        <h4>Roles</h4>
        <div class="tags-section">
          <Tag
            v-for="role in userRoles"
            :key="role"
            :value="role"
            :closable="canEdit"
          />
        </div>
      </div>

      <div class="actions-section">
        <Button
          label="Reset Password"
          icon="fa-duotone fa-lock"
          severity="warning"
          @click="openResetPasswordDialog"
        />
      </div>
    </div>
  </Dialog>

  <!-- Create User Dialog -->
  <Dialog
    v-model:visible="showCreateUserDialog"
    header="Create New User"
    :modal="true"
    :style="{ width: '50rem' }"
    :breakpoints="{ '960px': '75vw', '640px': '90vw' }"
  >
    <div class="create-user-form">
      <div class="field">
        <label for="new-email">Email:</label>
        <InputText
          id="new-email"
          v-model="newUserForm.email"
          type="email"
          placeholder="user@example.com"
        />
      </div>
      <div class="field">
        <label for="new-username">Username:</label>
        <InputText
          id="new-username"
          v-model="newUserForm.username"
          placeholder="username"
        />
      </div>
      <div class="field">
        <label for="new-password">Password:</label>
        <Password
          id="new-password"
          v-model="newUserForm.password"
          toggleMask
          placeholder="Enter password"
        />
      </div>
    </div>
    <template #footer>
      <Button
        label="Cancel"
        icon="fa-duotone fa-xmark"
        @click="showCreateUserDialog = false"
        text
      />
      <Button
        label="Create"
        icon="fa-duotone fa-plus"
        @click="handleCreateUser"
        :loading="isCreatingUser"
      />
    </template>
  </Dialog>

  <!-- Reset Password Dialog -->
  <Dialog
    v-model:visible="showResetPasswordDialog"
    header="Reset Password"
    :modal="true"
    :style="{ width: '50rem' }"
    :breakpoints="{ '960px': '75vw', '640px': '90vw' }"
  >
    <div class="reset-password-form">
      <div class="field">
        <label for="reset-new-password">New Password:</label>
        <Password
          id="reset-new-password"
          v-model="resetPasswordForm.newPassword"
          toggleMask
          placeholder="Enter new password"
        />
      </div>
      <div class="field">
        <label for="reset-confirm-password">Confirm Password:</label>
        <Password
          id="reset-confirm-password"
          v-model="resetPasswordForm.confirmPassword"
          toggleMask
          placeholder="Confirm password"
        />
      </div>
    </div>
    <template #footer>
      <Button
        label="Cancel"
        icon="fa-duotone fa-xmark"
        @click="showResetPasswordDialog = false"
        text
      />
      <Button
        label="Reset"
        icon="fa-duotone fa-check"
        @click="handleResetPassword"
        :loading="isResettingPassword"
      />
    </template>
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
  gap: 0.2rem;
}

.subsection-header {
  padding: 0;
}

.search-bar {
  width: 100%;
  position: sticky;
  top: 70px;
  background: var(--surface-card);
  z-index: 1;
  padding: 0.5rem 0;

  .search-fields {
    display: flex;
    gap: 0.5rem;
    flex-wrap: wrap;

    >* {
      flex: 1;
      min-width: 150px;
    }
  }
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

.user-details {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;

  .detail-section {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;

    h4 {
      margin: 0;
      font-size: 1rem;
      font-weight: 600;
      color: var(--text-color);
    }

    .detail-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.5rem 0;
      border-bottom: 1px solid var(--surface-border);

      label {
        font-weight: 600;
        color: var(--text-color-secondary);
      }

      span {
        color: var(--text-color);
      }
    }

    .tags-section {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
    }
  }

  .actions-section {
    display: flex;
    gap: 0.5rem;
    padding-top: 1rem;
    border-top: 1px solid var(--surface-border);
  }
}

.create-user-form,
.reset-password-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;

  .field {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;

    label {
      font-weight: 600;
      font-size: 0.9rem;
    }
  }
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
