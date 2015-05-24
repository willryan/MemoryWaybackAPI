module MemoryWayback.OrmlitePersistence

open System
open ServiceStack.OrmLite
open System.Linq
open System.Linq.Expressions
open Microsoft.FSharp.Linq.RuntimeHelpers
open Microsoft.FSharp.Quotations
open MemoryWayback.Persistence

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

let DbUpdate record = 
  ignore <| connection.Update<'dbType> [|record|]
  record
let DbInsert record = 
  ignore <| connection.Insert<'dbType> [|record|]
  record
let DbDelete record = 
  connection.Delete<'dbType> [|record|] > 0

type OrmlitePersistence() =
  member x.change (updFun:('dbType -> 'ig)) record =
    (updFun record, x :> IPersistence)
  interface IPersistence with
    member x.Select<'dbType> e =
      DbSelect<'dbType> e, x :> IPersistence
    member x.Update<'dbType> (r:'dbType) = x.change DbUpdate r
    member x.Insert<'dbType> (r:'dbType) = x.change DbInsert r
    member x.Delete<'dbType> (r:'dbType) = x.change DbDelete r

