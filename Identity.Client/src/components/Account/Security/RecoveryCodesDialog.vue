<script setup lang="ts">
import { RecoveryCodesDto } from '@/resources/api-clients/identity-api-client'

interface Props {
  visible: boolean
  recoveryCodesData: RecoveryCodesDto | null
}

interface Emits {
  (e: 'update:visible', value: boolean): void
}

defineProps<Props>()
const emit = defineEmits<Emits>()

const copyRecoveryCodes = () => {
  const codes = document.querySelector('.codes-textarea') as HTMLTextAreaElement
  if (codes) {
    codes.select()
    navigator.clipboard.writeText(codes.value)
  }
}
</script>

<template>
  <Dialog
    :visible="visible"
    header="Save Recovery Codes"
    :modal="true"
    class="recovery-codes-dialog"
    @update:visible="(val) => emit('update:visible', val)"
  >
    <div class="recovery-content">
      <Message severity="warning">
        Save these recovery codes in a safe place. You can use them to access your account if you lose access to your
        authenticator app.
      </Message>
      <div
        v-if="recoveryCodesData"
        class="codes-section"
      >
        <textarea
          readonly
          :value="recoveryCodesData.recoveryCodes?.join('\n')"
          class="codes-textarea"
        ></textarea>
        <Button
          label="Copy Codes"
          icon="fa-duotone fa-copy"
          severity="info"
          @click="copyRecoveryCodes"
        />
      </div>
    </div>
    <template #footer>
      <Button
        label="Done"
        icon="fa-duotone fa-check"
        @click="() => emit('update:visible', false)"
      />
    </template>
  </Dialog>
</template>

<style lang="scss" scoped>
.recovery-codes-dialog {
  :deep(.p-dialog-content) {
    padding: 1.5rem;
  }
}

.recovery-content {
  display: flex;
  flex-direction: column;
  gap: 1rem;

  .codes-section {
    display: flex;
    flex-direction: column;
    gap: 1rem;

    .codes-textarea {
      width: 100%;
      min-height: 200px;
      padding: 1rem;
      border: 1px solid var(--surface-border);
      border-radius: var(--border-radius);
      font-family: monospace;
      font-size: 0.9rem;
      background: var(--surface-section);
      color: var(--text-color);
      resize: vertical;

      &:focus {
        outline: none;
        border-color: var(--primary-color);
      }
    }
  }
}
</style>
