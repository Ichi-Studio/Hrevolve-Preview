module.exports = {
    //脚本運行的環境
  env: {
    browser: true, //啓用瀏覽器全局變量
    es2021: true, //啓用ES2021語法支持
    node: true //啓用Node.js全局變量
  },
  //繼承預定義的配置
  extends: [
    'eslint:recommended',  //eslint基本規則
    'plugin:vue/vue3-recommended', //eslint-plugin-vue插件為Vue3提供推薦規則
    'plugin:@typescript-eslint/recommended', //@typescript-eslint插件的推薦規則
    'plugin:prettier/recommended' //禁用所有與Prettier衝突的規則
  ],
  //指定ESLint使用的語法解析器
  parser: 'vue-eslint-parser',
  //配置解析器選項
  parserOptions: {
    ecmaVersion: 'latest',//使用最新版本的ECMAScript語法
    parser: '@typescript-eslint/parser',//用於解析<script>部分的TypeScript代碼
    sourceType: 'module', //代碼使用ES6模塊系統(import/export)
    ecmaFeatures: {
      jsx: false //不使用JSX
    }
  },
  plugins: ['vue', '@typescript-eslint'], //加載ESLint插件
  rules: {
    // 自定義规则配置...
  },
  settings: { //為插件提供全局設置
    'import/resolver': {
      typescript: {
        alwaysTryTypes: true
      }
    }
  },
  ignorePatterns: ['dist', 'node_modules', '*.d.ts'] //ignorePatterns 忽略文件
};