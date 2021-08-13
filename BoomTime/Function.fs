namespace BoomTime

open FSharp.Data
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open System
open Twilio
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
    
    let sid = Environment.GetEnvironmentVariable("TWILIO_SID")
    let token = Environment.GetEnvironmentVariable("TWILIO_TOKEN")
    let fromPhone = Environment.GetEnvironmentVariable("TWILIO_FROM") |> PhoneNumber
    let toPhone = Environment.GetEnvironmentVariable("TWILIO_TO") |> PhoneNumber
    let mostRecentMessage =
        TwilioClient.Init(sid, token)
        MessageResource.Read(
            ``to``  = toPhone,
            from    = fromPhone,
            limit   = 1L)
        |> Seq.head
    
    [<Literal>]
    let bullsStadium = "Durham Bulls Athletic Park"    
    
    let tryFindOngoingGameAtVenue venueName =
        let gameDetails = 
            schedule.Dates.[0].Games
            |> Seq.tryFind (fun game -> game.Status.AbstractGameState = "Live" && game.Venue.Name = venueName)
            |> Option.map (fun game -> Game.Load($"{apiBase}{game.Link}"))
        gameDetails
        
    let createSms (game: Game.Root) =
        let formattedMessage =
            $"\n🎆BOOMTIME🎇\n{game.GameData.Teams.Home.Name} vs {game.GameData.Teams.Away.Name}\n{game.LiveData.Linescore.CurrentInningOrdinal} inning 🐶"
        CreateMessageOptions(
            ``to``  = toPhone,
            From    = fromPhone,
            Body    = formattedMessage)
        
    let filterOngoingAndSend venue f =
        if mostRecentMessage.DateSent.Value > DateTime.Now.Subtract(TimeSpan.FromHours(1.0)) then
            ()
        else                
            tryFindOngoingGameAtVenue venue
            |> Option.filter f
            |> Option.map createSms
            |> Option.map MessageResource.Create
            |> ignore
        
    [<FunctionName("BoomTime")>]
    let boomTime([<TimerTrigger("0 */10 * * * 5,6")>] timer: TimerInfo) =
        filterOngoingAndSend bullsStadium (fun g -> g.LiveData.Linescore.CurrentInning >= 8)

    [<FunctionName("Manual")>]    
    let testBinding ([<HttpTrigger(AuthorizationLevel.Function, "get")>]req: HttpRequest) =
        let venue = req.Query.["venue"].ToString()
        filterOngoingAndSend venue (fun _ -> true)
            
        