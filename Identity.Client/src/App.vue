<script setup lang="ts"></script>

<template>
  <RouterView />
  <Toast
    position="bottom-right"
    group="notifications"
  />
  <DynamicDialog />
</template>

<style lang="scss">
#app {
  display: grid;
  height: 100%;
}
</style>
