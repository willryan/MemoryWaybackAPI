module MemoryWayback.App

open System
open System.Diagnostics
open Suave
open Suave.Operators
open Suave.Filters
open Suave.Utils
open System.IO
open System.Text
open ExtCore.Control
open MemoryWayback.DbTypes
open MemoryWayback.Types
open MemoryWayback.Persistence
open MemoryWayback.OrmlitePersistence
open MemoryWayback.MemoryPersistence
open Newtonsoft.Json

//let logger = Loggers.sane_defaults_for Debug

let getDbPersistence () = OrmlitePersistence() :> IPersistence

let loadPersistence = 
  MediaLibraryUpdate.updateMedia DateTime.Now FileHelper.realFileHelper

let getMemoryPersistence = 
  let per = MemoryPersistence("mem",[],mediasId) :> IPersistence
  loadPersistence per

let mutable Persistence = getMemoryPersistence []

let persist stateFunc =
  let (res,p) = stateFunc Persistence
  Persistence <- p
  res

let serSettings = new JsonSerializerSettings(ContractResolver = new Serialization.CamelCasePropertyNamesContractResolver())
let okJson o =
  Successful.OK (JsonConvert.SerializeObject(o, serSettings))
    >=> Writers.setMimeType "application/json"

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

let getQuery ctx = 
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
  o

let handleQuery ctx =
  let o = getQuery ctx
  let results = persist (MediaQuery.getResults o)
  okJson results ctx


let app publicDir mediaDirs =
  let browseMount (mnt,path) =
    let dir = List.find (fun dir -> dir.Mount = mnt) mediaDirs
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
    Writers.defaultMimeTypesMap
      @@ (fun ext ->
        maybe {
          let! suffix = Map.tryFind ext FileHelper.videoExtensionsToMime
          let mimeType = sprintf "video/%s" suffix
          return! Writers.createMimeType mimeType false
        }
      )
  let publicDir = Path.Combine(System.Environment.CurrentDirectory, "public")
  let cfg = { defaultConfig with homeFolder = Some publicDir ; mimeTypesMap = mimeTypes }
  ignore <| Process.Start("http://localhost:8083/index.html")
  startWebServer cfg <| app publicDir dirs
  0

let start (args : string[]) =
  let realArgs = if (args.Length = 0) then defaultArgs else args

  let dirs =
    realArgs
    |> Array.toList
    |> List.tail
    |> List.mapi (fun i arg -> { Id = i ; Mount = sprintf "%d" i ; Path = arg.TrimEnd('/', '\\') })
  let mDirs = dirs |> List.map Types.makeMediaDirectory
  match realArgs.[0] with
  | "db" -> startApp mDirs
  | "file" -> 
    Persistence <- getMemoryPersistence dirs
    startApp mDirs
  | _ -> printfn "Unrecognized argument %s" args.[0] ; 1
