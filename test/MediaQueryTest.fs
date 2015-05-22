namespace MemoryWayback.MeddiaQueryTest

open NUnit.Framework
open FsUnit
open MemoryWayback.DbTypes
open System.Data
open ServiceStack.OrmLite
open Foq

[<TestFixture>]
type ``media queries`` ()=

  let mutable mockConn = Mock<IDbConnection>()
  let mutable oldConn : IDbConnection = null

  [<SetUp>]
  member x.Setup ()=
    oldConn <- Connection
    mockConn <- Mock<IDbConnection>()
    Connection <- mockConn.Create()

  [<TearDown>]
  member x.TearDown ()=
    Connection <- oldConn

  [<Test>]
  member x.``findDbMedias hits db`` ()=
    //mockConn.Setup(fun m -> <@ m.Select<medias>(
    2 + 2
    |> should equal 5

  [<Test>]
  member x.``dbToCodeType converts medias to Media`` ()=
    2 + 2
    |> should equal 5

  [<Test>]
  member x.``findMedias finds and maps`` ()=
    2 + 2
    |> should equal 5

  [<Test>]
  member x.``mediasToResults converts Medias to Results`` ()=
    2 + 2
    |> should equal 5

  [<Test>]
  member x.``findMedia uses a bunch of functions`` ()=
    2 + 2
    |> should equal 5

