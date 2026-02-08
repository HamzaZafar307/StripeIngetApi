$url = "http://localhost:5185/api/webhook"
$headers = @{ "Content-Type" = "application/json" }

Write-Host "Creating Sub..."
$body = Get-Content create_sub.json -Raw
try {
    Invoke-RestMethod -Uri $url -Method Post -Headers $headers -Body $body
} catch {
    Write-Host "Error Creating Sub: $_"
}

Start-Sleep -Seconds 1

Write-Host "Updating Sub..."
$body = Get-Content update_sub.json -Raw
try {
    Invoke-RestMethod -Uri $url -Method Post -Headers $headers -Body $body
} catch {
    Write-Host "Error Updating Sub: $_"
}

Start-Sleep -Seconds 1

Write-Host "Cancelling Sub..."
$body = Get-Content cancel_sub.json -Raw
try {
    Invoke-RestMethod -Uri $url -Method Post -Headers $headers -Body $body
} catch {
    Write-Host "Error Cancelling Sub: $_"
}

Start-Sleep -Seconds 1

Write-Host "Testing Multi-Item Sub..."
$multiItemJson = @{
    id = "evt_multi_item_1"
    type = "customer.subscription.created"
    created = 1724200000
    data = @{
        object = @{
            id = "sub_multi_1"
            customer = "cus_multi_1"
            status = "active"
            items = @{
                data = @(
                    @{
                        quantity = 1
                        plan = @{
                            id = "price_1"
                            product = "prod_1"
                            amount = 5000
                            currency = "usd"
                            interval = "month"
                        }
                    },
                    @{
                        quantity = 2
                        plan = @{
                            id = "price_2"
                            product = "prod_2"
                            amount = 2000
                            currency = "usd"
                            interval = "month"
                        }
                    }
                )
            }
        }
    }
} | ConvertTo-Json -Depth 10

try {
    Invoke-RestMethod -Uri $url -Method Post -Headers $headers -Body $multiItemJson
} catch {
    Write-Host "Error Creating Multi-Item Sub: $_"
}


Write-Host "Getting yearly MRR for verification..."
$yearlyMrrUrl = "http://localhost:5185/api/reports/mrr/yearly"
try {
    $mrr = Invoke-RestMethod -Uri $yearlyMrrUrl -Method Get
    $mrr | ConvertTo-Json -Depth 5
} catch {
    Write-Host "Error Getting Yearly MRR: $_"
}
