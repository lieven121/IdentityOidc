<script setup lang="ts">
import {
  AdminClient,
  RoleInfo,
  RoleMemberDto,
  RoleNameDto,
  UserDto,
  UsersFilter,
} from '@/resources/api-clients/identity-api-client'
import { apiErrorMessage, useConfirmDialog } from '@/utils/dialogs'

const adminClient = new AdminClient()
const { ask, confirm } = useConfirmDialog()

const roles = ref<RoleInfo[]>([])
const isLoading = ref(false)
const errorMessage = ref<string | null>(null)
const busyRole = ref<string | null>(null)
const search = ref('')

const filteredRoles = computed(() => {
  const term = search.value.trim().toLowerCase()
  return term ? roles.value.filter((role) => role.name.toLowerCase().includes(term)) : roles.value
})

const selectedRole = ref<RoleInfo | null>(null)
const showRoleDialog = ref(false)
const members = ref<RoleMemberDto[]>([])
const isLoadingMembers = ref(false)
const memberError = ref<string | null>(null)
const busyMemberId = ref<string | null>(null)
const userQuery = ref<UserDto | string | null>(null)
const userSuggestions = ref<UserDto[]>([])

const loadRoles = async () => {
  isLoading.value = true
  try {
    roles.value = (await adminClient.admin_GetAllRoles()) ?? []
  } catch (error) {
    console.error('Failed to load roles:', error)
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  loadRoles()
})

const roleNameInput = { label: 'Role name', placeholder: 'Support' }

const createRole = async () => {
  errorMessage.value = null
  const name = await ask({
    title: 'Create Role',
    message: "Letters, digits, spaces, '.', '_' or '-'.",
    confirm: { text: 'Create', severity: 'success' },
    cancel: { text: 'Cancel', severity: 'secondary' },
    input: roleNameInput,
  })
  if (name === null) return

  try {
    await adminClient.admin_CreateRole(new RoleNameDto({ name }))
    await loadRoles()
  } catch (error) {
    errorMessage.value = apiErrorMessage(error, 'The role could not be created.')
  }
}

const renameRole = async (role: RoleInfo) => {
  errorMessage.value = null
  const name = await ask({
    title: `Rename "${role.name}"`,
    message:
      'Users keep the role under its new name, and applications that require it are updated. Signed-in users pick up the new name on their next token refresh.',
    confirm: { text: 'Rename', severity: 'primary' },
    cancel: { text: 'Cancel', severity: 'secondary' },
    input: roleNameInput,
  })
  if (name === null) return

  busyRole.value = role.id
  try {
    await adminClient.admin_RenameRole(role.name, new RoleNameDto({ name }))
    await loadRoles()
  } catch (error) {
    errorMessage.value = apiErrorMessage(error, 'The role could not be renamed.')
  } finally {
    busyRole.value = null
  }
}

const deleteRole = async (role: RoleInfo) => {
  errorMessage.value = null
  const confirmed = await confirm({
    title: 'Delete Role',
    message: role.userCount
      ? `Delete "${role.name}"? It is removed from ${role.userCount} user(s). This can't be undone.`
      : `Delete "${role.name}"? This can't be undone.`,
    confirm: { text: 'Yes, Delete', severity: 'danger' },
    cancel: { text: 'Cancel', severity: 'secondary' },
  })
  if (!confirmed) return

  busyRole.value = role.id
  try {
    await adminClient.admin_DeleteRole(role.name)
    await loadRoles()
  } catch (error) {
    errorMessage.value = apiErrorMessage(error, 'The role could not be deleted.')
  } finally {
    busyRole.value = null
  }
}

const loadMembers = async () => {
  if (!selectedRole.value) return
  isLoadingMembers.value = true
  try {
    members.value = (await adminClient.admin_GetRoleMembers(selectedRole.value.name)) ?? []
  } catch (error) {
    console.error('Failed to load role members:', error)
    members.value = []
  } finally {
    isLoadingMembers.value = false
  }
}

