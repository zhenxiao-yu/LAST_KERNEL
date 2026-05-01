# Codex MCP Setup

This project uses the Unity MCP package from `Packages/manifest.json`:

```json
"com.coplaydev.unity-mcp": "https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main"
```

Project MCP endpoint:

```json
{
  "mcpServers": {
    "unity-mcp": {
      "type": "http",
      "url": "http://127.0.0.1:8080/mcp"
    }
  }
}
```

Codex is wired in `C:\Users\YZX06\.codex\config.toml`:

```toml
[features]
rmcp_client = true

[mcp_servers.unity-mcp]
url = "http://127.0.0.1:8080/mcp"
```

To use it:

1. Open this project in Unity.
2. Start/enable the Unity MCP server from the Unity MCP package UI.
3. Restart Codex so it reloads `config.toml`.
4. Confirm the Unity MCP server is listening at `http://127.0.0.1:8080/mcp`.
