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
  member x.``createNewMedia uses file info to determine fields``() =
    2 + 3 |> should equal 4


Tdd_support.runTests (``tdd tests``())
  [
    "createNewMedia uses file info to determine fields"
  ]
