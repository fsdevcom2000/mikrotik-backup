[Русский](README.ru.md)

# MikroTik Backup Manager

Windows command-line utility for automated MikroTik RouterOS backups.

MikroTik Backup Manager connects to configured MikroTik routers, creates binary `.backup` and text `.rsc` exports, downloads them over SFTP, verifies the downloaded files, calculates SHA-256 checksums, stores backup metadata, and removes temporary files from the router after successful local verification.

The project is designed for reliable unattended backups of one or more MikroTik routers.

## Features

* RouterOS API support
* RouterOS API-SSL support
* SFTP file transfer
* Binary `.backup` backup creation
* Text `.rsc` configuration export
* Remote and local file size verification
* SHA-256 checksums
* JSON metadata for every backup
* Configurable connection, operation and transfer timeouts
* Retry handling for transient failures
* Daily file logging
* Backup status reporting
* Configurable local retention
* Optional Telegram summary notifications
* Encrypted credential storage using Windows DPAPI
* Multi-router operation
* Windows x64 self-contained publishing
* No database or external service required

## Configuration

The application uses `config.yaml`.

A sanitized example is provided as `config.example.yaml`.

Configuration includes:

* Local backup storage
* Log location
* Backup options
* Parallelism
* Connection and operation timeouts
* Retry policy
* Retention period
* Optional Telegram notifications
* Router connection parameters

Router credentials are stored separately in the encrypted `credentials.dat` file and are not stored in `config.yaml`.

## Commands

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

`backup` performs the normal backup workflow.

`test` checks router connectivity and the backup/download workflow without creating a normal backup set.

`status` reads the latest local backup metadata and does not connect to the router.

`config validate` validates the current configuration.

`credentials add` stores router credentials in the encrypted local credential store.

## Backup layout

Backups are stored by date and router:

```text
D:\MikroTik\Backups\
└── 2026-09-06\
    └── office-01\
        ├── office-01.backup
        ├── office-01.rsc
        └── metadata.json
```

Metadata contains the router name, address, RouterOS version, timestamps, status, file sizes and SHA-256 checksums.

Secrets are not written to backup metadata.

## Remote cleanup

Temporary `.backup` and `.rsc` files are removed from the router only after the downloaded files have been successfully verified and the local backup metadata has been stored.

If local verification or storage fails, remote files are intentionally left in place.

A remote cleanup failure does not invalidate an otherwise successful backup. Such a result is reported as `SUCCESS_WITH_WARNING`.

## Telegram notifications

Telegram notifications are optional.

When enabled, one summary message is sent after all selected routers have been processed.

Telegram delivery is independent from the backup result. A Telegram API failure is logged and reported separately and does not turn a successful backup into a failed backup.

Bot tokens and chat IDs are never written to logs.

## Security

* Router credentials are stored separately from the main configuration.
* Credentials are protected with Windows DPAPI.
* Local configuration and credential files are excluded from Git.
* Logs do not contain passwords, bot tokens or full Telegram API URLs.
* Backup metadata does not contain credentials.

The backup directory and the Windows account running the application should be protected according to the requirements of the deployment environment.

## Project structure

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

The project separates CLI/orchestration, core services and models, RouterOS communication, storage and notifications.

## Compatibility

The RouterOS client supports RouterOS API and API-SSL transports.

SFTP is used for file transfer.

The project has been tested with RouterOS 6.x and 7.x.

---

## RouterOS API-SSL setup

If `api-ssl` is configured with `certificate=none`, RouterOS may reject the TLS handshake. Create a dedicated CA and server certificate for the RouterOS API-SSL service.

### 1. Create a dedicated CA certificate

```routeros
/certificate
add name=backup_api_ca common-name=backup_api_ca key-usage=key-cert-sign,crl-sign
```

Sign the CA:

```routeros
/certificate
sign backup_api_ca
```

### 2. Create a server certificate

```routeros
/certificate
add name=backup_api_server common-name=backup-api-server
```

Sign it with the new CA:

```routeros
/certificate
sign backup_api_server ca=backup_api_ca
```

### 3. Verify the certificates

```routeros
/certificate print
```

You should see entries similar to:

```text
backup_api_ca
backup_api_server
```

The server certificate should have a private key (`K` flag).

### 4. Assign the server certificate to API-SSL

```routeros
/ip service set api-ssl certificate=backup_api_server
```

Verify the configuration:

```routeros
/ip service print where name="api-ssl"
```

Expected result:

```text
api-ssl    8729    certificate=backup_api_server
```

### 5. Verify TLS from a Windows machine

OpenSSL can be used to verify that RouterOS accepts the TLS connection:

```powershell
openssl s_client -connect ROUTER_IP:8729 -tls1_2
```

A successful connection should show something similar to:

```text
Protocol: TLSv1.2
Cipher: ECDHE-RSA-AES256-GCM-SHA384
```

A self-signed certificate warning is expected when using a locally created CA.

### 6. Configure MikroTik Backup Manager

Use `api-ssl` and port `8729` in `config.yaml`:

```yaml
protocol: api-ssl
port: 8729
```

Then test the connection:

```powershell
MikroTikBackup.Cli.exe test --router ROUTER_NAME
```

If the test succeeds, run a real backup:

```powershell
MikroTikBackup.Cli.exe backup --router ROUTER_NAME
```

### Important

Use a dedicated certificate for API-SSL. Do not replace or modify certificates already used by VPN, HTTPS or other RouterOS services unless you know exactly what they are used for.

The certificate names `backup_api_ca` and `backup_api_server` are examples and can be changed if necessary.

---

## Config generator

For installations with many routers, the repository includes
`scripts/MikroTikBackupConfigGenerator.ps1`.

The generator reads router names and addresses from a simple text file and
updates only the `routers:` section of an existing `config.yaml`. Other
configuration settings are preserved.

Input format:

```text
# name;address
router-001;192.168.1.1
router-002;192.168.1.2
router-003;router-003.example.local
```

Validate the input without creating a configuration:

```
.\scripts\MikroTikBackupConfigGenerator.ps1 `
    -InputFile .\routers.txt `
    -ValidateOnly
```

Generate a configuration:

```
.\scripts\MikroTikBackupConfigGenerator.ps1 `
    -InputFile .\routers.txt `
    -OutputFile .\config.generated.yaml
```

The generated configuration can be checked with the built-in validator:

```
.\MikroTikBackup.Cli.exe config validate
```

Default router settings:

Setting	Default
Protocol	api-ssl
API port	8729
SFTP port	22
Credential	backup

These values can be changed using the generator parameters.


---

## Testing

The test suite covers the core backup workflow, storage, configuration-related services, retry handling, status processing and Telegram notifications.

At the current project state, the test suite has been verified with:

```text
130 tests passed
0 failed
0 skipped
```

The backup workflow has also been verified against a real MikroTik RouterOS device, including:

* API connection
* RouterOS version detection
* binary backup creation
* text export creation
* SFTP download
* file size verification
* SHA-256 calculation
* metadata creation
* remote cleanup
* status reporting
* Telegram notification delivery

---

## ⚠️ Scope & Limitations

This project is intentionally backup-only. Restore operations are strictly outside the scope of this utility, are not supported, and will never be added (WONTFIX). All restore procedures must be performed manually by the administrator using standard RouterOS tools such as WinBox, SSH, or the Netinstall recovery procedure.
Automatic restore is inherently risky and can easily lead to severe network downtime or complete lockout.

---

## License

MIT License.
