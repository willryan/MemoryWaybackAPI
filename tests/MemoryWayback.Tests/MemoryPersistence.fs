module MemoryWayback.Tests.MemoryPersistence

open MemoryWayback.Persistence
open Microsoft.FSharp.Quotations
open FSharp.Quotations.Evaluator

type MemoryPersistence(s:obj list) =
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
      (r , x :> IPersistence)
    member x.Insert<'dbType> (r:'dbType) =
      (r , MemoryPersistence(r :> obj :: set) :> IPersistence)
    member x.Delete<'dbType> (r:'dbType) =
      let p2 = MemoryPersistence(List.filter (fun x -> x <> (r :> obj)) set)
      (true , p2 :> IPersistence)
