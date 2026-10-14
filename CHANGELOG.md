# Changelog

## 1.0.7
- Fixed a bug with multidelivery pool add command where vehicles would lack a valid GUID in some cases
## 1.0.6
- Fixed a bug where allocations would sometimes be resolved by empty GUID
- Fixed a bug where capacity text wouldn't update correctly
- Bumped S1API version to 3.2.1
## 1.0.5
- Fixed a bug where missing optional dependencies would cause issues
## 1.0.4
- Fixed a bug where capacity text wouldn't update correctly
- Fixed rare bugs related to deliveries being mixed up on game load
- Updated the mod for compatibility with game version 0.4.6-f11
- Improved mod behavior when SteamNetworkLib is not installed
- Added a new logging system to help with debugging and troubleshooting with new config options, updated the dependency checker
## 1.0.3
- Added a `multidelivery` command for testing/debugging that allows for forcing the intro message, adding pool vehicles manually, etc. See `multidelivery help` or README for more info.
## 1.0.2
- Fix for pool vehicles spawning in the central canal on game load
- Added a dependencies checker for logging when a required/optional dependency is not found
- Minor fix for grammar in quest complete text
- Added a capacity text to delivery app
## 1.0.1
- Fix for dialogue not registering correctly.
## 1.0.0
- initial
