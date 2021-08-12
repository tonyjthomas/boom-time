namespace BoomTime

open FSharp.Data
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open System
open Twilio.Rest.Api.V2010.Account
open Twilio.Types

module Function =

    let apiBase = "http://statsapi.mlb.com"    
    [<Literal>]    
    let scheduleSample = "http://statsapi.mlb.com/api/v1/schedule?sportId=11"
    type Schedule = JsonProvider<scheduleSample>
    [<Literal>]    
    let gameSample = "http://statsapi.mlb.com/api/v1.1/game/647409/feed/live"
    type Game = JsonProvider<gameSample>
    
    let schedule = Schedule.GetSample()
    
    [<Literal>]
    let bulls = "Durham Bulls Athletic Park"
    
    let tryFindOngoingGameAtVenue venueName =
        let gameDetails = 
            schedule.Dates.[0].Games
            |> Seq.tryFind (fun game -> game.Status.AbstractGameState = "Live" && game.Venue.Name = venueName)
            |> Option.map (fun game -> Game.Load($"{apiBase}{game.Link}"))
        gameDetails
        
//    [<FunctionName("BoomTime")>]
//    let boomTime([<TimerTrigger("0 */10 * * * 5,6")>] timer: TimerInfo, log: ILogger) =
//        let venue = "Durham Bulls Athletic Park"
//        
//        tryFindOngoingGameAtVenue venue
//        |> Option.filter (fun g -> g.LiveData.Linescore.CurrentInning >= 8)
//        |> Option.map (postMessage venue)
//        |> ignore
//        
//        OkResult()

    let formattedMessage (game: Game.Root) =
        $"\n🎆BOOMTIME🎇\n{game.GameData.Teams.Home.Name} vs {game.GameData.Teams.Away.Name}\n{game.LiveData.Linescore.CurrentInningOrdinal} inning 🐶"
    
    let createSms (game: Game.Root) =
        let fromPhone = Environment.GetEnvironmentVariable("TWILIO_FROM")
        let toPhone = Environment.GetEnvironmentVariable("TWILIO_TO")
        CreateMessageOptions(
                    ``to``= PhoneNumber(toPhone),
                    From = PhoneNumber(fromPhone),
                    Body = formattedMessage game)

    [<FunctionName("BindingTest")>]    
    let testBinding
        ([<HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)>]req: HttpRequest)
        ([<TwilioSms(AccountSidSetting = "TWILIO_SID", AuthTokenSetting = "TWILIO_TOKEN")>]msg: outref<CreateMessageOptions>)
        (log: ILogger) =
    
        let venue = req.Query.["venue"].ToString()
        
        msg <-
            tryFindOngoingGameAtVenue venue
            |> Option.map createSms
            |> Option.toObj
        