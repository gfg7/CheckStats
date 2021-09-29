Get-ScheduledTask -TaskName "CheckDisk" -ErrorAction SilentlyContinue -OutVariable chkdsk 
                                    if (!$chkdsk){
                                    $Trigger = New-ScheduledTaskTrigger -AtStartup
                                    $User = "NT AUTHORITY\SYSTEM"
                                    $Action = New-ScheduledTaskAction -Execute "D:\Check\CheckStats\bin\Debug\CheckDisk.ps1"
                                    Register-ScheduledTask -TaskName "CheckDisk" -Trigger $Trigger -User $User -Action $Action -RunLevel Highest -Force
                    }
                      Get-ScheduledTask -TaskName "SendStats" -ErrorAction SilentlyContinue -OutVariable task 
                                    if (!$task){
                                    $Trigger = New-ScheduledTaskTrigger -AtStartup
                                    $User = "NT AUTHORITY\SYSTEM"
                                    $Action = New-ScheduledTaskAction -Execute "D:\Check\CheckStats\bin\Debug\CheckStats.exe"
                                    Register-ScheduledTask -TaskName "SendStats" -Trigger $Trigger -User $User -Action $Action -RunLevel Highest -Force
                    }