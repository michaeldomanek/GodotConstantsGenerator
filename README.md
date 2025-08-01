# Godot Constants Generator

A .NET CLI tool that extracts input actions, collision layers, and group names from your `project.godot` and generates strongly-typed C# constants.

## Godot Plugin

> [!NOTE]  
> **Recommended**\
> I also created the Godot plugin [ConstantsGeneratorPlugin](https://github.com/michaeldomanek/ConstantsGeneratorPlugin)
> that automatically generates these constants by listening to Project Settings changes.


## Features
- Parses `project.godot` to extract:
    - Input actions
    - Collision layers
    - Group names
- Generates static `StringName` constants or `uint` for collision layers
- Automatically infers namespace from `.csproj` or accepts manual override
- Customizable output filenames and locations
- Optional disabling of specific categories (actions, groups, layers)

## Installation

```bash
dotnet tool install --global GodotConstantsGenerator
```

## Usage

To run the tool, use the following command in your terminal or command prompt:
```bash
godot-constants-generator [options]
```
It works without any parameters, all options are optional. \
If no parameters are provided, it will infer the project name from the `.csproj` file and generate all files in the `scripts/generated` directory.

### Options

| Option                | Alias      | Description                                                  | Default                |
|-----------------------|------------|--------------------------------------------------------------|------------------------|
| `--help`              | `-h`, `-?` | Show help and usage information                              |                        |
| `--project`           | `-p`       | Name of the project or root namespace                        | Inferred from `.csproj` |
| `--output`            | `-o`       | Output directory for generated files                         | `scripts/generated`    |
| `--namespace`         | `-n`       | C# namespace to use in the generated files                   | Auto-computed          |
| `--no-actions`        |            | Disable generation of `Actions.cs`                           | false                  |
| `--no-groups`         |            | Disable generation of `Groups.cs`                            | false                  |
| `--no-layers`         |            | Disable generation of `CollisionLayers.cs`                   | false                  |
| `--actions-name`      | `-a`       | File/class name for input actions                            | `Actions`              |
| `--groups-name`       | `-g`       | File/class name for group names                              | `Groups`               |
| `--layers-name`       | `-l`       | File/class name for collision layers                         | `CollisionLayers`      |

## Example

```
dotnet run --project GodotConstantsGenerator \
  --project MyGame \
  --output ./src/Generated \
  --namespace MyGame.Constants
```

This will generate:

- `src/Generated/Actions.cs`
- `src/Generated/Groups.cs`
- `src/Generated/CollisionLayers.cs`

Each file will contain a static class like the following:

```csharp
using Godot;

namespace MyGame.Constants;

public static class Actions {
    public static readonly StringName Jump = "Jump";
    public static readonly StringName Shoot = "Shoot";
}
```

## Requirements
- Godot 4.x - (tested on 4.4.1)
- .NET 8.0 or later
- Must be run from the root of your Godot project (where `project.godot` and `.csproj` are located)

## Building

```bash
dotnet pack --output ./nupkg
dotnet tool install --global GodotConstantsGenerator --add-source ./nupkg
```

## License

MIT License
