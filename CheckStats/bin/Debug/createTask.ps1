Get-ScheduledTask -TaskName "SendStatsTask" -ErrorAction SilentlyContinue -OutVariable task 
                                    if (!$task){
                                    $Trigger = New-ScheduledTaskTrigger -AtStartup
                                    $User = "NT AUTHORITY\SYSTEM"
                                    $Action = New-ScheduledTaskAction -Execute "D:\Check\CheckStats\bin\Debug\CheckStats.exe"
                                    Register-ScheduledTask -TaskName "SendStatsTask" -Trigger $Trigger -User $User -Action $Action -RunLevel Highest -Force
                    } Read-Host -Prompt "Press Enter to exit"