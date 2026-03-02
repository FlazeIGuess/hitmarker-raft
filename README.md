# HitMarker Mod for Raft

A visual feedback mod that displays a hitmarker in the center of your screen whenever you successfully damage an enemy in Raft.

## Features

- Visual hitmarker appears on successful hits
- Smooth fade-in and fade-out animations (0.5 seconds total)
- Works with all weapons (arrows, spears, melee attacks)
- Lightweight and performance-friendly
- Client-side only (doesn't require all players to have it installed)

## Installation

1. Install [RaftModLoader](https://www.raftmodding.com/loader)
2. Download the latest `HitMarker.rmod` file from the [releases page](https://github.com/FlazeIGuess/hitmarker-raft/releases)
3. Place the `.rmod` file in your RaftModLoader mods folder
4. Launch Raft through RaftModLoader

## Building from Source

### Prerequisites

- Visual Studio 2019 or later
- .NET Framework 4.8
- Raft game installed
- RaftModLoader installed

### Build Steps

1. Clone the repository:
   ```bash
   git clone https://github.com/FlazeIGuess/hitmarker-raft.git
   cd hitmarker-raft
   ```

2. Update the reference paths in `HitMarker/HitMarker.csproj` to match your Raft installation directory

3. Build the solution:
   ```bash
   msbuild HitMarker.sln /p:Configuration=Debug
   ```
   
   Or open `HitMarker.sln` in Visual Studio and build from there

4. The mod will be automatically packaged as `HitMarker.rmod` in the root directory

## Customization (for developers)

To customize the mod, you need to modify the source code and rebuild:

### Timing Configuration
Edit the following values in `HitMarker.cs`:
- `displayDuration`: How long the hitmarker stays visible (default: 0.5 seconds)
- `fadeInDuration`: Fade-in animation duration (default: 0.1 seconds)
- `fadeOutDuration`: Fade-out animation duration (default: 0.15 seconds)

### Custom Hitmarker Image
Replace `HitMarker/hitmarker.png` with your own image and rebuild the mod. The image will be embedded in the compiled `.rmod` file.

## How It Works

The mod uses Harmony patches to intercept damage events:
- `Network_Host.DamageEntity` - Catches general entity damage
- `AI_StateMachine_Boar.OnDamageTaken` - Catches boar-specific damage
- `AI_StateMachine_Animal.OnDamageTaken` - Catches animal-specific damage

When damage is dealt by the player, the hitmarker UI is triggered.

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## License

This project is licensed under the GNU Affero General Public License v3.0 - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built with [RaftModLoader](https://www.raftmodding.com/)
- Uses [Harmony](https://github.com/pardeike/Harmony) for runtime patching
- Created by Flaze

## Support

- Report bugs on the [Issues page](https://github.com/FlazeIGuess/hitmarker-raft/issues)
- Join the [Raft Modding Discord](https://www.raftmodding.com/discord)

## Version History

### 1.0.3
- Current stable release
- Smooth fade animations
- Support for all weapon types
