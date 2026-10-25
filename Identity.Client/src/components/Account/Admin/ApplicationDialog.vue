<script setup lang="ts">
import {
  AdminClient,
  ApplicationDto,
  ApplicationSecretDto,
  SaveApplicationDto,
  SetClientSecretDto,
} from '@/resources/api-clients/identity-api-client'
import { apiErrorMessage, useConfirmDialog } from '@/utils/dialogs'

const visible = defineModel<boolean>('visible', { required: true })

const props = defineProps<{
  /** The application to edit, or null to register a new one. */
  application: ApplicationDto | null
  roles: string[]
  scopes: string[]
}>()

interface Emits {
  /** After a save or secret change; carries a secret when a new one was issued. */
  (e: 'saved', secret: ApplicationSecretDto | null): void
}

const emit = defineEmits<Emits>()

const adminClient = new AdminClient()
const { ask, confirm } = useConfirmDialog()

const clientTypes = [
  { label: 'Public', value: 'public', description: 'Browser and mobile apps. No secret; PKCE is always required.' },
  { label: 'Confidential', value: 'confidential', description: 'Server-side apps and services that can keep a secret. Also allows client credentials.' },
]

const form = reactive({
  clientId: '',
  displayName: '',
  clientType: 'public',
  pkce: true,
  redirectUris: '',
  postLogoutRedirectUris: '',
  scopes: [] as string[],
  requiredRoles: [] as string[],
})
const customScope = ref('')
const extraScopes = ref<string[]>([])
const isSaving = ref(false)
const isChangingSecret = ref(false)
const errorMessage = ref<string | null>(null)

const isNew = computed(() => props.application === null)
const readOnly = computed(() => props.application?.isConfigManaged === true)
const isPublic = computed(() => form.clientType === 'public')
const scopeOptions = computed(() => [...new Set([...props.scopes, ...extraScopes.value, ...form.scopes])].sort())
const savedAsConfidential = computed(() => props.application?.clientType === 'confidential')
const switchingToConfidential = computed(() => !isNew.value && !savedAsConfidential.value && !isPublic.value)

watch(visible, (open) => {
  if (!open) return
  const app = props.application
  form.clientId = app?.clientId ?? ''
  form.displayName = app?.displayName ?? ''
  form.clientType = app?.clientType ?? 'public'
  form.pkce = app?.pkce ?? true
  form.redirectUris = (app?.redirectUris ?? []).join('\n')
  form.postLogoutRedirectUris = (app?.postLogoutRedirectUris ?? []).join('\n')
  form.scopes = [...(app?.scopes ?? [])]
  form.requiredRoles = [...(app?.requiredRoles ?? [])]
  customScope.value = ''
  extraScopes.value = []
  errorMessage.value = null
}, { immediate: true })

const lines = (value: string) => value.split('\n').map((l) => l.trim()).filter((l) => l.length > 0)

const addCustomScope = () => {
  const scope = customScope.value.trim().toLowerCase()
  if (!scope) return
  if (!extraScopes.value.includes(scope)) extraScopes.value.push(scope)
  if (!form.scopes.includes(scope)) form.scopes.push(scope)
  customScope.value = ''
}

const save = async () => {
  if (readOnly.value) return
  errorMessage.value = null
  isSaving.value = true
  try {
    const dto = new SaveApplicationDto({
      clientId: isNew.value ? form.clientId.trim() : undefined,
      displayName: form.displayName.trim(),
      clientType: form.clientType,
      pkce: isPublic.value || form.pkce,
      redirectUris: lines(form.redirectUris),
      postLogoutRedirectUris: lines(form.postLogoutRedirectUris),
      scopes: form.scopes,
      requiredRoles: form.requiredRoles,
    })
    const result = isNew.value
      ? await adminClient.admin_CreateApplication(dto)
      : await adminClient.admin_UpdateApplication(props.application!.clientId, dto)
    visible.value = false
    emit('saved', result?.clientSecret ? result : null)
  } catch (error) {
    errorMessage.value = apiErrorMessage(error, 'The application could not be saved.')
    console.error('Failed to save application:', error)
  } finally {
    isSaving.value = false
  }
}

const changeSecret = async (custom: boolean) => {
  const app = props.application
  if (!app) return
  errorMessage.value = null

  let clientSecret: string | undefined
  if (custom) {
    const value = await ask({
      title: 'Set Client Secret',
      message: 'Enter a new secret of at least 24 characters. The current secret stops working immediately.',
      confirm: { text: 'Set Secret', severity: 'warning' },
      cancel: { text: 'Cancel', severity: 'secondary' },
      input: { label: 'New client secret', type: 'password', autocomplete: 'new-password' },
    })
    if (value === null) return
    clientSecret = value
  } else {
    const confirmed = await confirm({
      title: 'Regenerate Client Secret',
      message: `Generate a new secret for "${app.displayName ?? app.clientId}"? The current secret stops working immediately, so update the application's configuration right away.`,
      confirm: { text: 'Regenerate', severity: 'warning' },
      cancel: { text: 'Cancel', severity: 'secondary' },
    })
    if (!confirmed) return
  }

  isChangingSecret.value = true
  try {
    const result = await adminClient.admin_SetApplicationSecret(app.clientId, new SetClientSecretDto({ clientSecret }))
    visible.value = false
    emit('saved', result)
  } catch (error) {
    errorMessage.value = apiErrorMessage(error, 'The secret could not be changed.')
    console.error('Failed to change secret:', error)
  } finally {
    isChangingSecret.value = false
  }
}
</script>

