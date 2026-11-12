import { fileURLToPath, URL } from 'node:url'

import { defineConfig, lazyPlugins } from 'vite-plus'
import vue from '@vitejs/plugin-vue'
import Components from 'unplugin-vue-components/vite'
import AutoImport from 'unplugin-auto-import/vite'
import { PrimeVueResolver } from '@primevue/auto-import-resolver'
import vueDevTools from 'vite-plugin-vue-devtools'

// https://vite.dev/config/
export default defineConfig({
  lint: {
    plugins: ['oxc', 'typescript', 'unicorn', 'vue'],
    categories: {
      correctness: 'warn',
    },
    env: {
      builtin: true,
    },
    ignorePatterns: ['**/dist/**', '**/dist-ssr/**', '**/coverage/**'],
    rules: {
      'vue/no-arrow-functions-in-watch': 'error',
      'vue/no-async-in-computed-properties': 'error',
      'vue/no-computed-properties-in-data': 'error',
      'vue/no-deprecated-data-object-declaration': 'error',
      'vue/no-deprecated-delete-set': 'error',
      'vue/no-deprecated-destroyed-lifecycle': 'error',
      'vue/no-deprecated-events-api': 'error',
      'vue/no-deprecated-model-definition': 'error',
      'vue/no-deprecated-props-default-this': 'error',
      'vue/no-deprecated-vue-config-keycodes': 'error',
      'vue/no-dupe-keys': 'error',
      'vue/no-export-in-script-setup': 'error',
      'vue/no-expose-after-await': 'error',
      'vue/no-lifecycle-after-await': 'error',
      'vue/no-reserved-component-names': 'error',
      'vue/no-reserved-keys': 'error',
      'vue/no-reserved-props': 'error',
      'vue/no-shared-component-data': 'error',
      'vue/no-side-effects-in-computed-properties': 'error',
      'vue/no-watch-after-await': 'error',
      'vue/prefer-import-from-vue': 'error',
      'vue/require-prop-type-constructor': 'error',
      'vue/require-render-return': 'error',
      'vue/require-slots-as-functions': 'error',
      'vue/return-in-computed-property': 'error',
      'vue/return-in-emits-validator': 'error',
      'vue/valid-define-emits': 'error',
      'vue/valid-define-options': 'error',
      'vue/valid-define-props': 'error',
      'vue/valid-next-tick': 'error',
      'no-array-constructor': 'error',
      'no-unused-expressions': 'error',
      'no-unused-vars': 'error',
      'typescript/ban-ts-comment': 'error',
      'typescript/no-duplicate-enum-values': 'error',
      'typescript/no-empty-object-type': 'error',
      'typescript/no-explicit-any': 'error',
      'typescript/no-extra-non-null-assertion': 'error',
      'typescript/no-misused-new': 'error',
      'typescript/no-namespace': 'error',
      'typescript/no-non-null-asserted-optional-chain': 'error',
      'typescript/no-require-imports': 'error',
      'typescript/no-this-alias': 'error',
      'typescript/no-unnecessary-type-constraint': 'error',
      'typescript/no-unsafe-declaration-merging': 'error',
      'typescript/no-unsafe-function-type': 'error',
      'typescript/no-wrapper-object-types': 'error',
      'typescript/prefer-as-const': 'error',
      'typescript/prefer-namespace-keyword': 'error',
      'typescript/triple-slash-reference': 'error',
      'vite-plus/prefer-vite-plus-imports': 'error',
    },
    overrides: [
      {
        files: ['**/*.{ts,mts,tsx,vue}'],
        rules: {
          'typescript/no-explicit-any': 'off',
        },
      },
      {
        files: ['**/*.ts', '**/*.tsx', '**/*.mts', '**/*.cts', '**/*.vue'],
        rules: {
          'constructor-super': 'off',
          'getter-return': 'off',
          'no-class-assign': 'off',
          'no-const-assign': 'off',
          'no-dupe-class-members': 'off',
          'no-dupe-keys': 'off',
          'no-func-assign': 'off',
          'no-import-assign': 'off',
          'no-new-native-nonconstructor': 'off',
          'no-obj-calls': 'off',
          'no-redeclare': 'off',
          'no-setter-return': 'off',
          'no-this-before-super': 'off',
          'no-undef': 'off',
          'no-unreachable': 'off',
          'no-unsafe-negation': 'off',
          'no-var': 'error',
          'no-with': 'off',
          'prefer-const': 'error',
          'prefer-rest-params': 'error',
          'prefer-spread': 'error',
        },
      },
    ],
    options: {
      typeAware: true,
      typeCheck: true,
    },
    jsPlugins: [
      {
        name: 'vite-plus',
        specifier: 'vite-plus/oxlint-plugin',
      },
    ],
  },
  fmt: {
    semi: false,
    singleQuote: true,
    printWidth: 100,
    sortPackageJson: false,
    // Vendored and generated files.
    ignorePatterns: [
      'src/assets/font-awesome/**',
      'src/resources/api-clients/identity-api-client.ts',
      'src/components.d.ts',
      'auto-imports.d.ts',
      '.biomelintrc-auto-import.json',
    ],
  },
  plugins: lazyPlugins(() => [
    vue(),
    vueDevTools(),
    AutoImport({
      include: [
        /\.[tj]sx?$/, // .ts, .tsx, .js, .jsx
        /\.vue$/,
        /\.vue\?vue/, // .vue
        /\.md$/, // .md
      ],

      // global imports to register
      imports: [
        // presets
        'vue',
        'vue-router',
        '@vueuse/core',

        // custom
        {
          // '@vueuse/core': [
          //   // named imports
          //   'useMouse', // import { useMouse } from '@vueuse/core',
          //   // alias
          //   ['useFetch', 'useMyFetch'] // import { useFetch as useMyFetch } from '@vueuse/core',
          // ],
          // '[package-name]': [
          //   '[import-names]',
          //   // alias
          //   ['[from]', '[alias]']
          // ]
        },
        // example type import
        // {
        //   from: 'vue-router',
        //   imports: ['RouteLocationRaw'],
        //   type: true
        // }
      ],

      // Array of strings of regexes that contains imports meant to be filtered out.
      ignore: ['useMouse', 'useFetch'],

      // Enable auto import by filename for default module exports under directories
      defaultExportByFilename: false,

      // Auto import for module exports under directories
      // by default it only scan one level of modules under the directory
      dirs: [
        // './hooks',
        // './composables' // only root modules
        // ...
        './src/composables/**', // all nested modules
        './src/stores/**', // all nested modules
        // './src/resources/api-clients/*'
      ],

      // Filepath to generate corresponding .d.ts file.
      // Defaults to './auto-imports.d.ts' when `typescript` is installed locally.
      // Set `false` to disable.
      dts: './auto-imports.d.ts',

      // Array of strings of regexes that contains imports meant to be ignored during
      // the declaration file generation. You may find this useful when you need to provide
      // a custom signature for a function.
      ignoreDts: ['ignoredFunction', /^ignore_/],

      // Auto import inside Vue template
      // see https://github.com/unjs/unimport/pull/15 and https://github.com/unjs/unimport/pull/72
      vueTemplate: false,

      // Custom resolvers, compatible with `unplugin-vue-components`
      // see https://github.com/antfu/unplugin-auto-import/pull/23/
      resolvers: [
        // PrimeVueResolver()
        /* ... */
      ],

      // Include auto-imported packages in Vite's `optimizeDeps` options
      // Recommend to enable
      viteOptimizeDeps: true,

      // Inject the imports at the end of other imports
      injectAtEnd: true,

      // Generate corresponding .eslintrc-auto-import.json file.
      // eslint globals Docs - https://eslint.org/docs/user-guide/configuring/language-options#specifying-globals
      eslintrc: {
        enabled: false, // Default `false`
        // provide path ending with `.mjs` or `.cjs` to generate the file with the respective format
        filepath: './.eslintrc-auto-import.json', // Default `./.eslintrc-auto-import.json`
        globalsPropValue: true, // Default `true`, (true | false | 'readonly' | 'readable' | 'writable' | 'writeable')
      },

      // Generate corresponding .biomelintrc-auto-import.json file.
      // biomejs extends Docs - https://biomejs.dev/guides/how-biome-works/#the-extends-option
      biomelintrc: {
        enabled: false, // Default `false`
        filepath: './.biomelintrc-auto-import.json', // Default `./.biomelintrc-auto-import.json`
      },
    }),
    Components({
      extensions: ['vue', 'md'],
      include: [/\.vue$/, /\.vue\?vue/, /\.md$/],
      dts: 'src/components.d.ts',
      resolvers: [PrimeVueResolver()],
    }),
  ]),
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
})
