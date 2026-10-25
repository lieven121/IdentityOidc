<script setup lang="ts">
import ApplicationDialog from '@/components/Account/Admin/ApplicationDialog.vue'
import ClientSecretDialog from '@/components/Account/Admin/ClientSecretDialog.vue'
import CopyField from '@/components/Common/CopyField.vue'
import { AdminClient, ApplicationDto, ApplicationSecretDto } from '@/resources/api-clients/identity-api-client'
import { apiErrorMessage, useConfirmDialog } from '@/utils/dialogs'

const adminClient = new AdminClient()
const { confirm } = useConfirmDialog()

const applications = ref<ApplicationDto[]>([])
const roles = ref<string[]>([])
const scopes = ref<string[]>([])
const isLoading = ref(false)
const errorMessage = ref<string | null>(null)
const deletingId = ref<string | null>(null)

const selectedApp = ref<ApplicationDto | null>(null)
const showAppDialog = ref(false)
const issuedSecret = ref<ApplicationSecretDto | null>(null)
const showSecretDialog = ref(false)

const loadApplications = async () => {
  isLoading.value = true
  try {
    const [apps, allRoles, allScopes] = await Promise.all([
      adminClient.admin_GetApplications(),
      adminClient.admin_GetAllRoles(),
      adminClient.admin_GetScopes(),
    ])
    applications.value = apps ?? []
    roles.value = (allRoles ?? []).map((r) => r.name)
    scopes.value = allScopes ?? []
  } catch (error) {
    console.error('Failed to load applications:', error)
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  loadApplications()
})

const viewApplication = (app: ApplicationDto) => {
  errorMessage.value = null
  selectedApp.value = app
  showAppDialog.value = true
}

const createApplication = () => {
  errorMessage.value = null
  selectedApp.value = null
  showAppDialog.value = true
}

const handleSaved = async (secret: ApplicationSecretDto | null) => {
  if (secret?.clientSecret) {
    issuedSecret.value = secret
    showSecretDialog.value = true
  }
  await loadApplications()
}

const deleteApplication = async (app: ApplicationDto) => {
  errorMessage.value = null
  const confirmed = await confirm({
    title: 'Delete Application',
    message: `Delete "${app.displayName ?? app.clientId}"? Its tokens and authorizations are revoked and it can no longer sign users in. This can't be undone.`,
    confirm: { text: 'Yes, Delete', severity: 'danger' },
    cancel: { text: 'Cancel', severity: 'secondary' },
  })
  if (!confirmed) return

  deletingId.value = app.clientId
  try {
    await adminClient.admin_DeleteApplication(app.clientId)
    await loadApplications()
  } catch (error) {
    errorMessage.value = apiErrorMessage(error, 'The application could not be deleted.')
    console.error('Failed to delete application:', error)
  } finally {
    deletingId.value = null
  }
}
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
          Applications defined in configuration are read-only.
        </p>

        <Message
          v-if="errorMessage"
          severity="error"
          :closable="false"
          class="error-message"
        >
          {{ errorMessage }}
        </Message>

        <div
          v-if="isLoading && applications.length === 0"
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
                  <h3>{{ app.displayName || app.clientId }}</h3>
                  <Tag
                    :value="app.clientType === 'confidential' ? 'Confidential' : 'Public'"
                    :severity="app.clientType === 'confidential' ? 'warn' : 'info'"
                  />
                  <Tag
                    v-if="app.isConfigManaged"
                    value="Managed by configuration"
                    severity="secondary"
                    icon="fa-duotone fa-lock"
                  />
                </div>
                <div class="subsection-actions">
                  <Button
                    :icon="app.isConfigManaged ? 'fa-duotone fa-eye' : 'fa-duotone fa-pen'"
                    text
                    rounded
                    severity="info"
                    @click="viewApplication(app)"
                    v-tooltip.top="app.isConfigManaged ? 'View Details' : 'Edit'"
                  />
                  <Button
                    v-if="!app.isConfigManaged"
                    icon="fa-duotone fa-trash"
                    text
                    rounded
                    severity="danger"
                    :loading="deletingId === app.clientId"
                    @click="deleteApplication(app)"
                    v-tooltip.top="'Delete'"
                  />
                </div>
              </div>

              <div class="app-details-section">
                <CopyField
                  label="Client ID"
                  :value="app.clientId"
                />

                <div class="detail-item">
                  <label>Scopes</label>
                  <div class="roles-section">
                    <Tag
                      v-for="scope in app.scopes"
                      :key="scope"
                      :value="scope"
                      severity="secondary"
                    />
                    <span
                      v-if="!app.scopes?.length"
                      class="no-roles"
                    >
                      Default scopes only
                    </span>
                  </div>
                </div>

                <div class="detail-item">
                  <label>Required Roles ({{ app.requiredRoles?.length || 0 }})</label>
                  <div class="roles-section">
                    <Tag
                      v-for="role in app.requiredRoles"
                      :key="role"
                      :value="role"
                    />
                    <span
                      v-if="!app.requiredRoles?.length"
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

  <ApplicationDialog
    v-model:visible="showAppDialog"
    :application="selectedApp"
    :roles="roles"
    :scopes="scopes"
    @saved="handleSaved"
  />

  <ClientSecretDialog
    v-model:visible="showSecretDialog"
    :secret="issuedSecret"
  />
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

.error-message {
  margin: 1rem 0;
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
</style>
