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

open MemoryWayback.Routing

  //let logger = Loggers.sane_defaults_for Debug

let pickler = FsPickler.CreateJson(indent = true, omitHeader = true)

let okJson o =
  OK (pickler.PickleToString o) >>= Writers.setMimeType "application/json"

let appOld =
  choose
    [ GET >>= choose
        [ path "/transaction" >>= okJson []
          path "/goodbye" >>= OK "Good bye GET" ]
      POST >>= choose
        [ path "/hello" >>= OK "Hello POST"
          path "/goodbye" >>= OK "Good bye POST" ] ]


let handler resName actName ids =
  OK (sprintf "OK: %s#%s %A" resName actName ids)

let resources =
  [
  ]

let defaultArgs = [| "server" |]

let startApp () =
  let app = makeApp()
  startWebServer defaultConfig app
  0

let start (args : string[]) =
  let realArgs = if (args.Length = 0) then defaultArgs else args

  Resources <- resources
  //Debugger.Break()

  match args.[0] with
  | "routes" -> Debug.printRouteDefs() ; 0
  | "server" -> startApp()
  | _ -> printfn "Unrecognized argument %s" args.[0] ; 1

