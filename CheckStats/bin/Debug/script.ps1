Start-Transcript -path D:\info.txt
  $sep="***"
  $sep
  Get-WmiObject Win32_VideoController
  $sep 
  Get-WmiObject -Namespace root\wmi -Class WmiMonitorID
  Get-WmiObject -Win32_DesktopMonitor
  $sep
  Get-WmiObject Win32_PhysicalMemory
  $sep
  Get-WmiObject Win32_BaseBoard
  $sep
  Get-WmiObject Win32_Processor
  $sep
  Get-LocalUser | Select *
  Get-WmiObject Win32_UserAccount | Select *
  $sep
  Get-WmiObject Win32_DiskDrive | Select *
  $sep
  Get-WmiObject Win32_LogicalDisk | Select *
  $sep
   Get-WmiObject Win32_ComputerSystem | select *
   $sep
  Get-WmiObject win32_networkadapterconfiguration 
  Stop-Transcript