<script setup lang="ts">
interface Role {
  id: string
  name: string
  description: string
  userCount: number
  permissions: string[]
  isDefault?: boolean
}

const roles = ref<Role[]>([
  {
    id: '1',
    name: 'Admin',
    description: 'Full system access with all administrative privileges',
    userCount: 3,
    permissions: ['users.manage', 'roles.manage', 'applications.manage', 'system.configure'],
    isDefault: false
  },
  {
    id: '2',
    name: 'User',
    description: 'Standard user access',
    userCount: 47,
    permissions: ['profile.read', 'profile.update'],
    isDefault: true
  },
  {
    id: '3',
    name: 'Moderator',
    description: 'Can moderate content and manage basic user actions',
    userCount: 5,
    permissions: ['users.read', 'content.moderate'],
    isDefault: false
  }
])

const selectedRole = ref<Role | null>(null)
const showRoleDialog = ref(false)
// const isLoading = ref(false)

const viewRole = (role: Role) => {
  selectedRole.value = { ...role }
  showRoleDialog.value = true
}

const createRole = () => {
  selectedRole.value = {
    id: '',
    name: '',
    description: '',
    userCount: 0,
    permissions: []
  }
  showRoleDialog.value = true
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
        </p>

        <template
          v-for="(role, index) in roles"
          :key="role.id"
        >
          <Divider v-if="index > 0" />

          <div class="role-section">
            <div class="subsection-header">
              <div class="subsection-title">
                <h3>{{ role.name }}</h3>
                <Tag
                  v-if="role.isDefault"
                  value="Default"
                  severity="info"
                />
              </div>
              <div class="subsection-actions">
                <Button
                  icon="fa-duotone fa-eye"
                  text
                  rounded
                  severity="info"
                  @click="viewRole(role)"
                  v-tooltip.top="'View Details'"
                />
                <Button
                  icon="fa-duotone fa-pen"
                  text
                  rounded
                  severity="warning"
                  @click="() => { }"
                  v-tooltip.top="'Edit'"
                />
                <Button
                  v-if="!role.isDefault"
                  icon="fa-duotone fa-trash"
                  text
                  rounded
                  severity="danger"
                  @click="() => { }"
                  v-tooltip.top="'Delete'"
                />
              </div>
            </div>

            <div class="role-details-section">
              <p class="role-description">{{ role.description }}</p>

              <div class="role-stats">
                <div class="stat">
                  <i class="fa-duotone fa-users"></i>
                  <span>{{ role.userCount }} users</span>
                </div>
                <div class="stat">
                  <i class="fa-duotone fa-shield"></i>
                  <span>{{ role.permissions.length }} permissions</span>
                </div>
              </div>

              <div class="permissions-preview">
                <h4>Permissions</h4>
                <div class="permissions-list">
                  <Chip
                    v-for="(permission, permIndex) in role.permissions.slice(0, 4)"
                    :key="permIndex"
                    :label="permission"
                    class="permission-chip"
                  />
                  <Chip
                    v-if="role.permissions.length > 4"
                    :label="`+${role.permissions.length - 4} more`"
                    class="permission-chip more"
                  />
                </div>
              </div>
            </div>
          </div>
        </template>
      </div>
    </template>
  </Card>

  <!-- Role Details Dialog -->
  <Dialog
    v-model:visible="showRoleDialog"
    :header="selectedRole?.name || 'New Role'"
    :modal="true"
    :style="{ width: '50rem' }"
    :breakpoints="{ '960px': '75vw', '640px': '90vw' }"
  >
    <div
      v-if="selectedRole"
      class="role-details"
    >
      <div class="detail-section">
        <label>Role Name</label>
        <InputText
          v-model="selectedRole.name"
          disabled
        />
      </div>

      <div class="detail-section">
        <label>Description</label>
        <Textarea
          v-model="selectedRole.description"
          disabled
          rows="3"
        />
      </div>

      <div class="detail-section">
        <label>Permissions ({{ selectedRole.permissions.length }})</label>
        <div class="permissions-full-list">
          <Chip
            v-for="(permission, index) in selectedRole.permissions"
            :key="index"
            :label="permission"
            class="permission-chip"
          />
        </div>
      </div>

      <div class="detail-section">
        <label>Statistics</label>
        <div class="stats-grid">
          <div class="stat-item">
            <i class="fa-duotone fa-users"></i>
            <div>
              <span class="stat-value">{{ selectedRole.userCount }}</span>
              <span class="stat-label">Users</span>
            </div>
          </div>
          <div class="stat-item">
            <i class="fa-duotone fa-shield"></i>
            <div>
              <span class="stat-value">{{ selectedRole.permissions.length }}</span>
              <span class="stat-label">Permissions</span>
            </div>
          </div>
        </div>
      </div>
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

.role-section {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.role-details-section {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding-bottom: 0.5rem;
}

.role-description {
  margin: 0;
  color: var(--text-color-secondary);
  font-size: 0.9rem;
}

.role-stats {
  display: flex;
  gap: 1.5rem;

  .stat {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: var(--text-color-secondary);
    font-size: 0.9rem;

    i {
      font-size: 1rem;
    }
  }
}

.permissions-preview {
  h4 {
    margin: 0 0 0.5rem 0;
    font-size: 0.9rem;
    color: var(--text-color-secondary);
  }

  .permissions-list {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;

    .permission-chip {
      font-size: 0.75rem;

      &.more {
        background: var(--surface-200);
      }
    }
  }
}

.role-details {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;

  .permissions-full-list {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
    padding: 1rem;
    background: var(--surface-50);
    border-radius: var(--border-radius);

    .permission-chip {
      font-size: 0.85rem;
    }
  }
}

@media (max-width: 768px) {
  .roles-grid {
    grid-template-columns: 1fr;
  }
}
</style>
