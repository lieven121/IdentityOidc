<script setup lang="ts">
import { TwoFactorSetupDto, UsersClient, Enable2faDto, RecoveryCodesDto } from '@/resources/api-clients/identity-api-client'
import encodeQR from 'qr';


interface Props {
  visible: boolean
  twoFactorSetup: TwoFactorSetupDto | null
  isLoading?: boolean
}

interface Emits {
  (e: 'update:visible', value: boolean): void
  (e: 'confirm', data: RecoveryCodesDto): void
}

const props = withDefaults(defineProps<Props>(), {
  isLoading: false
})

const qrCode = computed(() => {
  if (props.twoFactorSetup?.authenticatorUri) {
    const svg = encodeQR(props.twoFactorSetup?.authenticatorUri, 'svg');
    return `data:image/svg+xml;base64,${btoa(svg)}`;
  }
  return null
})

const emit = defineEmits<Emits>()

const usersClient = new UsersClient()

const recoveryCode = ref('')
const isEnabling = ref(false)

const handleConfirm = async () => {
  if (!recoveryCode.value || recoveryCode.value.length !== 6) {
    return
  }

  isEnabling.value = true
  try {
    const enable2faDto = new Enable2faDto({
      code: recoveryCode.value
    })
    const recoveryCodesData = await usersClient.enable2fa(enable2faDto)
    if (recoveryCodesData) {
      emit('confirm', recoveryCodesData)
      recoveryCode.value = ''
      emit('update:visible', false)
    }
  } catch (error) {
    console.error('Failed to enable 2FA:', error)
  } finally {
    isEnabling.value = false
  }
}

const handleCancel = () => {
  recoveryCode.value = ''
  emit('update:visible', false)
}
</script>

<template>
  <Dialog
    :visible="visible"
    header="Set Up Two-Factor Authentication"
    :modal="true"
    class="two-factor-setup-dialog"
    @update:visible="(val) => emit('update:visible', val)"
  >
    <div class="setup-content">
      <p class="section-description">
        Scan this QR code with your authenticator app (Google Authenticator, Authy, Microsoft Authenticator, etc.)
      </p>
      <div
        v-if="twoFactorSetup"
        class="setup-form"
      >
        <div class="qr-code-section">
          <img
            v-if="qrCode"
            :src="qrCode"
            alt="2FA QR Code"
            class="qr-code"
          />
        </div>
        <p class="or-text">OR</p>
        <CopyField
          label="Manual Entry Key"
          :value="twoFactorSetup.sharedKey"
          readonly
        />
        <CopyField
          label="Manual Entry Key (URI Format)"
          value="otpauth://totp..."
          :extended-value="props.twoFactorSetup?.authenticatorUri"
          readonly
        />
        <div class="verification-field">
          <LabelInput
            label="Verification Code"
            input-id="verification-code"
          >
            <template #default="{ inputId }">
              <InputOtp
                v-model="recoveryCode"
                :length="6"
                input-type="text"
                :id="inputId"
                placeholder="Enter 6-digit code from your app"
              />
            </template>
          </LabelInput>
          <!-- <label for="verification-code">Verification Code:</label>
          <InputText
            id="verification-code"
            v-model="recoveryCode"
            placeholder="Enter 6-digit code from your app"
            maxlength="6"
          /> -->
        </div>
      </div>
    </div>
    <template #footer>
      <Button
        label="Cancel"
        icon="fa-duotone fa-xmark"
        @click="handleCancel"
        text
      />
      <Button
        label="Confirm"
        icon="fa-duotone fa-check"
        @click="handleConfirm"
        :loading="isEnabling"
        :disabled="!recoveryCode || recoveryCode.length !== 6"
      />
    </template>
  </Dialog>
</template>

<style lang="scss" scoped>
.two-factor-setup-dialog {
  :deep(.p-dialog-content) {
    padding: 1.5rem;
  }
}

.setup-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;

  .setup-form {
    display: flex;
    flex-direction: column;
    gap: 1rem;

    .qr-code-section {
      display: flex;
      justify-content: center;
      padding: 1rem;
      background: var(--surface-section);
      border-radius: var(--border-radius);

      .qr-code {
        max-width: 200px;
        height: auto;
      }
    }

    .or-text {
      text-align: center;
      color: var(--text-color-secondary);
      margin: 0;
      font-size: 0.9rem;
    }

    .manual-entry {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;

      label {
        font-weight: 600;
        font-size: 0.9rem;
      }

      .key-input {
        font-family: monospace;
        font-size: 0.85rem;
      }
    }

    .verification-field {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;

      label {
        font-weight: 600;
        font-size: 0.9rem;
      }
    }
  }
}

.section-description {
  margin: 0;
  color: var(--text-color-secondary);
  font-size: 0.9rem;
}
</style>
