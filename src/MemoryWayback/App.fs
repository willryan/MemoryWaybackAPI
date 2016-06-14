module MemoryWayback.App

open System
open System.Diagnostics
open Suave
open Suave.Web
open Suave.Json
open Suave.Http
open Suave.Http
open Suave.Files
open Suave.Successful
open Suave.RequestErrors
open Suave.Operators
open Suave.EventSource
open Suave.Filters
open Suave.Writers
open Suave.Utils
open System.IO
open System.Text
open ExtCore.Control
open Nessos.FsPickler
open Nessos.FsPickler.Json
open MemoryWayback.DbTypes
open MemoryWayback.Types
open MemoryWayback.MediaQuery
open MemoryWayback.Persistence
open MemoryWayback.OrmlitePersistence

//let logger = Loggers.sane_defaults_for Debug

let pickler = FsPickler.CreateJsonSerializer(indent = true, omitHeader = true)

let mutable Persistence : IPersistence = OrmlitePersistence() :> IPersistence

let persist stateFunc =
  let (res,p) = stateFunc Persistence
  Persistence <- p
  res

let greetings q =
  defaultArg (Option.ofChoice(q ^^ "name")) "World" |> sprintf "Hello %s"

let okJson o =
  OK (pickler.PickleToString o)
    >=> setMimeType "application/json"

let parseDateParam ctx parm deflt =
  let date = choice {
    let! stringDate = ctx.request.queryParam parm
    return DateTime.Parse stringDate
  }
  Choice.orDefault deflt date

let parseTypes ctx parm deflt =
  Choice.orDefault deflt (choice {
    let! types = ctx.request.queryParam parm
    return (types.Split ',')
      |> Array.map (fun v -> Enum.Parse(typeof<MediaType>, v) :?> MediaType)
      |> Array.toList
    })

let handleQuery ctx =
  printfn "%A" ctx.request.query
  let fromDt = parseDateParam ctx "from" (DateTime(0L))
  let toDt = parseDateParam ctx "to" DateTime.UtcNow
  let typeEnums = parseTypes ctx "types" [ MediaType.Photo ; MediaType.Video ]
  //printfn "%A - %A : %A" fromDt toDt typeEnums
  let o = {
    Query.From = fromDt
    To = toDt
    Types = typeEnums
  }
  printfn "%A" o
  (okJson <| persist (getResults o)) ctx


let app =
  choose
    [ GET >=> choose
        [ path "/media-query" >=> handleQuery ]
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
