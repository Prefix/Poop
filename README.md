# 💩 Poop Plugin for ModSharp

Some kind of plugin, that might people who want to move from CounterStrikeSharp would love digging to.
Quality of the plugin isn't really good, but you can definetly get grasp how to work on your own new module. (Grab the Managers folder)

## 🎮 Features

### Core Gameplay
- **Spawn Poops**: Drop poops on dead players or at your position
- **Multiple Commands**: `/poop`, `/shit`, `/poop_size`, `/poop_rnd`
- **Size System**: Poops range from tiny (0.3) to massive (2.0) with weighted randomization

### Customization
- **Rainbow Poops**: Animated rainbow color cycling with configurable speed
- **Color Menu**: Interactive menu for easy color selection

### Statistics & Leaderboards
- **Top Poopers**: Leaderboard of players who placed the most poops
- **Top Victims**: Leaderboard of players who were pooped on the most

### Server Management
- **Cooldown System**: Configurable command cooldowns (default: 3 seconds)
- **Max Poops Per Round**: Limit poops per round (0 = unlimited)
- **Multi-Language**: Full localization support (EN, CN included)

### Developer API (Poop.Shared)
- **Public Events**: Hook into poop commands and spawns
- **Command Blocking**: Block commands based on custom logic (permissions, donators, etc.)
- **Force Spawning**: Programmatically spawn poops from other plugins
- **Statistics API**: Query player stats and leaderboards
- **Color Management**: Get/set player color preferences

## 📋 Commands

| Command | Aliases | Description | Admin |
|---------|---------|-------------|-------|
| `/poop` | `/shit` | Spawn a poop on nearby dead player or at your position | No |
| `/poop_size <size>` | - | Spawn a poop with specific size (0.3-2.0) | No (Should be WIP)|
| `/poop_rnd` | - | Spawn a completely random-sized poop | No (Should be WIP) | 
| `/poopcolor` | `/poop_color`, `/colorpoop` | Open color selection menu | No |
| `/toppoopers` | `/pooperstop` | Show top 10 players who placed the most poops | No |
| `/toppoop` | `/pooptop` | Show top 10 players who were pooped on the most | No |

## ⚙️ Configuration

Configuration is stored in `appsettings.json`:

### Configuration Options Explained

- **Size System**: Common size range (85% chance) vs small sizes (10% chance) with legendary spawns (5%) - WIP it's still lacking some size related stuff
- **Dead Player Detection**: Searches for dead players within `MaxDeadPlayerDistance` units
- **Ragdoll Detection**: Optional ragdoll-based victim detection (Support for TTT gamemode)
- **Round Limits**: `MaxPoopsPerRound` (0 = unlimited) and optional cleanup on round end
- **Lifetime**: `PoopLifetimeSeconds` (0 = never remove, >0 = auto-remove after X seconds)
- **Colors**: Enable rainbow/custom colors with `EnableColorPreferences` and `EnableRainbowPoops`

## 🔌 Developer API (Poop.Shared)

The `Poop.Shared` project provides a public API for other plugins to interact with poop functionality.

### Installation

Add a reference to `Poop.Shared.dll` in your plugin:

```xml
<ProjectReference Include="..\Poop.Shared\Poop.Shared.csproj" Private="false" ExcludeAssets="runtime" //>
```

## 🌍 Localization

Locale files are stored in `Poop/locales/` directory:
- `EN.json` - English (default)
- `CN.json` - Chinese

Add your own locale files and set `"Locale": "YourLanguage"` in config.

## 🏗️ Architecture

### ModSharp Structure
- **Poop**: Main plugin with modules (Commands, Spawner, Database, ColorMenu, Lifecycle)
- **Poop.Shared**: Public API for external plugins (shared interface, events, models)
- **Dependency Injection**: Uses Microsoft.Extensions.DependencyInjection
- **Event-Driven**: Internal event subscription pattern to avoid circular dependencies

### Key Components
- **PoopCommands**: Chat command handlers
- **PoopSpawner**: Core spawning logic with physics and detection
- **PoopDatabase**: MySQL persistence layer
- **PoopLifecycleManager**: Manages poop lifetime and cleanup
- **PoopColorMenu**: Interactive color selection UI
- **SharedInterface**: Bridges internal modules to public API

## 🤝 This was done thanks to:

- ModSharp -> they finnaly released it! And it was worth to wait for it.
- Nuko, laper32 for providing some of the Managers
- CS# for Menu's - Modsharp should be providing their own ones in future as extension.