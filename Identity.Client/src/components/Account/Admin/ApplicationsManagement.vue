<script setup lang="ts">
import CopyField from '@/components/Common/CopyField.vue'
import { AdminClient, ApplicationRoleInfo } from '@/resources/api-clients/identity-api-client'

const adminClient = new AdminClient()

const applications = ref<ApplicationRoleInfo[]>([])
const isLoading = ref(false)
const selectedApp = ref<ApplicationRoleInfo | null>(null)
const showAppDialog = ref(false)
const appRequiredRoles = ref<string[]>([])
// const isUpdatingRoles = ref(false)

const canEdit = false

const loadApplications = async () => {
  isLoading.value = true
  try {
    const appInfos = await adminClient.admin_GetApplicationsWithRoles()
    if (appInfos) {
      applications.value = appInfos.map(app => new ApplicationRoleInfo({
        ...app,
      }))
    }
  } catch (error) {
    console.error('Failed to load applications:', error)
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  loadApplications()
})

const viewApplication = async (app: ApplicationRoleInfo) => {
  selectedApp.value = new ApplicationRoleInfo({ ...app })
  try {
    if (app.clientId) {
      appRequiredRoles.value = (await adminClient.admin_GetApplicationRequiredRoles(app.clientId)) || []
    }
  } catch (error) {
    console.error('Failed to load application roles:', error)
    appRequiredRoles.value = []
  }
  showAppDialog.value = true
}

const createApplication = () => {
  selectedApp.value = new ApplicationRoleInfo({
    clientId: '',
    displayName: '',
    requiredRoles: [],
  })
  showAppDialog.value = true
}


// const toggleAppStatus = async (app: ApplicationRoleInfo) => {
//   app.isActive = !app.isActive
// }

// const addRoleToApplication = async (role: string) => {
//   if (!selectedApp.value?.clientId) return

//   isUpdatingRoles.value = true
//   try {
//     appRequiredRoles.value.push(role)
//     await adminClient.admin_UpdateApplicationRequiredRoles(selectedApp.value.clientId, appRequiredRoles.value)
//   } catch (error) {
//     console.error('Failed to add role:', error)
//     appRequiredRoles.value = appRequiredRoles.value.filter(r => r !== role)
//   } finally {
//     isUpdatingRoles.value = false
//   }
// }

// const removeRoleFromApplication = async (role: string) => {
//   if (!selectedApp.value?.clientId) return

//   isUpdatingRoles.value = true
//   try {
//     appRequiredRoles.value = appRequiredRoles.value.filter(r => r !== role)
//     await adminClient.admin_UpdateApplicationRequiredRoles(selectedApp.value.clientId, appRequiredRoles.value)
//   } catch (error) {
//     console.error('Failed to remove role:', error)
//   } finally {
//     isUpdatingRoles.value = false
//   }
// }



</script>

