module MemoryWayback.Media

open System

type Query = {
  From : DateTime
  To : DateTime
  Types : string list
}

type Result = {
  Date: DateTime
  Photos: string list
  Videos: string list
}

type Results = {
  Query : Query
  Results: Result list
}

let findMedia (q:Query) : Results =
  {
    Query = q
    Results = 
    [
      {
        Date = DateTime.UtcNow
        Photos = ["/photo_url/1.jpg" ; "/photo_url/2.jpg"]
        Videos = []
      }
      {
        Date = DateTime.UtcNow
        Photos = ["/photo_url/2.jpg"]
        Videos = ["/video_url/1.mpg"]
      }
    ]
  }
