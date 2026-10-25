<script setup lang="ts">
import type TwoFactorAuth from './TwoFactorAuth.vue'
import { UsersClient, TwoFactorSetupDto, RecoveryCodesDto, TwoFactorCodeDto } from '@/resources/api-clients/identity-api-client'
import { useUserStore } from '@/stores/user'
import { followReturnUrl } from '@/utils/safe-redirect'

const usersClient = new UsersClient()
const userStore = useUserStore()

const setupRequired = computed(() => userStore.user?.requiresTwoFactorSetup === true)

// Called after any change to the user's factors. Once an enforced setup is complete, continue
// where the user was going (typically an OIDC authorize request).
const handleFactorsChanged = async () => {
  const wasRequired = setupRequired.value
  await userStore.loadUser()
  if (wasRequired && !setupRequired.value) followReturnUrl()
}

const handleRecoveryCodesVisible = async (visible: boolean) => {
  showRecoveryCodesDialog.value = visible
  if (!visible) await handleFactorsChanged()
}

const twoFactorSetup = ref<TwoFactorSetupDto | null>(null)
const show2faSetupDialog = ref(false)
const showRecoveryCodesDialog = ref(false)
const recoveryCodesData = ref<RecoveryCodesDto | null>(null)

const twoFactorAuthRef = ref<InstanceType<typeof TwoFactorAuth>>()

const handleEnable2fa = async () => {
  try {
    twoFactorSetup.value = await usersClient.get2faSetup()
    show2faSetupDialog.value = true
  } catch (error) {
    console.error('Failed to get 2FA setup:', error)
  }
}

const handleConfirmEnable2fa = (data: RecoveryCodesDto) => {
  recoveryCodesData.value = data
  showRecoveryCodesDialog.value = true
  // Refresh the 2FA status in the TwoFactorAuth component
  if (twoFactorAuthRef.value) {
    twoFactorAuthRef.value.refresh2faStatus()
  }
}

const handleGenerateRecoveryCodes = async (code: string) => {
  try {
    recoveryCodesData.value = await usersClient.generateRecoveryCodes(new TwoFactorCodeDto({ code }))
    showRecoveryCodesDialog.value = true
  } catch (error) {
    console.error('Failed to generate recovery codes:', error)
  }
}
</script>

<template>
  <Card class="account-card security-card">
    <template #title>
      <div class="card-header">
        <div class="title-section">
          <i class="fa-duotone fa-shield"></i>
          <h1>Security Settings</h1>
        </div>
      </div>
    </template>
    <template #content>
      <div class="security-content">
        <p class="section-description">
          Manage your account security, password, active sessions, and two-factor authentication.
        </p>

        <Message
          v-if="setupRequired"
          severity="warn"
          icon="fa-duotone fa-shield-exclamation"
          class="setup-required"
        >
          Your account requires two-factor authentication. Enable an authenticator app or add a passkey to continue.
        </Message>

        <Divider />

        <!-- Password Section -->
        <ChangePassword />

        <Divider />

        <!-- Two-Factor Authentication -->
        <TwoFactorAuth
          ref="twoFactorAuthRef"
          @enable2fa="handleEnable2fa"
          @generate-codes="handleGenerateRecoveryCodes"
          @disable2fa="handleFactorsChanged"
        />

        <Divider />

        <!-- Passkeys -->
        <PasskeyList @changed="handleFactorsChanged" />
      </div>
    </template>
  </Card>

  <!-- Two-Factor Setup Dialog -->
  <TwoFactorSetupDialog
    :visible="show2faSetupDialog"
    :two-factor-setup="twoFactorSetup"
    @update:visible="(val) => (show2faSetupDialog = val)"
    @confirm="handleConfirmEnable2fa"
  />

  <!-- Recovery Codes Dialog -->
  <RecoveryCodesDialog
    :visible="showRecoveryCodesDialog"
    :recovery-codes-data="recoveryCodesData"
    @update:visible="handleRecoveryCodesVisible"
  />
</template>

<style lang="scss" scoped>
@use '../account-shared.scss' as *;

.security-card {
  height: 100%;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.security-content {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.setup-required {
  margin-top: 1rem;
}
</style>
