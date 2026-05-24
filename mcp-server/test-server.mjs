// Simple test to verify MCP server responds correctly
import { spawn } from 'child_process';

const serverPath = 'D:\\RevitCommandRunner\\mcp-server\\dist\\index.js';

console.log('Starting MCP server...');
const server = spawn('node', [serverPath], {
  stdio: ['pipe', 'pipe', 'pipe']
});

server.stderr.on('data', (data) => {
  console.log('Server stderr:', data.toString());
});

// Send initialize request
const initRequest = {
  jsonrpc: '2.0',
  id: 1,
  method: 'initialize',
  params: {
    protocolVersion: '2024-11-05',
    capabilities: {},
    clientInfo: {
      name: 'test-client',
      version: '1.0.0'
    }
  }
};

console.log('Sending initialize request...');
server.stdin.write(JSON.stringify(initRequest) + '\n');

server.stdout.on('data', (data) => {
  console.log('Server response:', data.toString());
});

setTimeout(() => {
  console.log('Closing server...');
  server.kill();
}, 3000);
