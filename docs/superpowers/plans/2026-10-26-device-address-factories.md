# Device Address Factories Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Attribute every supported MAVLink source packet to an already discovered device by SystemId/ComponentId, independent of port.

**Architecture:** Existing client-device factories expose address hooks with compatible false defaults. Explorer maintains its own address bindings while retaining discovery factories. Shared MAVLink factory logic handles all packet types, leaving discovery Heartbeat-only.

**Tech Stack:** .NET 10, C#, R3, ObservableCollections, xUnit v3.

**Spec:** docs/superpowers/specs/2026-10-26-device-address-factories-design.md

## Global Constraints

- Preserve unrelated worktree changes. No commits, pushes, releases, remote changes or package publication. No dependency version bumps.
- `DeviceAddress` has canonical, non-empty Family and Value strings, ordinal equality, and invalid default.
- `TryIdentify` on Explorer resolves only discovered devices and never invokes discovery factories, creates devices or refreshes last seen.
- Addresses exclude port, endpoint, codec version, DeviceClass and local client identity.
- Existing factories without address support continue discovery; new interface hooks have false defaults and generic base hooks are virtual.
- Public DeviceId equality and serialized identity formats remain unchanged; use consistent full-identity comparison internally where necessary.
- No generated message edits, standalone address-provider registration or registry service.
- All repository comments and documentation are English.

## Review Focus

- Unsupported and pre-discovery packets return false/null without side effects: Task 1 lookup tests.
- Discovered but not initialized devices resolve immediately: Task 1 initialization-state test.
- Timeout and reacquisition do not leave stale address bindings; lookup does not keep devices alive: Task 1 lifecycle tests.
- Multiple same-class source addresses remain distinct, including deliberately colliding hashes: Task 1 identity test.
- MAVLink address hooks accept non-heartbeat and v1/v2 message bases while discovery remains heartbeat-only: Task 2 contract/integration tests.

### Task 1: Asv.IO factory address contract and Explorer lookup

**Files:**
- Create `src/Asv.IO/Devices/Client/DeviceAddress.cs`.
- Modify `src/Asv.IO/Devices/Explorer/Factories/IClientDeviceFactory.cs`, `IDeviceExplorer.cs`, and `DeviceExplorer.cs`.
- Modify/add focused tests under `src/Asv.IO.Test/Devices/Explorer/`.
- Update `src/Writerside/topics/IO-Devices.md` and `Asv-IO.md` for new lookup semantics.

**Produces:** public immutable `DeviceAddress(string family, string value)` with get-only `Family` and `Value`; the two exact factory methods from Spec; restored Explorer TryIdentify signature. Generic factory has public virtual implementations for both address methods with false defaults, so MAVLink can override on IProtocolMessage rather than TMessageBase.

**User clarification during execution:** expose both exact address methods on IDeviceExplorer as compatible false defaults and implement ordered, discovery-independent pure extraction in DeviceExplorer. Cover both methods before discovery, unsupported input, null input, first successful factory Order and absence of discovery/liveness side effects.

- [x] Write failing regression tests before production code. Cover known address from a non-discovery message; undiscovered/unknown input; null input; no factory registration; no address support retaining discovery; no factory discovery calls during public lookup; lookup before initialization completes; no liveness refresh; timeout, same-address reacquisition; same address ignores channel tags and differing discovered local identities; different addresses with same class/hash coexist; sync/async disposal clear state. Use deterministic existing in-memory test conventions, literals for expected addresses and real Explorer/factory logic. Avoid physical ports and sleeps.
- [x] Run focused tests, record RED failure (missing API compile failures are expected initially; verify behavioral RED where practical once additive stubs compile).
- [x] Implement minimum production changes. Keep address index lookup under existing synchronization, consistent with device creation/removal. First successful factory address extractor in existing Order supplies canonical source address. Deduplicate addressed discovery before creation; update using owning factory. Unsupported factory discovery remains intact. Keep public DeviceId equality unchanged, apply internal full-identity comparer consistently where needed. Do not refactor unrelated lifecycle behavior.
- [x] Run focused tests to clean exit, then full Asv.IO.Test suite once with CI=true. Existing unrelated warnings must be reported, not hidden or fixed outside scope.
- [x] Update English docs with a compiling discovery/lookup example and explicit false-before-discovery/source-only semantics.
- [x] Self-review and write report with exact RED/GREEN commands/results and affected files. No commit.

### Task 2: Shared MAVLink address hooks and cross-repository integration

**Files:**
- Modify `/Users/asv/RiderProjects/asv-mavlink/src/Asv.Mavlink/Devices/Client/MavlinkClientDevice.cs`.
- Create focused tests `/Users/asv/RiderProjects/asv-mavlink/src/Asv.Mavlink.Test/Devices/Client/MavlinkDeviceExplorerTest.cs` (split only if needed for existing style).
- Update existing `/Users/asv/RiderProjects/asv-mavlink/src/Writerside/topics/welcome-page.md` with a short device-address section.

**Consumes:** exact DeviceAddress, factory hooks, restored Explorer TryIdentify from Task 1. Uses current MavlinkMessage.SystemId/ComponentId and MavlinkClientDeviceId.Id.Target.

Also consume the user-requested Explorer address facades: verify source and DeviceId addresses can be extracted through IDeviceExplorer before any live discovery, for future File Inspector/Traffic Inspector callers.

- [x] Write failing tests first against the local Asv.IO contract. Assert literal `new DeviceAddress("mavlink", "1:1")`; changes to local Self/port/endpoint do not alter it; differing system or component changes it. Cover unsupported inputs. Real Explorer integration: discover one Generic device through Heartbeat, then every generated supported packet from the same sender resolves the exact device ID; other senders return false. Same sender across two real in-memory/virtual endpoints yields one discovered device; receiver matches regardless of endpoint. Removal/reacquisition resolves only the new live registration. Verify ordinary non-heartbeat packets do not trigger discovery. V1 extraction may use a minimal test-only subtype because current v1 wire factory is a stub; do not implement v1 serialization/parsing.
- [x] Create a temporary MSBuild targets import outside tracked files that replaces existing ASV PackageReferences in MAVLink projects with corresponding common ProjectReferences. Check evaluated references, restore with this import and run focused tests RED. Use the same import consistently for build/test; no csproj dependency rewrites or version bumps.
- [x] Implement public overrides once in MavlinkClientDeviceFactory<TDevice>: accept any MavlinkMessage, construct canonical invariant address from source; derive DeviceId address from Id.Target. Keep InternalTryIdentify/creation heartbeat-specific and all generated files unchanged.
- [x] Run focused tests GREEN, then full MAVLink test project once with CI=true using local-source import, and build relevant solution/library projects. Record unrelated failures by test name; do not fix unrelated tests or broaden protocols.
- [x] Add English docs for discovered-only source lookup and common address across channels; state source vs destination. Self-review, write report with RED/GREEN commands/results, limitation on v1 wire coverage and dependency validation. No commit. Restore ordinary MAVLink assets without the import after validation if feasible.

## Final validation

- [x] Independent task review after each task; fix Important/Critical findings before the next task.
- [x] One broad review of the final scoped diffs across both repositories.
- [x] Verify final diffs, test evidence, preserved unrelated changes and no commit/push/publication. Report module test totals, dependency-source validation and remaining limitations.
