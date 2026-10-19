# Regression scope

User requirement: Fix all five reported bugs in Asv.Common and add regression tests.

Targets and pairing (bounded inventory; find-untested-sources is unavailable):
- ConcurrentTimeDictionary -> new Collections/ConcurrentTimeDictionaryTest.cs: callback exceptions must release locks.
- UintBitArray -> existing Other/UintBitArrayTest.cs: all field widths including 32, logical index boundaries and unchanged neighboring bits.
- CircularBuffer2 -> existing Collections/CircularBuffer2Test.cs: reject negative indexes after shifting/wrapping, preserve contents.
- ConcurrentCircularTimeBuffer -> new Collections/ConcurrentCircularTimeBufferTest.cs: strictly newer selection including equality, empty and all/none boundaries, sorted insertion.

SDK-style net10.0 project uses xUnit v3 3.2.2 from Asv.XUnit. The test project lacks the xunit.v3 entry point and VSTest adapter. Add matching xunit.v3 3.2.2 and cached adapter 3.1.5, without changing library dependencies.

Existing modifications are preserved. No commits or publication authorized.
