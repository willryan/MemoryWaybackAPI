module MemoryWayback.DbTypes

open System
open ServiceStack.OrmLite
open System.Linq
open System.Linq.Expressions
open Microsoft.FSharp.Linq.RuntimeHelpers
open Microsoft.FSharp.Quotations
open MemoryWayback.Persistence

type MediaType =
  | Photo = 0
  | Video = 1

type medias = {
  mutable Id : int
  mutable Url : string
  mutable Taken : DateTime
  mutable Added : DateTime
  mutable Type : MediaType
}
