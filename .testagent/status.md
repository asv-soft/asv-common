# Completed regression review

All five reported defects are fixed and covered:
- GetValues_CallbackThrows_ReleasesLock: both existing-key and missing-key callback failures propagate the same exception; subsequent reads and writes succeed. ClearOld_CallbackThrows_ReleasesLock verifies the related upgradeable-lock failure path.
- GetBitU_AllWidths_ReturnsOnlyRequestedBits and SetBitU_AllWidths_PreservesOtherBits: widths 0..32, exact results using an independent 64-bit mask oracle, nonzero offsets, and unchanged neighboring bits.
- Indexer_InvalidIndex_ThrowsWithoutChangingValue: negative, equal-to-size, and beyond-size reads/writes are rejected while valid bits remain unchanged. BitRange_InvalidRange_ThrowsWithoutChangingValue also covers negative widths and integer-overflow input.
- Indexer_NegativeIndex_ThrowsWithoutChangingContents: negative reads and writes fail before and after wrapping while array contents remain intact.
- GetItemsWithTimeMoreThen_SelectsStrictlyNewerItems: all/some/none, exact timestamp boundaries, out-of-order insertion, preserved underlying ascending order; empty selection is tested separately.

Assertion review completed inline: concrete values/order, exception identity, preserved state, and successful subsequent dictionary operations. No reflection, external service or timing-based tests were added. No test-gap-analysis or assertion-quality tool was available.

Before production changes: focused VSTest run failed 17 cases and passed 6 (23 total), proving regression sensitivity.
After changes: focused VSTest run passed all 23 cases.
Final validation: `dotnet test src/Asv.Common.Test/Asv.Common.Test.csproj --no-restore -m:1 -p:GeneratePackageOnBuild=false -v:minimal` passed 829, failed 0, skipped 0, exit 0; no warnings.
Solution verification: `dotnet build src/Asv.Common.slnx --no-restore -t:Rebuild -m:1 -p:GeneratePackageOnBuild=false -v:quiet` succeeded with 0 errors and 184 existing warnings in other projects. Asv.Common and Asv.Common.Test had no warnings.

The test project now includes xunit.v3 3.2.2 and xunit.runner.visualstudio 3.1.5, enabling genuine test discovery. Five diagnostics newly exposed by its analyzers were corrected without changing test behavior. Existing unrelated user changes were preserved. No commit, push or publication performed.
