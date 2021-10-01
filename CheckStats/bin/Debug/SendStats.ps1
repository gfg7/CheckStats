Get-ScheduledTask -TaskName "CheckDiskHealth" -ErrorAction SilentlyContinue -OutVariable disk 
                    if (!$disk){
                    $Trigger = New-ScheduledTaskTrigger -AtStartup
                    $User = "NT AUTHORITY\SYSTEM"
                    $Action = New-ScheduledTaskAction -Execute "D:\Check\CheckStats\bin\Debug\CheckDiskHealth.ps1"
                    Register-ScheduledTask -TaskName "CheckDiskHealth" -Trigger $Trigger -User $User -Action $Action -RunLevel Highest -Force
                    }
                    Get-ScheduledTask -TaskName "SendStats" -ErrorAction SilentlyContinue -OutVariable task 
                    if (!$task){
                    $Trigger = New-ScheduledTaskTrigger -AtStartup
                    $User = "NT AUTHORITY\SYSTEM"
                    $Action = New-ScheduledTaskAction -Execute "D:\Check\CheckStats\bin\Debug"
                    Register-ScheduledTask -TaskName "SendStats" -Trigger $Trigger -User $User -Action $Action -RunLevel Highest -Force
                    }