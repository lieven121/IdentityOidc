<script setup lang="ts">
import { useToast } from 'primevue'

interface Application {
  id: string
  name: string
  description: string
  clientId: string
  redirectUris: string[]
  createdDate: Date
  isActive: boolean
}

const applications = ref<Application[]>([
  {
    id: '1',
    name: 'Web Portal',
    description: 'Main web application portal',
    clientId: 'web-portal-client-001',
    redirectUris: ['https://portal.example.com/callback', 'https://portal.example.com/silent-refresh'],
    createdDate: new Date('2024-01-15'),
    isActive: true
  },
  {
    id: '2',
    name: 'Mobile App',
    description: 'iOS and Android mobile application',
    clientId: 'mobile-app-client-002',
    redirectUris: ['myapp://callback'],
    createdDate: new Date('2024-03-22'),
    isActive: true
  },
  {
    id: '3',
    name: 'Admin Dashboard',
    description: 'Administrative management dashboard',
    clientId: 'admin-dashboard-003',
    redirectUris: ['https://admin.example.com/callback'],
    createdDate: new Date('2024-02-10'),
    isActive: false
  }
])

const selectedApp = ref<Application | null>(null)
const showAppDialog = ref(false)

const toast = useToast();
// const isLoading = ref(false)

const viewApplication = (app: Application) => {
  selectedApp.value = { ...app }
  showAppDialog.value = true
}

const createApplication = () => {
  selectedApp.value = {
    id: '',
    name: '',
    description: '',
    clientId: '',
    redirectUris: [],
    createdDate: new Date(),
    isActive: true
  }
  showAppDialog.value = true
}

const formatDate = (date: Date) => {
  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}

const toggleAppStatus = (app: Application) => {
  app.isActive = !app.isActive
}

const { isSupported, copy, copied } = useClipboard()
const copyToClipboard = (text: string) => {
  if (isSupported.value) {
    copy(text)
  }
}

watch(copied, (newValue) => {
  if (newValue) {
    toast.add({
      severity: 'info',
      summary: 'Copied to clipboard',
      detail: 'The value has been copied successfully.',
      life: 3000,
      group: 'notifications'
    })
  }
})

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
        </p>

        <template
          v-for="(app, index) in applications"
          :key="app.id"
        >
          <Divider v-if="index > 0" />

          <div class="app-section">
            <div class="subsection-header">
              <div class="subsection-title">
                <h3>{{ app.name }}</h3>
                <Tag
                  :value="app.isActive ? 'Active' : 'Inactive'"
                  :severity="app.isActive ? 'success' : 'secondary'"
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
                <Button
                  icon="fa-duotone fa-pen"
                  text
                  rounded
                  severity="warning"
                  @click="() => { }"
                  v-tooltip.top="'Edit'"
                />
                <Button
                  :icon="app.isActive ? 'fa-duotone fa-ban' : 'fa-duotone fa-check'"
                  text
                  rounded
                  :severity="app.isActive ? 'danger' : 'success'"
                  @click="toggleAppStatus(app)"
                  v-tooltip.top="app.isActive ? 'Disable' : 'Enable'"
                />
              </div>
            </div>

            <div class="app-details-section">
              <p class="app-description">{{ app.description }}</p>

              <div class="detail-item">
                <label>Client ID</label>
                <div class="value-with-copy">
                  <code>{{ app.clientId }}</code>
                  <Button
                    icon="fa-duotone fa-copy"
                    text
                    size="small"
                    v-tooltip.top="'Copy Client ID'"
                    :disabled="!isSupported"
                    @click="() => copyToClipboard(app.clientId)"
                  />
                </div>
              </div>

              <div class="detail-item">
                <label>Redirect URIs ({{ app.redirectUris.length }})</label>
                <div class="redirect-uris">
                  <Chip
                    v-for="(uri, uriIndex) in app.redirectUris.slice(0, 2)"
                    :key="uriIndex"
                    :label="uri"
                    class="uri-chip"
                  />
                  <Chip
                    v-if="app.redirectUris.length > 2"
                    :label="`+${app.redirectUris.length - 2} more`"
                    class="uri-chip more"
                  />
                </div>
              </div>

              <div class="detail-item">
                <label>Created</label>
                <span>{{ formatDate(app.createdDate) }}</span>
              </div>
            </div>
          </div>
        </template>
      </div>
    </template>
  </Card>

  <!-- Application Details Dialog -->
  <Dialog
    v-model:visible="showAppDialog"
    :header="selectedApp?.name || 'New Application'"
    :modal="true"
    :style="{ width: '50rem' }"
    :breakpoints="{ '960px': '75vw', '640px': '90vw' }"
  >
    <div
      v-if="selectedApp"
      class="app-dialog-content"
    >
      <div class="detail-section">
        <label>Application Name</label>
        <InputText
          v-model="selectedApp.name"
          disabled
        />
      </div>

      <div class="detail-section">
        <label>Description</label>
        <Textarea
          v-model="selectedApp.description"
          disabled
          rows="2"
        />
      </div>

      <div class="detail-section">
        <label>Client ID</label>
        <div class="value-with-copy">
          <InputText
            v-model="selectedApp.clientId"
            disabled
            style="flex: 1;"
          />
          <Button
            icon="fa-duotone fa-copy"
            outlined
            v-tooltip.top="'Copy Client ID'"
            :disabled="!isSupported"
            @click="() => selectedApp && copyToClipboard(selectedApp.clientId)"
          />
        </div>
      </div>

      <div class="detail-section">
        <label>Redirect URIs</label>
        <div class="redirect-uris-full">
          <div
            v-for="(uri, index) in selectedApp.redirectUris"
            :key="index"
            class="uri-item"
          >
            <code>{{ uri }}</code>
            <Button
              icon="fa-duotone fa-copy"
              text
              size="small"
              v-tooltip.top="'Copy URI'"
              :disabled="!isSupported"
              @click="() => copyToClipboard(uri)"
            />
          </div>
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
          disabled
        />
      </div>
    </div>
  </Dialog>
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

  .redirect-uris-full {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;

    .uri-item {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 0.5rem;
      padding: 0.75rem;
      background: var(--surface-50);
      border-radius: var(--border-radius);

      code {
        flex: 1;
        background: transparent;
        padding: 0;
        font-size: 0.85rem;
        word-break: break-all;
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
