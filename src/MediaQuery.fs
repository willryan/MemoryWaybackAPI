module MemoryWayback.MediaQuery

open System
open MemoryWayback.Persistence
open MemoryWayback.DbTypes
open MemoryWayback.Types
open ServiceStack.OrmLite
open ExtCore.Control

module Internal =
  let findDbMedias q (p:IPersistence) =
    p.Select<medias>(<@ fun (m:medias) -> q.From <= m.Taken && m.Taken <= q.To @>)

  let findMedias q =

    let dbToCodeType (m:medias) =
      {
        Media.Id = m.Id
        Type = m.Type
        Taken = m.Taken
        Url = m.Url
      }

    state {
      let! res = findDbMedias q
      return List.map dbToCodeType res
    }


  let getResults dbFinder q =

    let mediasToResults ms =
      ms
      |> Seq.groupBy (fun r -> r.Taken)
      |> Seq.map (fun (d,rs) ->
      {
        Date = d
        Photos =
          rs
          |> Seq.filter (fun r -> r.Type = MediaType.Photo)
          |> Seq.map (fun r -> r.Url)
          |> Seq.toList
        Videos =
          rs
          |> Seq.filter (fun r -> r.Type = MediaType.Video)
          |> Seq.map (fun r -> r.Url)
          |> Seq.toList
      })
      |> Seq.toList

    state {
      let! medias = dbFinder q
      return {
        Query = q
        Results = mediasToResults medias
      }
    }


let getResults : (Query -> StateFunc<IPersistence,Results>) =
  Internal.getResults Internal.findMedias

