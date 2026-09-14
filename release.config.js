// semantic-release — runs in .github/workflows/release.yml on every push to main.
//
// Version = the git tag only. Nothing is written back to the repo (no CHANGELOG.md, no
// package.json bump), so the bot never commits to main. Release notes live on the GitHub
// Release; the backend can take `-p:Version=` from the tag when a deploy pipeline exists.
//
// conventional-changelog-conventionalcommits@10 renders through conventional-changelog-writer@9
// (render functions instead of Handlebars) and marks hidden types with `effect: 'hidden'`.
// semantic-release's release-notes-generator still pins writer ^8, so package.json
// overrides the writer to 9 until upstream catches up.
//
// Bump rules (Conventional Commits, one commit per squash-merged PR; epic branches are
// rebase-merged so every child commit is analysed on its own):
//   feat                                  → minor
//   fix, perf, refactor, hotfix, revert   → patch
//   type! / "BREAKING CHANGE:" footer     → major
//   docs, style, test, chore, ci, build   → no release
export default {
  branches: ['main'],
  tagFormat: 'v${version}',
  plugins: [
    [
      '@semantic-release/commit-analyzer',
      {
        preset: 'conventionalcommits',
        releaseRules: [
          { type: 'refactor', release: 'patch' },
          { type: 'hotfix', release: 'patch' },
          { type: 'revert', release: 'patch' },
        ],
      },
    ],
    [
      '@semantic-release/release-notes-generator',
      {
        preset: 'conventionalcommits',
        presetConfig: {
          types: [
            { type: 'feat', section: 'Features' },
            { type: 'fix', section: 'Bug Fixes' },
            { type: 'hotfix', section: 'Bug Fixes' },
            { type: 'perf', section: 'Performance' },
            { type: 'refactor', section: 'Refactoring' },
            { type: 'revert', section: 'Reverts' },
            { type: 'docs', effect: 'hidden' },
            { type: 'style', effect: 'hidden' },
            { type: 'test', effect: 'hidden' },
            { type: 'chore', effect: 'hidden' },
            { type: 'ci', effect: 'hidden' },
            { type: 'build', effect: 'hidden' },
          ],
        },
      },
    ],
    [
      '@semantic-release/github',
      {
        // One "released in vX.Y.Z" comment per closed issue / merged PR is useful; the
        // extra "released" label is noise on a small board.
        releasedLabels: false,
      },
    ],
  ],
};
