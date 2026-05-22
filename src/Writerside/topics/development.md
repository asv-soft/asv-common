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

## Documentation Checklist

For Writerside topics:

- Use Markdown for short conceptual pages and contributor-facing guidance.
- Use one top-level `#` heading per topic.
- Give files stable, descriptive names because they are used as generated page URLs.
- Keep table-of-contents entries in `asv-common.tree` in reader workflow order.
- Prefer linking to existing code or tests over copying long implementation details.
