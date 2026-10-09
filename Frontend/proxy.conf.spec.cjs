const { before, after, test } = require('node:test');
const assert = require('node:assert/strict');
const http = require('node:http');
const { once } = require('node:events');
const configuration = require('./proxy.conf.cjs');

let backend;
let vite;
let port;
let backendPort;
let forwarded = 0;

before(async () => {
  backend = http.createServer((request, response) => {
    forwarded++;
    response.setHeader('Content-Type', 'application/json');
    response.end(JSON.stringify(request.headers));
  });
  backend.listen(0, '127.0.0.1');
  await once(backend, 'listening');
  backendPort = backend.address().port;
  const { createServer } = await import('vite');
  vite = await createServer({
    configFile: false,
    logLevel: 'error',
    server: {
      host: '127.0.0.1',
      port: 0,
      hmr: false,
      watch: null,
      proxy: {
        '/api': { ...configuration['/api'], target: `http://127.0.0.1:${backendPort}` },
      },
    },
  });
  await vite.listen();
  port = vite.httpServer.address().port;
});

after(async () => {
  await vite?.close();
  if (backend) await new Promise((resolve) => backend.close(resolve));
});

function send(method = 'GET', headers = {}) {
  return new Promise((resolve, reject) => {
    const request = http.request(
      {
        host: '127.0.0.1',
        port,
        path: '/api/v1/session',
        method,
        headers: { host: '127.0.0.1:4200', ...headers },
      },
      (response) => {
        let body = '';
        response.on('data', (chunk) => {
          body += chunk;
        });
        response.on('end', () => resolve({ status: response.statusCode, body }));
      },
    );
    request.on('error', reject);
    request.end();
  });
}

test('same-origin reads reach the API with its target Host', async () => {
  assert.equal(configuration['/api'].target, 'http://127.0.0.1:5080');
  const response = await send();
  assert.equal(response.status, 200);
  assert.equal(JSON.parse(response.body).host, `127.0.0.1:${backendPort}`);
});

test('trusted development writes adapt Origin and retain cookie and CSRF header', async () => {
  const response = await send('POST', {
    origin: 'http://127.0.0.1:4200',
    cookie: 'session=test-fixture',
    'x-csrf-token': 'test-antiforgery',
  });
  assert.equal(response.status, 200);
  const headers = JSON.parse(response.body);
  assert.equal(headers.origin, 'http://127.0.0.1:5080');
  assert.equal(headers.cookie, 'session=test-fixture');
  assert.equal(headers['x-csrf-token'], 'test-antiforgery');
});

test('foreign and absent write origins never reach the backend', async () => {
  const beforeRequests = forwarded;
  for (const origin of [undefined, 'null', 'https://attacker.example', 'http://127.0.0.1:5099']) {
    const response = await send('POST', origin === undefined ? {} : { origin });
    assert.equal(response.status, 404);
  }
  assert.equal(forwarded, beforeRequests);
});

test('unexpected incoming Host and foreign read Origin never reach the backend', async () => {
  const beforeRequests = forwarded;
  assert.equal((await send('GET', { host: '127.0.0.1:5099' })).status, 404);
  assert.equal((await send('GET', { origin: 'https://attacker.example' })).status, 404);
  assert.equal(forwarded, beforeRequests);
});
