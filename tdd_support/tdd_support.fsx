#I "../tests/MemoryWayback.Tests/bin/Debug"

#r "ExtCore.dll"
#r "FSharp.Quotations.Evaluator.dll"
#r "FsPickler.dll"
#r "FsPickler.Json.dll"
#r "MySql.Data.dll"
#r "Newtonsoft.Json.dll"
#r "ServiceStack.Common.dll"
#r "ServiceStack.Interfaces.dll"
#r "ServiceStack.OrmLite.dll"
#r "ServiceStack.OrmLite.MySql.dll"
#r "ServiceStack.Text.dll"
#r "Suave.dll"
#r "Zlib.Portable.dll"
#r "MemoryWayback.exe"
#r "MemoryWayback.Tests.dll"
#r "xUnit.dll"
#r "FsUnit.Xunit.dll"
#r "FsUnit.CustomMatchers.dll"
#r "NHamcrest.dll"

let runTests (fixture: obj) (tests: string list) =
  let typ = fixture.GetType()
  let useMethods =
    match tests with
    | [] -> typ.GetMethods()
    | _ ->
      tests
      |> List.map (fun name -> typ.GetMethod(name))
      |> List.toArray
  useMethods
  |> Array.iter (fun methd ->
    printfn "<<<<<TEST>>>>> '%s': " methd.Name
    try
      ignore <| methd.Invoke(fixture, [||])
      printfn "_____PASS_____"
    with
      | _ as ex ->
        printfn "!!!!!FAIL!!!!!"
        let err = sprintf "%A" ex
        err.Split('\n')
        |> Array.iter (printfn "    %s")
  )
