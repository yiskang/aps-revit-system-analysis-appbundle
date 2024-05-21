# Revit System Analysis App bundle for Autodesk APS Design Automation

[![Design Automation](https://img.shields.io/badge/Design%20Automation-v3-green.svg)](http://developer.autodesk.com/)

![Revit](https://img.shields.io/badge/Plugins-Revit-lightgrey.svg)
![.NET](https://img.shields.io/badge/.NET%20Framework-4.8-blue.svg)
[![Revit](https://img.shields.io/badge/Revit-2024-lightgrey.svg)](https://www.autodesk.com/products/revit/overview/)

![Advanced](https://img.shields.io/badge/Level-Advanced-red.svg)
[![MIT](https://img.shields.io/badge/License-MIT-blue.svg)](http://opensource.org/licenses/MIT)

# Description

This sample demonstrates the below on Design Automation:

- How to export RVT to GBXML with energy analysis settings.
- How to do system analysis using [OpenStudio CLI for Revit](https://github.com/NREL/gbxml-to-openstudio).

### Notice

This is a workaround for `RVTDA-2035` and `REVIT-222142`, which Revit API [ViewSystemsAnalysisReport](https://www.revitapidocs.com/2024/a7b5b7de-dfdb-e57f-7fac-1ff1dbf35e4d.htm) cannot be run on Design Automation env.

# Development Setup

## Prerequisites

1. **APS Account**: Learn how to create a APS Account, activate subscription and create an app at [this tutorial](https://aps.autodesk.com/tutorials).
2. **Visual Studio 2022 and later** (Windows).
3. **Revit 2024 and later**: required to compile changes into the plugin.

## Design Automation Setup

### AppBundle example

```json
{
    "id": "RevitSystemAnalysis",
    "engine": "Autodesk.Revit+2024",
    "description": "Revit System Analysis"
}
```

### Activity example

```json
{
    "id": "RevitSystemAnalysisActivity",
    "commandLine": [
        "$(engine.path)\\\\revitcoreconsole.exe /i \"$(args[rvtFile].path)\" /al \"$(appbundles[RevitSystemAnalysis].path)\""
    ],
    "parameters": {
        "openStudioSDK": {
            "verb": "get",
            "localName": "OpenStudio CLI For Revit.zip",
            "description": "The ZIP package of OpenStudio CLI For Revit",
            "required": true
        },
        "weatherFilesCache": {
            "verb": "get",
            "localName": "RevitWeatherFilesCache.zip",
            "description": "The ZIP package of OpenStudio weather data files",
            "required": true
        },
        "rvtFile": {
            "verb": "get",
            "description": "Input Revit File",
            "required": true,
            "localName": "input.rvt"
        },
        "result": {
            "zip": true,
            "verb": "put",
            "description": "The result of Revit system analysis",
            "localName": "Output"
        }
    },
    "engine": "Youralias.Revit+2024",
    "appbundles": [
        "Youralias.RevitSystemAnalysis+dev"
    ],
    "description": "Activity for RevitSystemAnalysis"
}
```

### Workitem example

```json
{
    "activityId": "Youralias.RevitSystemAnalysisActivity+dev",
    "arguments": {
        "openStudioSDK": {
            "verb": "get",
            "url": "https://developer.api.autodesk.com/oss/v2/apptestbucket/a0c4ba18-fa11-46ee-8d83-7a4dde3805d1?region=US"
        },
        "weatherFilesCache": {
            "verb": "get",
            "url": "https://developer.api.autodesk.com/oss/v2/apptestbucket/97d9904c-bf90-4188-b5c2-3e7d1320e085?region=US"
        },
        "rvtFile": {
            "verb": "get",
            "url": "https://developer.api.autodesk.com/oss/v2/apptestbucket/e1d5044c-8a38-43ef-9909-6df89f93399b?region=US"
        },
        "result": {
            "verb": "put",
            "url": "https://developer.api.autodesk.com/oss/v2/apptestbucket/44c60687-a17e-472a-92fc-fc5345e3fe49?region=US"
        }
    }
}
```

## License

This sample is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT). Please see the [LICENSE](LICENSE) file for full details.

## Written by

Eason Kang [in/eason-kang-b4398492/](https://www.linkedin.com/in/eason-kang-b4398492), [Developer Advocate](http://aps.autodesk.com)