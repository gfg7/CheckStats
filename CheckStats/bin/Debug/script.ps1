Start-Transcript -path D:\info.txt
  $sep="#"
  $sep
  "GPU"
  Get-WmiObject Win32_VideoController
  $sep 
  "Monitor"
  Get-WmiObject -Namespace root\wmi -Class WmiMonitorID
  Get-WmiObject -Win32_DesktopMonitor
  $sep
  "RAM"
  Get-WmiObject Win32_PhysicalMemory
  $sep
  "Motherboard"
  Get-WmiObject Win32_BaseBoard
  $sep
  "CPU"
  Get-WmiObject Win32_Processor
  $sep
  "Users"
  Get-LocalUser | Select *
  Get-WmiObject Win32_UserAccount | Select *
  $sep
  "Disk"
  Get-WmiObject Win32_DiskDrive | Select *
  $sep
  "DiskVolume"
  Get-WmiObject Win32_LogicalDisk | Select *
  $sep
  "Net"
   Get-WmiObject Win32_ComputerSystem | select *
   $sep
   "System"
  Get-WmiObject win32_networkadapterconfiguration 
  Stop-Transcript