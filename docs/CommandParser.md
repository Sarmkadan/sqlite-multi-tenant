# CommandParser

The `CommandParser` is a utility for parsing command-line arguments into structured command objects, enabling robust handling of multi-tenant SQLite commands with support for main commands, subcommands, and required arguments. It is designed to validate input, provide help messages, and distinguish between successful parsing, errors, and help requests.

## API

### `CommandParser` (class)

A parser for SQLite multi-tenant commands that converts raw command-line arguments into a `ParsedCommand` object.

#### Constructors
