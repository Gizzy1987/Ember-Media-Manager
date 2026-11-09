← [Back to overview](index.md)

# Jellyfin Interface — API

## Overview

The interface consists of the VB module `generic.Interface.Jellyfin` (`Addons/generic.Interface.Jellyfin/Interface.Jellyfin.vb`) and the HTTP wrapper `clsAPIJellyfin` (`Addons/generic.Interface.Jellyfin/clsAPIJellyfin.vb`). All calls run asynchronously against the Jellyfin HTTP API; errors are not thrown but returned as a result (`JellyfinResult`) or as `Nothing`.

## Authentication

Every request carries the API key of the host as a header:

| Header | Content |
|--------|---------|
| `X-Emby-Token` | API key of the configured host |
| `X-Emby-Authorization` | Client identification (`MediaBrowser Client="Ember", Device="Ember"`), version `1.12.0.0`, device ID (SHA-256 of the machine name and the user name) and the token |

## Endpoints in use

### `GET {base URL}/System/Info`

| Purpose | Result |
|---------|--------|
| Connection test, system information of the server | `JellyfinServerInfo` with `ServerName`, `Version`, `Id`; `Nothing` on errors |

### `GET {base URL}/Library/VirtualFolders`

| Purpose | Result |
|---------|--------|
| Show the libraries configured on the server | List of the library names, an empty list on errors |

### `POST {base URL}/Library/Refresh`

| Purpose | Result |
|---------|--------|
| Trigger a library scan on the server | `JellyfinResult` with a success flag and a message (`HTTP 200` or similar) |

## Further helper functions

| Method | Purpose |
|--------|---------|
| `NormalizeBaseUrl(url)` | Remove whitespace, add a missing scheme, remove trailing slashes |
| `IsValidBaseUrl(url)` | Check whether the normalised URL is an absolute http or https URL |

## Error handling

| Case | Behaviour |
|------|-----------|
| Host unreachable or timeout (15 s per call) | `GetServerInfoAsync` returns `Nothing`, `RefreshLibraryAsync` returns an error result; the error is logged |
| Wrong or missing API key | The server answers with an error status; the result contains the HTTP status |
| Invalid base URL | Already rejected while entering the host in the host dialog |
