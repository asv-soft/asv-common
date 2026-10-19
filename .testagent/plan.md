# Plan

1. Add regression cases before production edits and enable actual test discovery.
2. Confirm the new cases fail against the current code.
3. Add exception-safe lock release, correct bit masks and bounds, reject negative ring indexes, and select strictly newer timestamps.
4. Run focused regressions, then the Asv.Common.Test suite and a Debug solution build.
5. Review assertions for each of the five requirements and record results in status.md.

Mapping:
- Lock leak: GetValues_CallbackThrows_ReleasesLock; ClearOld_CallbackThrows_ReleasesLock.
- Bit masks: GetBitU_AllWidths_ReturnsOnlyRequestedBits; SetBitU_AllWidths_PreservesOtherBits.
- Bit bounds: Indexer_InvalidIndex_ThrowsWithoutChangingValue.
- Ring bounds: Indexer_NegativeIndex_ThrowsWithoutChangingContents.
- Time selection: GetItemsWithTimeMoreThen_SelectsStrictlyNewerItems.
