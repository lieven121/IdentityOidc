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
    options.value.input = params.input;
  }
})

const inputValue = ref('');
const confirmDisabled = computed(() => !!options.value.input && !inputValue.value.trim());

function confirm() {
  if (confirmDisabled.value) return;
  if (!options.value.input) {
    dialogRef?.value.close(true);
    return;
  }
  // Passwords may legitimately start or end with spaces.
  dialogRef?.value.close(options.value.input.type === 'password' ? inputValue.value : inputValue.value.trim());
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
      <div
        v-if="options.input"
        class="confirm-input"
      >
        <label for="ConfirmDialogInput">{{ options.input.label }}</label>
        <InputText
          id="ConfirmDialogInput"
          v-model="inputValue"
          :type="options.input.type ?? 'text'"
          :placeholder="options.input.placeholder"
          :autocomplete="options.input.autocomplete ?? 'off'"
          autofocus
          @keydown.enter="confirm"
        />
      </div>
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
        :disabled="confirmDisabled"
        @click="confirm"
      />
    </div>
  </div>
</template>

<style lang="scss" scoped>
.confirm-input {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
}
</style>
