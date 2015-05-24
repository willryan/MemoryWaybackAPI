module MemoryWayback.MeddiaQueryTest

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

let mockSelect<'dbType> (allValues:'dbType list) =
  let callback (q:Expression<Func<'dbType,bool>>) =
    let filtered =
      allValues
      |> List.filter (fun m ->
        let compiled = q.Compile()
        compiled.Invoke(m)
      )
    new Collections.Generic.List<'dbType>(filtered)
  Mock<IDbConnection>()
    .Setup(fun c -> <@ c.Select<'dbType>(It.IsAny<Expression<Func<'dbType,bool>>>()) @>)
    .Calls<Expression<Func<'dbType,bool>>>(callback).Create()

[<TestFixture>]
type ``media queries`` ()=

  let mutable mockConn = Mock<IDbConnection>()
  let mutable oldConn : IDbConnection = null
  let makeDbPhoto id taken =
    {
      medias.Id = id
      Type = MediaType.Photo
      Taken = taken
      Url = sprintf "/photo/foo_%d.jpg" id
    }
  let makeDbVideo id taken =
    {
      medias.Id = id
      Type = MediaType.Video
      Taken = taken
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
    DateTime.Now - TimeSpan.FromDays(float num)

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

  [<SetUp>]
  member x.Setup ()=
    oldConn <- Connection

  [<TearDown>]
  member x.TearDown ()=
    Connection <- oldConn

  [<Test>]
  member x.``findMedias hits db and transfers into fsharp type`` ()=
    Connection <- mockSelect dbMedias
    let q = {
      From = daysAgo 100
      To = daysAgo 1
      Types = [ MediaType.Photo ; MediaType.Video ]
    }
    Internal.findMedias q
    |> should equal medias

  [<Test>]
  member x.``findMedias filters too old`` ()=
    Connection <- mockSelect dbMedias
    let q = {
      From = daysAgo 8
      To = daysAgo 1
      Types = [ MediaType.Photo ; MediaType.Video ]
    }
    Internal.findMedias q
    |> should equal [| medias.[0] ; medias.[2] |]

  [<Test>]
  member x.``findMedias filters too new`` ()=
    Connection <- mockSelect dbMedias
    let q = {
      From = daysAgo 18
      To = daysAgo 4
      Types = [ MediaType.Photo ; MediaType.Video ]
    }
    Internal.findMedias q
    |> should equal [| medias.[1] ; medias.[2] ; medias.[3] |]

  [<Test>]
  member x.``findMedias filters type`` ()=
    Connection <- mockSelect dbMedias
    let q = {
      From = daysAgo 18
      To = daysAgo 0
      Types = [ MediaType.Video ]
    }
    Internal.findMedias q
    |> should equal [| medias.[2] ; medias.[3] |]

  [<Test>]
  member x.``getResults transforms medias into results`` ()=
    2 + 2
    |> should equal 5

