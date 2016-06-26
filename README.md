# CloudFS
The **CloudFS** library is a collection of .NET assemblies as gateways to various publicly accessible Cloud storage services.

[![License](https://img.shields.io/github/license/mashape/apistatus.svg)](https://github.com/viciousviper/CloudFS/blob/master/LICENSE.md)
[![Release](https://img.shields.io/github/tag/viciousviper/CloudFS.svg)](https://github.com/viciousviper/CloudFS/releases)
[![Build status](https://ci.appveyor.com/api/projects/status/wjyq2wugi651ut0x/branch/master?svg=true)](https://ci.appveyor.com/project/viciousviper/cloudfs)
[![Version](https://img.shields.io/nuget/v/CloudFS.svg)](https://www.nuget.org/packages/CloudFS)
[![NuGet downloads](https://img.shields.io/nuget/dt/CloudFS.svg)](https://www.nuget.org/packages/CloudFS)
[![NuGet downloads (signed)](https://img.shields.io/nuget/dt/CloudFS-Signed.svg)](https://www.nuget.org/packages/CloudFS-Signed)

## Objective

This library provides access to file system operations of various publicly accessible Cloud storage services behind a common interface. It thus facilitates the flexible integration of Cloud storage into arbitrary .NET applications.

## Supported Cloud storage services

Consideration of a cloud storage service as a target for CloudFS depends on these conditions:

- free storage space quota of at least 10 GB
- file expiration period no shorter than 90 days for free users
- availability of a .NET-accessible API under a non-invasive open source license (Apache, MIT, MS-PL)

Currently the following cloud storage services are supported in CloudFS via the specified API libraries:

| Cloud storage service                                            | API library                                                             | version    | sync/async | origin    | status |
| :--------------------------------------------------------------- | :---------------------------------------------------------------------- | :--------: | :--------: | :-------: | :----: |
| *(local files)*                                                  | *System.IO (.NET Framework)*                                            | *N/A*      | *sync*     |           | stable |
| [Google Drive](https://drive.google.com/ "Google Drive")         | [Google Apis V3](https://github.com/google/google-api-dotnet-client)    | 1.13.1.525 | async      | official  | stable |
| [Box](https://app.box.com/ "Box")                                | [Box.V2](https://github.com/box/box-windows-sdk-v2)                     | 2.8.0      | async      | official  | stable |
| [hubiC](https://hubic.com/ "hubiC")                              | [SwiftClient](https://github.com/vtfuture/SwiftClient)                  | 1.2.2      | async      | 3<sup>rd</sup> party | stable |
| [MediaFire](https://www.mediafire.com "MediaFire")               | [MediaFire SDK](https://github.com/MediaFire/mediafire-csharp-open-sdk) | 1.0.0.2    | async      | 3<sup>rd</sup> party | experimental |
| [MEGA](https://mega.co.nz/ "MEGA")                               | [MegaApiClient](https://github.com/gpailler/MegaApiClient)              | 1.2.2      | async      | 3<sup>rd</sup> party | stable |
| [pCloud](https://www.pcloud.com/ "pCloud")                       | [pCloud.NET](https://github.com/nirinchev/pCloud.NET)                   | N/A        | async      | 3<sup>rd</sup> party | stable |
| [Yandex Disk](https://disk.yandex.com/client/disk "Yandex Disk") | [Yandex Disk API Client](https://github.com/raidenyn/yandexdisk.client) | 1.0.7      | async      | 3<sup>rd</sup> party | stable |
| **Degraded services**                                            |
| [Microsoft OneDrive](https://onedrive.live.com/ "OneDrive")      | [OneDrive SDK for CSharp](https://github.com/OneDrive/onedrive-sdk-csharp) | 1.1.47     | async      | official  | stable |
| **Superseded services**                                            |
| [Microsoft OneDrive](https://onedrive.live.com/ "OneDrive-Legacy")<sup id="a1">[1](#f1)</sup> | [OneDriveSDK](https://github.com/OneDrive/onedrive-explorer-win)<sup id="a2">[2](#f2)</sup> | N/A        | async      | inofficial  | obsolete |
| [Google Drive](https://drive.google.com/ "Google Drive V2")      | [Google Apis V2](https://github.com/google/google-api-dotnet-client)    | 1.13.1.525 | async      | official  | stable |
| **Obsolete services**                                            |
| *[Copy](https://www.copy.com/ "Copy")*<sup id="a3">[3](#f3)</sup> | *[CopyRestAPI](https://github.com/saguiitay/CopyRestAPI)*              | *1.1.0*    | *async*    | *3<sup>rd</sup> party* | *retired* |

> <sup><b id="f1">1</b></sup> Following Microsoft's November 2<sup>nd</sup>, 2015 announcement of its "[OneDrive storage plans change in pursuit of productivity and collaboration](https://blog.onedrive.com/onedrive_changes/)" the OneDrive cloud storage service will fail to meet the requirements for support in CloudFS as stated above after mid-July 2016.<br/> Despite this unprecedented and highly objectionable degradation of service quality, OneDrive will continue to be supported by CloudFS for historical reasons. [^](#a1)<br/>
> <sup><b id="f2">2</b></sup> This version of OneDriveSDK has been deprecated by Microsoft. [^](#a2)<br/>
> <sup><b id="f3">3</b></sup> The Copy cloud storage service was discontinued as of May 1<sup>st</sup> 2016 according to this [announcement](https://www.copy.com/page/home;cs_login:login;;section:plans).<br/>The Copy gateway has therefore been retired from CloudFS. [^](#a3)<br/>


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
| Microsoft OneDrive    | [Microsoft Account - Developer Center](https://account.live.com/developers/applications/index) |
| Google Drive          | [Google Developers Console](https://console.developers.google.com) |
| Box                   | [Box Developers Services](https://app.box.com/developers/services/edit/) |
| hubiC                 | [Develop hubiC applications](https://hubic.com/home/browser/developers/) |
| MediaFire             | [MediaFire - Developers](https://www.mediafire.com/index.php#settings/applications) |
| MEGA                  | [Mega Core SDK - Developers](https://mega.nz/#sdk)     |
| pCloud                | *- no configuration required -*                        |
| Yandex Disk           | [Yandex OAuth Access](https://oauth.yandex.com/)       |
| **Obsolete**          |                                                        |
| <del>Copy</del>       | <del>[Copy Developers - Applications]()</del>          |

## Release Notes

| Date       | Version     | Comments                                                                       |
| :--------- | :---------- | :----------------------------------------------------------------------------- |
| 2016-05-20 | 1.0.6-alpha | - Fixed broken package references in NuGet specs (present since 1.0.3-alpha)<br/>- Version update to API library for Box |
| 2016-05-18 | 1.0.5-alpha | - Retired gateway for Cloud<br/>- Version update to API library for Google Drive<br/>- Support for Windows Explorer new file creation sequence in MEGA<br/>- Improved online editing capability in non-encrypting File gateway
| 2016-04-17 | 1.0.4-alpha | - New gateway for hubiC/Swift added.<br/>- Version updates to API libraries for Google Drive, MEGA, and Yandex Disk.<br/>- Converted Mega gateway to Async operation mode.<br/>- Gateways now explicitely declare their capabilities in the ExportMetadata.<br/>- Improvements to login window handling if logins are requested for multiple drives.<br/>- Various bug fixes. |
| 2016-02-01 | 1.0.3-alpha | - New gateways for MediaFire and Yandex Disk added.                            |
| 2016-01-24 | 1.0.2-alpha | - Gateway configuration extended to accept custom parameters. This change **breaks compatibility** with earlier API versions.<br/>- File Gateway now configurable with target root directory |
| 2016-01-19 | 1.0.1-alpha | - NuGet dependencies updated, schema of App.config in tests project refactored |
| 2016-01-08 | 1.0.0-alpha | - Initial release and NuGet registration                                       |
| 2015-12-29 | 1.0.0.0     | - Initial commit                                                               |

## Future plans

- include additional gateways for more Cloud storage services
- update OneDrive and Google Drive gateways to new API versions
- improve usability of cloud service authentication dialogs