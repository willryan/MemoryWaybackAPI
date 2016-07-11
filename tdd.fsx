fsi.ShowDeclarationValues <- false
fsi.ShowProperties <- false
fsi.ShowIEnumerable <- false

#load "TddSupport/TddSupport.fsx"
#load "TddProjectRefs.fsx"

open FsUnit.Xunit
open MemoryWayback.DbTypes
open MemoryWayback.Types
open MemoryWayback.MediaQuery
open MemoryWayback.Tests.MediaQueryTest.``media queries``

module MyNewStuff =
  let add x y =
    x +  y

//[<TestFixture>]

  //[<Fact>]
let ``next module``() =
  MyNewStuff.add 2 3 |> should equal 5

//[<Fact>]
let ``nother module``() =
  MyNewStuff.add 2 2 |> should equal 4

let ``real test`` ()=
  let q = {
    From = daysAgo 10
    To = daysAgo 1
    Types = [ MediaType.Photo ; MediaType.Video ]
  }
  let results = fst (Internal.findMedias q persistence)
  List.toArray results |> should equal medias

TddSupport.runTestsExpr [
  <@ ``next module`` @>
  <@ ``nother module`` @>
  <@ ``real test`` @>
]
