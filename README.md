# Advertising Platform Service

Простой веб-сервис для хранения и поиска рекламных площадок по локациям.  

Сервис позволяет:

- Загружать рекламные площадки из текстового файла.
- Искать рекламные площадки для заданной локации (учитывается вложенность локаций).
- Хранить данные полностью в памяти (in-memory) с быстрым доступом.
- Юнит-тестирование основных функций.

---

## 🛠 Структура проекта

- **Controllers/** – API контроллеры (`AdController`)  
- **Services/Ads/** – Бизнес-логика (`AdvertisingService`)  
- **Services/Parser/** – Парсер файлов (`FileParser`)  
- **Services/Storage/MemoryStorage/** – Кэш в памяти (`MemoryCacheService`)  
- **Models/** – Модели данных (`AdvertisingData`)  
- **Tests/** – Юнит-тесты для сервиса

---

## ⚡ Требования

- .NET 7 SDK или выше
- Visual Studio 2022 / VS Code
- Пакеты NuGet:
  - `Microsoft.Extensions.Caching.Memory`
  - `Microsoft.NET.Test.Sdk`
  - `xunit`
  - `xunit.runner.visualstudio`
  - `Moq`

---

## 🚀 Запуск веб-сервиса

1. Клонируйте репозиторий:

```bash
git clone https://github.com/USERNAME/AdvertisingPlatformService.git
cd AdvertisingPlatformService
