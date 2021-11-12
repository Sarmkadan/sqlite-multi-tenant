# EventBusImplTestsExtensions

Utility class providing extension methods for test scenarios involving `EventBusImpl`. These methods help establish clean test environments, validate initial states, and safely dispose of resources.

## API

### `EnsureCleanState`

Initializes a fresh state for the event bus and its dependencies, clearing any existing events or history.
