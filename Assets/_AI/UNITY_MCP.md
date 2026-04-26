# Unity MCP

## Status
- MCP Unity is installed in the project.
- Live Unity bridge connection is confirmed.
- The editor server is using port `8090`.
- The bridge was used to create, wire, compile, and save the new scene.

## Project settings
- `ProjectSettings/McpUnitySettings.json`
- `AutoStartServer: true`
- `Port: 8090`

## What is available through MCP
- Scene inspection.
- GameObject inspection and editing.
- Component editing.
- Asset placement.
- Scene creation and saving.
- Script recompilation.
- Test execution.
- Console log access.

## Current note
- MCP is working as a development tool for this project.
- If a future MCP call fails, retry after checking the bridge state and Unity console.

## Fast verification pattern
- Check `unity://scenes_hierarchy`.
- Check `unity://logs/error`.
- Recompile scripts.
- Save the active scene.
