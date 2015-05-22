module MemoryWayback.DbTypes

open System
open ServiceStack.OrmLite

type MediaType =
  | Photo = 0
  | Video = 1

type medias = {
  mutable Id : int
  mutable Url : string
  mutable Taken : DateTime
  mutable Type : MediaType
}

let dbConnection server db uid = 
  let connString = sprintf "Server=%s;Database=%s;UID=%s" server db uid
  let fact = new OrmLiteConnectionFactory(connString,MySqlDialect.Provider)
  fact.Open()

let mutable Connection = dbConnection "localhost" "memory_wayback" "root"

