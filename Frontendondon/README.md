# Balancer Front

Runtime panel for the current load balancer backend.

## Run

```bash
npm install
npm run dev
```

Open `http://localhost:5173`.

By default the dev server proxies the actual backend endpoints:

- `/lb/*` -> `http://localhost:5120/*`
- `/backend/server-1/*` -> `http://localhost:5101/*`
- `/backend/server-2/*` -> `http://localhost:5102/*`

If ports differ, start Vite with custom targets:

```bash
BALANCER_API_TARGET=http://localhost:5120 \
BALANCER_SERVER_1_TARGET=http://localhost:5101 \
BALANCER_SERVER_2_TARGET=http://localhost:5102 \
npm run dev
```

## API

- `GET /lb/test`
- `GET /backend/server-1/health`
- `GET /backend/server-2/health`
