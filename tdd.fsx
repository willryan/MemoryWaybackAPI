fsi.ShowDeclarationValues <- false
fsi.ShowProperties <- false
fsi.ShowIEnumerable <- false

#load "tdd_support/tdd_support.fsx"

open Xunit
open FsUnit.Xunit
open System.Reflection
open System
open System.IO

module MyNewStuff =
  let add x y =
    x + y + 1

//[<TestFixture>]
type ``tdd tests``() =

  [<Fact>]
  member x.``next module``() =
    MyNewStuff.add 2 3 |> should equal 5

  [<Fact>]
  member x.``nother module``() =
    MyNewStuff.add 2 2 |> should equal 4


Tdd_support.runTests (``tdd tests``())
  [
    "next module"
    "nother module"
  ]

