fsi.ShowDeclarationValues <- false
fsi.ShowProperties <- false
fsi.ShowIEnumerable <- false

#load "tdd_support/tdd_support.fsx"

open Xunit
open FsUnit.Xunit
open System.Reflection
open System
open System.IO

open MemoryWayback.DbTypes
open MemoryWayback.Types
open MemoryWayback.MediaQuery
open MemoryWayback.Persistence
open MemoryWayback.MediaLibraryUpdate
open MemoryWayback.Tests.MemoryPersistence

module MyNewStuff =
  let add x y =
    x + y

//[<TestFixture>]
type ``tdd tests``() =

  [<Fact>]
  member x.``next module``() =
    MyNewStuff.add 2 2 |> should equal 4


Tdd_support.runTests (``tdd tests``())
  [
    "next module"
  ]
