﻿$folderPath = "$HOME\Desktop\TerritoriesHtml" 
mkdir $folderPath -ErrorAction SilentlyContinue
$users = Get-AlbaUser
$userMap = @{}
ForEach($user in $users) {
  $userMap[$user.Name] = $user
}
$names = @("Marc Durham", "Some Otherguy")  
$territories = Get-AlbaTerritory
$territories |
  Where Status -eq Signed-out |
  Where {$names -Contains $_.SignedOutTo } |
  Group -Property SignedOutTo |
  % { 
      $email = $users |
        Where Name -eq $_.Name |
        Select -ExpandProperty Email
      $html = $_.Group |
        Sort SignedOut |
        Select Number, 
            Description, 
            @{ Name = "CheckedOut"; Expression = { $_.SignedOut.ToString("yyyy-MMM-dd") } },
            @{ Name = "Months"; Expression = { $m = ((Get-Date).Subtract($_.SignedOut).Days/30).ToString('F0'); "($m months)"; } },
            @{ Name = "Link"; Expression = { $_.MobileLink } } |
        ConvertTo-Html -As Table -Fragment `
            -PreContent "<p>Hello, this is an automatic email sent from your territory system.  This is a report of all the territories we have checked out under your name.  If they are completed just click the provided link and mark them as completed. Please just reply to this email if any of them are wrong.</p>" `
            -PostContent "<p>$($_.Group.Count) Territories</p>" |
          ForEach { $_ -Replace "<td>(https://[^< ]*?)</td>", "<td><a href=`"`$1`">link</a></td>" } 
      $html | Out-File "$folderPath\$($_.Name).html" -Encoding utf8       
      $emailParams = @{
        From = "Your Territory System <auto@territorytools.org>"
        To = $userMap[$_.Name].EMail
        Subject = "You Have $($_.Group.Count) Territories Checked Out" 
        Message = "$html" 
        ContentType = "text/html"
      }
      Send-SendGridMail @emailParams
    }

   