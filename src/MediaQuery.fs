module MemoryWayback.MediaQuery

open System
open MemoryWayback.DbTypes
open MemoryWayback.Types
open ServiceStack.OrmLite

module Internal =
  let findDbMedias q =
    Connection.Select<medias>(fun (m:medias) -> q.From <= m.Taken && m.Taken <= q.To)
    |> List.ofSeq

  let dbToCodeType (m:medias) =
    {
      Media.Id = m.Id
      Type = m.Type
      Taken = m.Taken
      Url = m.Url
    }

  let findMedias dbFinder mapper q =
    dbFinder q
    |> List.map mapper

  let mediasToResults q ms =
    let dateGrps =
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
      Results = dateGrps
    }

  let findMedia dbFinder resFinder mediaMapper resMapper q =
    resFinder dbFinder mediaMapper q
    |> resMapper q


let findMedia : (Query -> Results) =
  Internal.findMedia Internal.findDbMedias Internal.findMedias Internal.dbToCodeType Internal.mediasToResults
