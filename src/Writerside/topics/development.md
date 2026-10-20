# Development

## Code Conventions

- Keep nullable reference types enabled and handle nullability explicitly.
- Prefer existing helpers from this repository before adding new abstractions.
- Keep public APIs small and stable; shared packages are consumed by multiple ASV projects.
- Treat serialization formats and stored data contracts as compatibility-sensitive.
- Add XML documentation to public APIs when the behavior is not obvious from the member name and type signature.

## Change Checklist

Before submitting a change:

1. Build the affected projects.
2. Run the matching test projects.
3. Update package-level or Writerside documentation when behavior, workflows, or module boundaries change.
4. Check that new dependencies are necessary and remain inside the intended package boundary.
5. Keep generated artifacts, local IDE files, and benchmark outputs out of commits unless they are part of the requested change.

## Time And Synchronization In Tests

Pass a controllable `TimeProvider` to the component whose timers are being tested, then advance that same provider explicitly. Creating a fake provider without injecting it does not control the component's timers or operating-system I/O.

Use completion signals or observable events to wait for asynchronous work. Avoid `Thread.Sleep`, finite `Task.Delay`, and elapsed-time comparisons as substitutes for synchronization. A real-time timeout may bound a hung test; it should not determine when the expected behavior occurs.

When an observable callback completes a test signal, use `TaskCompletionSource` with `RunContinuationsAsynchronously` and dispose the subscription when the wait ends. This keeps test continuations from changing state or adding subscriptions inside an in-flight notification. See `ProtocolPortLifecycleTest.WhenStatus` for this pattern and `Reconnect_UsesProviderAndDisableReleasesPendingTimer` for separate virtual-time advancement and completion checks.

## Documentation Checklist

For Writerside topics:

- Use Markdown for short conceptual pages and contributor-facing guidance.
- Use one top-level `#` heading per topic.
- Give files stable, descriptive names because they are used as generated page URLs.
- Keep table-of-contents entries in `asv-common.tree` in reader workflow order.
- Prefer linking to existing code or tests over copying long implementation details.

## Code Block Languages

Use `csharp` for C# examples, `bash` for shell commands, and `mermaid` for diagrams. For blocks without syntax highlighting, use Writerside's `plain text` identifier rather than `text`, which produces `CDE016` in this documentation. See the [Writerside code-block reference](https://www.jetbrains.com/help/writerside/semantic-markup-reference.html#code-block) for the plain-text language setting.

## Publishing Documentation

The `Publish documentation` GitHub Actions workflow builds and checks this Writerside instance. Pull requests that change `src/Writerside/**`, `src/Directory.Build.props`, or the workflow are checked without deployment. Matching changes pushed to `main` publish the site to [GitHub Pages](https://asv-soft.github.io/asv-common/). The workflow can also be started manually from the Actions tab; only runs on `main` deploy.

Before building, the workflow reads `ProductVersion` from `src/Directory.Build.props` and sets the instance's `version` attribute in its temporary checkout of `writerside.cfg`. Writerside displays this version next to the documentation name in the header, including development or release-candidate suffixes. There is no separate documentation version to update manually; local previews do not receive this CI-generated attribute.

Before the first deployment, open the repository's **Settings → Pages** and select **GitHub Actions** as the build and deployment source. No custom deployment token is required: the workflow uses the built-in `GITHUB_TOKEN`.

The workflow uses the [official Writerside Docker builder](https://www.jetbrains.com/help/writerside/build-with-docker.html) and checks its report before uploading the website. A failed build or documentation check prevents publication.
