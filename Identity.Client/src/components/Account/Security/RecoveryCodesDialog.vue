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

const { copy } = useClipboard()
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
      <p class="warning">
        Save these recovery codes in a safe place. You can use them to access your account if you lose access to your
        authenticator app.
      </p>
      <div
        v-if="recoveryCodesData"
        class="codes-section"
      >
        <ul class="codes-list">
          <li
            v-for="code in recoveryCodesData.recoveryCodes"
            :key="code"
          >
            {{ code }}
          </li>
        </ul>
        <Button
          label="Copy Codes"
          icon="fa-duotone fa-copy"
          severity="info"
          @click="() => copy(recoveryCodesData?.recoveryCodes?.join('\n') ?? '')"
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
.codes-section {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  container-type: inline-size;

  .codes-list {
    display: grid;
    grid-template-columns: repeat(2, minmax(120px, 1fr));
    list-style: none;
    font-family: monospace;


    @container (max-width: 400px) {
      grid-template-columns: 1fr;
      text-align: center;
    }
  }
}
</style>
