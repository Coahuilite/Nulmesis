module.exports = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    'type-enum': [
      2,
      'always',
      ['feat', 'fix', 'docs', 'refactor', 'test', 'chore', 'build', 'ci', 'perf'],
    ],
    'scope-enum': [
      1,
      'always',
      ['core', 'cli', 'wpf', 'release', 'tests', 'docs'],
    ],
  },
};
