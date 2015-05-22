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
open Suave.Log
open System.IO
open System.Text
open Nessos.FsPickler
open Nessos.FsPickler.Json
open MemoryWayback.Types
open MemoryWayback.MediaQuery

//let logger = Loggers.sane_defaults_for Debug

let pickler = FsPickler.CreateJson(indent = true, omitHeader = true)

let okJson o =
  OK (pickler.PickleToString o) 
  >>= Writers.setMimeType "application/json"

let handleQuery ctx =
  printfn "%A" ctx.request.query
  let fromValue = defaultArg (ctx.request.queryParam "from") ""
  let toValue = defaultArg (ctx.request.queryParam "to") ""
  let types = defaultArg (ctx.request.queryParam "types") ""
  printfn "%s - %s : %s" fromValue toValue types
  let o = {
    From = DateTime.Parse fromValue
    To = DateTime.Parse toValue
    Types = Array.toList (types.Split ',')
  }
  (okJson <| findMedia o) ctx

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

