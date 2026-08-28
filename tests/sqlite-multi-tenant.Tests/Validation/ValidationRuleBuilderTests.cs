using FluentAssertions;
using SqliteMultiTenant.Validation;
using Xunit;

namespace SqliteMultiTenant.Tests.Validation
{
    /// <summary>
    /// Tests for the <see cref="ValidationRuleBuilder{T}"/> class.
    /// </summary>
    public class ValidationRuleBuilderTests
    {
        /// <summary>
        /// Returns a string representation of the test instance with default values.
        /// </summary>
        public override string ToString()
        {
            return $"ValidationRuleBuilderTests {{ Name = none, Email = none, Age = none, Password = none, ConfirmPassword = none, Phone = none }}";
        }

        private class TestModel
        {
            public string? Name { get; set; }
            public string? Email { get; set; }
            public int? Age { get; set; }
            public string? Password { get; set; }
            public string? ConfirmPassword { get; set; }
            public string? Phone { get; set; }
        }

        private class ProductModel
        {
            public string? Title { get; set; }
            public decimal? Price { get; set; }
            public int? Stock { get; set; }
        }

        /// <summary>
        /// Verifies that the Required rule passes when the specified property has a non-null, non-whitespace value.
        /// </summary>
        [Fact]
        public void Required_WithValidValue_ReturnsValidResult()
        {
            // Arrange
            var model = new TestModel { Name = "John Doe" };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.Required("Name");
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that the Required rule fails when the specified property is null.
        /// </summary>
        [Fact]
        public void Required_WithNullValue_ReturnsInvalidResult()
        {
            // Arrange
            var model = new TestModel { Name = null };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.Required("Name");
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].FieldName.Should().Be("Name");
            result.Errors[0].Message.Should().Be("Name is required");
        }

        /// <summary>
        /// Verifies that the Required rule fails when the specified property contains only whitespace.
        /// </summary>
        [Fact]
        public void Required_WithEmptyString_ReturnsInvalidResult()
        {
            // Arrange
            var model = new TestModel { Name = "   " };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.Required("Name");
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].FieldName.Should().Be("Name");
        }

        /// <summary>
        /// Verifies that the Required rule uses a custom error message when validation fails.
        /// </summary>
        [Fact]
        public void Required_WithCustomMessage_ReturnsCustomErrorMessage()
        {
            // Arrange
            var model = new TestModel { Name = null };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.Required("Name", "Name field cannot be empty");
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors[0].Message.Should().Be("Name field cannot be empty");
        }

        /// <summary>
        /// Verifies that the StringLength rule passes when the property value is within the specified length range.
        /// </summary>
        [Fact]
        public void StringLength_WithValidLength_ReturnsValidResult()
        {
            // Arrange
            var model = new TestModel { Name = "ValidName" };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.StringLength("Name", minLength: 3, maxLength: 20);
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that the StringLength rule fails when the property value is shorter than the minimum length.
        /// </summary>
        [Fact]
        public void StringLength_WithMinLengthViolation_ReturnsInvalidResult()
        {
            // Arrange
            var model = new TestModel { Name = "AB" };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.StringLength("Name", minLength: 3);
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].FieldName.Should().Be("Name");
            result.Errors[0].Message.Should().Be("Name must be at least 3 characters");
        }

