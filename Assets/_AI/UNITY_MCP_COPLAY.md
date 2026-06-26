# Unity MCP — CoplayDev Setup Guide

Setup guide for **CoplayDev `unity-mcp`** (the "MCP for Unity" package by Aura),
added *alongside* the existing CoderGamester `mcp-unity` bridge.

> Project: Drift Therapy · Unity `6000.3.7f1` · added 2026-06-25

---

## 1. What was done automatically

`Packages/manifest.json` now contains **both** bridges:

```json
"com.gamelovers.mcp-unity": "https://github.com/CoderGamester/mcp-unity.git",
"com.coplaydev.unity-mcp":  "https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main",
```

When you next focus the Unity Editor it will resolve and import the CoplayDev
package from git. No further package action is needed.

---

## 2. Steps you must do in the Unity Editor

These steps require the Editor GUI and a client restart, so they can't be done headlessly.

1. **Open the project in Unity** and let it finish importing
   `com.coplaydev.unity-mcp`. Watch the Console for import/compile errors.
   - If the package does not appear: **Window → Package Manager → In Project**,
     confirm "MCP for Unity" is listed. If not, use the **+ → Add package from git URL**
     button and paste:
     `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`
2. **Window → MCP for Unity** opens the bridge window. Confirm the embedded
   **Python MCP server** auto-installs (it uses `uv`; see prerequisites below).
3. **Window → MCP for Unity → Configure All Detected Clients.** This writes the
   MCP server entry into your AI client's config file (Claude Desktop, Cursor,
   VS Code, etc.). Pick the client(s) you actually use.
4. **Fully restart the AI client** (quit and reopen — not just reload) so it
   picks up the new MCP server.
5. **Verify** with a prompt such as:
   *"Create a red, blue, and yellow cube in the current scene."*

---

## 3. Prerequisites

- **Python 3.10+** on PATH.
- **`uv`** (the Python package manager the bridge uses to run its server).
  - macOS/Linux: `curl -LsSf https://astral.sh/uv/install.sh | sh`
  - Windows: `powershell -c "irm https://astral.sh/uv/install.ps1 | iex"`
- Git (you already have it — the project is a git repo).

If the bridge window reports it can't find Python or `uv`, install them, then
use the bridge window's **re-detect / repair** button.

---

## 4. Running two bridges side-by-side (important)

You now have **two independent Unity MCP bridges**. They are different products:

| | CoderGamester `mcp-unity` | CoplayDev `unity-mcp` |
|---|---|---|
| Server | Node.js / WebSocket | Python (`uv`) |
| Default port | `8090` (set in `McpUnitySettings.json`, `AutoStartServer: true`) | its own port (commonly `6400`) — set in the MCP for Unity window |
| Client tools exposed | one set of Unity tools | a second set of Unity tools |

Coexistence notes:

- **Ports:** they default to different ports, so they should not collide. If you
  ever change either to share a port you'll get a bind error — keep them distinct.
- **Duplicate tools:** your AI client will now see *two* families of Unity tools.
  That can be confusing and occasionally causes the model to pick the wrong one.
- **Recommendation:** decide on a primary. If CoplayDev becomes your daily driver,
  set CoderGamester's `ProjectSettings/McpUnitySettings.json` to
  `"AutoStartServer": false` so only one bridge auto-starts, and optionally
  unregister its client entry. The package can stay installed.

To go single-bridge later, remove the line you don't want from
`Packages/manifest.json` and let Unity resolve.

---

## 5. Troubleshooting

- **Package won't resolve:** check internet access and that the git URL (with
  `?path=/MCPForUnity#main`) is intact in `manifest.json`. Delete
  `Library/PackageCache` entry and reimport if stale.
- **Client doesn't see tools after Configure:** you must fully **quit and reopen**
  the client; a window reload is not enough.
- **Bridge shows "disconnected":** confirm Unity is open and the in-Editor server
  is running (MCP for Unity window → Start), then reconnect from the client.
- **Compile errors block the bridge:** the bridge needs a clean compile. Clear
  Console errors first — the live bridge can't attach to a project that won't compile.

---

## 6. Quick verification checklist

- [ ] `manifest.json` lists `com.coplaydev.unity-mcp`.
- [ ] Package imported, no Console errors.
- [ ] Python 3.10+ and `uv` present.
- [ ] "Configure All Detected Clients" run for your client.
- [ ] Client fully restarted.
- [ ] Test prompt creates objects in the scene.
- [ ] Decided primary bridge; secondary set to not auto-start (optional).
