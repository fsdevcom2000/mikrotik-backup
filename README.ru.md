# Русский

# MikroTik Backup Manager

Консольная утилита для Windows для автоматизированного резервного копирования MikroTik RouterOS.

MikroTik Backup Manager подключается к настроенным MikroTik, создаёт бинарный `.backup` и текстовый `.rsc` export, скачивает файлы по SFTP, проверяет загруженные файлы, рассчитывает SHA-256, сохраняет метаданные и удаляет временные файлы с роутера после успешной локальной проверки.

Проект предназначен для надёжного автоматического резервного копирования одного или нескольких MikroTik.

## Возможности

* Поддержка RouterOS API
* Поддержка RouterOS API-SSL
* Передача файлов по SFTP
* Создание бинарного `.backup`
* Создание текстового `.rsc` export
* Проверка размеров удалённых и локальных файлов
* SHA-256 контрольные суммы
* JSON metadata для каждого backup
* Настраиваемые таймауты подключения, операций и передачи
* Повторные попытки для временных ошибок
* Ежедневное файловое логирование
* Просмотр статуса backup
* Настраиваемое локальное хранение
* Необязательные уведомления в Telegram
* Шифрование credentials через Windows DPAPI
* Работа с несколькими роутерами
* Self-contained публикация для Windows x64
* Без базы данных и внешних сервисов

## Конфигурация

Программа использует файл `config.yaml`.

Обезличенный пример конфигурации находится в `config.example.yaml`.

В конфигурации задаются:

* Каталог локальных backup
* Каталог логов
* Параметры backup
* Параллелизм
* Таймауты подключения и операций
* Политика повторных попыток
* Срок хранения backup
* Необязательные Telegram уведомления
* Параметры подключения к роутерам

Учётные данные роутеров хранятся отдельно в зашифрованном файле `credentials.dat` и не записываются в `config.yaml`.

## Команды

```text
MikroTikBackup.Cli.exe backup
MikroTikBackup.Cli.exe backup --router office-01

MikroTikBackup.Cli.exe config validate

MikroTikBackup.Cli.exe credentials add

MikroTikBackup.Cli.exe test
MikroTikBackup.Cli.exe test --router office-01

MikroTikBackup.Cli.exe status
MikroTikBackup.Cli.exe status --router office-01
```

`backup` выполняет обычный сценарий резервного копирования.

`test` проверяет подключение к роутеру и сценарий создания и скачивания backup без создания обычного набора backup.

`status` читает локальные metadata последнего backup и не подключается к роутеру.

`config validate` проверяет текущую конфигурацию.

`credentials add` сохраняет credentials в зашифрованное локальное хранилище.

## Структура backup

Backup хранятся по дате и имени роутера:

```text
D:\MikroTik\Backups\
└── 2026-09-06\
    └── office-01\
        ├── office-01.backup
        ├── office-01.rsc
        └── metadata.json
```

Metadata содержит имя роутера, адрес, версию RouterOS, временные метки, статус, размеры файлов и SHA-256.

Секреты в metadata не записываются.

## Удаление временных файлов на роутере

Временные `.backup` и `.rsc` удаляются с роутера только после успешного скачивания, проверки локальных файлов и сохранения локальных metadata.

Если локальная проверка или сохранение не удалось, удалённые файлы намеренно остаются на роутере.

Ошибка удаления на роутере не отменяет уже успешно выполненный backup. В таком случае результат получает статус `SUCCESS_WITH_WARNING`.

## Telegram уведомления

Telegram уведомления являются необязательными.

При включении после обработки всех выбранных роутеров отправляется одно итоговое сообщение.

Ошибка Telegram не влияет на результат самого backup. Она отдельно записывается в лог и выводится как ошибка уведомления.

Bot token и chat ID не записываются в логи.

## Безопасность

* Учётные данные роутеров хранятся отдельно от основной конфигурации.
* Credentials защищаются Windows DPAPI.
* Локальная конфигурация и credentials исключены из Git.
* Пароли, bot tokens и полные Telegram API URL не записываются в логи.
* Credentials не записываются в backup metadata.

Каталог backup и Windows-учётная запись, от имени которой запускается программа, должны быть защищены в соответствии с требованиями конкретного окружения.

## Структура проекта

```text
src/
├── MikroTikBackup.Cli/
├── MikroTikBackup.Core/
├── MikroTikBackup.Notifications/
├── MikroTikBackup.RouterOS/
└── MikroTikBackup.Storage/

tests/
├── Core.Tests/
├── Notifications.Tests/
└── Storage.Tests/
```

Проект разделяет CLI/orchestration, core-сервисы и модели, работу с RouterOS, хранение файлов и уведомления.

## Совместимость

RouterOS client поддерживает RouterOS API и API-SSL transports.

