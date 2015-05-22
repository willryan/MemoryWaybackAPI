#r @"./packages/FAKE/tools/FakeLib.dll"

open Fake

Target "build_all" (fun _ ->
  MSBuildDebug "" "Build" [ "MemoryWayback.fsproj" ]
  |> Log "TestBuild-Output: "
)

Target "build_src" (fun _ ->
  MSBuildDebug "" "Build" [ "src/MemoryWayback.App.fsproj" ]
  |> Log "TestBuild-Output: "
)

Target "test" (fun _ ->
  let testDlls = !! ("test/bin/Debug/*Tests.dll")
  testDlls
  |> NUnit (fun p ->
    {p with
       DisableShadowCopy = true;
       OutputFile = "test/TestResults.xml"})
)

"build_all" ==> "test"

let runApp args =
  let appExe = "src/bin/Debug/MemoryWayback.exe"
  let result =
    ExecProcess (fun info ->
      info.FileName <- appExe
      info.Arguments <- args
    ) System.TimeSpan.MaxValue

  if result <> 0 then failwith (sprintf "Couldn't run '%s'" args)


Target "server" (fun _ -> runApp "server")

Target "spec" (fun _ ->
  // can't find file for some reason, or a mono issue
  let canopyExe = "spec/bin/Debug/MemoryWayback.Specs.exe"
  let result =
    ExecProcess (fun info ->
      info.FileName <- canopyExe
      info.WorkingDirectory <- "spec"
    ) (System.TimeSpan.FromMinutes 5.)

  //ProcessHelper.killProcessById webSiteProcess.Id

  if result <> 0 then failwith "Failed result from canopy tests"
)

RunTargetOrDefault "build"
