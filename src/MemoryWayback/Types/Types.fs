module MemoryWayback.Types

open System
open MemoryWayback.DbTypes

type Query = {
  From : DateTime
  To : DateTime
  Types : MediaType list
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Query =
  let getFrom q = q.From
  let getTo q = q.To
  let getTypes q = q.Types |> Seq.ofList

let getRange q =
  (Query.getFrom q, Query.getTo q)

type Result =
  {
    Date: string
    Photos: string list
    Videos: string list
  }
  override x.ToString () : string =
    sprintf "%A" x

type Results = {
  Query : Query
  Results: Result list
}

type Media =
  {
    Id : int
    Type : MediaType
    Taken : DateTime
    Url : string
  }
  override x.ToString () : string =
    sprintf "%A" x

type MediaDirectory = {
  Path : string
  Mount : string
}

let makeMediaDirectory (dir:media_directories) = 
  { MediaDirectory.Path = dir.Path ; Mount = dir.Mount }
