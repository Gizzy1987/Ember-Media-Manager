← [Back to overview](index.md)

# Jellyfin Interface — Setup for Users

## Purpose

So that the module can reach the server and trigger a library refresh, an API key is needed on the Jellyfin server, and the host has to be entered in Ember.

## Settings

| Setting | Meaning |
|---------|---------|
| Name | Label of the host; shows up in the *Jellyfin* menu and in the host list of the settings |
| Base URL | Address of the Jellyfin server (e. g. `http://server:8096`); a missing scheme is completed to `http://`, a trailing slash is removed |
| API key | Key from the Jellyfin API settings; it is sent as `X-Emby-Token` with every call |
| Enabled | Only enabled hosts are refreshed and shown in the menu |
| Module enabled | Switch the module on in the settings dialog |
| Notifications | Report the success and the failure of a refresh as a notification |

## Steps

1. Open the administration of the Jellyfin server and create a new key for the user under *API Keys*.
2. Open *Edit → Settings...* in Ember and go to the section of the Jellyfin interface.
3. Enable the module and create a host with *Add*.
4. Enter name, base URL and API key in the host dialog and check them with *Check Connection*: on success the server name, the version and the libraries of the server are shown.
5. Save the host and pick *Update Library* under *Jellyfin* in the Tools menu to trigger the update once manually.

## Notes

- With several Jellyfin servers one host is created per server; disabled hosts do not show up in the menu.
- With an invalid base URL or an unreachable server the module reports an error; the Ember library stays untouched.
- The update only takes effect once Jellyfin actually sees the changed files in the library folder — the files therefore have to be located in the media folder of the server.
