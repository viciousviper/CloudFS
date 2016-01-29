# CloudFS
The **CloudFS** library is a collection of .NET assemblies as gateways to various publicly accessible Cloud storage services.

[![Build status](https://ci.appveyor.com/api/projects/status/wjyq2wugi651ut0x/branch/master?svg=true)](https://ci.appveyor.com/project/viciousviper/cloudfs)
[![Version](https://img.shields.io/nuget/v/CloudFS.svg)](https://www.nuget.org/packages/CloudFS)
[![NuGet downloads](https://img.shields.io/nuget/dt/CloudFS.svg)](https://www.nuget.org/packages/CloudFS)
[![NuGet downloads](https://img.shields.io/nuget/dt/CloudFS-Signed.svg)](https://www.nuget.org/packages/CloudFS-Signed)

## Objective

This library provides access to file system operations of various publicly accessible Cloud storage services behind a common interface. It thus facilitates the flexible integration of Cloud storage into arbitrary .NET applications.

## Supported Cloud storage services

Consideration of a cloud storage service as a target for CloudFS depends on these conditions:

- free storage space quota of at least 10 GB
- file expiration period no shorter than 90 days for free users
- availability of a .NET-accessible API under a non-invasive open source license (Apache, MIT, MS-PL)

Currently the following cloud storage services are supported in CloudFS via the specified API libraries:

| Cloud storage service                                            | API library                                                             | version     | sync/async | origin    | status |
| :--------------------------------------------------------------- | :---------------------------------------------------------------------- | :---------: | :--------: | :-------: | :----: |
| *(local files)*                                                  | *System.IO (.NET Framework)*                                            | *N/A*       | *sync*     |           | stable |
| [Microsoft OneDrive](https://onedrive.live.com/ "OneDrive")      | [OneDriveSDK](https://github.com/OneDrive/onedrive-explorer-win)        | N/A         | async      | official  | stable<sup id="a1">[1](#f1)</sup> |
| [Google Drive](https://drive.google.com/ "Google Drive")         | [Google Apis](https://github.com/google/google-api-dotnet-client)       | 1.10.0.1990 | async      | official  | stable |
| [Box](https://app.box.com/ "Box")                                | [Box.V2](https://github.com/box/box-windows-sdk-v2)                     | 2.6.2       | async      | official  | stable |
| [Copy](https://www.copy.com/ "Copy")                             | [CopyRestAPI](https://github.com/saguiitay/CopyRestAPI)                 | 1.1.0       | async      | 3<sup>rd</sup> party | stable |
| [MEGA](https://mega.co.nz/ "MEGA")                               | [MegaApiClient](https://github.com/gpailler/MegaApiClient)              | 1.1.3       | sync       | 3<sup>rd</sup> party | stable |
| [pCloud](https://www.pcloud.com/ "pCloud")                       | [pCloud.NET](https://github.com/nirinchev/pCloud.NET)                   | N/A         | async      | 3<sup>rd</sup> party | stable |
| [Yandex Disk](https://disk.yandex.com/client/disk "Yandex Disk") | [Yandex Disk API Client](https://github.com/raidenyn/yandexdisk.client) | 1.0.5       | async      | 3<sup>rd</sup> party | **experimental** |

> <sup><b id="f1">1</b></sup>This version of OneDriveSDK has been deprecated by Microsoft. It will be replaced by [OneDrive SDK for C#](https://github.com/OneDrive/onedrive-sdk-csharp) at some point in the future. [^](#a1)

## System Requirements

- Platform
  - .NET 4.6
- Operating system
  - tested on Windows 8.1 x64 and Windows Server 2012 R2 (until version 1.0.0-alpha) /<br/>Windows 10 x64 (from version 1.0.1-alpha)
  - expected to run on Windows 7/8/8.1/10 and Windows Server 2008(R2)/2012(R2)

## Local compilation

Several cloud storage services require additional authentication of external applications for access to cloud filesystem contents.<br/>For cloud storage services with this kind of authentication policy in place you need to take the following steps before compiling CloudFS locally:

- register for a developer account with the respective cloud service
- create a cloud application configuration with sufficient rights to access the cloud filesystem
- enter the service-provided authentication details into the prepared fields in the `Secrets` class of the affected PowerShellCloudProvider gateway project

At the time of writing this Readme, the following URLs provided access to application management tasks such as registering a new application or changing an application's configuration:

| Cloud storage service | Application registration / configuration URL           |
| :-------------------- | :----------------------------------------------------: |
| Microsoft OneDrive    | https://account.live.com/developers/applications/index |
| Google Drive          | https://console.developers.google.com                  |
| Box                   | https://app.box.com/developers/services/edit/          |
| Copy                  | https://developers.copy.com/applications               |
| MEGA                  | https://mega.nz/#sdk                                   |
| pCloud                | *- no configuration required -*                        |

## Release Notes

| Date       | Version     | Comments                                                                       |
| :--------- | :---------- | :----------------------------------------------------------------------------- |
| 2016-01-24 | 1.0.2-alpha | - Gateway configuration extended to accept custom parameters. This change **breaks compatibility** with earlier API versions.<br/>- File Gateway now configurable with target root directory |
| 2016-01-19 | 1.0.1-alpha | - NuGet dependencies updated, schema of App.config in tests project refactored |
| 2016-01-08 | 1.0.0-alpha | - Initial release and NuGet registration                                       |
| 2015-12-29 | 1.0.0.0     | - Initial commit                                                               |

## Future plans

- include additional gateways for more Cloud storage services
