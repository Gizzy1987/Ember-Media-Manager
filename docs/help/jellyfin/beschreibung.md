← [Back to overview](index.md)

# Jellyfin Interface — Description

## Purpose

The *Jellyfin* module connects Ember Media Manager with one or more Jellyfin servers. After scanning, scraping or editing in Ember the library of the Jellyfin server is refreshed, so the changes show up in Jellyfin without any manual follow-up work.

## How it works

- **Hosts:** Multiple Jellyfin servers are managed with a name, a base URL and an API key; every host can be enabled or disabled individually. The entries are stored in `Interface.Jellyfin.xml` in the profile folder.
- **Check connection:** The host dialog queries the server information and shows the server name and version; afterwards the libraries configured on the server are listed.
- **Update library:** The *Jellyfin* entry in the Tools menu and in the tray menu offers *Update Library* and *Check Connection*. The update triggers a library scan on the server.
- **Automatic update:** After sync and remove events in Ember the refresh is triggered automatically and debounced — a bulk import therefore results in exactly one scan.
- **Notifications:** If notifications are enabled, the module reports the success or the failure of a refresh as a notification.

## Examples

- A movie was edited in Ember → the library of the configured Jellyfin host is refreshed after a short delay.
- Several Jellyfin servers (living room, basement) → create one host per server and refresh them separately.

## Limits

- Jellyfin cannot update single items, so the module only ever triggers a library scan.
- Every request needs a valid API key of the server and the host has to be reachable from the network.
- Playback state and other data held by the server are not read back.
