# Интеграционные тесты

Этот проект содержит интеграционные тесты для GraphQL и REST эндпоинтов.

## Требования

- Docker Desktop должен быть запущен (для Testcontainers)
- .NET 10.0 SDK

## Запуск тестов

```bash
dotnet test
```

## Структура тестов

- `IntegrationTestBase.cs` - базовый класс для интеграционных тестов, который настраивает InfluxDB контейнер через Testcontainers
- `GraphQLEndpointTests.cs` - тесты для GraphQL эндпоинтов (`/graphql`)
- `RestEndpointTests.cs` - тесты для REST эндпоинта (`/metrics/metadata`)

## Использование Testcontainers

Тесты используют Testcontainers для автоматического создания и управления InfluxDB контейнером. Контейнер запускается перед каждым тестом и автоматически удаляется после завершения.

