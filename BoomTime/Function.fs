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
    
    let sid = Environment.GetEnvironmentVariable("twilioSid")
    let token = Environment.GetEnvironmentVariable("twilioToken")
    let fromPhone = Environment.GetEnvironmentVariable("twilioFrom") |> PhoneNumber
    let toPhone = Environment.GetEnvironmentVariable("twilioTo") |> PhoneNumber
    let getMostRecentMessage() =
        TwilioClient.Init(sid, token)
        MessageResource.Read(
            ``to``  = toPhone,
            from    = fromPhone,
            limit   = Nullable 1L)
        |> Seq.head
    
    [<Literal>]
    let bullsStadium = "Durham Bulls Athletic Park"    
    
    let tryFindOngoingGameAtVenue venueName =
        let gameDetails = 
            schedule.Dates.[0].Games
            |> Seq.tryFind (fun game -> game.Status.AbstractGameState = "Live" && game.Venue.Name = venueName)
            |> Option.map (fun game -> Game.Load(sprintf "%s%s" apiBase game.Link))
        gameDetails
        
    let createSms (game: Game.Root) =
        let formattedMessage =
            sprintf "\n🎆BOOMTIME🎇\n%s vs %s\n%s inning\nInnings: %d"
                game.GameData.Teams.Home.Name
                game.GameData.Teams.Away.Name
                game.LiveData.Linescore.CurrentInningOrdinal
                game.LiveData.Linescore.ScheduledInnings
        CreateMessageOptions(
            ``to``  = toPhone,
            From    = fromPhone,
            Body    = formattedMessage)
        
    let filterOngoingAndSend venue f =
        if getMostRecentMessage().DateSent.Value > DateTime.Now.Subtract(TimeSpan.FromHours(2.0)) then
            ()
        else                
            tryFindOngoingGameAtVenue venue
            |> Option.filter f
            |> Option.map createSms
            |> Option.map MessageResource.Create
            |> ignore
        
    [<FunctionName("BoomTime")>]
    let boomTime([<TimerTrigger("0 */10 0-4 * * 0,6")>] _timer: TimerInfo) =
        filterOngoingAndSend bullsStadium (fun g -> g.LiveData.Linescore.CurrentInning >= g.LiveData.Linescore.ScheduledInnings - 1)

    [<FunctionName("Manual")>]    
    let testBinding ([<HttpTrigger(AuthorizationLevel.Function, "get")>]req: HttpRequest) =
        let venue = req.Query.["venue"].ToString()
        filterOngoingAndSend venue (fun _ -> true)
            
        
