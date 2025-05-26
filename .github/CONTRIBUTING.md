# Contributing Guidelines

First off, thank you for considering contributing to this project.

Following these guidelines helps to communicate that you respect the time of the developers managing and developing this open source project. In return, they should reciprocate that respect in addressing your issue, assessing changes, and helping you finalize your pull requests.

This is an open source project, and we love to receive contributions from our community — you! There are many ways to contribute, from writing tutorials or blog posts, improving the documentation, submitting bug reports and feature requests or writing code which can be incorporated into the SnapX itself.

## Ground Rules

By contributing to this project, you agree to follow our [Code of Conduct](./CODE_OF_CONDUCT.md).
Please read this next section carefully and check if your contribution meets all requirements in this checklist.
This will save you and us a lot of time.

## Issues: Feature Requests & Bug Reports

Please make sure you checked all the following before submitting a new issue.

- Before submitting a new issue, search the existing (including closed) issues for any duplicates.
- If you found an existing issue - use the react feature to show you have the same problem or need the same feature. Don't post comments like +1.
- Use the Bug & Feature Issue templates and provide all the required information.
- Be as detailed as possible and include screenshots wherever possible. This will help reproducing your bug or understanding your feature request.
- Do not create GitHub issues with AI.

## Contributing Code

Pull requests are the best way to propose changes to the codebase (we use [GitHub Flow](https://docs.github.com/en/get-started/quickstart/github-flow)). We actively welcome your pull requests:

- **Commit using the [conventional-commit](https://www.conventionalcommits.org/en/v1.0.0/) format!**
- Create issues for any major changes and enhancements that you wish to make. Discuss things transparently and get community feedback.
- Keep feature versions as small as possible, preferably one new feature per version.
- [Be welcoming to newcomers and encourage diverse new contributors from all backgrounds](./CODE_OF_CONDUCT.md).

## Your First Contribution

Unsure where to begin contributing to this project? You can start by looking through these beginner and help-wanted issues: Good first issues - issues which should only require a few lines of code, and a test or two. Help wanted issues - issues which should be a bit more involved than beginner issues.

Remember, all the code and comments you write should be in American English.

Working on your first Pull Request? You can learn how from this article, [How to Contribute to an Open Source Project on GitHub](https://docs.github.com/en/get-started/exploring-projects-on-github/contributing-to-a-project).

At this point, you're ready to make your changes! Feel free to ask for help; everyone is a beginner at first :smile_cat:

If a maintainer asks you to "rebase" your PR, they're saying that a lot of code has changed in the `develop` branch, and that you need to update your branch, so it's easier to merge.

## Getting started

1. Fork the repo and create your branch from `develop`. Name it accordingly using the [Conventional Branch standard](https://conventional-branch.github.io/).
2. If you've added code that should be tested, add tests.
3. If you've changed APIs or added new features, update the documentation.
4. Ensure the test suite passes.
5. Test your changes.
6. Commit & Push.
7. Send the dang pull request to the `develop` branch.

## Code review process

The core team will look at your change as soon as possible and review it. However, since this a project that is done in free time, reviewing might sometimes take some time.

### Code, commit message and labeling conventions

We have very precise rules over how our Git commit messages can be formatted. This leads to **more readable messages** that are easy to follow when looking through the **commit history**.

It is important to note that we use the Git commit messages to **generate** the [CHANGELOG](./CHANGELOG.md) document. Improperly formatted commit messages may result in your change not appearing in the CHANGELOG of the next release.

### <a name="commit-message-format"></a> Commit Message Format

Each commit message consists of a **header**, a **body** and a **footer**. The header has a special format that includes a **type**, a **scope** and a **subject**:

```html
<type
  >(<scope
    >):
    <subject>
      <BLANK LINE>
        <body>
          <BLANK LINE> <footer></footer></BLANK></body></BLANK></subject></scope
></type>
```

> Any line of the commit message cannot be longer 100 characters!<br/> This allows the message to be easier to read on GitHub as well as in various Git tools.

##### Type

Must be one of the following:

- **feat**: A new feature
- **fix**: A bug fix
- **docs**: Documentation only changes
- **style**: Changes that do not affect the meaning of the code (white-space, formatting, missing semicolons, etc.)
- **refactor**: A code change that neither fixes a bug nor adds a feature
- **perf**: A code change that improves performance
- **test**: Adding missing tests
- **ci**: Anything related to the CI/CD process ie GitHub Actions
- **build**: Anything related to the building process including packaging such as RPM
- **chore**: Everything else

##### Scope

The scope could be anything that helps to specify the scope (or feature) that is changing.
It is not required.

Examples

- Avalonia
- CLI
- spec (snapx.spec)
- PKGBUILD (PKGBUILD)
- CommonUI
- SharpCapture
- NMH (NativeMessagingHost)
- Core

##### Subject

The subject contains a succinct description of the change:

- use the imperative, present tense: "change" not "changed" nor "changes"
- don't capitalize the first letter
- no dot (.) at the end

##### Body

Just as in the **subject**, use the imperative, present tense: "change" not "changed" nor "changes" The body should include the motivation for the change and contrast this with previous behavior.

##### Footer

The footer should contain any information about **Breaking Changes** and is also the place to reference GitHub issues that this commit **Closes**, **Fixes**, or **Relates to**.

> Breaking Changes are intended to be highlighted in the ChangeLog as changes that will require community users to modify their code after updating to a version that contains this commit.

##### Sample Commit messages

```text
fix(ui): avoid loading tray icon when show is set to false

* for some reason, setting show: false in the window options makes it render

Fixes #11231
```

```text
feat(settings): add new setting to enable dark mode

* new setting added to enable dark mode

Fixes #3857
```

````text
refactor(Core): use asynchronous save instead of sync
````

<br/>

## Releasing a new version of SnapX

One of the major benefits of this approach is the fact that it will **automatically release a new version** on every successful push to either `develop` based on the commit messages. This makes sure SnapX is released following the [semantic versioning](https://semver.org/) guidelines. For this to work you have to follow a few simple rules:

- The master branch should always have working code that is tested. _Use Pull Requests to work on bigger features or bug fixes and merge them when you are ready._
- Every bugfix, feature and change should have one commit associated with it. _Do not mix multiple bugs, features, etc. into one huge commit message. Keep your commit size small and commit often._
- Your commit messages must follow the [conventional commit rules](https://www.conventionalcommits.org/) additionally, the Angular flavor is accepted as well.

## One More Thing

All commits from humans ie (NOT GitHub Actions) should be signed. Please take some time to learn how to [sign your commits if you haven't already](https://docs.github.com/en/authentication/managing-commit-signature-verification/signing-commits).
