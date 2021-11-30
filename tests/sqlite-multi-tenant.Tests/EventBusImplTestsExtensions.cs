using System;

namespace SqliteMultiTenant.Tests
{
    /// <summary>
    /// Extension methods that provide convenient wrappers around the public test members
    /// of <see cref="EventBusImplTests"/>. These helpers can be used by other test code
    /// to set up, verify, and clean up the event bus test fixture without duplicating
    /// the underlying test method calls.
    /// </summary>
    public static class EventBusImplTestsExtensions
    {
        /// <summary>
        /// Ensures the event history is cleared, providing a clean state for subsequent tests.
        /// </summary>
        /// <param name="test">The <see cref="EventBusImplTests"/> instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="test"/> is <see langword="null"/>.</exception>
        public static void EnsureCleanState(this EventBusImplTests test)
        {
            ArgumentNullException.ThrowIfNull(test);

            // Clears any existing events to start from a known state.
            test.ClearHistory_WhenHasEvents_ShouldClearList();
        }

        /// <summary>
        /// Verifies that the event history is empty initially.
        /// </summary>
        /// <param name="test">The <see cref="EventBusImplTests"/> instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="test"/> is <see langword="null"/>.</exception>
        public static void VerifyEmptyHistory(this EventBusImplTests test)
        {
            ArgumentNullException.ThrowIfNull(test);

            // Asserts that the event history is empty at the start of the test suite.
            test.GetEventHistory_Initially_ShouldBeEmpty();
        }

        /// <summary>
        /// Runs the initialization test to ensure the event bus properties are correctly set.
        /// </summary>
        /// <param name="test">The <see cref="EventBusImplTests"/> instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="test"/> is <see langword="null"/>.</exception>
        public static void InitializeAndValidate(this EventBusImplTests test)
        {
            ArgumentNullException.ThrowIfNull(test);

            // Executes the initialization verification.
            test.EventBus_Initialization_PropertiesAreSet();
        }

        /// <summary>
        /// Calls <see cref="EventBusImplTests.Dispose_WhenCalled_DoesNotThrow"/> to safely dispose the test fixture.
        /// </summary>
        /// <param name="test">The <see cref="EventBusImplTests"/> instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="test"/> is <see langword="null"/>.</exception>
        public static void DisposeSafely(this EventBusImplTests test)
        {
            ArgumentNullException.ThrowIfNull(test);

            // Ensures Dispose does not throw any exceptions.
            test.Dispose_WhenCalled_DoesNotThrow();
        }
    }
}