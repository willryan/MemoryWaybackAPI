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

let getMemoryPersistence dirs = 
  let time = DateTime.Now
  let per = MemoryPersistence("mem",[],mediasId) :> IPersistence
  let res = List.fold (fun p d -> MediaLibraryUpdate.updateMedia time realFileHelper d p) per dirs
  res

let mutable Persistence = getMemoryPersistence []

let persist stateFunc =
  let (res,p) = stateFunc Persistence
  Persistence <- p
  res

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


let app publicDir mediaDirs =
  let browseMount (mnt,path) =
    let dir = 
      mediaDirs
      |> List.find (fun dir -> dir.Mount = mnt)
    Files.browseFile dir.Path path
  choose
    [ GET >=> choose
        [ 
          path "/api/media-query" >=> handleQuery 
          pathScan "/api/media/%s/%s" browseMount
          path "/" >=> Files.file (Path.Combine(publicDir,"index.hml"))
          Files.browseHome
        ]
    ]

let defaultArgs = [| "file" ; "." |]

let startApp (dirs : MediaDirectory list) =
  let mimeTypes =
    defaultMimeTypesMap
      @@ (function 
        | ".avi" -> createMimeType "video/avi" false 
        | ".mp4" -> createMimeType "video/mp4" false
        | ".m4v" -> createMimeType "video/mp4" false
        | ".mov" -> createMimeType "video/quicktime" false
        | ".mpg" -> createMimeType "video/mpeg" false
        | ".mpeg" -> createMimeType "video/mpeg" false 
        | _ -> None)
  let publicDir = Path.Combine(System.Environment.CurrentDirectory, "public")
  let cfg = { defaultConfig with homeFolder = Some publicDir ; mimeTypesMap = mimeTypes }
  ignore <| Process.Start("http://localhost:8083/index.html")
  startWebServer cfg <| app publicDir dirs
  0

let start (args : string[]) =
  let realArgs = if (args.Length = 0) then defaultArgs else args

  let dirs =
    realArgs
    |> Array.skip 1
    |> Array.mapi (fun i arg -> { Mount = sprintf "%d" i ; Path = arg.TrimEnd('/', '\\') })
    |> Array.toList
  match realArgs.[0] with
  | "db" -> startApp dirs
  | "file" -> 
    Persistence <- getMemoryPersistence dirs
    startApp dirs
  | _ -> printfn "Unrecognized argument %s" args.[0] ; 1
