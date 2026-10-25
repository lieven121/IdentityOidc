<script setup lang="ts">
import { browserSupportsWebAuthn, startRegistration } from '@simplewebauthn/browser'
import {
  AddPasskeyDto,
  ApiException,
  PasskeyCreationOptionsRequestDto,
  PasskeyDto,
  UsersClient,
} from '@/resources/api-clients/identity-api-client'
import { apiErrorMessage, useConfirmDialog } from '@/utils/dialogs'

interface Emits {
  (e: 'changed'): void
}

const emit = defineEmits<Emits>()

const { ask, confirm } = useConfirmDialog()
const usersClient = new UsersClient()

const passkeys = ref<PasskeyDto[]>([])
const isLoading = ref(false)
const isAdding = ref(false)
const removingId = ref<string | null>(null)
const errorMessage = ref<string | null>(null)
const webAuthnSupported = ref(false)

onMounted(async () => {
  webAuthnSupported.value = browserSupportsWebAuthn()
  await loadPasskeys()
})

const loadPasskeys = async () => {
  isLoading.value = true
  try {
    passkeys.value = (await usersClient.getPasskeys()) ?? []
  } catch (error) {
    console.error('Failed to load passkeys:', error)
  } finally {
    isLoading.value = false
  }
}

const handleAddPasskey = async () => {
  errorMessage.value = null

  const name = await ask({
    title: 'Add a Passkey',
    message: 'Give this passkey a name so you can recognise it later, for example the device or password manager it is stored in.',
    confirm: { text: 'Next', severity: 'primary' },
    cancel: { text: 'Cancel', severity: 'secondary' },
    input: { label: 'Passkey name', placeholder: 'My laptop' },
  })
  if (name === null) return

  const currentPassword = await ask({
    title: 'Confirm Your Password',
    message: 'For your security, enter your current password before adding a new way to sign in.',
    confirm: { text: 'Continue', severity: 'primary' },
    cancel: { text: 'Cancel', severity: 'secondary' },
    input: { label: 'Current password', type: 'password', autocomplete: 'current-password' },
  })
  if (currentPassword === null) return

  isAdding.value = true
  try {
    let options
    try {
      options = await usersClient.passkeyCreationOptions(new PasskeyCreationOptionsRequestDto({ currentPassword }))
    } catch (error) {
      errorMessage.value = error instanceof ApiException && error.status === 400
        ? 'The password is incorrect.'
        : 'Could not start passkey registration.'
      return
    }
    if (!options?.optionsJson) return

    const credential = await startRegistration({ optionsJSON: JSON.parse(options.optionsJson) })
    await usersClient.addPasskey(new AddPasskeyDto({
      credentialJson: JSON.stringify(credential),
      name,
    }))

    await loadPasskeys()
    emit('changed')
  } catch (error) {
    // NotAllowedError means the user cancelled the browser prompt.
    if (!(error instanceof Error && error.name === 'NotAllowedError'))
      errorMessage.value = apiErrorMessage(error, 'The passkey could not be registered.')
    console.error('Failed to add passkey:', error)
  } finally {
    isAdding.value = false
  }
}

const handleRemovePasskey = async (passkey: PasskeyDto) => {
  errorMessage.value = null
  if (!passkey.id) return
  const id = passkey.id

  const confirmed = await confirm({
    title: 'Remove Passkey',
    message: `Remove the passkey "${passkey.name ?? 'Passkey'}"? You will no longer be able to sign in with it.`,
    confirm: { text: 'Yes, Remove', severity: 'danger' },
    cancel: { text: 'Cancel', severity: 'secondary' },
  })
  if (!confirmed) return

  removingId.value = id
  try {
    await usersClient.removePasskey(id)
    await loadPasskeys()
    emit('changed')
  } catch (error) {
    errorMessage.value = apiErrorMessage(error, 'The passkey could not be removed.')
    console.error('Failed to remove passkey:', error)
  } finally {
    removingId.value = null
  }
}

const formatDate = (date: Date | undefined) => (date ? new Date(date).toLocaleDateString() : '')
</script>

<template>
  <div class="security-section">
    <div class="subsection-header">
      <div class="subsection-title">
        <i class="fa-duotone fa-key"></i>
        <h3>Passkeys</h3>
      </div>
    </div>

    <div class="section-details">
      <p class="section-description">
        Sign in without a password using your device's fingerprint, face or screen lock. A passkey also counts as
        two-factor authentication.
      </p>

      <div
        v-if="isLoading"
        class="loading-state"
      >
        <ProgressSpinner />
        <p>Loading passkeys...</p>
      </div>

      <template v-else>
        <ul
          v-if="passkeys.length"
          class="passkey-list"
        >
          <li
            v-for="passkey in passkeys"
            :key="passkey.id"
            class="passkey-item"
          >
            <div class="passkey-info">
              <i class="fa-duotone fa-fingerprint"></i>
              <div>
                <h4>{{ passkey.name ?? 'Passkey' }}</h4>
                <p>
                  Added {{ formatDate(passkey.createdAt) }}
                  <span v-if="passkey.isBackedUp"> · Synced</span>
                </p>
              </div>
            </div>
            <Button
              label="Remove"
              icon="fa-duotone fa-trash"
              severity="danger"
              text
              :loading="removingId === passkey.id"
              @click="handleRemovePasskey(passkey)"
            />
          </li>
        </ul>

        <div class="passkey-actions">
          <p
            v-if="!passkeys.length"
            class="empty-state"
          >No passkeys registered.</p>
          <Button
            label="Add Passkey"
            icon="fa-duotone fa-plus"
            severity="secondary"
            outlined
            :disabled="!webAuthnSupported"
            :loading="isAdding"
            @click="handleAddPasskey"
          />
        </div>

        <small
          v-if="!webAuthnSupported"
          class="passkey-message"
        >This browser does not support passkeys.</small>
        <small
          v-if="errorMessage"
          class="passkey-message error"
        >{{ errorMessage }}</small>
      </template>
    </div>
  </div>
</template>

<style lang="scss" scoped>
@use '../account-shared.scss' as *;

.passkey-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
}

.passkey-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 1rem 1.5rem;

  &:not(:last-child) {
    border-bottom: var(--border-width, 1px) solid var(--p-surface-200);
  }
}

.passkey-info {
  display: flex;
  align-items: center;
  gap: 1rem;

  i {
    font-size: 1.5rem;
  }

  h4 {
    margin: 0 0 0.25rem 0;
    font-size: 1.1rem;
  }

  p {
    margin: 0;
    color: var(--p-surface-600);
    font-size: 0.9rem;
  }
}

.passkey-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 1rem 1.5rem;

  .empty-state {
    margin: 0;
    color: var(--p-surface-600);
  }
}

.passkey-message {
  display: block;
  padding: 0 1.5rem;

  &.error {
    color: var(--p-red-500);
  }
}

@media (max-width: 768px) {
  .passkey-actions {
    flex-direction: column;
    align-items: stretch;

    button {
      width: 100%;
    }
  }
}
</style>
