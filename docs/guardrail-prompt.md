# The guardrail prompt

The prompt behind this stage, verbatim. This is what the agent ran on top of 3-guidance to produce
Directory.Build.props, .editorconfig, CodeMetricsConfig.txt, the secret scan, scripts/check.mjs and the four Claude
Code hooks. After the session you can try it on a repository of your own.

```text
Set up guardrails in this repo so that every change you make is checked automatically and you see the errors immediately.

Constraints of this lab: the laptops have the .NET 10 SDK, Node 22 or later with npm, and git; no admin rights, no bash, no Python, and some of them run Windows. A .NET tool goes in the local tool manifest (.config/dotnet-tools.json), a NuGet package is pinned in the project file, an npm tool is a devDependency pinned in package-lock.json. Every script must be a Node script (.mjs), never a shell script.

Checks, at the strictest reasonable setting, in a Directory.Build.props at the root so that they apply to every project:
- nullable reference types on, and every warning an error;
- the .NET analyzers at their strictest reasonable level, with the code-style rules enforced in the build;
- complexity at most 10 per method;
- the NuGet vulnerability audit, so that a package with a known vulnerability fails the build;
- a secret scan of the working tree with secretlint.

All of it behind one script: scripts/check.mjs. With a file name as argument it runs the fast checks on that file (the build with its analyzers) and must finish in a few seconds. Without arguments it runs everything, tests included. Exit non-zero on any finding. Add "check": "node scripts/check.mjs" to package.json scripts.

Claude Code hooks in .claude/settings.json, each a Node script under scripts/hooks/:
- PostToolUse on Edit and Write: run scripts/check.mjs on the edited file; on failure exit 2 and print the errors to stderr;
- PreToolUse on Edit and Write: reject with exit 2 any change that disables a check instead of fixing its cause: #pragma warning disable, [SuppressMessage], #nullable disable, the null-forgiving operator used to silence a nullable warning (null!, x!.Member), and edits to the checks' configuration (Directory.Build.props, .editorconfig, the tool manifest), to scripts/ and to .claude/;
- PreToolUse on Bash: reject --no-verify and commands that modify the same protected files;
- Stop: run the full check; if it fails, exit 2 with the failures on stderr so you keep working, at most five rounds in a row (track the round count in a temp file keyed by session id, honour stop_hook_active), then let the human take over.

Test each hook once with a hand-written JSON input before you rely on it.

Then run the full check and fix everything it finds in the existing code. Silence nothing; if you think a finding is wrong, say so instead. At the end, list briefly what the checks found and what you changed.
```
