# IISLogManager

[![CodeQL](https://github.com/rbleattler/IISLogManager/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/rbleattler/IISLogManager/actions/workflows/codeql-analysis.yml)
[![pages-build-deployment](https://github.com/rbleattler/IISLogManager/actions/workflows/pages/pages-build-deployment/badge.svg)](https://github.com/rbleattler/IISLogManager/actions/workflows/pages/pages-build-deployment)
[![.NET](https://github.com/rbleattler/IISLogManager/actions/workflows/dotnet.yml/badge.svg)](https://github.com/rbleattler/IISLogManager/actions/workflows/dotnet.yml)

IISLogManager was built to facilitate central logging of IIS log messages from any number of IIS servers and websites.
The core components of the tool were originally built in Windows PowerShell to maintain compatibility across a number of
different operating system versions. The PowerShell versions of the log Core will be available for the time being but
may become deprecated at some point in the future.

> <font color="dark red">**NOTE**</font> : This tool set is under active development. At this time there are likely many bugs.

## Compatibility Statement

Libraries associated with `IISLogManager.Core` are built to target `.NET Standard 2.0.X` to maintain maximum compatibility. IISLogManager.CLI is built targeting the latest LTS version of .NET (currently, 6.x).

## Core

The Core library is the backbone of this project, and may be paired with any ingestion solution. This library
facilitates importing and parsing IIS log files. The default output of the parser is `IISLogObjectCollection`
(which is fundamentally a `List<IISLogObject>`). The end goal is for this to support numerous forms of output. At the time of this update, it currently only supports output to `JSON` locally or as JSON + Compressed Data to a remote endpoint.

When using remote export, the library expects to dump to a rest endpoint expecting a `POST` request. The request code is shown below.

### Remote Processing

```c#
async Task<Guid> SubmitLogs(
    IISLogObjectCollection logs,
    string? siteUrl = null,
    string? siteName = null,
    string? hostName = null
) {
    var jLogs = logs.ToJson(siteUrl, siteName, hostName);
    var compressedLogs = Utils.CompressString(jLogs);
    var requestContent = $"{{\"RawContent\" : \"{compressedLogs}\"}}";
    Guid taskGuid = Guid.NewGuid();
    var httpResponse =
        await SendRequestAsync(Uri?.ToString() ?? throw new InvalidOperationException(), requestContent);
    _responseCodes.Add(taskGuid, httpResponse.StatusCode);
    return taskGuid;
}
```

First, we compress the JSON objects using gzip compression, which can be decompressed using the `Utils.DecompressString` method (or implementing similar logic).

After compressing the JSON, it's stored inside of json in the format `{ "RawContent" : "CompressedLogData" }` and sent asynchronously to the target endpoint. As of this time there is no way to modify the method of compression or sending. This is planned for a later release.

### Compression Code

`IISLogManager.Core.Utils`

```c#
public static string CompressString(string text) {
    byte[] buffer = Encoding.UTF8.GetBytes(text);
    var memoryStream = new MemoryStream();
    using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true)) {
        gZipStream.Write(buffer, 0, buffer.Length);
    }

    memoryStream.Position = 0;

    var compressedData = new byte[memoryStream.Length];
    memoryStream.Read(compressedData, 0, compressedData.Length);

    var gZipBuffer = new byte[compressedData.Length + 4];
    Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
    Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
    return Convert.ToBase64String(gZipBuffer);
}
```

### Decompression Code

`IISLogManager.Core.Utils`

```c#
public static string DecompressString(string compressedText) {
    byte[] gZipBuffer = Convert.FromBase64String(compressedText);
    using (var memoryStream = new MemoryStream()) {
        int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
        memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);
        var buffer = new byte[dataLength];
        memoryStream.Position = 0;
        using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress)) {
            gZipStream.Read(buffer, 0, buffer.Length);
        }

        return Encoding.UTF8.GetString(buffer);
    }
}
```

## CLI

The CLI is being developed using the dotnet 6.0.x sdk. The interactive parts of the CLI are written using an ANSI
library, and thus as of this time, do not work properly on older systems (Server 2012 R2 or older). For more information
on this, check out this MS
DevBlog. [24 Bit Color in Windows Consoles](https://devblogs.microsoft.com/commandline/24-bit-color-in-the-windows-console/)

The CLI implements the Core library to provide an intuitive and useful interface for interacting with logs on the local
server.

### Usage

> **Note**: This section is under construction. As this readme was adapted from a previous iteration of this project, this may be out of date. It will likely be moved to it's own directory with other documentation at a later time. Additionally, there may still be debug/development artifacts in the CLI output. (For example, messages that clearly are meant for debugging purposes)

By default all commands assume `Default Web Site` should be ignored. To enable processing this site, include the `-z` flag.

#### Help

Displays usage information.

```PowerShell
❯ IISLogManager.CLI.exe -h
USAGE:
    IISLogManager [GetSites] [CommandArgument] [OPTIONS]

EXAMPLES:
    IISLogManager
    IISLogManager -h
    IISLogManager -i
    IISLogManager -r Target -s "Default Web Site, Web Site 2" -O Local -o C:\Test
    IISLogManager -r Target -s Default Web Site -F -f 01/01/2021 -t 01/01/2022

ARGUMENTS:
    [GetSites]           List all Websites              Name (Url)
    [CommandArgument]    Argument                       Id | LogRoot

OPTIONS:
    -h, --help                 Prints help information
    -i, --interactive          Interactive Mode.                NOTE :  DISABLES ALL OTHER COMMAND LINE OPTIONS
    -r, --runmode              Run Mode.                        All sites / Target sites

    -s, --sites                (-r Target)                      Site Names      'a.com,b.com'

    -S, --Sites                (-r Target)                      Site Urls       'a.com,b.com'

    -O, --OutputMode           Output Mode.                     Local disk / Remote endpoint

    -A, --AuthMode             (-O Remote)                      Use DefaultCredentials or provide a BearerToken
    -a, --AuthToken            (-O Remote -A BearerToken)       Provide a BearerToken (Excluide "Bearer ")
    -o, --OutputDirectory      (-O Local)
    -u, --Uri                  (-O Remote)
    -F, --Filter               Enable Filtering Logs
    -f, --FromDate             (-F true)                        Format: "MM/dd/yyyy"

    -t, --ToDate               (-F true)                        Format: "MM/dd/yyyy"

    -z, --IgnoreDefaultSite    Ignore Default Web Site          Default : true                           
```

#### GetSites

Neither the command, nor its arguments are not case sensitive.

```PowerShell
❯ IISLogManager.CLI.exe getsites
Site Name       (Site Url)
Other Web Site   (myotherwebsite.mydomain.local)

❯ IISLogManager.CLI.exe getsites -z false
Site Name       (Site Url)
Default Web Site        ()
Other Web Site   (myotherwebsite.mydomain.local)
```

#### GetSites LogRoot

This option outputs the log root directory for each site (excluding site specific directory).

```PowerShell
❯ IISLogManager.CLI.exe getsites logroot -z false
Log Roots
C:\inetpub\logs\LogFiles
C:\inetpub\logs\LogFiles

❯ IISLogManager.CLI.exe getsites logroot
Log Roots
C:\inetpub\logs\LogFiles
```

#### GetSites Id

This option will return the site ID of each site. 

```PowerShell
❯ IISLogManager.CLI.exe GetSites id -z false
Site Ids
1
2

❯ IISLogManager.CLI.exe GetSites id
Site Ids
2

```

#### Default Invocation

Running the executable with no inputs assumes all logs for all sites should be processed. By default the file will be saved to a dated directory beneath `%USERPROFILE%\IISLogManager`. The file name will be the name of the website (spaces replaced with `-`) and a randomly generated 5 digit number. This is to prevent file collision.

```PowerShell
❯ IISLogManager.CLI.exe
Run mode: All
Output Mode : Local
Output Directory : C:\Users\User\IISLogManager\2022-03-09
Target Sites :
1 : Other Web Site (myotherwebsite.mydomain.local)
Beginning Log Processing...
[DEBUG] Processing Other Web Site...
processed 46 logs in 00:00:00.0046749
[DEBUG] Output mode Local...
Output File : C:\Users\User\IISLogManager\2022-03-09\Other-Web-Site-67560
Finished Processing Logs!
```

### Adaptation Credit Statement

A portion of the codebase in this project was adapted
from [Kabindas/IISLogParser (Github)](https://github.com/Kabindas/IISLogParser). While the core code already existed in PowerShell prior to discovering this project, adopting his codebase solved some problems with memory usage I was running into at the time. It was adopted with permission from the project's creator. If you find this project useful, please drop a star on that project as well to give thanks.
