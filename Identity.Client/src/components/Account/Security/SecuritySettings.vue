<script setup lang="ts">
import type TwoFactorAuth from './TwoFactorAuth.vue'
import { UsersClient, TwoFactorSetupDto, RecoveryCodesDto } from '@/resources/api-clients/identity-api-client'

const usersClient = new UsersClient()

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

const handleGenerateRecoveryCodes = async () => {
  try {
    recoveryCodesData.value = await usersClient.generateRecoveryCodes()
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

        <Divider />

        <!-- Password Section -->
        <ChangePassword />

        <Divider />

        <!-- Two-Factor Authentication -->
        <TwoFactorAuth
          ref="twoFactorAuthRef"
          @enable2fa="handleEnable2fa"
          @generate-codes="handleGenerateRecoveryCodes"
        />
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
    @update:visible="(val) => (showRecoveryCodesDialog = val)"
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
</style>
