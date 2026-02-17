<script setup lang="ts">
withDefaults(
  defineProps<{
    inputId: string
    label?: string
  }>(),
  {
    inputId: () => 'input-' + Math.random().toString(36).substring(7),
  },
)
</script>

<template>
  <div>
    <label
      v-if="label"
      :for="inputId"
    >{{ label }}</label>
    <slot :inputId="inputId"></slot>
  </div>
</template>

<style lang="scss" scoped>
label {
  display: block;
  margin-bottom: var(--label-margin-bottom, 0.5rem);
  font-weight: 600;
  font-size: 0.9rem;
}
</style>
