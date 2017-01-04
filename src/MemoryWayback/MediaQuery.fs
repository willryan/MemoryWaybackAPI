module MemoryWayback.MediaQuery

open System
open MemoryWayback.Persistence
open MemoryWayback.DbTypes
open MemoryWayback.Types
open ServiceStack.OrmLite
open ExtCore.Control

module Internal =
  let findDbMedias (q:Query) (p:IPersistence) =
    let res, p2 = p.Select<medias>(<@ fun (m:medias) ->
      q.From <= m.Taken && m.Taken <= q.To @>)
    let res2 = res |> List.filter (fun (m:medias) ->
      List.contains m.Type q.Types)
    res2, p2
  //&& (List.contains m.Type q.Types)

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

  let getResults dbFinder query =
    let getUrl typ = 
      Seq.choose (fun r -> if r.Type = typ then Some r.Url else None) 
      >> Seq.toList
    let mediasToResults =
      Seq.groupBy (fun r -> r.Taken.Date)
      >> Seq.sortBy fst
      >> Seq.map (fun (d,rs) ->
        {
          Date = d.ToShortDateString()
          Photos = getUrl MediaType.Photo rs
          Videos = getUrl MediaType.Video rs
        })
      >> Seq.toList

    state {
      let! medias = dbFinder query
      return {
        Query = query
        Results = mediasToResults medias
      }
    }


let getResults : (Query -> StateFunc<IPersistence,Results>) =
  Internal.getResults Internal.findMedias
