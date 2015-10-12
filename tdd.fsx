#I "tests/MemoryWayback.Tests/bin/Debug"

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
#r "xUnit.dll"
#r "FsUnit.Xunit.dll"
#r "FsUnit.CustomMatchers.dll"
#r "NHamcrest.dll"

open MemoryWayback
open Xunit
open FsUnit.Xunit
open System.Reflection

open MediaLibraryUpdate.Internal

//[<TestFixture>]
type ``tdd tests``() =

  [<Fact>]
  member x.``test a thing``() =
    3 + 2 |> should equal 5

let runTests() =
  ``tdd tests``().``test a thing``()

runTests()
printfn "PASS!!!!!!!"
