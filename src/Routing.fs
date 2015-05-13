namespace MemoryWayback

open System
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
open FSharp.Data.Runtime.NameUtils
open MemoryWayback.Util

module Routing = 

  type RouteIdMap = Map<string,string>

  type ResourceHandler = (RouteIdMap -> WebPart)

  type ResourceActions = {
    Index : ResourceHandler option
    Show : ResourceHandler option
    Create : ResourceHandler option
    Update : ResourceHandler option
    Destroy : ResourceHandler option
  }

  type ResourceContext = {
    Prefix : string
    Names : string list
  }

  type Resource = {
    Context : ResourceContext
    Name : string
    UrlWithId : string
    UrlNoId : string
    Actions : ResourceActions
    SubResources : Resource list
  }

  type IdsFormat<'tuple> = PrintfFormat<string -> 'tuple -> unit, unit, string, string, 'tuple>

  let buildRouteIds ctx ids =
    List.rev ids
    |> List.zip ctx.Names
    |> Map.ofList

  let routeMatchFn url ctx (fn:ResourceHandler) =
    match ctx.Names.Length with
      | 0 -> path url >>= (fn Map.empty)
      | 1 -> pathScan (new IdsFormat<string>(url)) (fun id1 -> fn <| buildRouteIds ctx [id1])
      | 2 -> pathScan (new IdsFormat<string * string>(url)) (fun (id1, id2) -> fn <| buildRouteIds ctx [id1 ; id2])
      | 3 -> pathScan (new IdsFormat<string * string * string>(url)) (fun (id1, id2, id3) -> fn <| buildRouteIds ctx [id1 ; id2 ; id3])
      | 4 -> pathScan (new IdsFormat<string * string * string * string>(url)) (fun (id1, id2, id3, id4) -> fn <| buildRouteIds ctx [id1 ; id2 ; id3 ; id4])
      | 5 -> pathScan (new IdsFormat<string * string * string * string * string>(url)) (fun (id1, id2, id3, id4, id5) -> fn <| buildRouteIds ctx [id1 ; id2 ; id3 ; id4 ; id5])
      | _ -> raise (new Exception("too many nested ids"))

  let methodHandler mthd resHndl ids =
    match resHndl with
    | Some fn -> mthd >>= fn ids
    | None -> (fun _ -> async.Return None)

  let noIdRoutes resource =
    routeMatchFn resource.UrlNoId resource.Context (fun ids ->
      choose [
        methodHandler GET resource.Actions.Index ids
        methodHandler POST resource.Actions.Create ids
      ])

  let idRoutes resource =
    let subCtx = { Prefix = resource.UrlWithId ; Names = resource.Name :: resource.Context.Names }
    routeMatchFn resource.UrlWithId subCtx (fun ids ->
      choose [
        methodHandler GET resource.Actions.Show ids
        methodHandler PUT resource.Actions.Update ids
        methodHandler DELETE resource.Actions.Destroy ids
      ])

  let rec mapResource resource =
    choose [
      choose <| List.map (fun rsrc -> mapResource rsrc) resource.SubResources
      idRoutes resource
      noIdRoutes resource
    ]

  let resource name hndl subResources ctx =
    let url = sprintf "%s/%s" ctx.Prefix name
    let urlWithId = url + "/%s"
    let subCtx = { Prefix = urlWithId ; Names = name :: ctx.Names }
    let subR = List.map (fun sr -> sr subCtx) subResources

    {
      UrlNoId = url
      UrlWithId = urlWithId
      Context = ctx
      Name = name
      Actions = hndl
      SubResources = subR
    }

  let Root = { Prefix = "" ; Names = [] }

  type ResourceDefinition = ResourceContext -> Resource

  let mutable Resources : ResourceDefinition list = []

  let makeApp () =
    Resources
    |> List.map (fun r -> mapResource (r Root))
    |> choose


  module Debug =

    let printRouteLnFun verbF uriF actionF =
      verbF()
      Console.SetCursorPosition(10, Console.CursorTop)
      uriF()
      Console.SetCursorPosition(70, Console.CursorTop)
      actionF()
      printfn ""

    let defaultRoutePrintFn v () = printf "%s" v
    let colorPrintFn c v () = cprintf c "%s" v

    let printRouteLn fn (verb:string) (uri:string) (action:string) =
      printRouteLnFun (fn verb) (fn uri) (fn action)

    let printResourceRoute verb withId action (getAction:ResourceActions -> ResourceHandler option) resource =
      if (getAction resource.Actions).IsSome then
        let uri = if withId then resource.UrlWithId else resource.UrlNoId
        let names = if withId then resource.Name :: resource.Context.Names else resource.Context.Names
        let singNames =
          List.map singularize names
          |> List.rev
        let parts = Array.toList (uri.Split([|"%s"|], System.StringSplitOptions.None))
        let someNames = None :: (List.map (fun n -> Some <| sprintf ":%s" n) singNames)
        let outUriParts =
          List.zip someNames parts
        let colorFn (pairs:(string option * string) list) () =
          pairs
          |> List.iter (fun (someName,part) ->
            match someName with
            | Some name -> ignore <| cprintf ConsoleColor.Blue "%s" name
            | None -> ()
            printf "%s" part
          )

        let actFunc = (sprintf "%s#%s" resource.Name action)
        let fn = defaultRoutePrintFn
        printRouteLnFun (fn verb) (colorFn outUriParts) (fn actFunc)

    let rec printRoute resource =
      [
        printResourceRoute "GET" false "Index" (fun a -> a.Index)
        printResourceRoute "POST" false "Create" (fun a -> a.Create)
        printResourceRoute "GET" true "Show" (fun a -> a.Show)
        printResourceRoute "PUT" true "Update" (fun a -> a.Update)
        printResourceRoute "DELETE" true "Delete" (fun a -> a.Destroy)
      ]
      |> List.iter (fun f -> f resource)
      printRoutes resource.SubResources

    and printRoutes resources =
      List.iter printRoute resources

    let printRouteDefs() =
      printfn ""
      printRouteLn (colorPrintFn ConsoleColor.Yellow) "Verb" "Uri Pattern" "Action"
      printRoutes <| List.map (fun r -> r Root) Resources
      printfn ""

