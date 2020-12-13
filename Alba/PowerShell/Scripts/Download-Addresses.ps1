﻿
$connection = Get-AlbaConnection `
    -AlbaHost $env:ALBA_HOST `
    -Account $env:ALBA_ACCOUNT `
    -User $env:ALBA_USER `
    -Password $env:ALBA_PASSWORD

$result = Get-AlbaAddress -Connection $connection -Search "Main St"

$result | Out-File -FilePath "addresses-$(Get-Date -Format 'yyyy-MM-dd-HHmm').txt" -Encoding utf8
 