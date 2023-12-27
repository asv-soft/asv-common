# asv-common
[![Deploy Nuget for Windows](https://github.com/asv-soft/asv-common/actions/workflows/nuget_windows.yml/badge.svg)](https://github.com/asv-soft/asv-common/actions/workflows/nuget_windows.yml)

Provides common types and extensions for asv-based libraries
## Async
### SingleThreadTaskScheduler
The SingleThreadTaskScheduler class is a custom implementation of TaskScheduler that schedules and executes tasks on a single thread. This means that all tasks scheduled to this scheduler will be executed sequentially on a single thread, rather than being distributed across multiple threads.
### LockByKeyExecutor
The purpose of the LockByKeyExecutor class is to ensure that only one asynchronous operation can execute at a time for a given key.
## Reactive 
### IRxValue
The RxValue<TValue> class is a generic class that represents a reactive value that can be observed for changes. The class provides an observable interface through which subscribers can be notified of changes to the value.
## Math
### NormalRandom 
The implementation uses the Marsaglia polar method, which generates normally distributed random variables by taking two independent random variables u and v uniformly distributed on the interval [-1,1) and transforming them into two independent random variables with a standard normal distribution.
### PiecewiseLinearFunction
This class represents a piecewise linear function that can be defined by a set of points.
## Other
### UintBitArray
Represents a bit array
### DepthFirstSearch
This code implements a depth-first search algorithm to sort a directed acyclic graph represented as a dictionary of node keys and their corresponding array of adjacent nodes.
### GeoPoint 
The GeoPoint struct represents a geographic point on the earth's surface in terms of its latitude, longitude, and altitude. The Latitude property represents the north-south position of the point, the Longitude property represents the east-west position, and the Altitude property represents the height above sea level.
### GeoPointLatitude & GeoPointLongitude
Provides methods for parsing, validating, and printing latitude and longitude values


# asv-io
Provides base input and output (I/O) port abstractions (TCP client\server, serial, udp) and binary serialization helpers

## How To Build Asv Common using Asv.Drones

**1. Setup required dependencies for Asv.Drones:**

Make sure next components installed: 
- .NET SDK 7 - https://dotnet.microsoft.com/en-us/download/dotnet/7.0 ; 
-  AvaloniaUI - https://docs.avaloniaui.net/docs/get-started/install ;
- Execute **dotnet install (package name)** command to setup required packages.

**2. Clone project repository:**
[ git clone https://github.com/your-repository-url.git](https://github.com/asv-soft/asv-drones.git)

