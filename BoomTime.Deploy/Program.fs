open System
open Farmer
open Farmer.Builders

[<Literal>]
let twilioSid = "twilioSid"
[<Literal>]
let twilioToken = "twilioToken"
[<Literal>]
let twilioTo = "twilioTo"
[<Literal>]
let twilioFrom = "twilioFrom"
[<Literal>]
let rgName = "boomtime"

let vault = keyVault {
    name $"kv-{rgName}"
}

let func = functions {
    name                $"func-{rgName}"
    service_plan_name   $"plan-{rgName}"
    link_to_keyvault    vault.Name
    secret_setting      twilioSid
    secret_setting      twilioToken
    secret_setting      twilioFrom
    secret_setting      twilioTo
    zip_deploy          "package.zip"
}

let deployment = arm {
    location Location.EastUS
    add_resources [ vault; func ]
}

let parameters =
    [ ("twilioSid",     Environment.GetEnvironmentVariable twilioSid)
      ("twilioToken",   Environment.GetEnvironmentVariable twilioToken)
      ("twilioFrom",    Environment.GetEnvironmentVariable twilioFrom)
      ("twilioTo",      Environment.GetEnvironmentVariable twilioTo) ]

printf "Beginning ARM deployment..."
match deployment |> Deploy.tryExecute rgName parameters with
| Ok _ -> $"Deployment to resource group '{rgName}' completed."
| Error e -> $"Deployment failed with error: {e}."
|> printfn "%s"
