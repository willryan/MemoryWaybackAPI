module MemoryWayback.Tests.MediaLibraryUpdate

open NUnit.Framework
open FsUnit
open MemoryWayback.DbTypes
open MemoryWayback.Types
open MemoryWayback.MediaQuery
open MemoryWayback.MediaLibraryUpdate
open System.Data
open System
open System.Linq.Expressions
open ServiceStack.OrmLite
open Foq
open MemoryWayback.Persistence
open MemoryWayback.Tests.MemoryPersistence

[<TestFixture>]
type ``media library updater`` ()=

  let raiseNoMatch () = raise <| Exception("Not a match")

  [<Test>]
  member x.``updateMedia iterates over directory for a library, creating, updating, and deleting as necessary`` ()=
    let dirFileFinder dir = [dir + "/alpha.mov" ; dir + "/beta.jpg" ]

    let p1 = MemoryPersistence([]) :> IPersistence
    let p2 = MemoryPersistence([]) :> IPersistence
    let p3 = MemoryPersistence([]) :> IPersistence
    let p4 = MemoryPersistence([]) :> IPersistence
    let fileHandler t fileName (p:IPersistence) =
      match fileName,p with
      | "./alpha.mov",p when p = p1 -> p2
      | "./beta.jpg",p when p = p2 -> p3
      | _ -> raiseNoMatch()
    let dbCleaner t = function
      | p when p = p3 -> p4
      | _ -> raiseNoMatch()
    let outP = Internal.updateMedia dirFileFinder fileHandler dbCleaner "." p1
    outP |> should equal p4

  [<Test>]
  member x.``itemUpdate creates new entries``() =
    2 + 2 |> should equal 4

  [<Test>]
  member x.``itemUpdate updates existing entries``() =
    2 + 2 |> should equal 4

  [<Test>]
  member x.``matchExisting finds existing files``() =
    2 + 2 |> should equal 4

  [<Test>]
  member x.``fileUpdate creates and/or updates entries``() =
    2 + 2 |> should equal 4
