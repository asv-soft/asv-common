# asv-common
Provides common types and extensions for asv-based libraries
## Async
### SingleThreadTaskScheduler
The SingleThreadTaskScheduler class is a custom implementation of TaskScheduler that schedules and executes tasks on a single thread. This means that all tasks scheduled to this scheduler will be executed sequentially on a single thread, rather than being distributed across multiple threads.
### LockByKeyExecutor
The purpose of the LockByKeyExecutor class is to ensure that only one asynchronous operation can execute at a time for a given key.
## Reactive 
### IRxValue
The RxValue<TValue> class is a generic class that represents a reactive value that can be observed for changes. The class provides an observable interface through which subscribers can be notified of changes to the value.
## Other
### UintBitArray
Represents a bit array

# asv-io
Provides base input and output (I/O) port abstractions (TCP client\server, serial, udp) and binary serialization helpers
