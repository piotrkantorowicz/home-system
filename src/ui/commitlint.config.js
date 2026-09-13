export default {
  extends: ['@commitlint/config-conventional'],
  rules: {
    'type-enum': [
      2,
      'always',
      ['feat', 'fix', 'docs', 'style', 'refactor', 'perf', 'test', 'chore', 'ci', 'revert', 'hotfix'],
    ],
    'subject-case': [2, 'always', 'lower-case'],
    'header-max-length': [2, 'always', 72],
  },
  // Dependabot subjects are already Conventional Commits ("chore(deps): bump x from a to b
  // in /dir") but the generated header often exceeds 72 chars and cannot be shortened
  // from our side, and the nuget ecosystem capitalises "Bump" while npm does not. The
  // prefixes come from .github/dependabot.yml.
  ignores: [(message) => /^(chore|ci)\(deps\): bump /i.test(message)],
};
