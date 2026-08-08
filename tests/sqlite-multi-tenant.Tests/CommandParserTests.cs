using System;
using SqliteMultiTenant.Cli;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public sealed class CommandParserTests
    {
        private readonly ILogger<CommandParser> _mockLogger;
        private readonly CommandParser _parser;

        public CommandParserTests()
        {
            _mockLogger = Substitute.For<ILogger<CommandParser>>();
            _parser = new CommandParser(_mockLogger);
        }

        [Fact]
        public void Parse_NullArguments_ThrowsNullReferenceException()
        {
            // Act & Assert
            Assert.Throws<NullReferenceException>(() => _parser.Parse(null!));
        }

        [Fact]
        public void Parse_EmptyArguments_ReturnsHelpCommand()
        {
            // Act
            var result = _parser.Parse(Array.Empty<string>());

            // Assert
            Assert.True(result.Success);
            Assert.True(result.IsHelpCommand);
            Assert.Equal("help", result.MainCommand);
            Assert.NotNull(result.Message);
            Assert.Contains("SQLite Multi-Tenant Manager", result.Message);
        }

        [Fact]
        public void Parse_HelpArgument_ReturnsHelpCommand()
        {
            // Act
            var result = _parser.Parse(new[] { "help" });

            // Assert
            Assert.True(result.Success);
            Assert.True(result.IsHelpCommand);
            Assert.Equal("help", result.MainCommand);
            Assert.NotNull(result.Message);
            Assert.Contains("SQLite Multi-Tenant Manager", result.Message);
        }

        [Fact]
        public void Parse_UnknownCommand_ReturnsErrorCommand()
        {
            // Act
            var result = _parser.Parse(new[] { "unknown" });

            // Assert
            Assert.False(result.Success);
            Assert.True(result.IsErrorCommand);
            Assert.Equal("", result.MainCommand);
            Assert.NotNull(result.Message);
            Assert.Contains("Unknown command 'unknown'", result.Message);
        }

        [Fact]
        public void Parse_KnownCommand_NoSubcommand_ReturnsHelpForThatCommand()
        {
            // Act
            var result = _parser.Parse(new[] { "tenant" });

            // Assert
            Assert.True(result.Success);
            Assert.True(result.IsHelpCommand);
            Assert.Equal("help", result.MainCommand);
            Assert.NotNull(result.Message);
            Assert.Contains("Command: tenant", result.Message);
            Assert.Contains("Manage tenants", result.Message);
            Assert.Contains("tenant create", result.Message);
            Assert.Contains("Create a new tenant", result.Message);
        }

        [Fact]
        public void Parse_KnownCommand_UnknownSubcommand_ReturnsError()
        {
            // Act
            var result = _parser.Parse(new[] { "tenant", "invalid" });

            // Assert
            Assert.False(result.Success);
            Assert.True(result.IsErrorCommand);
            Assert.Equal("", result.MainCommand);
            Assert.NotNull(result.Message);
            Assert.Contains("Unknown subcommand 'invalid' for 'tenant'", result.Message);
        }

        [Fact]
        public void Parse_KnownCommand_KnownSubcommand_MissingRequiredArgs_ReturnsError()
        {
            // Act
            var result = _parser.Parse(new[] { "tenant", "create" });

            // Assert
            Assert.False(result.Success);
            Assert.True(result.IsErrorCommand);
            Assert.Equal("", result.MainCommand);
            Assert.NotNull(result.Message);
            Assert.Contains("Missing required arguments for 'tenant create'", result.Message);
            Assert.Contains("Expected: name", result.Message);
        }

        [Fact]
        public void Parse_KnownCommand_KnownSubcommand_SufficientArgs_ReturnsSuccess()
        {
            // Act
            var result = _parser.Parse(new[] { "tenant", "create", "test-tenant" });

            // Assert
            Assert.True(result.Success);
            Assert.False(result.IsHelpCommand);
            Assert.False(result.IsErrorCommand);
            Assert.Equal("tenant", result.MainCommand);
            Assert.Equal("create", result.Subcommand);
            Assert.Single(result.Arguments);
            Assert.Equal("test-tenant", result.Arguments[0]);
            Assert.Equal("Create a new tenant", result.Description);
        }
    }
}