<template>
  <Dialog
    v-model:visible="visible"
    :header="isNew ? 'Register Application' : (application?.displayName || application?.clientId)"
    :modal="true"
    :style="{ width: '50rem' }"
    :breakpoints="{ '960px': '75vw', '640px': '90vw' }"
  >
    <form
      class="app-form"
      @submit.prevent="save"
    >
      <Message
        v-if="readOnly"
        severity="info"
        :closable="false"
      >
        This application is managed by configuration (OpenIddict:ApplicationConfigs) and is recreated on every
        start, so it can't be changed here.
      </Message>
      <Message
        v-if="errorMessage"
        severity="error"
        :closable="false"
      >
        {{ errorMessage }}
      </Message>

      <div class="field">
        <label for="app-display-name">Display name</label>
        <InputText
          id="app-display-name"
          v-model="form.displayName"
          :disabled="readOnly"
          maxlength="100"
        />
      </div>

      <div class="field">
        <label for="app-client-id">Client ID</label>
        <InputText
          id="app-client-id"
          v-model="form.clientId"
          :disabled="!isNew"
          placeholder="my-application"
          maxlength="100"
        />
        <small v-if="isNew">Letters, digits, '.', '_' or '-'. Can't be changed later.</small>
      </div>

      <div class="field">
        <label>Client type</label>
        <SelectButton
          v-model="form.clientType"
          :options="clientTypes"
          option-label="label"
          option-value="value"
          :allow-empty="false"
          :disabled="readOnly"
        />
        <small>{{ clientTypes.find((t) => t.value === form.clientType)?.description }}</small>
        <small
          v-if="switchingToConfidential"
          class="warn"
        >
          A client secret will be generated when you save.
        </small>
      </div>

      <div class="field checkbox-field">
        <Checkbox
          v-model="form.pkce"
          input-id="app-pkce"
          binary
          :disabled="readOnly || isPublic"
        />
        <label for="app-pkce">Require PKCE{{ isPublic ? ' (always on for public clients)' : '' }}</label>
      </div>

      <div class="field">
        <label for="app-redirect-uris">Redirect URIs</label>
        <Textarea
          id="app-redirect-uris"
          v-model="form.redirectUris"
          rows="3"
          auto-resize
          :disabled="readOnly"
          placeholder="https://app.example.com/oidc"
        />
        <small>One absolute URI per line.</small>
      </div>

      <div class="field">
        <label for="app-logout-uris">Post-logout redirect URIs</label>
        <Textarea
          id="app-logout-uris"
          v-model="form.postLogoutRedirectUris"
          rows="2"
          auto-resize
          :disabled="readOnly"
          placeholder="https://app.example.com"
        />
      </div>

      <div class="field">
        <label for="app-scopes">Scopes</label>
        <MultiSelect
          id="app-scopes"
          v-model="form.scopes"
          :options="scopeOptions"
          display="chip"
          filter
          placeholder="No additional scopes"
          :disabled="readOnly"
        />
        <InputGroup v-if="!readOnly">
          <InputText
            v-model="customScope"
            placeholder="New scope name"
            @keydown.enter.prevent="addCustomScope"
          />
          <Button
            label="Add scope"
            icon="fa-duotone fa-plus"
            severity="secondary"
            :disabled="!customScope.trim()"
            @click="addCustomScope"
          />
        </InputGroup>
        <small>
          openid, offline_access, email, profile and roles are always allowed. The admin scope grants the External
          API to confidential clients.
        </small>
      </div>

      <div class="field">
        <label for="app-roles">Required roles</label>
        <MultiSelect
          id="app-roles"
          v-model="form.requiredRoles"
          :options="roles"
          display="chip"
          placeholder="Any signed-in user"
          :disabled="readOnly"
        />
        <small>Users need at least one of these roles to sign in to this application.</small>
      </div>

      <div
        v-if="!isNew && !readOnly && savedAsConfidential && !isPublic"
        class="field secret-field"
      >
        <label>Client secret</label>
        <div class="secret-row">
          <Tag
            value="Secret set"
            severity="success"
            icon="fa-duotone fa-lock"
          />
          <span class="hint">Stored hashed; it can be replaced but not viewed.</span>
        </div>
        <div class="secret-actions">
          <Button
            label="Regenerate secret"
            icon="fa-duotone fa-arrows-rotate"
            severity="warning"
            outlined
            size="small"
            :loading="isChangingSecret"
            @click="changeSecret(false)"
          />
          <Button
            label="Set custom secret"
            icon="fa-duotone fa-pen"
            severity="secondary"
            outlined
            size="small"
            :disabled="isChangingSecret"
            @click="changeSecret(true)"
          />
        </div>
      </div>

      <!-- Lets Enter in a text field submit the form. -->
      <button
        type="submit"
        hidden
      />
    </form>

    <template #footer>
      <Button
        :label="readOnly ? 'Close' : 'Cancel'"
        severity="secondary"
        text
        @click="visible = false"
      />
      <Button
        v-if="!readOnly"
        :label="isNew ? 'Register' : 'Save'"
        severity="success"
        :loading="isSaving"
        :disabled="!form.displayName.trim() || (isNew && !form.clientId.trim())"
        @click="save"
      />
    </template>
  </Dialog>
</template>

<style lang="scss" scoped>
.app-form {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;

  label {
    font-weight: 600;
    font-size: 0.9rem;
  }

  small {
    color: var(--p-slate-500);

    &.warn {
      color: var(--p-orange-500);
    }
  }
}

.checkbox-field {
  flex-direction: row;
  align-items: center;

  label {
    font-weight: normal;
  }
}

.secret-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;

  .hint {
    color: var(--p-slate-500);
    font-size: 0.9rem;
  }
}

.secret-actions {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}
</style>
