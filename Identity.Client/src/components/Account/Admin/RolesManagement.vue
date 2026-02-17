<script setup lang="ts">
import { AdminClient, RoleInfo } from '@/resources/api-clients/identity-api-client'

const adminClient = new AdminClient()

interface RoleDetails extends RoleInfo {
  userCount?: number
}

const roles = ref<RoleDetails[]>([])
const isLoading = ref(false)
const selectedRole = ref<RoleDetails | null>(null)
const showRoleDialog = ref(false)

const canEdit = false

const loadRoles = async () => {
  isLoading.value = true
  try {
    const roleInfos = await adminClient.admin_GetAllRoles()
    if (roleInfos) {
      roles.value = roleInfos.map(role => (new RoleInfo({
        ...role,
      })))
    }
  } catch (error) {
    console.error('Failed to load roles:', error)
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  loadRoles()
})

const viewRole = (role: RoleDetails) => {
  selectedRole.value = new RoleInfo({
    ...role
  })
  showRoleDialog.value = true
}

const createRole = () => {
  selectedRole.value = new RoleInfo({
    id: '',
    name: '',
  })
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
          v-if="canEdit"
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

        <div
          v-if="isLoading"
          class="loading-state"
        >
          <ProgressSpinner />
          <p>Loading roles...</p>
        </div>

        <template v-else>
          <template
            v-for="(role, index) in roles"
            :key="role.id"
          >
            <Divider v-if="index > 0" />

            <div class="role-section">
              <div class="subsection-header">
                <div class="subsection-title">
                  <h3>{{ role.name }}</h3>
                </div>
                <div
                  class="subsection-actions"
                  v-if="canEdit"
                >
                  <Button
                    icon="fa-duotone fa-eye"
                    text
                    rounded
                    severity="info"
                    @click="viewRole(role)"
                    v-tooltip.top="'View Details'"
                  />
                </div>
              </div>

              <!-- <div class="role-details-section">
                <div class="role-stats">
                  <div class="stat">
                    <i class="fa-duotone fa-users"></i>
                    <span>{{ role.userCount || 0 }} users</span>
                  </div>
                </div>
              </div> -->
            </div>
          </template>

          <div
            v-if="roles.length === 0"
            class="empty-state"
          >
            <i class="fa-duotone fa-key"></i>
            <p>No roles found</p>
          </div>
        </template>
      </div>
    </template>
  </Card>

  <!-- Role Details Dialog -->
  <Dialog
    v-model:visible="showRoleDialog"
    :header="selectedRole?.name || 'Role Details'"
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
          :value="selectedRole.name"
          readonly
        />
      </div>

      <div class="detail-section">
        <label>Statistics</label>
        <div class="stats-grid">
          <div class="stat-item">
            <i class="fa-duotone fa-users"></i>
            <div>
              <span class="stat-value">{{ selectedRole.userCount || 0 }}</span>
              <span class="stat-label">Users</span>
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

  .detail-section {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;

    label {
      font-weight: 600;
      font-size: 0.9rem;
    }
  }

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

  .stats-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
    gap: 1rem;

    .stat-item {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 1rem;
      background: var(--surface-50);
      border-radius: var(--border-radius);

      i {
        font-size: 1.5rem;
        color: var(--primary-color);
      }

      div {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;

        .stat-value {
          font-size: 1.5rem;
          font-weight: 600;
        }

        .stat-label {
          font-size: 0.85rem;
          color: var(--text-color-secondary);
        }
      }
    }
  }
}

@media (max-width: 768px) {
  .roles-grid {
    grid-template-columns: 1fr;
  }
}
</style>