Для передачи файлов используется SFTP.

Проект протестирован с RouterOS 6.x и 7.x.

---

## Настройка API-SSL на RouterOS

Если у RouterOS сервис `api-ssl` настроен с `certificate=none`, TLS-соединение может завершаться ошибкой `HandshakeFailure`.

Для API-SSL можно создать отдельный CA и отдельный серверный сертификат.

### 1. Создание отдельного CA

Создаём собственный CA для API-SSL:

```routeros
/certificate
add name=backup_api_ca common-name=backup_api_ca key-usage=key-cert-sign,crl-sign
```

Подписываем CA:

```routeros
/certificate
sign backup_api_ca
```

### 2. Создание серверного сертификата

Создаём сертификат для API-SSL:

```routeros
/certificate
add name=backup_api_server common-name=backup-api-server
```

Подписываем его созданным CA:

```routeros
/certificate
sign backup_api_server ca=backup_api_ca
```

### 3. Проверка сертификатов

Проверяем созданные сертификаты:

```routeros
/certificate print
```

В списке должны появиться:

```text
backup_api_ca
backup_api_server
```

У `backup_api_server` должен присутствовать флаг `K` - это означает, что у сертификата есть приватный ключ.

### 4. Назначение сертификата для API-SSL

Назначаем серверный сертификат сервису `api-ssl`:

```routeros
/ip service set api-ssl certificate=backup_api_server
```

Проверяем:

```routeros
/ip service print where name="api-ssl"
```

Должно быть примерно:

```text
api-ssl    8729    certificate=backup_api_server
```

### 5. Проверка TLS

С Windows-компьютера можно проверить TLS напрямую через OpenSSL:

```powershell
openssl s_client -connect ROUTER_IP:8729 -tls1_2
```

При успешном подключении в выводе должны присутствовать, например:

```text
Protocol: TLSv1.2
Cipher: ECDHE-RSA-AES256-GCM-SHA384
```

Сообщение:

```text
self-signed certificate in certificate chain
```

для созданного нами собственного CA является нормальным. Оно означает, что OpenSSL не доверяет этому CA как системному центру сертификации, но сам TLS handshake при этом может быть успешно установлен.

### 6. Настройка MikroTik Backup Manager

В `config.yaml` для соответствующего роутера указываем:

```yaml
protocol: api-ssl
port: 8729
```

После этого проверяем подключение:

```powershell
MikroTikBackup.Cli.exe test --router ROUTER_NAME
```

Если тест завершился успешно, можно выполнить резервное копирование:

```powershell
MikroTikBackup.Cli.exe backup --router ROUTER_NAME
```

### Важно

Для API-SSL рекомендуется использовать отдельный сертификат.

Не изменяйте и не заменяйте сертификаты, которые уже используются для VPN, HTTPS или других сервисов RouterOS, если вы не уверены в их назначении.

В примере используются имена:

```text
backup_api_ca
backup_api_server
```

Их можно изменить при необходимости.

---

## Генератор конфигурации

Для использования с большим количеством роутеров в репозитории есть
`scripts/MikroTikBackupConfigGenerator.ps1`.

Генератор читает имена и адреса роутеров из простого текстового файла и
заменяет только секцию `routers:` в существующем `config.yaml`. Остальные
настройки конфигурации сохраняются.

Формат входного файла:

```text
# name;address
router-001;192.168.1.1
router-002;192.168.1.2
router-003;router-003.example.local
```

Проверить входной файл без создания конфигурации:

```
.\scripts\MikroTikBackupConfigGenerator.ps1 `
    -InputFile .\routers.txt `
    -ValidateOnly
```

Создать конфигурацию:

```
.\scripts\MikroTikBackupConfigGenerator.ps1 `
    -InputFile .\routers.txt `
    -OutputFile .\config.generated.yaml
```

После генерации конфигурацию можно проверить встроенным валидатором:

```
.\MikroTikBackup.Cli.exe config validate
```

Настройки роутеров по умолчанию:

Параметр	Значение
Протокол	api-ssl
API порт	8729
SFTP порт	22
Credential	backup

Эти значения можно изменить параметрами генератора.

---


## Тестирование

Тесты покрывают основной backup workflow, storage, конфигурационные сервисы, retry-логику, обработку статусов и Telegram уведомления.

На текущем состоянии проекта тестовый набор проверен с результатом:

```text
130 tests passed
0 failed
0 skipped
```

Backup workflow также проверен на реальном MikroTik с RouterOS, включая:

* подключение через API
* определение версии RouterOS
* создание бинарного backup
* создание текстового export
* скачивание по SFTP
* проверку размеров файлов
* расчёт SHA-256
* создание metadata
* удаление временных файлов на роутере
* получение статуса
* отправку Telegram уведомления

## Лицензия

MIT License.
