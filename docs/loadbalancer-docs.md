# Документация по проекту LoadBalancer

## Назначение

`LoadBalancer` — HTTP-балансировщик для распределения входящих запросов между несколькими backend-инстансами сервиса.

Проект реализует:
- проксирование запросов от клиента к backend;
- выбор целевого инстанса на основе алгоритма балансировки;
- периодическое обновление списка инстансов через service discovery;
- периодическую проверку состояния инстансов (health check);
- исключение недоступных инстансов из маршрутизации.

## Архитектура

Архитектура соответствует схемам в `docs/component_diagram.puml` и `docs/sequence_diagram.puml`.

Основные компоненты:
- **RoutingMiddleware** (`LoadBalancer/Rout/RoutingMiddleware.cs`) — точка входа HTTP-запросов, orchestrator маршрутизации.
- **Router** (`LoadBalancer/Rout/Router.cs`) — проксирует HTTP-запрос на выбранный backend, копирует заголовки и тело.
- **BalanceAlgoritm + MinWeightStrategy** (`LoadBalancer/Balance`) — выбирает backend с минимальным весом среди доступных.
- **ServiceCacheHandler** (`LoadBalancer/ServiceCache/ServiceCacheHandler.cs`) — потокобезопасный кэш инстансов сервиса.
- **ServiceDiscoveryUpdater** (`LoadBalancer/ServiceDiscovery/ServiceDiscoveryUpdater.cs`) — фоновая синхронизация списка сервисов и инстансов.
- **HealthChecker + HealthCheckHostedService** (`LoadBalancer/HealthCheck`) — фоновая проверка состояния backend-инстансов.
- **HealthCache** (`LoadBalancer/HealthCheck/HealthCache.cs`) — кэш health-статусов (адрес инстанса -> healthy/unhealthy).

## Поток обработки запроса

1. Клиент отправляет HTTP-запрос в балансировщик.
2. `RoutingMiddleware` запрашивает из `ServiceCacheHandler` список инстансов для сервиса `users-service`.
3. `RoutingMiddleware` фильтрует инстансы, оставляя только healthy-инстансы по данным `HealthCache`.
4. `BalanceAlgoritm` применяет `MinWeightStrategy`:
   - учитывает только `IsAlive = true`;
   - выбирает сервер с минимальным `Weight`;
   - при равенстве веса сортирует по имени сервера.
5. `Router` проксирует исходный запрос на выбранный backend.
6. Ответ backend возвращается клиенту.

Если сервер не выбран:
- возвращается `503 Service Unavailable` (например, пустой список или нет alive-серверов);
- при внутренних ошибках балансировки — `500 Internal Server Error`.

Если backend недоступен на этапе проксирования:
- `502 Bad Gateway` при `HttpRequestException`;
- `504 Gateway Timeout` при таймауте запроса к backend.

## Service Discovery

`ServiceDiscoveryUpdater` работает как `BackgroundService` и периодически:
1. запрашивает актуальные сервисы через `IServiceRegistry`;
2. строит immutable snapshot;
3. вычисляет diff (added/removed/updated);
4. атомарно обновляет `ServiceCacheHandler`.

Особенности:
- используется exponential backoff + jitter при ошибках;
- синхронизация не останавливает приложение при временных сбоях registry;
- в текущей реализации registry — `FakeServiceRegistry`, читающий backend-список из `appsettings.json`.

## Health Checking

`HealthCheckHostedService` периодически запускает `HealthChecker`:
1. для каждого backend выполняется `GET /health`;
2. успешный ответ помечает сервер как `IsAlive = true`;
3. тело ответа интерпретируется как `Weight` (целое число нагрузки);
4. результаты записываются в `HealthCache`.

Так балансировщик использует живое состояние серверов и динамический вес нагрузки для выбора инстанса.

## Конфигурация

Ключевые настройки находятся в `LoadBalancer/appsettings.json`:

- `Settings:Backends` — список backend-серверов (`Name`, `Host`, `Port`);
- `Settings:HealthCheck:IntervalSeconds` — период health-check;
- `Settings:HealthCheck:TimeoutMilliseconds` — таймаут проверки;
- `Settings:HealthCheck:HealthEndpoint` — endpoint проверки (по умолчанию `/health`).

Пример backend-адреса вычисляется как:
- `http://{Host}:{Port}` (через свойство `Address` в `BackendConfig`).

## Обработка HTTP в Router

`Router` реализует прокси-логику:
- переносит метод, путь и query string исходного запроса;
- пробрасывает большинство заголовков, исключая hop-by-hop;
- добавляет `X-Forwarded-For`, `X-Forwarded-Proto`, `X-Forwarded-Host`;
- передает request body без преждевременного закрытия входного stream;
- копирует заголовки и тело ответа backend в ответ клиенту.

## Запуск проекта

1. Запустить backend-инстансы (например, на портах `5101` и `5102`).
2. Запустить проект `LoadBalancer`.
3. Отправлять запросы на адрес балансировщика — он проксирует их на healthy backend.

## Ограничения текущей реализации

- маршрутизация выполняется для одного логического сервиса: `users-service`;
- retry между инстансами при backend-ошибках в middleware не реализован;
- в коде используется стратегия минимального веса (`MinWeightStrategy`), а не weighted round robin.

## Связанные материалы

- Компонентная диаграмма: `docs/component_diagram.puml`
- Диаграмма последовательности: `docs/sequence_diagram.puml`
