fsi.ShowDeclarationValues <- false
fsi.ShowProperties <- false
fsi.ShowIEnumerable <- false

#load "tdd_support/tdd_support.fsx"

open Xunit
open FsUnit.Xunit
open System.Reflection
open System

open MemoryWayback.DbTypes
open MemoryWayback.Types
open MemoryWayback.MediaQuery
open MemoryWayback.Persistence
open MemoryWayback.MediaLibraryUpdate.Internal
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
  matchExisting media p |> fst
let firstMatch media p =
  matches media p |> List.head

//[<TestFixture>]
type ``tdd tests``() =

  [<Fact>]
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

  [<Fact>]
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
    fileUpdate mkNewF matchF updF time file p1
    |> should equal p3

  [<Fact>]
  member x.``createNewMedia uses file info to determine fields``() =
    2 + 3 |> should equal 4

Tdd_support.runTests (``tdd tests``())
  [
    "matchExisting finds existing files"
    "fileUpdate creates and/or updates entries"
    "createNewMedia uses file info to determine fields"
  ]
