$ErrorActionPreference = "Stop"
$base = "http://localhost:5000"

function Show-Error($err) {
    Write-Host "FAILED: $($err.Exception.Message)"
    if ($err.Exception.Response) {
        try {
            $reader = New-Object System.IO.StreamReader($err.Exception.Response.GetResponseStream())
            Write-Host "BODY: $($reader.ReadToEnd())"
        } catch {}
    }
    exit 1
}

try {
    # 1. Admin: anonymous session + consent (onboard)
    $adminWs = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $s1 = Invoke-RestMethod -Uri "$base/api/auth/session" -WebSession $adminWs -Headers @{"X-Anonymous-Id"="smoke-admin-1"} -TimeoutSec 10
    $c1 = Invoke-RestMethod -Uri "$base/api/auth/session/consent" -Method Post -WebSession $adminWs -Headers @{"X-Anonymous-Id"="smoke-admin-1"; "X-CSRF-Token"=$s1.csrfToken} -ContentType "application/json" -Body '{"acceptedTerms":true,"turnstileToken":null}' -TimeoutSec 10
    Write-Host "1. ADMIN CONSENT OK  anon=$($c1.anonymousUserId)  recoveryKey=$($c1.recoveryToken.Substring(0,12))..."
    $adminCsrf = if ($c1.csrfToken) { $c1.csrfToken } else { $s1.csrfToken }

    # 2. Admin creates a league
    $league = Invoke-RestMethod -Uri "$base/api/leagues/create" -Method Post -WebSession $adminWs -Headers @{"X-Anonymous-Id"="smoke-admin-1"; "X-CSRF-Token"=$adminCsrf} -ContentType "application/json" -Body '{"name":"Office Pool","displayName":"Boss Wandi"}' -TimeoutSec 10
    Write-Host "2. LEAGUE CREATED    name=$($league.name)  code=$($league.inviteCode)  members=$($league.memberCount)/$($league.maxMembers)  admin=$($league.isAdmin)"
    $code = $league.inviteCode

    # 3. Public preview of the invite link (no auth needed)
    $preview = Invoke-RestMethod -Uri "$base/api/leagues/preview?inviteCode=$code" -TimeoutSec 10
    Write-Host "3. PREVIEW OK        name=$($preview.name)  members=$($preview.memberCount)/$($preview.maxMembers)"

    # 4. Member: new guest session + consent
    $memWs = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $s2 = Invoke-RestMethod -Uri "$base/api/auth/session" -WebSession $memWs -Headers @{"X-Anonymous-Id"="smoke-member-1"} -TimeoutSec 10
    $c2 = Invoke-RestMethod -Uri "$base/api/auth/session/consent" -Method Post -WebSession $memWs -Headers @{"X-Anonymous-Id"="smoke-member-1"; "X-CSRF-Token"=$s2.csrfToken} -ContentType "application/json" -Body '{"acceptedTerms":true,"turnstileToken":null}' -TimeoutSec 10
    $memCsrf = if ($c2.csrfToken) { $c2.csrfToken } else { $s2.csrfToken }
    Write-Host "4. MEMBER CONSENT OK anon=$($c2.anonymousUserId)  recoveryKey=$($c2.recoveryToken.Substring(0,12))..."

    # 5. Member joins via invite code with a display name
    $joinBody = '{"inviteCode":"' + $code + '","displayName":"Wandia Jr"}'
    $joined = Invoke-RestMethod -Uri "$base/api/leagues/join" -Method Post -WebSession $memWs -Headers @{"X-Anonymous-Id"="smoke-member-1"; "X-CSRF-Token"=$memCsrf} -ContentType "application/json" -Body $joinBody -TimeoutSec 10
    Write-Host "5. JOIN OK           league=$($joined.name)  members=$($joined.memberCount)/$($joined.maxMembers)  me=$($joined.myDisplayName)  admin=$($joined.isAdmin)"

    # 6. Duplicate display name should be rejected
    $dupWs = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $s3 = Invoke-RestMethod -Uri "$base/api/auth/session" -WebSession $dupWs -Headers @{"X-Anonymous-Id"="smoke-member-2"} -TimeoutSec 10
    $c3 = Invoke-RestMethod -Uri "$base/api/auth/session/consent" -Method Post -WebSession $dupWs -Headers @{"X-Anonymous-Id"="smoke-member-2"; "X-CSRF-Token"=$s3.csrfToken} -ContentType "application/json" -Body '{"acceptedTerms":true,"turnstileToken":null}' -TimeoutSec 10
    $dupCsrf = if ($c3.csrfToken) { $c3.csrfToken } else { $s3.csrfToken }
    try {
        Invoke-RestMethod -Uri "$base/api/leagues/join" -Method Post -WebSession $dupWs -Headers @{"X-Anonymous-Id"="smoke-member-2"; "X-CSRF-Token"=$dupCsrf} -ContentType "application/json" -Body $joinBody -TimeoutSec 10 | Out-Null
        Write-Host "6. DUPLICATE NAME    UNEXPECTEDLY ALLOWED"
    } catch {
        Write-Host "6. DUPLICATE NAME    correctly rejected"
    }

    # 7. Admin's league list
    $mine = Invoke-RestMethod -Uri "$base/api/leagues" -WebSession $adminWs -Headers @{"X-Anonymous-Id"="smoke-admin-1"} -TimeoutSec 10
    Write-Host "7. MY LEAGUES        count=$(@($mine).Count)  first=$(@($mine)[0].name)  members=$(@($mine)[0].memberCount)  admin=$(@($mine)[0].isAdmin)  me=$(@($mine)[0].myDisplayName)"

    # 8. League standings include both guests
    $standings = Invoke-RestMethod -Uri "$base/api/leagues/standings?leagueId=$($league.id)" -WebSession $adminWs -Headers @{"X-Anonymous-Id"="smoke-admin-1"} -TimeoutSec 10
    $names = (@($standings.standings) | ForEach-Object { $_.displayName }) -join ", "
    Write-Host "8. STANDINGS         entries=$(@($standings.standings).Count)  names=[$names]"

    Write-Host ""
    Write-Host "ALL SMOKE TESTS PASSED"
} catch {
    Show-Error $_
}
