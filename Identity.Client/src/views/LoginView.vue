<script setup lang="ts">
import logo from '@/assets/logo.svg'

</script>

<template>
  <div class="login-view">
    <LoginForm
      position="center"
      :logo="logo"
    />
  </div>
</template>

<style lang="scss">
.login-view {

  // --login-wrapper-color: #3338;
  // --login-wrapper-color: #fff8;
  .login-background {
    background-image: url('@/assets/bg.png');
    background-repeat: no-repeat;
    background-size: cover;
    background-position: right;
    background-position-x: 80%;
  }

  .login-card {
    .p-card-body {
      .logo-wrapper {
        .logo {
          margin: 1rem auto;
        }
      }

      h2 {
        text-align: left;
      }
    }
  }

}
</style>
