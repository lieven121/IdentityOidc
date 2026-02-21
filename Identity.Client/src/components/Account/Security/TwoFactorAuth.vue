<script setup lang="ts">
import ConfirmDialog from '@/components/Common/Dialogs/ConfirmDialog.vue'
import { UsersClient, TwoFactorStatusDto } from '@/resources/api-clients/identity-api-client'
import { useDialog } from 'primevue'

const dialog = useDialog()

interface Emits {
  (e: 'enable2fa'): void
  (e: 'disable2fa'): void
  (e: 'generate-codes'): void
}

const emit = defineEmits<Emits>()

const usersClient = new UsersClient()

const twoFactorStatus = ref<TwoFactorStatusDto | null>(null)
const isFetchingTwoFactor = ref(false)
const isDisabling2fa = ref(false)

onMounted(async () => {
  await loadTwoFactorStatus()
})

const loadTwoFactorStatus = async () => {
  isFetchingTwoFactor.value = true
  try {
    twoFactorStatus.value = await usersClient.get2faStatus()
  } catch (error) {
    console.error('Failed to load 2FA status:', error)
  } finally {
    isFetchingTwoFactor.value = false
  }
}

const handleEnable2fa = () => {
  emit('enable2fa')
}

const handleGenerateRecoveryCodes = () => {
  dialog.open(ConfirmDialog, {
    data: {
      title: 'Regenerate Recovery Codes',
      message: 'Are you sure you want to regenerate your recovery codes? This will invalidate your existing recovery codes.',
      confirm: {
        text: 'Yes, Regenerate',
        severity: 'warn'
      },
      cancel: {
        text: 'Cancel',
        severity: 'secondary'
      },
    },
    props: {
      style: {
        maxWidth: "40rem",
        width: "100%",
        // width: "50vw",
      },
      modal: true,
      closable: false,
      dismissableMask: false,
      closeOnEscape: false,
      showHeader: false,
    },
    onClose: (opt) => {
      if (opt?.data) {
        emit('generate-codes')
      }
    }
  });

}

const handleDisable2fa = async () => {
  isDisabling2fa.value = true
  dialog.open(ConfirmDialog, {
    data: {
      title: 'Disable Two-Factor Authentication',
      message: 'Are you sure you want to disable two-factor authentication? This will reduce the security of your account.',
      confirm: {
        text: 'Yes, Disable',
        severity: 'danger'
      },
      cancel: {
        text: 'Cancel',
        severity: 'secondary'
      },
    },
    props: {
      style: {
        maxWidth: "40rem",
        width: "100%",
        // width: "50vw",
      },
      modal: true,
      closable: false,
      dismissableMask: false,
      closeOnEscape: false,
      showHeader: false,
    },
    onClose: async (opt) => {
      try {
        if (!opt?.data)
          return
        await usersClient.disable2fa()
        await loadTwoFactorStatus()
        emit('disable2fa')
      } catch (error) {
        console.error('Failed to disable 2FA:', error)
      } finally {
        isDisabling2fa.value = false
      }
    }
  });
}

const refresh2faStatus = async () => {
  await loadTwoFactorStatus()
}

defineExpose({
  refresh2faStatus
})
</script>

<template>
  <div class="security-section">
    <div class="subsection-header">
      <div class="subsection-title">
        <i class="fa-duotone fa-shield-halved"></i>
        <h3>Two-Factor Authentication</h3>
      </div>
    </div>

    <div class="section-details">
      <p class="section-description">
        Add an extra layer of security to your account by enabling two-factor authentication.
      </p>

      <div
        v-if="isFetchingTwoFactor"
        class="loading-state"
      >
        <ProgressSpinner />
        <p>Loading 2FA status...</p>
      </div>

      <div
        v-else-if="!twoFactorStatus?.is2faEnabled"
        class="two-factor-status"
      >
        <div class="status-info">
          <i class="fa-duotone fa-circle-exclamation status-icon warning"></i>
          <div class="status-text">
            <h4>Not Enabled</h4>
            <p>Two-factor authentication is currently disabled for your account.</p>
          </div>
        </div>
        <Button
          label="Enable 2FA"
          icon="fa-duotone fa-plus"
          severity="secondary"
          outlined
          :loading="isFetchingTwoFactor"
          @click="handleEnable2fa"
        />
      </div>

      <div
        v-else
        class="two-factor-status enabled"
      >
        <div class="status-info">
          <i class="fa-duotone fa-circle-check status-icon success"></i>
          <div class="status-text">
            <h4>Enabled</h4>
            <p>Two-factor authentication is active on your account.</p>
          </div>
        </div>
        <div class="two-factor-actions">
          <Button
            label="Regenerate Codes"
            icon="fa-duotone fa-arrows-rotate"
            severity="info"
            text
            @click="handleGenerateRecoveryCodes"
          />
          <Button
            label="Disable 2FA"
            icon="fa-duotone fa-xmark"
            severity="danger"
            text
            :loading="isDisabling2fa"
            @click="handleDisable2fa"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
@use '../account-shared.scss' as *;

.two-factor-status {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.5rem;
  gap: 1rem;

  &.enabled {
    background-color: rgba(34, 197, 94, 0.05);
  }


  .status-text {
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


  .two-factor-actions {
    display: flex;
    gap: 0.5rem;
  }
}

@media (max-width: 768px) {
  .two-factor-status {
    flex-direction: column;
    align-items: stretch;

    button {
      width: 100%;
    }
  }
}
</style>
