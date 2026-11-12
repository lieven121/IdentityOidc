<script setup lang="ts">
import CopyField from '@/components/Common/CopyField.vue'
import type { ApplicationSecretDto } from '@/resources/api-clients/identity-api-client'

const visible = defineModel<boolean>('visible', { required: true })

defineProps<{
  secret: ApplicationSecretDto | null
}>()
</script>

<template>
  <Dialog
    v-model:visible="visible"
    header="Client Secret"
    :modal="true"
    :closable="false"
    :style="{ width: '40rem' }"
    :breakpoints="{ '640px': '90vw' }"
  >
    <div v-if="secret" class="secret-content">
      <Message severity="warn" :closable="false">
        Copy this secret now and store it securely. It is stored hashed and won't be shown again.
      </Message>
      <CopyField label="Client ID" :value="secret.clientId" />
      <CopyField label="Client Secret" :value="secret.clientSecret ?? ''" />
    </div>
    <template #footer>
      <Button label="I've stored the secret" severity="primary" @click="visible = false" />
    </template>
  </Dialog>
</template>

<style lang="scss" scoped>
.secret-content {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
</style>
