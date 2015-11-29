﻿/// Contains a task which can be used to run [ReportGenerator](https://reportgenerator.codeplex.com),
/// which converts XML reports generated by PartCover, OpenCover or NCover into a readable report in various formats.
module Fake.ReportGeneratorHelper

open System
open System.Text

type ReportGeneratorReportType =
    | Html = 0
    | HtmlSummary = 1
    | Xml = 2
    | XmlSummary = 3
    | Latex = 4
    | LatexSummary = 5
    | Badges = 6

type ReportGeneratorLogVerbosity =
    | Verbose = 0
    | Info = 1
    | Error = 2

/// ReportGenerator parameters, for more details see: https://github.com/danielpalme/ReportGenerator.
type ReportGeneratorParams =
    { /// (Required) Path to the ReportGenerator exe file.
      ExePath : string
      /// (Required) The directory where the generated report should be saved.
      TargetDir : string
      /// The output formats and scope.
      ReportTypes : ReportGeneratorReportType list
      /// Optional directories which contain the corresponding source code.
      SourceDirs : string list
      /// Optional list of assemblies that should be included or excluded
      /// in the report. Exclusion filters take precedence over inclusion
      /// filters. Wildcards are allowed.
      Filters : string list
      /// The verbosity level of the log messages.
      LogVerbosity : ReportGeneratorLogVerbosity
      /// The directory where the ReportGenerator process will be started.
      WorkingDir : string
      /// The timeout for the ReportGenerator process.
      TimeOut : TimeSpan }

/// ReportGenerator default parameters
let ReportGeneratorDefaultParams =
    { ExePath = "./tools/ReportGenerator/bin/ReportGenerator.exe"
      TargetDir = currentDirectory
      ReportTypes = [ ReportGeneratorReportType.Html ]
      SourceDirs = []
      Filters = []
      LogVerbosity = ReportGeneratorLogVerbosity.Verbose
      WorkingDir = currentDirectory
      TimeOut = TimeSpan.FromMinutes 5. }

/// Builds the report generator command line arguments from the given parameters and reports
/// [omit]
let buildReportGeneratorArgs parameters (reports : string seq) =
    let reportTypes = parameters.ReportTypes |> List.map (fun rt -> rt.ToString())
    let sourceDirs = sprintf "-sourcedirs:%s" (String.Join(";", parameters.SourceDirs))
    let filters = sprintf "-filters:%s" (String.Join(";", parameters.Filters))

    new StringBuilder()
    |> append (sprintf "-reports:%s" (String.Join(";", reports)))
    |> append (sprintf "-targetdir:%s" parameters.TargetDir)
    |> appendWithoutQuotes (sprintf "-reporttypes:%s" (String.Join(";", reportTypes)))
    |> appendIfTrue (parameters.SourceDirs.Length > 0) sourceDirs
    |> appendIfTrue (parameters.Filters.Length > 0) filters
    |> appendWithoutQuotes (sprintf "-verbosity:%s" (parameters.LogVerbosity.ToString()))
    |> toText

/// Runs ReportGenerator on one or more coverage reports.
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the default ReportGenerator parameters.
///  - `reports` - Coverage reports.
///
/// ## Sample
///
///      ReportGenerator (fun p -> { p with TargetDir = "c:/reports/" }) [ "c:/opencover.xml" ]
let ReportGenerator setParams (reports : string list) =
    let taskName = "ReportGenerator"
    let description = "Generating reports"
    traceStartTask taskName description
    let param = setParams ReportGeneratorDefaultParams

    let processArgs = buildReportGeneratorArgs param reports
    tracefn "ReportGenerator command\n%s %s" param.ExePath processArgs
    let ok =
        execProcess (fun info ->
            info.FileName <- param.ExePath
            if param.WorkingDir <> String.Empty then info.WorkingDirectory <- param.WorkingDir
            info.Arguments <- processArgs) param.TimeOut
    if not ok then failwithf "ReportGenerator reported errors."
    traceEndTask taskName description