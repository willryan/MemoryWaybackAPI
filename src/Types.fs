module MemoryWayback.Types

open System
open MemoryWayback.DbTypes

type Query = {
  From : DateTime
  To : DateTime
  Types : string list
}

type Result = {
  Date: DateTime
  Photos: string list
  Videos: string list
}

type Results = {
  Query : Query
  Results: Result list
}

type Media = {
  Id : int
  Type : MediaType
  Taken : DateTime
  Url : string
}

