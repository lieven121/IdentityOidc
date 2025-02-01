<script setup lang="ts">
withDefaults(
  defineProps<{
    inputId: string
  }>(),
  {
    inputId: () => 'input-' + Math.random().toString(36).substring(7),
  },
)
</script>

<template>
  <div>
    <label for="input"></label>
    <slot :inputId="inputId"></slot>
  </div>
</template>

<style lang="scss" scoped></style>
