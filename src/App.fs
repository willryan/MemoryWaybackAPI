module MemoryWayback.App

open System
open System.Diagnostics
open Suave
open Suave.Web
open Suave.Json
open Suave.Http
open Suave.Http.Applicatives
open Suave.Http.Files
open Suave.Http.Successful
open Suave.Http.RequestErrors
open Suave.Types
open Suave.Utils
open Suave.Log
open System.IO
open System.Text
open Nessos.FsPickler
open Nessos.FsPickler.Json
open MemoryWayback.DbTypes
open MemoryWayback.Types
open MemoryWayback.MediaQuery
open MemoryWayback.Persistence
open MemoryWayback.OrmlitePersistence

//let logger = Loggers.sane_defaults_for Debug

let pickler = FsPickler.CreateJson(indent = true, omitHeader = true)

let mutable Persistence : IPersistence = OrmlitePersistence() :> IPersistence

let persist stateFunc =
  let (res,p) = stateFunc Persistence
  Persistence <- p
  res

let okJson o =
  OK (pickler.PickleToString o)
  >>= Writers.setMimeType "application/json"

let handleQuery ctx =
  printfn "%A" ctx.request.query
  let fromValue = Choice.orDefault "" (ctx.request.queryParam "from")
  let toValue = Choice.orDefault "" (ctx.request.queryParam "to")
  let types = Choice.orDefault "" (ctx.request.queryParam "types")
  printfn "%s - %s : %s" fromValue toValue types
  let o = {
    From = DateTime.Parse fromValue
    To = DateTime.Parse toValue
    Types = (types.Split ',')
      |> Array.map (fun v -> Enum.Parse(typeof<MediaType>, v) :?> MediaType)
      |> Array.toList
  }
  (okJson <| persist (getResults o)) ctx

let app =
  choose
    [ GET >>= choose
        [ path "/media-query" >>= handleQuery ]
    ]

let defaultArgs = [| "server" |]

let startApp () =
  startWebServer defaultConfig app
  0

let start (args : string[]) =
  let realArgs = if (args.Length = 0) then defaultArgs else args

  match realArgs.[0] with
  | "server" -> startApp()
  | _ -> printfn "Unrecognized argument %s" args.[0] ; 1

