module MemoryWayback.DbTypes

open System
open ServiceStack.OrmLite
open System.Linq
open System.Linq.Expressions
open Microsoft.FSharp.Linq.RuntimeHelpers
open Microsoft.FSharp.Quotations

type MediaType =
  | Photo = 0
  | Video = 1

type medias = {
  mutable Id : int
  mutable Url : string
  mutable Taken : DateTime
  mutable Type : MediaType
}

let dbConnection server db uid =
  let connString = sprintf "Server=%s;Database=%s;UID=%s" server db uid
  let fact = new OrmLiteConnectionFactory(connString,MySqlDialect.Provider)
  fact.Open()

let connection = dbConnection "localhost" "memory_wayback" "root"

let DbSelect<'dbType> (expr:Expr<'dbType -> bool>) =
  let csExpr =
    expr
    |> LeafExpressionConverter.QuotationToExpression
    |> unbox<Expression<Func<'dbType,bool>>>
  connection.Select<'dbType>(csExpr) |> List.ofSeq

let DbUpdate record = connection.Update<'dbType> [|record|]
let DbInsert record = connection.Insert<'dbType> [|record|]
let DbDelete record = connection.Delete<'dbType> [|record|]

type Persistence =
  abstract Select<'dbType> : Expr<'dbType -> bool> -> 'dbType list
  abstract Update : 'dbType -> Persistence
  abstract Insert : 'dbType -> Persistence
  abstract Delete : 'dbType -> Persistence


type OrmlitePersistence() =
  member x.change (updFun:('dbType -> 'ig)) record =
    ignore <| updFun record
    x :> Persistence
  interface Persistence with
    member x.Select<'dbType> e =
      DbSelect<'dbType> e
    member x.Update<'dbType> (r:'dbType) = x.change DbUpdate r
    member x.Insert<'dbType> (r:'dbType) = x.change DbInsert r
    member x.Delete<'dbType> (r:'dbType) = x.change DbDelete r

