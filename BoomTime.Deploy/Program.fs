open Farmer
open Farmer.Builders

let vault = keyVault {
    name "kv-boomtime"
}

let func = functions {
    name "func-boomtime"
    service_plan_name "plan-boomtime"
    link_to_keyvault vault.Name
    secret_setting "twilioSid"
    secret_setting "twilioToken"
    secret_setting "twilioFrom"
    secret_setting "twilioTo"
}
let deployment = arm {
    location Location.EastUS
    name "boomtime"
    add_resources [ vault; func ]
}

printf "Generating ARM template..."
deployment |> Writer.quickWrite "template"
printfn "all done! Template written to template.json"

// Alternatively, deploy your resource group directly to Azure here.
// deployment
// |> Deploy.execute "farmer-resource-group" Deploy.NoParameters
// |> printfn "%A"