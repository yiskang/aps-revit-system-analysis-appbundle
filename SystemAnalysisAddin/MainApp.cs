//
// (C) Copyright 2024 by Autodesk, Inc. 
//
// Permission to use, copy, modify, and distribute this software
// in object code form for any purpose and without fee is hereby
// granted, provided that the above copyright notice appears in
// all copies and that both that copyright notice and the limited
// warranty and restricted rights notice below appear in all
// supporting documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS. 
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK,
// INC. DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL
// BE UNINTERRUPTED OR ERROR FREE.
//
// Use, duplication, or disclosure by the U.S. Government is
// subject to restrictions set forth in FAR 52.227-19 (Commercial
// Computer Software - Restricted Rights) and DFAR 252.227-7013(c)
// (1)(ii)(Rights in Technical Data and Computer Software), as
// applicable.
//

using System;
using System.IO;
using System.Diagnostics;
using System.IO.Compression;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using DesignAutomationFramework;
using Newtonsoft.Json;

namespace SystemAnalysisAddin
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class MainApp : IExternalDBApplication
    {
        public ExternalDBApplicationResult OnStartup(ControlledApplication application)
        {
            DesignAutomationBridge.DesignAutomationReadyEvent += HandleDesignAutomationReadyEvent;
            // Hook up the CustomFailureHandling failure processor.
            Autodesk.Revit.ApplicationServices.Application.RegisterFailuresProcessor(new OpenDocumentFailuresProcessor());

            return ExternalDBApplicationResult.Succeeded;
        }

        public ExternalDBApplicationResult OnShutdown(ControlledApplication application)
        {
            return ExternalDBApplicationResult.Succeeded;
        }

        [Obsolete]
        public void HandleApplicationInitializedEvent(object sender, Autodesk.Revit.DB.Events.ApplicationInitializedEventArgs e)
        {
            var app = sender as Autodesk.Revit.ApplicationServices.Application;
            DesignAutomationData data = new DesignAutomationData(app, "InputFile.rvt");
            this.DoExport(data);
        }

        private void HandleDesignAutomationReadyEvent(object sender, DesignAutomationReadyEventArgs e)
        {
            LogTrace("Design Automation Ready event triggered...");

            e.Succeeded = true;
            e.Succeeded = this.DoExportV2(e.DesignAutomationData);
        }

        private bool DoExport(DesignAutomationData data)
        {
            if (data == null)
                return false;

            Autodesk.Revit.ApplicationServices.Application app = data.RevitApp;
            if (app == null)
            {
                LogTrace("Error occured");
                LogTrace("Invalid Revit App");
                return false;
            }

            string modelPath = data.FilePath;
            if (string.IsNullOrWhiteSpace(modelPath))
            {
                LogTrace("Error occured");
                LogTrace("Invalid File Path");
                return false;
            }

            var doc = data.RevitDoc;
            if (doc == null)
            {
                LogTrace("Error occured");
                LogTrace("Invalid Revit DB Document");
                return false;
            }

            try
            {
                var currentDir = Directory.GetCurrentDirectory();
                var openStudioFolder = Path.Combine(currentDir, "OpenStudio CLI For Revit");
                try
                {
                    ZipFile.ExtractToDirectory(Path.Combine(currentDir, "OpenStudio CLI For Revit.zip"), currentDir);
                }
                catch (Exception ex) { }

                string outputDir = Path.Combine(currentDir, "Output");
                Directory.CreateDirectory(outputDir);
                LogTrace("outputDir " + outputDir);

                EnergyAnalysisDetailModel em = EnergyAnalysisDetailModel.GetMainEnergyAnalysisDetailModel(doc);
                // Create a new report and request systems analysis on the current energy model.
                ViewSystemsAnalysisReport newReport = null;
                SystemsAnalysisOptions theOptions = new SystemsAnalysisOptions();
                //string openStudioPath = Path.GetFullPath(Path.Combine(doc.Application.SystemsAnalysisWorkfilesRootPath, @"..\"));
                string openStudioPath = Path.Combine(currentDir, "OpenStudio CLI For Revit");
                string oswFilePath = Path.Combine(openStudioPath, "workflows", "HVAC Systems Loads and Sizing.osw");
                if (File.Exists(oswFilePath))
                    LogTrace("File Found at oswFilePath " + oswFilePath);
                else
                {
                    LogTrace("File NOT Found at oswFilePath " + oswFilePath);
                    return false;
                }
                string reportFolderPath = Path.Combine(outputDir, "Report");
                Directory.CreateDirectory(reportFolderPath);
                // The following are the default values for systems analysis if not specified.
                // If the weather file is not specified, the analysis will use the weather at the current site location.
                theOptions.WorkflowFile = oswFilePath;
                theOptions.OutputFolder = outputDir;
                using (Transaction transaction = new Transaction(doc))
                {
                    transaction.Commit();

                    transaction.Start("Create Systems Analysis View");
                    EnergyDataSettings energyData = EnergyDataSettings.GetFromDocument(doc);
                    energyData.SetReportsFolder(reportFolderPath);
                    newReport = ViewSystemsAnalysisReport.Create(doc, "APITestView");
                    // Create a new report of systems analysis.
                    if (newReport != null)
                    {
                        newReport.RequestSystemsAnalysis(theOptions);
                        // Request the systems analysis in the background process. When the systems analysis is completed,
                        // the result is automatically updated in the report view and the analytical space elements.
                        // You may check the status by calling newReport.IsAnalysisCompleted().
                        transaction.Commit();
                    }
                    else
                    {
                        transaction.RollBack();
                    }
                }
            }
            catch (Exception ex)
            {
                LogTrace("- Printing received exception...");
                this.PrintError(ex);
                LogTrace("-- DONE... ");
                return false;
            }

            return true;
        }

        private bool DoExportV2(DesignAutomationData data)
        {
            if (data == null)
                return false;

            Autodesk.Revit.ApplicationServices.Application app = data.RevitApp;
            if (app == null)
            {
                LogTrace("Error occured");
                LogTrace("Invalid Revit App");
                return false;
            }

            string modelPath = data.FilePath;
            if (string.IsNullOrWhiteSpace(modelPath))
            {
                LogTrace("Error occured");
                LogTrace("Invalid File Path");
                return false;
            }

            var doc = data.RevitDoc;
            if (doc == null)
            {
                LogTrace("Error occured");
                LogTrace("Invalid Revit DB Document");
                return false;
            }

            try
            {
                var currentDir = Directory.GetCurrentDirectory();
                string openStudioPath = Path.Combine(currentDir, "OpenStudio CLI For Revit");
                string weatherCachePath = Path.Combine(currentDir, "RevitWeatherFilesCache");

                if (!Directory.Exists(openStudioPath))
                {
                    LogTrace("- Extracting OpenStudio CLI for Revit...");
                    try
                    {
                        ZipFile.ExtractToDirectory(Path.Combine(currentDir, "OpenStudio CLI For Revit.zip"), openStudioPath);
                    }
                    catch (Exception ex) { }
                    LogTrace("-- DONE... ");
                }

                if (Directory.Exists(openStudioPath))
                {
                    LogTrace("-- OpenStudio CLI for Revit found at `" + openStudioPath + "`");
                }
                else
                {
                    LogTrace("-- Failed to OpenStudio CLI for Revit... ");
                    return false;
                }

                if (!Directory.Exists(weatherCachePath))
                {
                    LogTrace("- Extracting Weather data...");
                    try
                    {
                        ZipFile.ExtractToDirectory(Path.Combine(currentDir, "RevitWeatherFilesCache.zip"), weatherCachePath);
                    }
                    catch (Exception ex) { }
                    LogTrace("-- DONE... ");
                }

                if (Directory.Exists(weatherCachePath))
                {
                    LogTrace("-- Weather data found at `" + weatherCachePath + "`");
                }
                else
                {
                    LogTrace("-- Failed to extract Weather data... ");
                    return false;
                }

                string outputDir = Path.Combine(currentDir, "Output");
                Directory.CreateDirectory(outputDir);
                LogTrace("outputDir " + outputDir);
                
                string owsFolderPath = Path.Combine(openStudioPath, "workflows");
                string oswFilePath = Path.Combine(owsFolderPath, "HVAC Systems Loads and Sizing.osw");
                if (File.Exists(oswFilePath))
                {
                    LogTrace("Workflow file found at `" + oswFilePath + "`");
                }  
                else
                {
                    LogTrace("Workflow file cannot be found at `" + oswFilePath + "`");
                    return false;
                }

                LogTrace("- Exporting RVT to GBXML...");
                EnergyAnalysisDetailModel em = EnergyAnalysisDetailModel.GetMainEnergyAnalysisDetailModel(doc);
                //if (em == null)
                //{
                //    //define the energy model options
                //    var emOpt = new EnergyAnalysisDetailModelOptions();
                //    emOpt.EnergyModelType = EnergyModelType.AnalysisMode;
                //    emOpt.ExportMullions = false;
                //    emOpt.IncludeShadingSurfaces = true;
                //    emOpt.SimplifyCurtainSystems = true;
                //    emOpt.Tier = EnergyAnalysisDetailModelTier.SecondLevelBoundaries;

                //    //create the energy analysis (modifies model, wrapped in tx)
                //    using (Transaction tx = new Transaction(doc))
                //    {
                //        tx.Start("Create Internal E-Model");
                //        EnergyAnalysisDetailModel.Create(doc, emOpt);
                //        tx.Commit();
                //    }
                //}

                //set the gbXML options
                var gbxmlOpt = new GBXMLExportOptions()
                {
                    ExportEnergyModelType = ExportEnergyModelType.AnalysisMode,
                    ExportAnalyticalSystems = true
                };

                //export the gbXML file
                var gbxmlFilenameWithoutExt = "gbxml";
                var isExported = doc.Export(outputDir, gbxmlFilenameWithoutExt, gbxmlOpt);
                if (!isExported)
                {
                    LogTrace("-- Failed to export GBXML... ");
                    return false;
                }
                LogTrace("-- DONE... ");

                var gbxmlFilename = $"{gbxmlFilenameWithoutExt}.xml";
                var outputOswFilePath = Path.Combine(outputDir, "workflow.osw");
                try
                {
                    LogTrace("- Updating `workflow.osw`...");
                    var workflowJson = File.ReadAllText(oswFilePath);
                    var workflowData = JsonConvert.DeserializeObject<OpenStudioWorkflow>(workflowJson);
                    workflowData.RunDirectory = Path.Combine(outputDir, "run");
                    workflowData.MeasurePaths.Clear();
                    workflowData.MeasurePaths.Add(Path.Combine(owsFolderPath, "../measures/"));
                    workflowData.FilePaths.Clear();
                    workflowData.FilePaths.Add(Path.Combine(currentDir, "RevitWeatherFilesCache"));
                    workflowData.FilePaths.Add(Path.Combine(owsFolderPath, "../seeds"));
                    workflowData.FilePaths.Add(outputDir);

                    workflowData.WeatherFile = "TWN_CNR_Taichung.591590_TMYx.epw";
                    var weatherStep = workflowData.Steps.Find(step => step.Name == "Change Building Location");
                    weatherStep.Arguments["weather_file_name"] = "TWN_CNR_Taichung.591590_TMYx.epw";

                    var importGbxmlStep = workflowData.Steps.Find(step => step.Name == "ImportGbxml");
                    importGbxmlStep.Arguments["gbxml_file_name"] = gbxmlFilename;

                    var advancedImportGbxmlStep = workflowData.Steps.Find(step => step.Name == "Advanced Import Gbxml");
                    advancedImportGbxmlStep.Arguments["gbxml_file_name"] = gbxmlFilename;

                    var hvacImportGbxmlStep = workflowData.Steps.Find(step => step.Name == "GBXML HVAC Import");
                    hvacImportGbxmlStep.Arguments["gbxml_file_name"] = gbxmlFilename;
                    LogTrace("-- DONE... ");

                    LogTrace($"- Producing JSON for updated `workflow.osw`...");
                    var result = JsonConvert.SerializeObject(workflowData);
                    LogTrace("-- DONE... ");

                    LogTrace($"- Writting JSON to `workflow.osw`...");
                    using (StreamWriter sw = File.CreateText(outputOswFilePath))
                    {
                        sw.WriteLine(result);
                        sw.Close();
                    }
                    LogTrace("-- DONE... ");
                }
                catch (Exception ex)
                {
                    LogTrace("-- Failed to update `workflow.osw`... ");
                    return false;
                }

                try
                {
                    LogTrace("- Copying `reportConfig.json` to `Output`...");
                    var reportConfigPath = Path.Combine(openStudioPath, "measures", "systems_analysis_report_generator", "resources", "build", "reportConfig.json");
                    if (!File.Exists(reportConfigPath))
                        throw new FileNotFoundException("File not found at `" + reportConfigPath + "`");

                    var newReportConfigPath = Path.Combine(outputDir, "reportConfig.json");
                    File.Copy(reportConfigPath, newReportConfigPath, true);
                    LogTrace("-- DONE");
                }
                catch (Exception ex)
                {
                    LogTrace("-- Error occured");
                    LogTrace("-- Failed to copy `reportConfig.json` to `Output`");
                    LogTrace(ex.Message);

                    if (ex.InnerException != null)
                        LogTrace(ex.InnerException.Message);

                    return false;
                }

                try
                {
                    LogTrace("- Running system analysis and generating reports...");
                    var exePath = Path.Combine(openStudioPath, "bin", "openstudio.exe");
                    using (var exeProcess = new Process())
                    {
                        //exeProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        exeProcess.StartInfo.CreateNoWindow = false;
                        exeProcess.StartInfo.UseShellExecute = false;
                        exeProcess.StartInfo.RedirectStandardOutput = true;
                        exeProcess.StartInfo.RedirectStandardError = true;
                        exeProcess.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
                        exeProcess.ErrorDataReceived += new DataReceivedEventHandler(ErrorOutputHandler);
                        exeProcess.StartInfo.Arguments = $"run -w \"{outputOswFilePath}\"";
                        exeProcess.StartInfo.FileName = exePath;

                        LogTrace("-- Current exe working dir: `{0}`", exeProcess.StartInfo.WorkingDirectory);
                        //exeProcess.StartInfo.WorkingDirectory = Directory.GetcurrentDirectory();
                        //LogTrace("- Change current exe working dir: `{0}`", exeProcess.StartInfo.WorkingDirectory);

                        LogTrace("-- Running `OpenStudio.exe`...");
                        var runResult = exeProcess.Start();
                        if (runResult == false)
                        {
                            LogTrace("-- Error occured");
                            LogTrace("-- Failed to run `OpenStudio.exe`");
                            return false;
                        }

                        exeProcess.BeginOutputReadLine();
                        exeProcess.BeginErrorReadLine();
                        exeProcess.WaitForExit();

                        //var inProcess = true;

                        //while (inProcess)
                        //{
                        //    exeProcess.Refresh();
                        //    System.Threading.Thread.Sleep(100);
                        //    if (exeProcess.HasExited)
                        //    {
                        //        inProcess = false;
                        //    }
                        //}

                        if (exeProcess.ExitCode < 0)
                        {
                            LogTrace("-- Error occured");
                            LogTrace("-- Failed to run `OpenStudio.exe`, which exit code is {0}", exeProcess.ExitCode);
                            return false;
                        }

                        if (!exeProcess.HasExited)
                        {
                            LogTrace("-- Kill porcess of `OpenStudio.exe`");
                            exeProcess.Kill();
                        }

                        LogTrace("-- DONE... ");
                    }
                }
                catch(Exception ex)
                {
                    LogTrace("-- Failed... ");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogTrace("- Printing received exception...");
                this.PrintError(ex);
                LogTrace("-- DONE... ");
                return false;
            }

            return true;
        }

        static void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            //* Do your stuff with the output (write to console/log/StringBuilder)

            if (!string.IsNullOrWhiteSpace(outLine.Data))
            {
#if DEBUG
                System.Diagnostics.Trace.WriteLine(outLine.Data);
#endif
                System.Console.WriteLine(outLine.Data);
            }
        }

        static void ErrorOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            //* Do your stuff with the output (write to console/log/StringBuilder)

            if (!string.IsNullOrWhiteSpace(outLine.Data))
            {
#if DEBUG
                System.Diagnostics.Trace.WriteLine(outLine.Data);
#endif
                System.Console.WriteLine(outLine.Data);
            }
        }

        private void PrintError(Exception ex)
        {
            LogTrace("Error occured");
            LogTrace(ex.Message);

            if (ex.InnerException != null)
                LogTrace(ex.InnerException.Message);
        }

        /// <summary>
        /// This will appear on the Design Automation output
        /// </summary>
        internal static void LogTrace(string format, params object[] args)
        {
#if DEBUG
            System.Diagnostics.Trace.WriteLine(string.Format(format, args));
#endif
            System.Console.WriteLine(format, args);
        }

    }
}
