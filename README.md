# MultiDelivery
[![MLVScan IL2CPP](https://mlvscan.com/attestations/att_GgZcHqd2jJayE5F6hlpmUCot/badge.svg)](https://mlvscan.com/attestations/att_GgZcHqd2jJayE5F6hlpmUCot)
[![MLVScan Mono](https://mlvscan.com/attestations/att_Sjpr2rR7gT1_1cYTKfIyWEU7/badge.svg)](https://mlvscan.com/attestations/att_Sjpr2rR7gT1_1cYTKfIyWEU7)

Expand your delivery capacity - order multiple deliveries from the same store simultaneously!

## Features
- **Order multiple deliveries** - no more micro-managing which property gets priority
- **Vehicle pool system** - expand once, use everywhere
    - Each shop's base delivery vehicle is prioritized and selected first
    - If unavailable (already delivering), a free vehicle from the pool is assigned automatically
    - If no vehicles are available, complete deliveries to free them up or expand the pool
    - Example: With 3 pool vehicles, you can run 2 Oscar deliveries + 3 Gas Mart (Central) deliveries simultaneously (5 total)
- **Pool expansion quest** - unlock at Enforcer rank 1
    - Receive a message and go talk to Jeremy at the car dealership
    - Repeatable - visit him again to expand further
    - **Note:** If you're already Enforcer 1+, gain any XP to trigger the intro message
- **Experimental multiplayer support** - vehicle pool is shared across the lobby (requires SteamNetworkLib)

Note: May not be compatible with other delivery-adjacent mods.

## Screenshots

![Multiple delivery vans, some custom](https://raw.githubusercontent.com/k073l/s1-multidelivery/master/assets/screenshots/multiple-deliveries-world.png)

![Multiple delivery entries from the same shop](https://raw.githubusercontent.com/k073l/s1-multidelivery/master/assets/screenshots/multiple-deliveries-app.png)

![Dropoff location for adding more vehicles](https://raw.githubusercontent.com/k073l/s1-multidelivery/master/assets/screenshots/dropoff.png)

## Installation
1. Install MelonLoader
2. Extract the zip file
3. Place the dll file into the Mods directory for your branch
    - For none/beta use IL2CPP
    - For alternate/alternate beta use Mono
4. Install S1API (Forked)
5. Install SteamNetworkLib (optional, for multiplayer)
   - For [none/beta](https://thunderstore.io/c/schedule-i/p/ifBars/SteamNetworkLib_Il2Cpp/)
   - For [alternate/alternate beta](https://thunderstore.io/c/schedule-i/p/ifBars/SteamNetworkLib_Mono/)
6. Launch the game

## Uninstallation
1. Remove the mod's dll from Mods
2. Optionally, if other mods aren't using MultiDelivery's dependencies remove them as well
3. If you have saved deliveries (deliveries that weren't completed when the save happened) remove them as well
    - Navigate to your save folder and remove `Deliveries.json` to remove all delivery data - saved deliveries, receipts, history (make a backup!)

## Troubleshooting
1. Make sure you have the correct version of the mod for your branch (IL2CPP for none/beta, Mono for alternate/alternate beta)
2. Make sure you have the required dependencies 
   - S1API Forked (min version 3.0.1, latest recommended)
   - optionally SteamNetworkLib for multiplayer
3. Check the Known Issues section to see if your issue is a known one - if so, follow the recommended workaround if available
4. Check the MelonLoader logs if the mod loaded correctly, if dependencies were found, and for any errors
   - Standard log file location: `<game folder>/MelonLoader/Latest.log`
5. If issues persist, try disabling other mods to check for compatibility issues, as well as reporting the issue with as much detail as possible (steps to reproduce, expected vs actual behavior, logs, etc.)

### Testing/Debugging Commands
- `multidelivery help` - lists available commands and their usage
- `multidelivery quest force` - forces the pool expansion quest intro message, skipping the rank requirement check
- `multidelivery quest start` - starts the pool expansion quest without talking to the NPC
- `multidelivery pool add <number>` - adds the specified number of vehicles to the pool
- `multidelivery pool set <number>` - sets the pool size to the specified number. Lowering the pool size will delete vehicles, including in-use ones. Use with caution. Downsizing is also not networked.
- `multidelivery pool get` - prints the current pool size and number of vehicles currently in use

## Known Issues
- Pool vehicles don't load in correctly on second and subsequent save loads - if you start a game, enter a save, exit to menu and enter the save again, the vehicles will not load correctly. This can be fixed by quitting the game, since first load into a given save works correctly or by loading into a different save and then back to the original one.
- Graffiti on pool vehicles isn't saved to a save file. Arrived deliveries (vehicles that are waiting on the loading docks) will persist their graffiti though.

## License
This mod is licensed under MIT License. See the LICENSE.md file for more information.

[Van icon, used in the quest icon](https://lucide.dev/icons/van), Lucide, [ISC License](https://lucide.dev/license).
