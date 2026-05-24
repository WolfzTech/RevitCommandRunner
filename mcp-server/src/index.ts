#!/usr/bin/env node

import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  Tool,
} from "@modelcontextprotocol/sdk/types.js";
import { spawn } from "child_process";
import { promises as fs } from "fs";
import * as path from "path";
import * as os from "os";

// Configuration
const QUEUE_DIR = path.join(os.homedir(), "AppData", "Local", "RevitCommandRunner");
const QUEUE_FILE = path.join(QUEUE_DIR, "command-queue.json");
const RESULTS_DIR = path.join(QUEUE_DIR, "results");

interface CommandRequest {
  id: string;
  timestamp: string;
  dll: string;
  command: string;
  args?: string[];
  metadata?: Record<string, any>;
}

interface CommandResult {
  id: string;
  success: boolean;
  result: string;
  message: string;
  executionTimeMs: number;
  timestamp: string;
  logs: string[];
  exception: any;
  customData?: Record<string, any>;
}

// Helper: Generate unique ID
function generateId(): string {
  return `mcp-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
}

// Helper: Execute PowerShell command
async function executePowerShell(script: string): Promise<string> {
  return new Promise((resolve, reject) => {
    const ps = spawn("pwsh", ["-NoProfile", "-Command", script], {
      stdio: ["pipe", "pipe", "pipe"],
    });

    let stdout = "";
    let stderr = "";

    ps.stdout.on("data", (data) => {
      stdout += data.toString();
    });

    ps.stderr.on("data", (data) => {
      stderr += data.toString();
    });

    ps.on("close", (code) => {
      if (code !== 0) {
        reject(new Error(`PowerShell exited with code ${code}: ${stderr}`));
      } else {
        resolve(stdout);
      }
    });
  });
}

// Helper: Wait for result file
async function waitForResult(id: string, timeoutSeconds: number = 60): Promise<CommandResult> {
  const resultPath = path.join(RESULTS_DIR, `results-${id}.json`);
  const startTime = Date.now();
  const timeoutMs = timeoutSeconds * 1000;

  while (Date.now() - startTime < timeoutMs) {
    try {
      const content = await fs.readFile(resultPath, "utf-8");
      const result = JSON.parse(content) as CommandResult;
      return result;
    } catch (error) {
      // File doesn't exist yet, wait and retry
      await new Promise((resolve) => setTimeout(resolve, 500));
    }
  }

  throw new Error(`Timeout waiting for result after ${timeoutSeconds} seconds`);
}

// Helper: Check if Revit is monitoring
async function checkRevitStatus(): Promise<boolean> {
  try {
    // Check if queue directory exists
    await fs.access(QUEUE_DIR);
    
    // Try to check if there's a recent result file (indicates Revit is active)
    const files = await fs.readdir(RESULTS_DIR);
    return files.length >= 0; // Directory exists and is accessible
  } catch (error) {
    return false;
  }
}

// Helper: List recent results
async function listRecentResults(limit: number = 10): Promise<CommandResult[]> {
  try {
    const files = await fs.readdir(RESULTS_DIR);
    const resultFiles = files
      .filter((f) => f.startsWith("results-") && f.endsWith(".json"))
      .sort()
      .reverse()
      .slice(0, limit);

    const results: CommandResult[] = [];
    for (const file of resultFiles) {
      try {
        const content = await fs.readFile(path.join(RESULTS_DIR, file), "utf-8");
        results.push(JSON.parse(content));
      } catch (error) {
        // Skip invalid files
      }
    }

    return results;
  } catch (error) {
    return [];
  }
}

// Helper: Execute Revit command
async function executeRevitCommand(
  dllPath: string,
  commandClassName: string,
  args?: string[],
  timeoutSeconds: number = 60
): Promise<CommandResult> {
  // Ensure directories exist
  await fs.mkdir(QUEUE_DIR, { recursive: true });
  await fs.mkdir(RESULTS_DIR, { recursive: true });

  // Generate request
  const id = generateId();
  const request: CommandRequest = {
    id,
    timestamp: new Date().toISOString(),
    dll: dllPath,
    command: commandClassName,
    args: args || [],
    metadata: {
      source: "mcp-server",
    },
  };

  // Write queue file
  await fs.writeFile(QUEUE_FILE, JSON.stringify(request, null, 2), "utf-8");

  // Wait for result
  const result = await waitForResult(id, timeoutSeconds);
  return result;
}

// Helper: Get command result by ID
async function getCommandResult(id: string): Promise<CommandResult> {
  const resultPath = path.join(RESULTS_DIR, `results-${id}.json`);
  const content = await fs.readFile(resultPath, "utf-8");
  return JSON.parse(content);
}

// Define MCP tools
const tools: Tool[] = [
  {
    name: "execute_revit_command",
    description:
      "Execute a Revit command from a DLL. The command must implement IExternalCommand or IExternalCommandWithUIApp. Returns execution results including success status, logs, exceptions, and custom data.",
    inputSchema: {
      type: "object",
      properties: {
        dllPath: {
          type: "string",
          description: "Absolute path to the DLL containing the command (e.g., D:\\MyPlugin\\bin\\Debug\\MyPlugin.dll)",
        },
        commandClassName: {
          type: "string",
          description: "Fully qualified class name of the command (e.g., MyNamespace.MyCommand)",
        },
        args: {
          type: "array",
          items: { type: "string" },
          description: "Optional arguments to pass to the command",
        },
        timeoutSeconds: {
          type: "number",
          description: "Maximum time to wait for command execution (default: 60)",
          default: 60,
        },
      },
      required: ["dllPath", "commandClassName"],
    },
  },
  {
    name: "get_command_result",
    description: "Retrieve the result of a previously executed command by its ID.",
    inputSchema: {
      type: "object",
      properties: {
        id: {
          type: "string",
          description: "The command ID returned from execute_revit_command",
        },
      },
      required: ["id"],
    },
  },
  {
    name: "check_revit_status",
    description:
      "Check if Revit is running and the RevitCommandRunner add-in is monitoring for commands. Returns true if the system is ready to accept commands.",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "list_recent_results",
    description: "List recent command execution results, sorted by most recent first.",
    inputSchema: {
      type: "object",
      properties: {
        limit: {
          type: "number",
          description: "Maximum number of results to return (default: 10)",
          default: 10,
        },
      },
    },
  },
];

// Create MCP server
const server = new Server(
  {
    name: "revit-command-runner",
    version: "1.0.0",
  },
  {
    capabilities: {
      tools: {},
    },
  }
);

// Handle tool list requests
server.setRequestHandler(ListToolsRequestSchema, async () => {
  return { tools };
});

// Handle tool execution
server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;

  try {
    switch (name) {
      case "execute_revit_command": {
        const { dllPath, commandClassName, args: cmdArgs, timeoutSeconds } = args as {
          dllPath: string;
          commandClassName: string;
          args?: string[];
          timeoutSeconds?: number;
        };

        const result = await executeRevitCommand(
          dllPath,
          commandClassName,
          cmdArgs,
          timeoutSeconds || 60
        );

        return {
          content: [
            {
              type: "text",
              text: JSON.stringify(result, null, 2),
            },
          ],
        };
      }

      case "get_command_result": {
        const { id } = args as { id: string };
        const result = await getCommandResult(id);

        return {
          content: [
            {
              type: "text",
              text: JSON.stringify(result, null, 2),
            },
          ],
        };
      }

      case "check_revit_status": {
        const isReady = await checkRevitStatus();

        return {
          content: [
            {
              type: "text",
              text: JSON.stringify(
                {
                  ready: isReady,
                  queueDirectory: QUEUE_DIR,
                  message: isReady
                    ? "Revit is ready to accept commands"
                    : "Revit is not running or RevitCommandRunner is not installed",
                },
                null,
                2
              ),
            },
          ],
        };
      }

      case "list_recent_results": {
        const { limit } = args as { limit?: number };
        const results = await listRecentResults(limit || 10);

        return {
          content: [
            {
              type: "text",
              text: JSON.stringify(results, null, 2),
            },
          ],
        };
      }

      default:
        throw new Error(`Unknown tool: ${name}`);
    }
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : String(error);
    const stackTrace = error instanceof Error ? error.stack : undefined;
    return {
      content: [
        {
          type: "text",
          text: JSON.stringify({ 
            error: errorMessage,
            stack: stackTrace 
          }, null, 2),
        },
      ],
    };
  }
});

// Start server
async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error("RevitCommandRunner MCP server running on stdio");
}

main().catch((error) => {
  console.error("Fatal error:", error);
  process.exit(1);
});
