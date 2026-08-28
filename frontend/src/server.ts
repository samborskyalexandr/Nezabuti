import { APP_BASE_HREF } from '@angular/common';
import { CommonEngine, isMainModule } from '@angular/ssr/node';
import express from 'express';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import bootstrap from './main.server';

const serverDistFolder = dirname(fileURLToPath(import.meta.url));
const browserDistFolder = resolve(serverDistFolder, '../browser');
const indexHtml = join(serverDistFolder, 'index.server.html');

const app = express();

const extraHosts = (process.env['SSR_ALLOWED_HOSTS'] || '')
  .split(',')
  .map((h) => h.trim())
  .filter(Boolean);

const commonEngine = new CommonEngine({
  allowedHosts: [
    'localhost',
    '127.0.0.1',
    'nezabuti.com.ua',
    'www.nezabuti.com.ua',
    'frontend',
    ...extraHosts
  ]
});

app.use(
  express.static(browserDistFolder, {
    maxAge: '1y',
    index: false,
    redirect: false
  })
);

function readSsrStatus(html: string): number {
  const match = html.match(/<meta\s+name="ssr:status"\s+content="(\d+)"/i);
  if (!match) {
    return 200;
  }
  const status = Number(match[1]);
  return Number.isFinite(status) ? status : 200;
}

app.get('**', (req, res, next) => {
  const { protocol, originalUrl, baseUrl, headers } = req;

  commonEngine
    .render({
      bootstrap,
      documentFilePath: indexHtml,
      url: `${protocol}://${headers.host}${originalUrl}`,
      publicPath: browserDistFolder,
      providers: [{ provide: APP_BASE_HREF, useValue: baseUrl }]
    })
    .then((html) => {
      res.status(readSsrStatus(html)).send(html);
    })
    .catch((err) => next(err));
});

if (isMainModule(import.meta.url)) {
  const port = process.env['PORT'] || 4000;
  app.listen(port, () => {
    console.log(`Node Express server listening on http://localhost:${port}`);
  });
}

export default app;
