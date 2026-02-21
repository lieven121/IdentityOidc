<script setup lang="ts">
import type { DynamicDialogInstance } from 'primevue/dynamicdialogoptions';
import type { ConfirmDialogModel } from './ConfirmDialogModel';

const dialogRef = inject<Ref<DynamicDialogInstance>>('dialogRef');

const options = ref<ConfirmDialogModel>({
  title: 'Confirm',
  message: 'Are you sure you want to proceed?',
  confirm: {
    text: 'Confirm',
    severity: 'success'
  },
  cancel: {
    text: 'Cancel',
    severity: 'secondary'
  }
} as ConfirmDialogModel);



onMounted(() => {
  const params = dialogRef?.value.data;
  if (params) {
    options.value.title = params.title;
    options.value.message = params.message;
    options.value.confirm = params.confirm;
    options.value.cancel = params.cancel;
  }
})

function confirm() {
  dialogRef?.value.close(true);
}

function cancel() {
  dialogRef?.value.close(false);
}
</script>

<template>
  <div class="confirm-dialog">
    <div>
      <h2 class="confirm-title">{{ options.title }}</h2>
      <p class="confirm-message">{{ options.message }}</p>
    </div>
    <div class="actions">
      <Button
        :label="options.cancel.text"
        :severity="options.cancel.severity"
        @click="cancel"
        class="p-button-text"
      />
      <Button
        :label="options.confirm.text"
        :severity="options.confirm.severity"
        @click="confirm"
      />
    </div>
  </div>
</template>

<style lang="scss" scoped>
.actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
}
</style>
