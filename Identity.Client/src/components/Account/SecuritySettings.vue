<script setup lang="ts">
// const userStore = useUserStore()

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
    // TODO: Implement actual password change API call
    await new Promise(resolve => setTimeout(resolve, 1500))
    passwordSuccess.value = true
    passwordData.value = {
      currentPassword: '',
      newPassword: '',
      confirmPassword: ''
    }
  } catch {
    passwordError.value = 'Failed to change password. Please try again.'
  } finally {
    isChangingPassword.value = false
  }
}

const sessions = ref([
  {
    id: 1,
    device: 'Chrome on Windows',
    location: 'Belgium, Brussels',
    lastActive: new Date(),
    current: true
  },
  {
    id: 2,
    device: 'Firefox on Android',
    location: 'Belgium, Antwerp',
    lastActive: new Date(Date.now() - 86400000 * 2),
    current: false
  }
])

const revokeSession = (sessionId: number) => {
  sessions.value = sessions.value.filter(s => s.id !== sessionId)
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

              <div class="actions no-border">
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

        <Divider />

        <!-- Active Sessions -->
        <div class="security-section">
          <div class="subsection-header">
            <div class="subsection-title">
              <i class="fa-duotone fa-desktop"></i>
              <h3>Active Sessions</h3>
            </div>
          </div>

          <div class="section-details">
            <p class="section-description">
              Manage devices where you're currently signed in. Sign out of any unfamiliar sessions.
            </p>

            <div class="sessions-list">
              <div
                v-for="session in sessions"
                :key="session.id"
                class="session-item"
              >
                <div class="session-icon">
                  <i class="fa-duotone fa-desktop"></i>
                </div>
                <div class="session-info">
                  <div class="session-device">
                    {{ session.device }}
                    <Tag
                      v-if="session.current"
                      severity="success"
                      value="Current"
                      class="session-tag"
                    />
                  </div>
                  <div class="session-details">
                    <span>{{ session.location }}</span>
                    <span class="separator">•</span>
                    <span>Last active {{ session.lastActive.toLocaleDateString() }}</span>
                  </div>
                </div>
                <div class="session-actions">
                  <Button
                    v-if="!session.current"
                    label="Revoke"
                    icon="fa-duotone fa-xmark"
                    severity="danger"
                    text
                    @click="revokeSession(session.id)"
                  />
                </div>
              </div>
            </div>
          </div>
        </div>

        <Divider />

        <!-- Two-Factor Authentication -->
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

            <div class="two-factor-status">
              <div class="status-info">
                <i class="fa-duotone fa-circle-exclamation status-icon warning"></i>
                <div>
                  <h4>Not Enabled</h4>
                  <p>Two-factor authentication is currently disabled for your account.</p>
                </div>
              </div>
              <Button
                label="Enable 2FA"
                icon="fa-duotone fa-plus"
                severity="secondary"
                outlined
              />
            </div>
          </div>
        </div>
      </div>
    </template>
  </Card>
</template>

<style lang="scss" scoped>
@use './account-shared.scss' as *;

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

.sessions-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.session-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem;
  border: 1px solid var(--surface-border);
  border-radius: var(--border-radius);
  transition: background-color 0.2s;

  &:hover {
    background: var(--surface-hover);
  }

  .session-icon {
    i {
      font-size: 1.5rem;
      color: var(--text-color-secondary);
    }
  }

  .session-info {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 0.25rem;

    .session-device {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-weight: 600;
      font-size: 1rem;

      .session-tag {
        font-size: 0.75rem;
      }
    }

    .session-details {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.875rem;
      color: var(--text-color-secondary);

      .separator {
        opacity: 0.5;
      }
    }
  }

  .session-actions {
    display: flex;
    gap: 0.5rem;
  }
}

.two-factor-status {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.5rem;
  border: 1px solid var(--surface-border);
  border-radius: var(--border-radius);
  gap: 1rem;

  .status-info {
    h4 {
      margin: 0 0 0.25rem 0;
      font-size: 1.1rem;
    }

    p {
      margin: 0;
      color: var(--text-color-secondary);
      font-size: 0.9rem;
    }
  }
}

@media (max-width: 768px) {
  .session-item {
    flex-direction: column;
    align-items: flex-start;

    .session-actions {
      width: 100%;

      button {
        width: 100%;
      }
    }
  }

  .two-factor-status {
    flex-direction: column;
    align-items: stretch;

    button {
      width: 100%;
    }
  }
}
</style>
