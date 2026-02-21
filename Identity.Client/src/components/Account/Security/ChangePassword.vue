<script setup lang="ts">
import { UsersClient, UpdatePasswordDto } from '@/resources/api-clients/identity-api-client'

const usersClient = new UsersClient()

const passwordData = ref({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
})

const isChangingPassword = ref(false)
const passwordError = ref('')
const passwordSuccess = ref(false)

const handleChangePassword = async () => {
  passwordError.value = ''
  passwordSuccess.value = false

  // Validate passwords
  if (passwordData.value.newPassword !== passwordData.value.confirmPassword) {
    passwordError.value = 'New passwords do not match'
    return
  }

  if (passwordData.value.newPassword.length < 8) {
    passwordError.value = 'Password must be at least 8 characters long'
    return
  }

  isChangingPassword.value = true
  try {
    const updatePasswordDto = new UpdatePasswordDto({
      currentPassword: passwordData.value.currentPassword,
      newPassword: passwordData.value.newPassword
    })
    await usersClient.updatePassword(updatePasswordDto)
    passwordSuccess.value = true
    passwordData.value = {
      currentPassword: '',
      newPassword: '',
      confirmPassword: ''
    }
    setTimeout(() => {
      passwordSuccess.value = false
    }, 5000)
  } catch (error) {
    passwordError.value = 'Failed to change password. Please try again.'
    console.error('Password change error:', error)
  } finally {
    isChangingPassword.value = false
  }
}
</script>

<template>
  <div class="security-section">
    <div class="subsection-header">
      <div class="subsection-title">
        <i class="fa-duotone fa-lock"></i>
        <h3>Change Password</h3>
      </div>
    </div>

    <div class="section-details">
      <p class="section-description">
        Ensure your account stays secure by using a strong, unique password.
      </p>

      <Message
        v-if="passwordError"
        severity="error"
        :closable="false"
      >
        {{ passwordError }}
      </Message>

      <Message
        v-if="passwordSuccess"
        severity="success"
        :closable="false"
      >
        Password changed successfully!
      </Message>

      <form
        @submit.prevent="handleChangePassword"
        class="password-form"
      >
        <div class="field">
          <label for="current-password">Current Password</label>
          <Password
            id="current-password"
            v-model="passwordData.currentPassword"
            :feedback="false"
            toggleMask
            required
          />
        </div>

        <div class="field">
          <label for="new-password">New Password</label>
          <Password
            id="new-password"
            v-model="passwordData.newPassword"
            toggleMask
            required
          >
            <template #footer>
              <div class="password-requirements">
                <p>Password requirements:</p>
                <ul>
                  <li :class="{ valid: passwordData.newPassword.length >= 8 }">
                    At least 8 characters
                  </li>
                  <li :class="{ valid: /[A-Z]/.test(passwordData.newPassword) }">
                    One uppercase letter
                  </li>
                  <li :class="{ valid: /[a-z]/.test(passwordData.newPassword) }">
                    One lowercase letter
                  </li>
                  <li :class="{ valid: /[0-9]/.test(passwordData.newPassword) }">
                    One number
                  </li>
                </ul>
              </div>
            </template>
          </Password>
        </div>

        <div class="field">
          <label for="confirm-password">Confirm New Password</label>
          <Password
            id="confirm-password"
            v-model="passwordData.confirmPassword"
            :feedback="false"
            toggleMask
            required
          />
        </div>

        <div class="actions align-right">
          <Button
            type="submit"
            label="Update Password"
            icon="fa-duotone fa-check"
            :loading="isChangingPassword"
            severity="success"
          />
        </div>
      </form>
    </div>
  </div>
</template>

<style lang="scss" scoped>
@use '../account-shared.scss' as *;

.security-section {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.section-details {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  padding-bottom: 0.5rem;
}

.subsection-header {
  .subsection-title {
    i {
      color: var(--primary-color);
    }
  }
}

.password-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.password-requirements {
  padding: 0.5rem 0;

  p {
    margin: 0 0 0.5rem 0;
    font-weight: 600;
    font-size: 0.875rem;
  }

  ul {
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    flex-direction: column;
    gap: 0.25rem;

    li {
      font-size: 0.875rem;
      color: var(--text-color-secondary);

      &::before {
        content: '○ ';
        margin-right: 0.5rem;
      }

      &.valid {
        color: var(--green-500);

        &::before {
          content: '✓ ';
        }
      }
    }
  }
}
</style>