        /// <summary>
        /// Verifies that the StringLength rule fails when the property value exceeds the maximum length.
        /// </summary>
        [Fact]
        public void StringLength_WithMaxLengthViolation_ReturnsInvalidResult()
        {
            // Arrange
            var model = new TestModel { Name = "ThisNameIsWayTooLong" };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.StringLength("Name", maxLength: 10);
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].FieldName.Should().Be("Name");
            result.Errors[0].Message.Should().Be("Name must not exceed 10 characters");
        }

        /// <summary>
        /// Verifies that the StringLength rule fails when the property value is shorter than the minimum length (when both min and max are specified).
        /// </summary>
        [Fact]
        public void StringLength_WithBothMinAndMax_ReturnsInvalidResult()
        {
            // Arrange
            var model = new TestModel { Name = "AB" };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.StringLength("Name", minLength: 3, maxLength: 10);
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].FieldName.Should().Be("Name");
        }

        /// <summary>
        /// Verifies that the Email rule passes when the property contains a valid email address.
        /// </summary>
        [Fact]
        public void Email_WithValidEmail_ReturnsValidResult()
        {
            // Arrange
            var model = new TestModel { Email = "test@example.com" };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.Email("Email");
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that the Email rule fails when the property contains an invalid email address.
        /// </summary>
        [Fact]
        public void Email_WithInvalidEmail_ReturnsInvalidResult()
        {
            // Arrange
            var model = new TestModel { Email = "invalid-email" };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.Email("Email");
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].FieldName.Should().Be("Email");
            result.Errors[0].Message.Should().Be("Email must be a valid email address");
        }

        /// <summary>
        /// Verifies that the Email rule passes when the property is null (null is considered valid).
        /// </summary>
        [Fact]
        public void Email_WithNullEmail_ReturnsValidResult()
        {
            // Arrange
            var model = new TestModel { Email = null };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.Email("Email");
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that the Email rule passes when the property is an empty string (empty is considered valid).
        /// </summary>
        [Fact]
        public void Email_WithEmptyEmail_ReturnsValidResult()
        {
            // Arrange
            var model = new TestModel { Email = "" };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.Email("Email");
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that the Range rule passes when the numeric property value is within the specified range.
        /// </summary>
        [Fact]
        public void Range_WithValidNumericValue_ReturnsValidResult()
        {
            // Arrange
            var model = new ProductModel { Price = 15.50m };
            var builder = new ValidationRuleBuilder<ProductModel>();

            // Act
            builder.Range("Price", minValue: 10.00m, maxValue: 100.00m);
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that the Range rule fails when the numeric property value is below the minimum allowed value.
        /// </summary>
        [Fact]
        public void Range_WithValueBelowMinimum_ReturnsInvalidResult()
        {
            // Arrange
            var model = new ProductModel { Price = 5.00m };
            var builder = new ValidationRuleBuilder<ProductModel>();

            // Act
            builder.Range("Price", minValue: 10.00m, maxValue: 100.00m);
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].FieldName.Should().Be("Price");
            result.Errors[0].Message.Should().Be("Price must be between 10.00 and 100.00");
        }

        /// <summary>
        /// Verifies that the Range rule fails when the numeric property value is above the maximum allowed value.
        /// </summary>
        [Fact]
        public void Range_WithValueAboveMaximum_ReturnsInvalidResult()
        {
            // Arrange
            var model = new ProductModel { Price = 150.00m };
            var builder = new ValidationRuleBuilder<ProductModel>();

            // Act
            builder.Range("Price", minValue: 10.00m, maxValue: 100.00m);
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].FieldName.Should().Be("Price");
        }

        /// <summary>
        /// Verifies that the Range rule with only a minimum value fails when the property value is below that minimum.
        /// </summary>
        [Fact]
        public void Range_WithOnlyMinValue_ReturnsInvalidResultWhenBelow()
        {
            // Arrange
            var model = new ProductModel { Price = 5.00m };
            var builder = new ValidationRuleBuilder<ProductModel>();

            // Act
            builder.Range("Price", minValue: 10.00m);
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].FieldName.Should().Be("Price");
            result.Errors[0].Message.Should().Be("Price must be at least 10.00");
        }

        /// <summary>
        /// Verifies that the Range rule with only a maximum value fails when the property value is above that maximum.
        /// </summary>
        [Fact]
        public void Range_WithOnlyMaxValue_ReturnsInvalidResultWhenAbove()
        {
            // Arrange
            var model = new ProductModel { Price = 150.00m };
            var builder = new ValidationRuleBuilder<ProductModel>();

            // Act
            builder.Range("Price", maxValue: 100.00m);
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].FieldName.Should().Be("Price");
            result.Errors[0].Message.Should().Be("Price must not exceed 100.00");
        }

        /// <summary>
        /// Verifies that the Pattern rule passes when the property value matches the specified regular expression.
        /// </summary>
        [Fact]
        public void Pattern_WithMatchingPattern_ReturnsValidResult()
        {
            // Arrange
            var model = new TestModel { Phone = "123-456-7890" };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.Pattern("Phone", @"^\d{3}-\d{3}-\d{4}$");
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that the Pattern rule fails when the property value does not match the specified regular expression.
        /// </summary>
        [Fact]
        public void Pattern_WithNonMatchingPattern_ReturnsInvalidResult()
        {
            // Arrange
            var model = new TestModel { Phone = "1234567890" };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.Pattern("Phone", @"^\d{3}-\d{3}-\d{4}$");
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].FieldName.Should().Be("Phone");
            result.Errors[0].Message.Should().Be("Phone does not match the required pattern");
        }

        /// <summary>
        /// Verifies that the Custom rule passes when the provided predicate returns true for the property value.
        /// </summary>
        [Fact]
        public void Custom_WithPassingPredicate_ReturnsValidResult()
        {
            // Arrange
            var model = new TestModel { Name = "ValidName" };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.Custom("Name", obj => obj is TestModel m && m.Name != null && m.Name.Length > 5);
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that the Custom rule fails when the provided predicate returns false for the property value.
        /// </summary>
        [Fact]
        public void Custom_WithFailingPredicate_ReturnsInvalidResult()
        {
            // Arrange
            var model = new TestModel { Name = "AB" };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.Custom("Name", obj => obj is TestModel m && m.Name != null && m.Name.Length > 5);
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].FieldName.Should().Be("Name");
            result.Errors[0].Message.Should().Be("Name validation failed");
        }

        /// <summary>
        /// Verifies that the MustMatch rule passes when the two specified properties have equal values.
        /// </summary>
        [Fact]
        public void MustMatch_WithMatchingValues_ReturnsValidResult()
        {
            // Arrange
            var model = new TestModel { Password = "secret123", ConfirmPassword = "secret123" };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.MustMatch("Password", "ConfirmPassword");
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that the MustMatch rule fails when the two specified properties have different values.
        /// </summary>
        [Fact]
        public void MustMatch_WithNonMatchingValues_ReturnsInvalidResult()
        {
            // Arrange
            var model = new TestModel { Password = "secret123", ConfirmPassword = "different456" };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.MustMatch("Password", "ConfirmPassword");
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].FieldName.Should().Be("Password,ConfirmPassword");
            result.Errors[0].Message.Should().Be("Password and ConfirmPassword must match");
        }

        /// <summary>
        /// Verifies that validation passes when multiple rules are applied and all properties satisfy their respective rules.
        /// </summary>
        [Fact]
        public void Validate_WithMultipleRules_AllPassing_ReturnsValidResult()
        {
            // Arrange
            var model = new TestModel { Name = "John Doe", Email = "john@example.com", Age = 25 };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.Required("Name");
            builder.Email("Email");
            builder.Range("Age", minValue: 18, maxValue: 120);
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that validation fails when multiple rules are applied and exactly one property fails its rule.
        /// </summary>
        [Fact]
        public void Validate_WithMultipleRules_OneFailing_ReturnsInvalidResult()
        {
            // Arrange
            var model = new TestModel { Name = "John Doe", Email = "invalid-email", Age = 25 };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.Required("Name");
            builder.Email("Email");
            builder.Range("Age", minValue: 18, maxValue: 120);
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].FieldName.Should().Be("Email");
        }

        /// <summary>
        /// Verifies that validation returns all error messages when multiple rules are applied and multiple properties fail their respective rules.
        /// </summary>
        [Fact]
        public void Validate_WithMultipleRules_MultipleFailing_ReturnsAllErrors()
        {
            // Arrange
            var model = new TestModel { Name = "", Email = "invalid-email", Age = 15 };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.Required("Name");
            builder.Email("Email");
            builder.Range("Age", minValue: 18, maxValue: 120);
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(3);
            result.Errors[0].FieldName.Should().Be("Name");
            result.Errors[1].FieldName.Should().Be("Email");
            result.Errors[2].FieldName.Should().Be("Age");
        }

        /// <summary>
        /// Verifies that validation fails when the object to validate is null.
        /// </summary>
        [Fact]
        public void Validate_WithNullObject_ReturnsInvalidResult()
        {
            // Arrange
            TestModel? model = null;
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.Required("Name");
            var result = builder.Validate(model!);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].FieldName.Should().Be("Name");
        }

        /// <summary>
        /// Verifies that validation passes when rules are chained using fluent syntax and all properties satisfy their respective rules.
        /// </summary>
        [Fact]
        public void Validate_WithChainedRules_ReturnsValidResult()
        {
            // Arrange
            var model = new TestModel { Name = "Valid Name", Email = "test@example.com", Age = 30 };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act - Chain multiple rules
            var result = builder
                .Required("Name")
                .Email("Email")
                .Range("Age", minValue: 18, maxValue: 120)
                .Validate(model);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that validation fails when rules are chained using fluent syntax and exactly one property fails its rule.
        /// </summary>
        [Fact]
        public void Validate_WithChainedRules_OneFailing_ReturnsInvalidResult()
        {
            // Arrange
            var model = new TestModel { Name = "Valid Name", Email = "invalid-email", Age = 30 };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act - Chain multiple rules
            var result = builder
                .Required("Name")
                .Email("Email")
                .Range("Age", minValue: 18, maxValue: 120)
                .Validate(model);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].FieldName.Should().Be("Email");
        }

        /// <summary>
        /// Verifies that validation fails when a required string property is null, even if other properties are present.
        /// </summary>
        [Fact]
        public void Validate_WithNullStringField_ReturnsInvalidResult()
        {
            // Arrange
            var model = new TestModel { Name = null, Email = null };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.Required("Name");
            builder.Email("Email");
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].FieldName.Should().Be("Name");
        }

        /// <summary>
        /// Verifies that validation fails when a required string property contains only whitespace, even if other properties are present.
        /// </summary>
        [Fact]
        public void Validate_WithEmptyStringField_ReturnsInvalidResult()
        {
            // Arrange
            var model = new TestModel { Name = "   ", Email = null };
            var builder = new ValidationRuleBuilder<TestModel>();

            // Act
            builder.Required("Name");
            builder.Email("Email");
            var result = builder.Validate(model);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].FieldName.Should().Be("Name");
        }
    }
}