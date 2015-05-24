module MemoryWayback.MediaQuery

open System
open MemoryWayback.DbTypes
open MemoryWayback.Types
open ServiceStack.OrmLite

module Internal =
  let findMedias q =

    let findDbMedias q =
      DbSelect<medias>(<@ fun (m:medias) -> q.From <= m.Taken && m.Taken <= q.To @>)

    let dbToCodeType (m:medias) =
      {
        Media.Id = m.Id
        Type = m.Type
        Taken = m.Taken
        Url = m.Url
      }

    q
    |> findDbMedias
    |> List.map dbToCodeType


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
    {
      Query = q
      Results = mediasToResults <| dbFinder q
    }


let getResults : (Query -> Results) =
  Internal.getResults Internal.findMedias
