# DLIOutletController

A few years back I need some code to control a DLI Web Power Switch 7 (https://dlidirect.com/products/web-power-switch-7), eventually this code was ported to a Raspberry Pi running .NET Core 1.1 (and then later versions), to provide a library for knowing the state of individual switches and changing their state.

This has only been tested against my switch (running *Version 1.8.1 (Dec 16 2014 / 02:00:17)* (which is a hair out of date)), as well as some of the demo units they provide online.

This works by (unfortunately) pretending to be a web browser and parsing out specific known HTML values. It's ugly, but necessary given there is no RESTful (or other clean programmatic) interface for the Power Switch 1-7.

Some newer models have a REST interface: http://www.digital-loggers.com/restapi.pdf, something not currently (if ever) available on older models.

## Project contents:

* DLIOutletController - .NET Standard 1.3 client library (usable in .NET Core & full framework)
* OutletController - .NET Core 1.1 console app which accepts IP address, username/password and command to:
   * get status of switches
   * toggle switch
   * set individual switch state

## Usage:

```
OutletController.exe <controller ip address> -u <username> -p <password> --GetStatus
OutletController.exe <controller ip address> -u <username> -p <password> --Cycle 8
OutletController.exe <controller ip address> -u <username> -p <password> --SetSwitch 8 false
```

### ex:
```
OutletController.exe 192.168.1.42 -u admin -p 1234 --Cycle 8
```
