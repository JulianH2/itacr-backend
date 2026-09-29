// Conventional Commits + ITACR rules. Format: type(scope): short description (ITACR-N)
// Keep TYPES and SCOPES identical in itacr-frontend and itacr-backend.

const TYPES = [
  'feat',
  'fix',
  'docs',
  'style',
  'refactor',
  'perf',
  'test',
  'build',
  'ci',
  'chore',
  'revert',
];

const SCOPES = [
  'auth',
  'catalog',
  'orders',
  'kitchen',
  'tracking',
  'manager',
  'pwa',
  'db',
  'core',
  'deps',
  'repo',
];

const JIRA_KEY_SUFFIX = /\(ITACR-\d+\)$/;

// Dependency bumps (usually opened by Dependabot) do not belong to a Jira issue.
const SCOPES_WITHOUT_JIRA_KEY = ['deps'];

export default {
  extends: ['@commitlint/config-conventional'],
  plugins: [
    {
      rules: {
        'jira-key-suffix': ({ scope, subject }) => {
          if (SCOPES_WITHOUT_JIRA_KEY.includes(scope ?? '')) {
            return [true];
          }
          return [
            JIRA_KEY_SUFFIX.test(subject ?? ''),
            'the subject must end with the Jira key, e.g. "add pin validation (ITACR-7)"',
          ];
        },
      },
    },
  ],
  rules: {
    'type-enum': [2, 'always', TYPES],
    'scope-enum': [2, 'always', SCOPES],
    'scope-empty': [2, 'never'],
    'subject-full-stop': [2, 'never', '.'],
    'jira-key-suffix': [2, 'always'],
  },
};