const viewRole = async (role: RoleInfo) => {
  selectedRole.value = role
  members.value = []
  memberError.value = null
  userQuery.value = null
  showRoleDialog.value = true
  await loadMembers()
}

const searchUsers = async (event: { query: string }) => {
  try {
    const page = await adminClient.admin_GetUsersSearch(
      new UsersFilter({ email: event.query, username: '', page: 0, pageSize: 10 }),
    )
    const memberIds = new Set(members.value.map((m) => m.id))
    userSuggestions.value = (page?.items ?? []).filter((u) => u.id && !memberIds.has(u.id))
  } catch (error) {
    console.error('Failed to search users:', error)
    userSuggestions.value = []
  }
}

const addMember = async () => {
  const user = userQuery.value
  if (!selectedRole.value || !user || typeof user === 'string' || !user.id) return
  memberError.value = null
  busyMemberId.value = user.id
  try {
    await adminClient.admin_AddUserToRoleById(user.id, selectedRole.value.name)
    userQuery.value = null
    await Promise.all([loadMembers(), loadRoles()])
  } catch (error) {
    memberError.value = apiErrorMessage(error, 'The user could not be added to the role.')
  } finally {
    busyMemberId.value = null
  }
}

const removeMember = async (member: RoleMemberDto) => {
  if (!selectedRole.value) return
  memberError.value = null
  busyMemberId.value = member.id
  try {
    await adminClient.admin_RemoveUserFromRoleById(member.id, selectedRole.value.name)
    await Promise.all([loadMembers(), loadRoles()])
  } catch (error) {
    memberError.value = apiErrorMessage(error, 'The user could not be removed from the role.')
  } finally {
    busyMemberId.value = null
  }
}
</script>

