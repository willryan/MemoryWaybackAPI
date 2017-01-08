module MemoryWayback.MemoryPersistence

open MemoryWayback.Persistence
open Microsoft.FSharp.Quotations
open FSharp.Quotations.Evaluator
open MemoryWayback.DbTypes

type DbIdentity =
  {
    GetId : obj -> int
    SetId : obj -> int -> obj
  }

let mediasGetId (o:obj) : int =
  let r = o :?> medias
  r.Id

let mediasSetId (o:obj) (id:int) : obj =
  let r = o :?> medias
  { r with Id = id } :> obj

let mediasId =
  {
    GetId = mediasGetId
    SetId = mediasSetId
  }

type MemoryPersistence(name: string, s:obj list, idF : DbIdentity) =
  let mutable nextId = 0
  let set = s
  let name = name
  interface IPersistence with
    member x.Select<'dbType> (e:Expr<'dbType -> bool>) =
      let evalF = (QuotationEvaluator.Evaluate<'dbType -> bool> e)
      let res =
        set
        |> List.filter (fun o -> o :? 'dbType)
        |> List.map (fun o -> o :?> 'dbType)
        |> List.filter evalF
      (res , x :> IPersistence)
    // note: really needs to start using ID
    member x.Update<'dbType> (r:'dbType) =
      let without = set |> List.filter (fun er ->
         (idF.GetId er) <> (idF.GetId (r :> obj))
      )
      (MemoryPersistence(name + "'", without, idF) :> IPersistence).Insert r
    member x.Insert<'dbType> (r:'dbType) =
      let useR =
        match idF.GetId (r :> obj) with
        | -1 ->
          nextId <- nextId + 1
          idF.SetId (r :> obj) nextId
        | _ -> r :> obj
      (useR :?> 'dbType , MemoryPersistence(name + "'", useR :: set, idF) :> IPersistence)
    member x.Delete<'dbType> (r:'dbType) =
      let p2 = MemoryPersistence(name + "'", List.filter (fun x -> x <> (r :> obj)) set, idF)
      (true , p2 :> IPersistence)

  override x.ToString() =
    sprintf "%s: %A" name set
