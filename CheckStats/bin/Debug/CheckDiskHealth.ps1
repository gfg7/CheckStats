Start-Transcript -path D:\Check\CheckStats\bin\Debug\CheckDiskHealth.txt $var=@(Get-Volume | Select -Property DriveLetter, Drivetype | Where-Object { ($_.DriveLetter -ne $Null) -and($_.Drivetype -like "Fixed")} | Select -Property DriveLetter)foreach ($disk in $var) {$result = Repair-Volume -DriveLetter ($disk.DriveLetter) -Scan ($disk.DriveLetter) + '=' + $result } Stop-Transcript