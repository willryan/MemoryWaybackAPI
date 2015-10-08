module MemoryWayback.Tests.MemoryPersistence

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
  { r with Id = id} :> obj

let mediasId =
  {
    GetId = mediasGetId
    SetId = mediasSetId
  }

type MemoryPersistence(s:obj list, idF : DbIdentity) =
  let mutable nextId = 1
  let set = s
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
      (MemoryPersistence(without, idF) :> IPersistence).Insert r
    member x.Insert<'dbType> (r:'dbType) =
      (r , MemoryPersistence(r :> obj :: set, idF) :> IPersistence)
    member x.Delete<'dbType> (r:'dbType) =
      let p2 = MemoryPersistence(List.filter (fun x -> x <> (r :> obj)) set, idF)
      (true , p2 :> IPersistence)