<template>
  <Card class="account-card apps-card">
    <template #title>
      <div class="card-header with-actions">
        <div class="title-section">
          <i class="fa-duotone fa-box"></i>
          <h1>Application Management</h1>
        </div>
        <Button
          v-if="canEdit"
          label="Register Application"
          icon="fa-duotone fa-plus"
          severity="success"
          @click="createApplication"
        />
      </div>
    </template>
    <template #content>
      <div class="apps-content">
        <p class="section-description">
          Manage OAuth/OIDC applications, client credentials, and redirect URIs for integrated services.
        </p>

        <div
          v-if="isLoading"
          class="loading-state"
        >
          <ProgressSpinner />
          <p>Loading applications...</p>
        </div>

        <template v-else>
          <template
            v-for="(app, index) in applications"
            :key="app.clientId"
          >
            <Divider v-if="index > 0" />

            <div class="app-section">
              <div class="subsection-header">
                <div class="subsection-title">
                  <h3>{{ app.displayName }}</h3>
                  <Tag
                    :value="true ? 'Active' : 'Inactive'"
                    :severity="true ? 'success' : 'secondary'"
                  />
                </div>
                <div class="subsection-actions">
                  <Button
                    icon="fa-duotone fa-eye"
                    text
                    rounded
                    severity="info"
                    @click="viewApplication(app)"
                    v-tooltip.top="'View Details'"
                  />
                </div>
              </div>

              <div class="app-details-section">
                <CopyField
                  label="Client ID"
                  :value="app.clientId || ''"
                />

                <div class="detail-item">
                  <label>Required Roles ({{ app.requiredRoles?.length || 0 }})</label>
                  <div class="roles-section">
                    <Tag
                      v-for="role in app.requiredRoles"
                      :key="role"
                      :value="role"
                    />
                    <span
                      v-if="!app.requiredRoles || app.requiredRoles.length === 0"
                      class="no-roles"
                    >
                      No roles required
                    </span>
                  </div>
                </div>


              </div>
            </div>
          </template>

          <div
            v-if="applications.length === 0"
            class="empty-state"
          >
            <i class="fa-duotone fa-box"></i>
            <p>No applications found</p>
          </div>
        </template>
      </div>
    </template>
  </Card>

  <!-- Application Details Dialog -->
  <!-- <Dialog
    v-model:visible="showAppDialog"
    :header="selectedApp?.displayName || 'Application Details'"
    :modal="true"
    :style="{ width: '50rem' }"
    :breakpoints="{ '960px': '75vw', '640px': '90vw' }"
  >
    <div
      v-if="selectedApp"
      class="app-dialog-content"
    >
      <div class="detail-section">
        <label>Client ID</label>
        <CopyField :value="selectedApp.clientId || ''" />
      </div>

      <div class="detail-section">
        <label>Required Roles</label>
        <div class="roles-section">
          <Tag
            v-for="role in appRequiredRoles"
            :key="role"
            :value="role"
            :closable="canEdit"
            @remove="() => removeRoleFromApplication(role)"
          />
          <span
            v-if="appRequiredRoles.length === 0"
            class="no-roles"
          >
            No roles required
          </span>
        </div>
      </div>

      <div class="detail-section">
        <label>Status</label>
        <div class="status-section">
          <Tag
            :value="selectedApp.isActive ? 'Active' : 'Inactive'"
            :severity="selectedApp.isActive ? 'success' : 'secondary'"
          />
          <span class="status-text">
            <template v-if="selectedApp.isActive">
              This application is currently active and can authenticate users.
            </template>
            <template v-else>
              This application is currently disabled and cannot authenticate users.
            </template>
          </span>
        </div>
      </div>

      <div class="detail-section">
        <label>Created Date</label>
        <InputText
          :value="formatDate(selectedApp.createdDate)"
          readonly
        />
      </div>
    </div>
  </Dialog> -->
</template>

<style lang="scss" scoped>
@use '../account-shared.scss' as *;

.apps-card {
  height: 100%;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.apps-content {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.app-section {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.app-details-section {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding-bottom: 0.5rem;
}

.app-description {
  margin: 0;
  color: var(--text-color-secondary);
  font-size: 0.9rem;
}

.detail-item {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;

  label {
    font-weight: 600;
    font-size: 0.9rem;
    color: var(--text-color-secondary);
  }

  .roles-section {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;

    .no-roles {
      color: var(--text-color-secondary);
      font-size: 0.9rem;
      padding: 0.5rem 0;
    }
  }
}

.redirect-uris {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;

  .uri-chip {
    font-size: 0.75rem;
    max-width: 300px;
    overflow: hidden;
    text-overflow: ellipsis;

    &.more {
      background: var(--surface-200);
    }
  }
}

.app-dialog-content {
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

    .roles-section {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;

      .no-roles {
        color: var(--text-color-secondary);
        font-size: 0.9rem;
        padding: 0.5rem 0;
      }
    }
  }

  .status-section {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 1rem;
    background: var(--surface-50);
    border-radius: var(--border-radius);

    .status-text {
      font-size: 0.9rem;
      color: var(--text-color-secondary);
    }
  }
}

@media (max-width: 768px) {
  .app-card {
    .app-header {
      flex-direction: column-reverse;

      subsection-header {
        .subsection-actions {
          flex-wrap: wrap
        }
      }
    }
  }
}
</style>
