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
open MemoryWayback.DbTypes
open MemoryWayback.Types
open MemoryWayback.MediaQuery
open MemoryWayback.Persistence
open MemoryWayback.OrmlitePersistence
open MemoryWayback.MemoryPersistence
open MemoryWayback.FileHelper
open Newtonsoft.Json

//let logger = Loggers.sane_defaults_for Debug

let getDbPersistence () = OrmlitePersistence() :> IPersistence

let getMemoryPersistence dir = 
  let p = MemoryPersistence("mem",[],mediasId) :> IPersistence
  match dir with
  | Some d -> MediaLibraryUpdate.updateMedia realFileHelper d p
  | None -> p

let mutable Persistence = getMemoryPersistence None

let persist stateFunc =
  let (res,p) = stateFunc Persistence
  Persistence <- p
  res

let greetings q =
  defaultArg (Option.ofChoice(q ^^ "name")) "World" |> sprintf "Hello %s"

let serSettings = new JsonSerializerSettings(ContractResolver = new Serialization.CamelCasePropertyNamesContractResolver())
let okJson o =
  OK (JsonConvert.SerializeObject(o, serSettings))
    >=> setMimeType "application/json"

let parseDateParam ctx parm deflt =
  let date = choice {
    let! stringDate = ctx.request.queryParam parm
    return DateTime.Parse stringDate
  }
  Choice.orDefault deflt date

let queryParamMulti (req:HttpRequest) parm = 
  let types = 
    req.query 
    |> List.choose (fun (k,v) -> 
      match k with 
      | k1 when k1 = parm -> v
      | _ -> None)
  match types with
    | [] -> Choice.failwith "not found"
    | l -> Choice.result l

let parseTypes ctx parm deflt =
  Choice.orDefault deflt (choice {
    let! types = queryParamMulti ctx.request parm
    return types
      |> List.map (fun v -> Enum.Parse(typeof<MediaType>, v) :?> MediaType)
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
        [ 
          path "/api/media-query" >=> handleQuery 
          pathScan "/api/media/%s" Files.browseFileHome
        ]
    ]

let defaultArgs = [| "db" ; "." |]

let startApp (MediaDirectory dir) =
  let cfg = { defaultConfig with homeFolder = Some dir }
  startWebServer cfg app
  0

let start (args : string[]) =
  let realArgs = if (args.Length = 0) then defaultArgs else args

  let dir = MediaDirectory realArgs.[1]
  match realArgs.[0] with
  | "db" -> startApp dir
  | "file" -> 
    Persistence <- getMemoryPersistence <| Some dir
    startApp dir
  | _ -> printfn "Unrecognized argument %s" args.[0] ; 1
