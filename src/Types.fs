module MemoryWayback.Types

open System
open MemoryWayback.DbTypes

type Query = {
  From : DateTime
  To : DateTime
  Types : MediaType list
}

type Result =
  {
    Date: DateTime
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
