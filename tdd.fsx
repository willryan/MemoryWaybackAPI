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
#r "nunit.framework.dll"
#r "tdd_support/nunit.core.dll"
#r "tdd_support/nunit.core.interfaces.dll"
#r "FsUnit.NUnit.dll"
//#r "FsUnit.CustomMatchers.dll"

open MemoryWayback
open NUnit.Framework
open NUnit.Core
open FsUnit
open System.Reflection

open MediaLibraryUpdate.Internal

//[<TestFixture>]
type ``tdd tests``() =

  //[<Test>]
  member x.``test a thing``() =
    2 + 2 |> should equal 5

let runTests() =
  ``tdd tests``().``test a thing``()

runTests()
