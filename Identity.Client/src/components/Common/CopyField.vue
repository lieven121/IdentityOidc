<script setup lang="ts">
import { useClipboard } from '@vueuse/core'
import { useToast } from 'primevue';

const toast = useToast();

const props = withDefaults(
  defineProps<{
    inputId?: string
    label?: string
    value?: string
    extendedValue?: string
  }>(),
  {
    inputId: () => 'input-' + Math.random().toString(36).substring(7),
  }
)

const copyValue = computed(() => props.extendedValue || props.value)

const { isSupported, copy, copied } = useClipboard()
const copyToClipboard = (text?: string) => {
  if (isSupported.value && text) {
    copy(text)
  }
}

watch(copied, (newValue) => {
  if (newValue) {
    toast.add({
      severity: 'info',
      summary: 'Copied to clipboard',
      detail: 'The value has been copied successfully.',
      life: 3000,
      group: 'notifications'
    })
  }
})

</script>

<template>
  <LabelInput
    class="copy-field"
    :inputId="props.inputId"
    :label="props.label"
  >
    <template #default>
      <div class="value-with-copy">
        <code>{{ props.value }}</code>
        <Button
          icon="fa-duotone fa-copy"
          text
          size="small"
          v-tooltip.top="`Copy ${label}`"
          :disabled="!isSupported || !copyValue"
          @click="() => copyToClipboard(copyValue)"
        />
      </div>
    </template>
  </LabelInput>
</template>

<style lang="scss" scoped>
.copy-field {
  --label-margin-bottom: 0rem;
}
</style>
