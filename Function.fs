namespace BoomTime

open FSharp.Data
open System
open System.IO
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open Newtonsoft.Json
open Microsoft.Extensions.Logging

module Function =

    let apiBase = "http://statsapi.mlb.com"    
    [<Literal>]    
    let schedule = "http://statsapi.mlb.com/api/v1/schedule?sportId=11"
    type Schedule = JsonProvider<schedule>
    [<Literal>]    
    let gameSample = "http://statsapi.mlb.com/api/v1.1/game/647409/feed/live"
    type Game = JsonProvider<gameSample>
    
    let todaysSchedule = Schedule.GetSample()
    
    let bulls = "Durham Bulls Athletic Park"
    
    let tryFindOngoingGameInVenue venueName =
        todaysSchedule.Dates.[0].Games
        |> Seq.tryFind (fun game -> game.Status.AbstractGameState = "Live" && game.Venue.Name = venueName)
    
    let getOngoingGameDetails venueName =
        let gameDetails =
            todaysSchedule.Dates.[0].Games
            |> Seq.tryFind (fun game -> game.Status.AbstractGameState = "Live" && game.Venue.Name = venueName)    
            |> Option.bind (fun game ->
                let details = Game.Load($"{apiBase}{game.Link}")
                if details.LiveData.Linescore.CurrentInning >= 8 then Some details
                else None)
        gameDetails
        
//    [<FunctionName("Function2")>]
//    let boomTime([<TimerTrigger("0 */10 * * * 5,6")>] timer: TimerInfo, ILogger log)
//        ()1Xu 
        
//    [<FunctionName("Function")>]
//    let run ([<HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)>]req: HttpRequest) (log: ILogger) =
//        async {
//            log.LogInformation("F# HTTP trigger function processed a request.")
//
//            let nameOpt = 
//                if req.Query.ContainsKey(Name) then
//                    Some(req.Query.[Name].[0])
//                else
//                    None
//
//            use stream = new StreamReader(req.Body)
//            let! reqBody = stream.ReadToEndAsync() |> Async.AwaitTask
//
//            let data = JsonConvert.DeserializeObject<NameContainer>(reqBody)
//
//            let name =
//                match nameOpt with
//                | Some n -> n
//                | None ->
//                   match data with
//                   | null -> ""
//                   | nc -> nc.Name
//            
//            let responseMessage =             
//                if (String.IsNullOrWhiteSpace(name)) then
//                    "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
//                else
//                    "Hello, " +  name + ". This HTTP triggered function executed successfully."
//
//            return OkObjectResult(responseMessage) :> IActionResult
//        } |> Async.StartAsTask