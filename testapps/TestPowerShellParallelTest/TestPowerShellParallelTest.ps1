$SharedVariable = "Hello Shared Variable"
@(1..10) | ForEach-Object -Parallel { 
    $i = $_
    Write-Host "Running against: $i for SharedVariable: $($using:SharedVariable)"
}