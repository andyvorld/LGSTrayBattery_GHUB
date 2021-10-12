# LGSTRayBattery_GHUB

A tray app used to track battery levels of wireless Logitech **Gaming** devices.

Requires Logitech G HUB running in the background.

Created by reverse-engineering the websocket connection by `lghub_agent.exe`.

## Features
### Tray tooltips
![https://i.imgur.com/49XFDzb.png](https://i.imgur.com/49XFDzb.png)

Tray battery level indicator and tooltip showing battery percentage.

### "HTTP" API
By default the running of the http server is disabled, to enable modify `HttpConfig.ini` and change `serverEnable = false` to `serverEnable = true`. The IP address and port used for bindings are under `tcpAddr` and `tcpPort` respectively with the defaults being `localhost` and `12321`.

`tcpAddr` accepts either a hostname (`DESKTOP-1234`) or an IP address (`127.0.0.1`) to bind to, if you are not sure use `localhost` or if you have admin permission `0.0.0.0` to allow for external access to the devices. If an invalid hostname is provided, the server will fall back to binding on `127.0.0.1`.

![https://i.imgur.com/haYJ0se.png](https://i.imgur.com/haYJ0se.png)

Navigate to `{tcpAddr}:{tcpPort}/devices` for a list of connected devices.

![https://i.imgur.com/YrTRlYt.png](https://i.imgur.com/YrTRlYt.png)

`{tcpAddr}:{tcpPort}/device/{device_id}` for an xml data set of the selected device.

Provides additional info such as mileage (Hours left) and charging status.

## Working with
 - G403 - Mouse
 - G430 - Headset (Detects it properly, no battery)