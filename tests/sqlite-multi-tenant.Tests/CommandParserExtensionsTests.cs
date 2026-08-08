using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using SqliteMultiTenant.Cli;

namespace SqliteMultiTenant.Tests
{
    public sealed class CommandParserExtensionsTests
    {
        private readonly ILogger<CommandParser> _mockLogger;
        private readonly CommandParser _parser;

        public CommandParserExtensionsTests()
        {
            _mockLogger = Substitute.For<ILogger<CommandParser>>();
            _parser = new CommandParser(_mockLogger);
        }

        [Fact]
        public void ValidateSubcommandArguments_NullParser_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CommandParserExtensions.ValidateSubcommandArguments(null!, new ParsedCommand(), new CommandHandler()));
        }

        [Fact]
        public void ValidateSubcommandArguments_EmptyArguments_ReturnsSuccess()
        {
            var commandHandler = new CommandHandler();
            var parsedCommand = new ParsedCommand { Subcommand = "" };

            var result = CommandParserExtensions.ValidateSubcommandArguments(_parser, parsedCommand, commandHandler);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateSubcommandArguments_KnownSubcommand_MissingRequiredArgs_ReturnsError()
        {
            var commandHandler = new CommandHandler
            {
                Subcommands = new[] { new Subcommand { Name = "create", RequiredArgs = new[] { "name" } } }
            };
            var parsedCommand = new ParsedCommand { Subcommand = "create" };

            var result = CommandParserExtensions.ValidateSubcommandArguments(_parser, parsedCommand, commandHandler);

            Assert.Single(result);
            Assert.Contains("Missing required argument: name for subcommand 'create'", result);
        }

        [Fact]
        public void HasSubcommand_NullParser_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CommandParserExtensions.HasSubcommand(null!, new CommandHandler(), "test"));
        }

        [Fact]
        public void HasSubcommand_KnownSubcommand_ReturnsTrue()
        {
            var commandHandler = new CommandHandler
            {
                Subcommands = new[] { new Subcommand { Name = "create" } }
            };

            var result = CommandParserExtensions.HasSubcommand(_parser, commandHandler, "create");

            Assert.True(result);
        }

        [Fact]
        public void GenerateHelpText_NullParser_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CommandParserExtensions.GenerateHelpText(null!, new CommandHandler()));
        }

        [Fact]
        public void GenerateHelpText_KnownCommandHandler_ReturnsSuccess()
        {
            var commandHandler = new CommandHandler { Name = "test-handler", Description = "Test handler" };

            var result = CommandParserExtensions.GenerateHelpText(_parser, commandHandler);

            Assert.NotNull(result);
            Assert.Contains("Usage: test-handler", result);
            Assert.Contains("Description: Test handler", result);
        }
    }
}
