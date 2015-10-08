module MemoryWayback.Tests.MediaQueryTest

open NUnit.Framework
open FsUnit
open MemoryWayback.DbTypes
open MemoryWayback.Types
open MemoryWayback.MediaQuery
open System.Data
open System
open System.Linq.Expressions
open ServiceStack.OrmLite
open Foq
open MemoryWayback.Persistence
open MemoryWayback.Tests.MemoryPersistence


[<TestFixture>]
type ``media queries`` ()=
  let now = DateTime.Now
  let makeDbPhoto id taken =
    {
      medias.Id = id
      Type = MediaType.Photo
      Taken = taken
      Added = taken
      Url = sprintf "/photo/foo_%d.jpg" id
    }
  let makeDbVideo id taken =
    {
      medias.Id = id
      Type = MediaType.Video
      Taken = taken
      Added = taken
      Url = sprintf "/video/bar_%d.jpg" id
    }
  let makePhoto id taken =
    {
      Media.Id = id
      Type = MediaType.Photo
      Taken = taken
      Url = sprintf "/photo/foo_%d.jpg" id
    }
  let makeVideo id taken =
    {
      Media.Id = id
      Type = MediaType.Video
      Taken = taken
      Url = sprintf "/video/bar_%d.jpg" id
    }
  let daysAgo (num:int) =
    now - TimeSpan.FromDays(float num)

  let dbMedias = [
      makeDbPhoto 1 (daysAgo 3)
      makeDbPhoto 2 (daysAgo 10)
      makeDbVideo 3 (daysAgo 5)
      makeDbVideo 4 (daysAgo 12)
    ]
  let medias = [|
      makePhoto 1 (daysAgo 3)
      makePhoto 2 (daysAgo 10)
      makeVideo 3 (daysAgo 5)
      makeVideo 4 (daysAgo 12)
    |]

  let persistence =
    MemoryPersistence(List.map (fun m -> m :> obj) dbMedias, mediasId)


  [<Test>]
  member x.``findMedias hits db and transfers into fsharp type`` ()=
    let q = {
      From = daysAgo 100
      To = daysAgo 1
      Types = [ MediaType.Photo ; MediaType.Video ]
    }
    fst (Internal.findMedias q persistence)
    |> should equal medias

  [<Test>]
  member x.``findMedias filters too old`` ()=
    let q = {
      From = daysAgo 8
      To = daysAgo 1
      Types = [ MediaType.Photo ; MediaType.Video ]
    }
    fst (Internal.findMedias q persistence)
    |> should equal [ medias.[0] ; medias.[2] ]

  [<Test>]
  member x.``findMedias filters too new`` ()=
    let q = {
      From = daysAgo 18
      To = daysAgo 4
      Types = [ MediaType.Photo ; MediaType.Video ]
    }
    fst (Internal.findMedias q persistence)
    |> should equal [ medias.[1] ; medias.[2] ; medias.[3] ]

  [<Test>]
  member x.``findMedias filters type`` ()=
    let q = {
      From = daysAgo 18
      To = daysAgo 0
      Types = [ MediaType.Video ]
    }
    fst (Internal.findMedias q persistence)
    |> should equal [ medias.[2] ; medias.[3] ]

  [<Test>]
  member x.``getResults transforms medias into results`` ()=
    let medias = [|
        makePhoto 1 (daysAgo 3)
        makePhoto 2 (daysAgo 1)
        makeVideo 3 (daysAgo 3)
        makeVideo 4 (daysAgo 2)
        makeVideo 5 (daysAgo 2)
      |]
    let query = {
      From = daysAgo 18
      To = daysAgo 0
      Types = [ MediaType.Video ]
    }
    let dbFinderFake (q:Query) (p:IPersistence) : (seq<Media> * IPersistence) =
      if (q = query && p = (persistence :> IPersistence)) then
        Array.toSeq medias, p
      else
        Seq.empty, p
    let output = Internal.getResults dbFinderFake query (persistence :> IPersistence)
    let results = Seq.toArray (fst output).Results
    Array.length results |> should equal 3
    results.[0] |> should equal {
      Date = daysAgo 3
      Photos = [medias.[0].Url]
      Videos = [medias.[2].Url]
    }
    results.[1] |> should equal {
      Date = daysAgo 2
      Photos = []
      Videos = [medias.[3].Url ; medias.[4].Url]
    }
    results.[2] |> should equal {
      Date = daysAgo 1
      Photos = [medias.[1].Url]
      Videos = []
    }
