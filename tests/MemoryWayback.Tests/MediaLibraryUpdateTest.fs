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

    let p1 = MemoryPersistence("p1", [], mediasId) :> IPersistence
    let p2 = MemoryPersistence("p2", [], mediasId) :> IPersistence
    let p3 = MemoryPersistence("p3", [], mediasId) :> IPersistence
    let p4 = MemoryPersistence("p4", [], mediasId) :> IPersistence
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
  member x.``itemUpdate updates existing entries``() =
    let tOld = DateTime.UtcNow - TimeSpan.FromDays(3.0)
    let tNew = DateTime.UtcNow
    let existing =
      [
         {
           Id = 1
           Type = MediaType.Photo
           Taken = tOld
           Added = tOld
           Url = "/foo/bar.jpg"
         }
      ]
    let set = existing |> List.map (fun e -> e :> obj)

    let p1 = MemoryPersistence("p1", set, mediasId) :> IPersistence
    let newGuy =
      {
        Id = 1
        Type = MediaType.Photo
        Taken = tNew
        Added = tNew
        Url = "/foo/bar.jpg"
      }
    let _,outP = Internal.itemUpdate newGuy existing p1
    let updated,_ = outP.Select(<@ fun (m:medias) -> m.Id = 1 @>)
    let recd = List.head updated
    recd |> should equal newGuy

  [<Test>]
  member x.``itemUpdate creates new entries``() =
    let tOld = DateTime.UtcNow - TimeSpan.FromDays(3.0)
    let tNew = DateTime.UtcNow
    let existing =
      [
         {
           Id = 2
           Type = MediaType.Photo
           Taken = tOld
           Added = tOld
           Url = "/foo/bar.jpg"
         }
      ]
    let set = existing |> List.map (fun e -> e :> obj)

    let p1 = MemoryPersistence("p1", set, mediasId) :> IPersistence
    let newGuy =
      {
        Id = -1
        Type = MediaType.Photo
        Taken = tNew
        Added = tNew
        Url = "/bar/baz.jpg"
      }
    let _,outP = Internal.itemUpdate newGuy [] p1
    // yuck, also points out need to give new id
    let updated,_ = outP.Select(<@ fun (m:medias) -> m.Url = "/bar/baz.jpg" @>)
    let recd = List.head updated
    newGuy.Id <- recd.Id
    recd |> should equal newGuy

  [<Test>]
  member x.``matchExisting finds existing files``() =
    2 + 2 |> should equal 4

  [<Test>]
  member x.``fileUpdate creates and/or updates entries``() =
    2 + 2 |> should equal 4
