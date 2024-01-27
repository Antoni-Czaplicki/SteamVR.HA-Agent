# Home Assistant Agent for SteamVR
[![GitHub release](https://img.shields.io/github/release/Antoni-Czaplicki/SteamVR.HA-Agent?include_prereleases=&sort=semver&color=blue&style=for-the-badge)](https://github.com/Antoni-Czaplicki/SteamVR.HA-Agent/releases/)
[![License](https://img.shields.io/badge/License-AGPL--3.0-blue?style=for-the-badge)](#license)
![OS - Windows 11](https://img.shields.io/badge/OS-Windows_11-09e0fe?style=for-the-badge)
![OS - Windows 10 1809+](https://img.shields.io/badge/OS-Windows_10_1809%2B-00adef?style=for-the-badge)

## About
This app allows you to connect your Home Assistant instance with SteamVR, to learn more visit the [integration repo](https://github.com/Antoni-Czaplicki/SteamVR.HA).

## Installation instructions
- Option 1
Compile application from the source code and install it by yourself

- Option 2
1. Download compiled app package attached to the [latest release](https://github.com/Antoni-Czaplicki/SteamVR.HA-Agent/releases/latest)
2. Install certificate (the app is using self-signed cert for now so you have to add it to trusted by your machine)
`Right mouse click on downloaded file` -> `Properties` -> `Digital Signatures` -> `The first cert "antek"` -> `View Certificate` -> `Install Certificate` -> `Local Machine` -> `Next` -> `Place all certificates in the following store` -> `Browse` -> `Trusted People` -> `OK` -> `Next` -> `Finish`
3. Double click on the app package to start installation

## Screenshots
![image](https://github.com/Antoni-Czaplicki/SteamVR.HA-Agent/assets/56671347/e431afe0-0b40-48d0-a916-124767b454e5)
![image](https://github.com/Antoni-Czaplicki/SteamVR.HA-Agent/assets/56671347/83702950-4d03-47ea-ae47-8cf92ae967c5)
![image](https://github.com/Antoni-Czaplicki/SteamVR.HA-Agent/assets/56671347/65056c33-ce40-4320-b411-54010aba90b5)
![image](https://github.com/Antoni-Czaplicki/SteamVR.HA-Agent/assets/56671347/023d2605-ae84-4e0e-9e9a-64e470a55b41)
![image](https://github.com/Antoni-Czaplicki/SteamVR.HA-Agent/assets/56671347/379ecd60-3f22-4cab-8b05-ab8c9fbd8933)




Based on https://github.com/BOLL7708/OpenVROverlayPipe for displaying notifications
