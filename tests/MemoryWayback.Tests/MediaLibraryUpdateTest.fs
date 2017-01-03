module MemoryWayback.Tests.MediaLibraryUpdate

open Xunit
open FsUnit.Xunit
open MemoryWayback.DbTypes
open MemoryWayback.Types
open MemoryWayback.MediaQuery
open MemoryWayback.MediaLibraryUpdate
open MemoryWayback.FileHelper
open System.Data
open System.IO
open System
open System.Linq.Expressions
open MemoryWayback.Persistence
open MemoryWayback.MemoryPersistence

let mutable time = DateTime.UtcNow - TimeSpan.FromDays(100.)
let makeMedia typ id url =
  time <- time + TimeSpan.FromDays(1.)
  {
      Id = id
      MediaDirectoryId = 1
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

module ``media library updater`` =

  let raiseNoMatch () = raise <| Exception("Not a match")

  [<Fact>]
  let ``updateMedia iterates over directory for a library, creating, updating, and deleting as necessary`` ()=
    let dirFileFinder (dir:MediaDirectory) =
      [
        new FileInfo(dir.Path + "/alpha.mov")
        new FileInfo(dir.Path + "/beta.jpg")
      ]

    let p1 = MemoryPersistence("p1", [], mediasId) :> IPersistence
    let p2 = MemoryPersistence("p2", [], mediasId) :> IPersistence
    let p3 = MemoryPersistence("p3", [], mediasId) :> IPersistence
    let p4 = MemoryPersistence("p1", [], mediasId) :> IPersistence
    let fileHandler fh t dir (fileName:FileInfo) (p:IPersistence) =
      match fileName.Name,p with
      | "alpha.mov",p when p = p1 -> p2
      | "beta.jpg",p when p = p2 -> p3
      | _ -> raiseNoMatch()
    let dbCleaner t = function
      | p when p = p3 -> p4
      | _ -> raiseNoMatch()
    let fh = {
      takenTime = (fun f -> Some DateTime.Now)
      urlBuilder = (fun d f -> "")
      fileFinder = dirFileFinder
      transformPath = (fun d -> id)
    }
    let outP = Internal.updateMedia (fileHandler,dbCleaner) DateTime.Now fh ({ Id = 1 ; Path = "." ; Mount = "1" }) p1
    outP |> should equal p4

  [<Fact>]
  let ``itemUpdate updates existing entries``() =
    let tOld = DateTime.UtcNow - TimeSpan.FromDays(3.0)
    let tNew = DateTime.UtcNow
    let existing =
      [
         {
           Id = 1
           MediaDirectoryId = 1
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
        MediaDirectoryId = 1
        Type = MediaType.Photo
        Taken = tNew
        Added = tNew
        Url = "/foo/bar.jpg"
      }
    let _,outP = Internal.itemUpdate newGuy existing p1
    let updated,_ = outP.Select(<@ fun (m:medias) -> m.Id = 1 @>)
    let recd = List.head updated
    recd |> should equal newGuy

  [<Fact>]
  let ``itemUpdate creates new entries``() =
    let tOld = DateTime.UtcNow - TimeSpan.FromDays(3.0)
    let tNew = DateTime.UtcNow
    let existing =
      [
         {
           Id = 2
           MediaDirectoryId = 1
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
        MediaDirectoryId = 1
        Type = MediaType.Photo
        Taken = tNew
        Added = tNew
        Url = "/bar/baz.jpg"
      }
    let _,outP = Internal.itemUpdate newGuy [] p1
    // yuck, also points out need to give new id
    let updated,_ = outP.Select(<@ fun (m:medias) -> m.Url = "/bar/baz.jpg" @>)
    let recd = List.head updated
    let newGuy2 = {newGuy with Id = recd.Id}
    recd |> should equal newGuy2

  [<Fact>]
  let ``matchExisting finds existing files``() =
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

  [<Fact>]
  let ``fileUpdate creates and/or updates entries``() =
    let newRec = makeMedia MediaType.Video 1 "/a/b/c.jpg"
    let mkNewF tf time dir (fileInfo:System.IO.FileInfo) = newRec

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
    let takenF f t = DateTime.UtcNow
    let dir = { Id = 1 ; Path = "." ; Mount = "1" }
    Internal.fileUpdate (mkNewF, matchF, updF) takenF time dir file p1
    |> should equal p3

  [<Fact>]
  let ``createNewMedia uses file info to determine fields``() =
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

    let mutable rTime = time1

    let takenF fi = Some rTime
    let fh = {
      takenTime = takenF
      fileFinder = (fun d -> [])
      urlBuilder = (fun s f -> "")
      transformPath = (fun d -> id)
    }

    let res = Internal.createNewMedia fh time ({ Id = 1 ; Path = "" ; Mount = "1"}) fi1
    (res.Taken - time1).TotalSeconds |> should lessThanOrEqualTo 1.
    let resTime = {res with Taken = time1}
    resTime
    |> should equal
      {
        Id = -1
        MediaDirectoryId = 1
        Url = sprintf "/api/media/1%s" fn1
        Taken = time1
        Added = time
        Type = MediaType.Photo
      }

    File.Delete fn1

    rTime <- time2


    let res2 = Internal.createNewMedia fh time ({ Id = 1 ; Path = "" ; Mount = "1" }) fi2
    (res2.Taken - time2).TotalSeconds |> should lessThanOrEqualTo 1.
    let res2Time = {res2 with Taken = time2 }
    res2Time
    |> should equal
      {
        Id = -1
        MediaDirectoryId = 1
        Url = sprintf "/api/media/1%s" fn2
        Taken = time2
        Added = time
        Type = MediaType.Video
      }

    File.Delete fn2
