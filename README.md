# revit-mcp-commandset-BIG

> Forked from [mcp-servers-for-revit/revit-mcp-commandset](https://github.com/mcp-servers-for-revit/revit-mcp-commandset)
> Extended by [YonseiBIG](https://github.com/YonseiBIG) with NADIA commands for BIM element type management

## What's Added (YonseiBIG)

This fork adds **17 new commands** with corresponding EventHandlers, ported from the NADIA_SK Revit plugin:

### Query Commands (7)
| Command | Handler | Description |
|---|---|---|
| `get_rooms_by_name` | GetRoomsByNameEventHandler | Get rooms by their names |
| `get_levels_by_name` | GetLevelsByNameEventHandler | Get levels by their names |
| `find_elements_by_level` | FindElementsByLevelEventHandler | Find elements by level association |
| `find_elements_by_room` | FindElementsByRoomEventHandler | Find elements by room boundary |
| `find_hosted_elements` | FindHostedElementsEventHandler | Find doors/windows on host walls |
| `get_adjacent_rooms` | GetAdjacentRoomsEventHandler | Get rooms adjacent to element |
| `get_elements_by_type` | GetElementsByTypeEventHandler | Get instances of specific type |

### Type Change Commands (6)
| Command | Handler | Description |
|---|---|---|
| `change_wall_type` | ChangeWallTypeEventHandler | Change wall WallType |
| `change_beam_type` | ChangeFamilyInstanceTypeEventHandler | Change beam FamilySymbol |
| `change_column_type` | ChangeFamilyInstanceTypeEventHandler | Change column FamilySymbol |
| `change_door_type` | ChangeFamilyInstanceTypeEventHandler | Change door FamilySymbol |
| `change_floor_type` | ChangeFloorTypeEventHandler | Change floor FloorType |
| `change_window_type` | ChangeWindowTypeEventHandler | Change window family (preserves dimensions) |

### Type Creation Commands (4)
| Command | Handler | Description |
|---|---|---|
| `create_wall_type` | CreateWallTypeEventHandler | Create WallType with compound layers |
| `get_wall_type_info` | GetWallTypeInfoEventHandler | Get wall type layer structure |
| `create_door_type` | CreateOpeningTypeEventHandler | Create door type with dimensions |
| `create_window_type` | CreateOpeningTypeEventHandler | Create window type with dimensions |

### Shared Handlers
- **ChangeFamilyInstanceTypeEventHandler** — shared by beam/column/door (same pattern: FamilyInstance.Symbol swap)
- **CreateOpeningTypeEventHandler** — shared by door/window type creation (same pattern: duplicate + set dimensions)

---

## Installation

1. Create folder: `RevitMCPCommandSet` at the end of the usual Revit addins directory like so `C:\Users\[USERNAME]\AppData\Roaming\Autodesk\Revit\Addins\20XX\RevitMCPCommandSet\`

2. Add files:
   - Copy `command.json` from this repo to the `RevitMCPCommandSet` folder
   - Create `20XX` subfolder
   - Place compiled output from this repo in the `20XX` subfolder

3. In Revit: Go to **Add-ins** > **Settings** > **Refresh** > **Save**

## Important Note

- Command names must be identical between `revit-mcp-BIG` and `revit-mcp-commandset-BIG` repositories, otherwise the AI client cannot find them.
- The `commandRegistry.json` is created automatically, do not import it from the installer.

## Related Repositories

| Repository | Role |
|---|---|
| [revit-mcp-BIG](https://github.com/YonseiBIG/revit-mcp-BIG) | TypeScript MCP Server (Layer 1) |
| [revit-mcp-commandset-BIG](https://github.com/YonseiBIG/revit-mcp-commandset-BIG) | C# Command Set (Layer 3) — this repo |
| [revit-mcp-plugin-BIG](https://github.com/YonseiBIG/revit-mcp-plugin-BIG) | C# Plugin Framework (Layer 2) |

## License

MIT

## Credits

- Original project: [mcp-servers-for-revit](https://github.com/mcp-servers-for-revit)
- Extended by: [YonseiBIG](https://github.com/YonseiBIG)
