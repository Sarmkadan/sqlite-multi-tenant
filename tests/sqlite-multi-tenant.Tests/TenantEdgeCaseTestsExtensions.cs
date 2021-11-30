using System;

namespace SqliteMultiTenant.Tests
{
    /// <summary>
    /// Extension methods that provide convenient groupings of the existing
    /// <see cref="TenantEdgeCaseTests"/> test methods.
    /// </summary>
    public static class TenantEdgeCaseTestsExtensions
    {
        /// <summary>
        /// Executes all validation related test methods on the supplied
        /// <see cref="TenantEdgeCaseTests"/> instance.
        /// </summary>
        /// <param name="tests">The test instance.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="tests"/> is <see langword="null"/>.
        /// </exception>
        public static void RunAllValidationTests(this TenantEdgeCaseTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            tests.Validate_NullTenantId_ReturnsError();
            tests.Validate_EmptyTenantId_ReturnsError();
            tests.Validate_WhitespaceTenantId_ReturnsError();
            tests.Validate_TenantIdExceedsMaxLength_ReturnsError();
            tests.Validate_TenantIdExactlyMaxLength_IsValid();
            tests.Validate_NameExceedsMaxLength_ReturnsError();
            tests.Validate_ZeroMaxConnections_ReturnsError();
            tests.Validate_NegativeMaxConnections_ReturnsError();
            tests.Validate_CreatedAtAfterUpdatedAt_ReturnsError();
            tests.Validate_MultipleErrors_ReturnsAllErrors();
        }

        /// <summary>
        /// Deactivates a tenant and then re‑activates it, exercising the
        /// corresponding status‑change test methods.
        /// </summary>
        /// <param name="tests">The test instance.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="tests"/> is <see langword="null"/>.
        /// </exception>
        public static void ToggleActivation(this TenantEdgeCaseTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            tests.Deactivate_SetsStatusToInactive();
            tests.Activate_AfterDeactivate_SetsStatusToActive();
        }

        /// <summary>
        /// Runs the suite of metadata‑related test methods, ensuring that
        /// metadata handling behaves as expected.
        /// </summary>
        /// <param name="tests">The test instance.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="tests"/> is <see langword="null"/>.
        /// </exception>
        public static void EnsureMetadataOperations(this TenantEdgeCaseTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            tests.SetMetadata_WhenMetadataIsNull_InitializesAndSetsValue();
            tests.SetMetadata_OverwritesExistingKey();
            tests.GetMetadata_NonexistentKey_ReturnsNull();
            tests.GetMetadata_WhenMetadataIsNull_ReturnsNull();
            tests.SetMetadata_ConcurrentAccess_DoesNotThrow();
        }
    }
}
