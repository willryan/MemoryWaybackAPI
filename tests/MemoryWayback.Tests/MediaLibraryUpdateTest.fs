module MemoryWayback.Tests.MediaLibraryUpdate

open NUnit.Framework
open FsUnit
open MemoryWayback.DbTypes
open MemoryWayback.Types
open MemoryWayback.MediaQuery
open MemoryWayback.MediaLibraryUpdate
open System.Data
open System.IO
open System
open System.Linq.Expressions
open ServiceStack.OrmLite
open Foq
open MemoryWayback.Persistence
open MemoryWayback.Tests.MemoryPersistence

let mutable time = DateTime.UtcNow - TimeSpan.FromDays(100.)
let makeMedia typ id url =
  time <- time + TimeSpan.FromDays(1.)
  {
      Id = id
      Url = url
      Taken = time + TimeSpan.FromHours(1.)
      Added = time + TimeSpan.FromHours(2.)
      Type = typ
  }
let makePhoto = makeMedia MediaType.Photo
let makeVideo  = makeMedia MediaType.Video
let matches media p =
  Internal.matchExisting media p |> fst
let firstMatch media p =
  matches media p |> List.head

[<TestFixture>]
type ``media library updater`` ()=

  let raiseNoMatch () = raise <| Exception("Not a match")

  [<Test>]
  member x.``updateMedia iterates over directory for a library, creating, updating, and deleting as necessary`` ()=
    let dirFileFinder dir = [dir + "/alpha.mov" ; dir + "/beta.jpg" ]

    let p1 = MemoryPersistence("p1", [], mediasId) :> IPersistence
    let p2 = MemoryPersistence("p2", [], mediasId) :> IPersistence
    let p3 = MemoryPersistence("p3", [], mediasId) :> IPersistence
    let p4 = MemoryPersistence("p1", [], mediasId) :> IPersistence
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
    let existing1 = makePhoto 1 "/a/b/c.jpg"
    let existing2 = makeVideo 2 "/d/e/f.mov"
    let p = MemoryPersistence("p1", [], mediasId) :> IPersistence
    let _,p2 = p.Insert(existing1)
    let _,p3 = p2.Insert(existing2)
    let match1 = makePhoto 3 "/d/e/f.mov" // type doesn't matter
    let match2 = makeVideo 4 "/a/b/c.jpg" // type doesn't matter
    let match3 = makeVideo 5 "/g/h/i.jpg"
    firstMatch match1 p3 |> should equal existing2
    firstMatch match2 p3 |> should equal existing1
    matches match3 p3 |> should equal List.empty<medias>

  [<Test>]
  member x.``fileUpdate creates and/or updates entries``() =
    let newRec = makeMedia MediaType.Video 1 "/a/b/c.jpg"
    let mkNewF time (fileInfo:System.IO.FileInfo) = newRec

    let p1 = MemoryPersistence("p1", [], mediasId) :> IPersistence
    let p2 = MemoryPersistence("p2", [], mediasId) :> IPersistence
    let p3 = MemoryPersistence("p3", [], mediasId) :> IPersistence
    let matchF nm p =
      match (nm, p) with
      | (nm', p') when nm' = newRec && p' = p1 -> ([newRec], p2)
      | _ -> raise <| Exception("no match")
    let updF nm ex p =
      match (nm, ex, p) with
      | (nm', ex', p') when nm' = newRec && ex' = [newRec] && p' = p2 -> (newRec, p3)
      | _ -> raise <| Exception("no match")
    let file = System.IO.FileInfo("a")
    Internal.fileUpdate mkNewF matchF updF time file p1
    |> should equal p3

  [<Test>]
  member x.``createNewMedia uses file info to determine fields``() =
    let time = DateTime.UtcNow

    let time1 = time - TimeSpan.FromDays(3.)
    let fn1 = sprintf "%s%s.jpg" (Path.GetTempPath()) (Guid.NewGuid().ToString())
    let f = File.Create(fn1)
    f.Close()
    let fi1 = new System.IO.FileInfo(fn1)
    fi1.LastWriteTime <- time1

    let time2 = time - TimeSpan.FromDays(2.)
    let fn2 = sprintf "%s%s.avi" (Path.GetTempPath()) (Guid.NewGuid().ToString())
    let f2 = File.Create(fn2)
    f2.Close()
    let fi2 = new System.IO.FileInfo(fn2)
    fi2.LastWriteTime <- time2

    let res = Internal.createNewMedia time fi1
    (res.Taken - time1).TotalSeconds |> should lessThanOrEqualTo 1.
    res.Taken <- time1
    res
    |> should equal
      {
        Id = -1
        Url = fn1
        Taken = time1
        Added = time
        Type = MediaType.Photo
      }

    File.Delete fn1

    let res2 = Internal.createNewMedia time fi2
    (res2.Taken - time2).TotalSeconds |> should lessThanOrEqualTo 1.
    res2.Taken <- time2
    res2.Id |> should equal -1
    res2.Url |> should equal fn2
    res2.Added |> should equal time
    res2.Type |> should equal MediaType.Video
    res2
    |> should equal
      {
        Id = -1
        Url = fn2
        Taken = time2
        Added = time
        Type = MediaType.Video
      }

    File.Delete fn2