<template>
  <Card class="account-card roles-card">
    <template #title>
      <div class="card-header with-actions">
        <div class="title-section">
          <i class="fa-duotone fa-key"></i>
          <h1>Role Management</h1>
        </div>
        <Button
          label="Create Role"
          icon="fa-duotone fa-plus"
          severity="success"
          @click="createRole"
        />
      </div>
    </template>
    <template #content>
      <div class="roles-content">
        <p class="section-description">
          Define and manage roles to control user permissions and access levels across the platform.
          Roles defined in configuration can't be renamed or deleted.
        </p>

        <InputGroup class="search-bar">
          <InputGroupAddon>
            <i class="fa-duotone fa-magnifying-glass"></i>
          </InputGroupAddon>
          <InputText v-model="search" placeholder="Search roles" />
          <InputGroupAddon v-if="search">
            <Button
              icon="fa-duotone fa-xmark"
              text
              rounded
              severity="secondary"
              @click="search = ''"
            />
          </InputGroupAddon>
        </InputGroup>

        <Message v-if="errorMessage" severity="error" :closable="false" class="error-message">
          {{ errorMessage }}
        </Message>

        <div v-if="isLoading && roles.length === 0" class="loading-state">
          <ProgressSpinner />
          <p>Loading roles...</p>
        </div>

        <template v-else>
          <template v-for="(role, index) in filteredRoles" :key="role.id">
            <Divider v-if="index > 0" />

            <div class="role-section">
              <div class="subsection-header">
                <div class="subsection-title">
                  <h3>{{ role.name }}</h3>
                  <span class="user-count">
                    <i class="fa-duotone fa-users"></i>
                    {{ role.userCount }} {{ role.userCount === 1 ? 'user' : 'users' }}
                  </span>
                  <Tag
                    v-if="role.isConfigManaged"
                    value="Managed by configuration"
                    severity="secondary"
                    icon="fa-duotone fa-lock"
                  />
                </div>
                <div class="subsection-actions">
                  <Button
                    icon="fa-duotone fa-users"
                    text
                    rounded
                    severity="info"
                    @click="viewRole(role)"
                    v-tooltip.top="'Members'"
                  />
                  <template v-if="!role.isConfigManaged">
                    <Button
                      icon="fa-duotone fa-pen"
                      text
                      rounded
                      severity="secondary"
                      :disabled="busyRole === role.id"
                      @click="renameRole(role)"
                      v-tooltip.top="'Rename'"
                    />
                    <Button
                      icon="fa-duotone fa-trash"
                      text
                      rounded
                      severity="danger"
                      :loading="busyRole === role.id"
                      @click="deleteRole(role)"
                      v-tooltip.top="'Delete'"
                    />
                  </template>
                </div>
              </div>
            </div>
          </template>

          <div v-if="filteredRoles.length === 0" class="empty-state">
            <i class="fa-duotone fa-key"></i>
            <p>{{ search ? `No roles match "${search}"` : 'No roles found' }}</p>
          </div>
        </template>
      </div>
    </template>
  </Card>

  <!-- Role Members Dialog -->
  <Dialog
    v-model:visible="showRoleDialog"
    :header="selectedRole ? `${selectedRole.name} members` : 'Role Members'"
    :modal="true"
    :style="{ width: '50rem' }"
    :breakpoints="{ '960px': '75vw', '640px': '90vw' }"
  >
    <div class="role-details">
      <Message v-if="memberError" severity="error" :closable="false">
        {{ memberError }}
      </Message>

      <div class="add-member">
        <AutoComplete
          v-model="userQuery"
          :suggestions="userSuggestions"
          option-label="email"
          placeholder="Search users by email"
          force-selection
          fluid
          @complete="searchUsers"
        >
          <template #option="{ option }">
            <div class="user-option">
              <span>{{ option.email }}</span>
              <small>{{ option.username }}</small>
            </div>
          </template>
        </AutoComplete>
        <Button
          label="Add"
          icon="fa-duotone fa-user-plus"
          :disabled="!userQuery || typeof userQuery === 'string'"
          :loading="
            !!busyMemberId && typeof userQuery === 'object' && userQuery?.id === busyMemberId
          "
          @click="addMember"
        />
      </div>

      <div v-if="isLoadingMembers" class="loading-state">
        <ProgressSpinner />
      </div>

      <ul v-else-if="members.length" class="member-list">
        <li v-for="member in members" :key="member.id">
          <div class="member-info">
            <span class="email">{{ member.email }}</span>
            <small>{{ member.username }}</small>
          </div>
          <Tag
            v-if="member.isConfigManaged"
            value="Configured user"
            severity="secondary"
            icon="fa-duotone fa-lock"
            v-tooltip.top="
              'Roles of users in OpenIddict:Users are re-synced from configuration on every start.'
            "
          />
          <Button
            v-else
            icon="fa-duotone fa-user-minus"
            text
            rounded
            severity="danger"
            :loading="busyMemberId === member.id"
            @click="removeMember(member)"
            v-tooltip.top="'Remove from role'"
          />
        </li>
      </ul>

      <p v-else class="no-members">No users have this role.</p>
    </div>
  </Dialog>
</template>

<style lang="scss" scoped>
@use '../account-shared.scss' as *;

.roles-card {
  height: 100%;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.roles-content {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.search-bar {
  margin: 1rem 0 0.5rem;
}

.error-message {
  margin: 1rem 0;
}

.role-section {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.user-count {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  color: var(--p-slate-500);
  font-size: 0.9rem;
}

.role-details {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.add-member {
  display: flex;
  gap: 0.5rem;

  :deep(.p-autocomplete) {
    flex: 1;
  }
}

.user-option,
.member-info {
  display: flex;
  flex-direction: column;

  small {
    color: var(--p-slate-500);
  }
}

.member-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;

  li {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.75rem;
    padding: 0.5rem 0;
    border-bottom: 1px solid var(--p-content-border-color);

    &:last-child {
      border-bottom: none;
    }
  }

  .member-info {
    flex: 1;
    min-width: 0;

    .email {
      overflow: hidden;
      text-overflow: ellipsis;
    }
  }
}

.no-members {
  margin: 0;
  color: var(--p-slate-500);
}
</style>
