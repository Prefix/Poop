# 💩 Poop Plugin for ModSharp

Some kind of plugin, that might people who want to move from CounterStrikeSharp would love digging to.
Quality of the plugin isn't really good, but you can definetly get grasp how to work on your own new module. (Grab the Managers folder)

## 🎮 Features

### Core Gameplay
- **Smart Poop Spawning**: Drop poops on dead players or at your position to compete who is top pooper / victim
- **Multiple Commands**: `/poop`, `/poopcolor` with customizable aliases
- **Advanced Size System**: 7-tier weighted randomization system (0.3-2.6) with rare legendary spawns
  - Normal (40%), Above Average (25%), Small (15%), Large (10%), Tiny (5%), Huge (3%), Rare (2%)
  - Special rare tier with Massive, Legendary, and Ultra Legendary sub-tiers

### Visual & Audio Experience
- **Rainbow Poops**: Possible to choose for animated rainbow color cycling with configurable speed
- **17 Color Options**: Including brown, rainbow, random, and 14 solid colors
- **Size Announcements**: Special messages for massive poops (>2.0 size)

### Statistics & Persistence
- **MySQL Database**: Persistent player statistics and preferences
- **Top Poopers**: Leaderboard of players who placed the most poops
- **Top Victims**: Leaderboard of players who were pooped on the most
- **Player Preferences**: Remembers color choices and settings across sessions

### Developer API (Poop.Shared)
Please check PoopExample folder

## 📋 Commands

| Command | Aliases | Description |
|---------|---------|-------------|
| `/poop` | `/shit` | Spawn a poop on nearby dead player or at your position |
| `/poopcolor` | `/poop_color`, `/colorpoop` | Open color selection menu |
| `/toppoopers` | `/pooperstop` | Show top 10 players who placed the most poops |
| `/toppoop` | `/pooptop` | Show top 10 players who were pooped on the most |

## ⚙️ Configuration

Configuration is stored in `appsettings.json`:

## 🌍 Localization

Locale files are stored in `Poop/locales/` directory:
- `EN.json` - English (default)
- `CN.json` - Chinese

Add your own locale files and set `"Locale": "YourLanguage"` in config.

## 🤝 This was done thanks to:

- ModSharp -> they finnaly released it! And it was worth to wait for it.
- Nuko, laper32 for providing some of the Managers
- laper32 Code Reviewing
- CS# for Menu's - Modsharp should be providing their own ones in future as extension.