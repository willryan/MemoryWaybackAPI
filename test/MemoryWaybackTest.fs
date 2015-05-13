namespace MemoryWayback.Tests

open NUnit.Framework
open FsUnit
open MemoryWayback


[<TestFixture>]
type ``stuff`` ()=

  [<Test>]
  member x.``works`` ()=
    2 + 2
    |> should equal 5

