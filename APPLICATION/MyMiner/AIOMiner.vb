Imports System.Management
Imports System.ComponentModel
Imports System.Configuration
Imports OpenHardwareMonitor.Hardware
Imports AIOminer.CoinMining
Imports AIOminer.GPU
Imports AIOminer.Log
Imports AIOminer.Email
Imports AIOminer.General_Utils
Imports AIOminer.JSON_Utils
Imports AIOminer.MinerApi
Imports AIOminer.whattomine_line
Imports AIOminer.AIOMinerWebAPI
Imports System.Drawing.Drawing2D
Imports System.Net
Imports System.IO
Imports System.Text.RegularExpressions
Imports Newtonsoft.Json
Imports System.ServiceProcess

Public Class AIOMiner
    Public Mining = ""
    Public ShouldBeMining As String
    Public TodaysDate As Date = Date.Today
    Public LoadedMain As Boolean = False
    Public RebootTime As Integer = 0
    Public RestartMiningTime As Integer = 0

    'GPU Stats List
    Private rtnlist As New List(Of ListViewItem)()
    Private inFormLoad As Boolean
    Private My_BgWorker As BackgroundWorker = New BackgroundWorker() With {.WorkerSupportsCancellation = True}  'Marquee
    Private My_BgWorker2 As BackgroundWorker = New BackgroundWorker() With {.WorkerSupportsCancellation = True} 'GPU Information
    Private My_BgWorker3 As BackgroundWorker = New BackgroundWorker() With {.WorkerSupportsCancellation = True} 'Really GPU Info
    Private My_BgWorker4 As BackgroundWorker = New BackgroundWorker() With {.WorkerSupportsCancellation = True} 'Timed Mining
    Private My_BgWorker5 As BackgroundWorker = New BackgroundWorker() With {.WorkerSupportsCancellation = True} 'Update Checker
    Private My_BgWorkerMonitor As BackgroundWorker = New BackgroundWorker() With {.WorkerSupportsCancellation = True}   ' Monitor
    Private My_BgWorkerGpuStatsApi As BackgroundWorker = New BackgroundWorker() With {.WorkerSupportsCancellation = True}   ' update api with our gpu stats   15sec cycle
    Private My_BgWorkerGetMyWorkApi As BackgroundWorker = New BackgroundWorker() With {.WorkerSupportsCancellation = True}  ' get my work to do from web user
    Private My_Bg_DebugMining As BackgroundWorker = New BackgroundWorker() With {.WorkerSupportsCancellation = True}
    ' Debug Mining
    Private Monitor_running As Boolean = False
    Private rebootcount As Integer = 5

    Public stopjob As New Boolean
    Public IdelTIME As Int16
    Private Declare Function GetLastInputInfo Lib "user32.dll" (ByRef inputStructure As inputInfo) As Boolean
    Private Structure inputInfo
        Dim structSize As Int32
        Dim tickCount As Int32
    End Structure
    Private info As inputInfo
    Dim firstTick As Int32 = -1337
    Dim lastTick As Int32

    Private Sub Systems_Check()
        ' Windows Update
        Try
            For Each s As ServiceController In ServiceController.GetServices()
                If s.ServiceName = "wuauserv" Then
                    If s.Status = ServiceControllerStatus.Running Then
                        LogUpdate("Systems Check: Windows Update Service is Enabled.  Review Window Settings/System Settings to adjust")
                        PubShared.WindowsUpdatePassed = True

                    Else
                        LogUpdate("Systems Check: Windows Update Service is Disabled.")
                        'MsgBox("windows update is not runnig")
                        PubShared.WindowsUpdatePassed = True
                    End If
                End If

            Next
        Catch ex As Exception
            LogUpdate(ex.Message)
            LogUpdate("Systems Check: Unable to verify Windows Service")
            PubShared.WindowsUpdatePassed = True
        End Try


        'Windows Defender
        Try

            Dim keyname As String = Application.StartupPath()
            Dim readValue As String
            readValue = My.Computer.Registry.GetValue("HKEY_LOCAL_MACHINE\Software\Microsoft\Windows Defender\Exclusions\Paths", keyname, Nothing)

            ' Dim regKey As Object = My.Computer.Registry.LocalMachine.OpenSubKey("Software\Microsoft\Windows Defender\Exclusions\Paths", True).GetValue(keyname)
            If readValue Is Nothing Then
                LogUpdate("Systems Check: Windows Defender is missing a whitelist. Review Window Settings/System Settings to adjust")
                PubShared.WindowsUpdatePassed = True

            Else
                LogUpdate("Systems Check: Windows Defender whitelist is correct.")
                PubShared.WindowsDefenderPassed = True
            End If
        Catch ex As Exception
            LogUpdate(ex.Message)
            LogUpdate("Systems Check: Unable to verify Windows Defender whitelist")
            PubShared.WindowsUpdatePassed = True
        End Try



        'DiskSpace
        Try
            Dim RETURNED_SPACE As String = ""
            RETURNED_SPACE = ReturnAvaliableDiskSpace()

            If RETURNED_SPACE <> "ERR" Then
                Try
                    If Convert.ToInt32(RETURNED_SPACE) < 1.5 Then
                        LogUpdate("Systems Check: Low Disk Space! (1.5GB Required) Review Window Settings/System Settings to adjust")
                        PubShared.HardDriveCheckPassed = True
                    Else
                        LogUpdate("Systems Check: Disk Space is Good!")
                        PubShared.HardDriveCheckPassed = True
                    End If
                Catch ex As Exception
                    LogUpdate("Systems Check: Unable to review disk space")
                    PubShared.HardDriveCheckPassed = True
                End Try
            Else
                LogUpdate("Systems Check: Unable to review disk space")
                PubShared.HardDriveCheckPassed = True
            End If

        Catch ex As Exception
            LogUpdate(ex.Message)
            LogUpdate("Systems Check: Unable to review disk space")
            PubShared.HardDriveCheckPassed = True
        End Try


        'VirtualMemory
        Try
            Dim RETURNED_VIRUTAL_MEMORY As Integer = 0
            RETURNED_VIRUTAL_MEMORY = Convert.ToInt32(ReturnVirtualMemory)
            Dim RETURNED_GB As String = ""
            RETURNED_GB = (RETURNED_VIRUTAL_MEMORY / 1024).ToString

            If PubShared.cardcount <= 1 Then
                'This usually doesn' matter with one card, do as you want
                LogUpdate("Systems Check: Virtual Memory is good!")
                PubShared.VirtualMemoryPassed = True
            Else

                If RETURNED_VIRUTAL_MEMORY < 15000 Then
                    LogUpdate("Systems Check: Virtual Memory should be increased for Ethash mining, Review Window Settings/System Settings to adjust ")
                    PubShared.VirtualMemoryPassed = True

                Else
                    LogUpdate("Systems Check: Virtual Memory is good for Ethash mining!")
                    PubShared.VirtualMemoryPassed = True
                End If


                If RETURNED_VIRUTAL_MEMORY < 31000 Then
                    LogUpdate("Systems Check: Virtual Memory should be increased for Cryptonight* mining, Review Window Settings/System Settings to adjust ")
                    PubShared.VirtualMemoryPassed = True
                Else
                    LogUpdate("Systems Check: Virtual Memory is good for Cryptonight mining, Review Window Settings/System Settings to adjust ")
                    PubShared.VirtualMemoryPassed = True
                End If
            End If
        Catch ex As Exception
            LogUpdate(ex.Message)
            LogUpdate("Systems Check: Unable to review Virtual Memory")
            PubShared.VirtualMemoryPassed = True
        End Try

    End Sub
    Private Function GetMyIP() As IPAddress



        Try
            Dim outputIP As IPAddress
            Using wClient As New WebClient
                Dim dls As String = wClient.DownloadString("http://www.ip-adress.com/")
                Dim myIP As String = Regex.Match(dls, "(?<=<h1>What Is My IP Address\? Your IP address is: <strong>)[0-9.]*?(?=<\/strong><\/h1>)", RegexOptions.Compiled).Value
                outputIP = IPAddress.Parse(myIP)
            End Using
            Return outputIP
        Catch ex As Exception

        End Try


    End Function

    Private Sub MyMiner_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' 2-5-2019 - Change Profit's time from 180 to be 3 minutes 
        ' 2-5-2019 - Add CPU power to the power useage



        Dim appPath As String = Application.StartupPath()
        Timer2.Start()

        If ReturnAIOsetting("apienabled") = "True" Then

            PubShared.ShouldBeUpdatingToApi = True
        Else
            PubShared.ShouldBeUpdatingToApi = False

        End If

        Try
            PubShared.DTime_Started = DateTime.Now
            PubShared.WTM_Check_Tick = ReturnAIOsetting("WhatToMineUpdate")
            PubShared.ApiKey = ReturnAIOsetting("apikey")
            PubShared.uptime = "0"
            PubShared.miner_uptime = "0"
            PubShared.powercosts = ReturnAIOsetting("powercosts")
        Catch ex As Exception

        End Try

        'Setup Any Form changes
        Button1.BackgroundImage = My.Resources.Resources.START
        PricesLBL.BackColor = Color.Transparent
        inFormLoad = True


#If DEBUG Then
        Timer4.Interval = 10000
#End If

        PubShared.LogsListbox = Me.LogsListbox


        ListView1.View = View.Details
        GpuStatList.View = View.Details


        'Try
        '    ' get ip
        '    PubShared.systemsIP = GetMyIP().ToString()
        'Catch ex As Exception
        '    PubShared.systemsIP = "0.0.0.0"
        'End Try

        'This does things

        If ReturnAIOsetting("marquee") = "True" Then
            Marquee.Start()
            PricesLBL.Visible = True
        Else
            PricesLBL.Visible = False
        End If




        'Add Donation to the combo list
        If ComboBox1.Items.Contains("Donate") Then
        Else
            ComboBox1.Items.Add("Donate")
        End If

        PubShared.cardcount = 0

        MinerInstances.RunningMiners = New List(Of Process)()

        'Check for previous instance
        If Process.GetProcessesByName(Process.GetCurrentProcess.ProcessName).Length > 1 Then
            MsgBox("You may only have one instance running at a time!")
            Application.Exit()
        End If

        'Check for First Run
        ' Dim appPath As String = Application.StartupPath()
        Try
            If ReturnAIOsetting("firstrun") = "True" Then
            Else
                'Check if the user is not a normie
                'Don't store the value just if else, check every time
                Try
                    If checkPlanStatus() = True Then
                        PubShared.Subscriber = True
                        'Also don't send if invalid api


                    Else
                        'not a subscriber, show advertising
                        ' PubShared.AdvertiseShowing = True

                        'Advertising.Show()

                        While PubShared.AdvertiseShowing = True
                            Application.DoEvents()
                            Threading.Thread.Sleep(100)
                        End While
                    End If
                Catch ex As Exception
                    PubShared.ShouldBeUpdatingToApi = False
                End Try


            End If
        Catch ex As Exception
            LogUpdate(ex.Message, eLogLevel.Err)
        End Try

        ' set timed mining chkbox if needed
        Me.chkTimedMining.Checked = False
        PubShared.TimedMiningSettings = GetTimerSettingsJson()

        If PubShared.TimedMiningSettings IsNot Nothing AndAlso PubShared.TimedMiningSettings.Settings IsNot Nothing Then
            If PubShared.TimedMiningSettings.Settings(24).isOn.ToLower.Trim = "true" Then
                Me.chkTimedMining.Checked = True
                PubShared.TimedMining = True
                LogUpdate("Timed Mining in effect ...")
                Timer4.Enabled = True

            End If
        End If


        'Get Version
        ToolStripStatusLabel1.Text = PubShared.Version

        PubShared.amdpwr = 0
        'Get list of video cards
        Dim GraphicsCardName As String
        Dim i As Int64
        'Use an API to get this crap, tired of seeing it hard coded
        'Generate the list of GPU names, send in a request and get back the answer
        'We will need to change the way PubShared.CARDNAME is done.  
        '
        Try
            i = 0
            Dim WmiSelect As New ManagementObjectSearcher("Select * FROM Win32_VideoController")
            For Each WmiResults As ManagementObject In WmiSelect.Get()
                GraphicsCardName = WmiResults.GetPropertyValue("Name").ToString
                'Get the lits of NVIDIA CARDS
                If GraphicsCardName.ToString.ToUpper.Contains("NVIDIA") Then
                    LogUpdate("I found an " + GraphicsCardName + "     [" + i.ToString + "]" + "-" + DateAndTime.Now)
                    If GraphicsCardName.ToString.ToUpper.Contains("2070") Then
                        PubShared.twenty70 += 1
                    ElseIf GraphicsCardName.ToString.ToUpper.Contains("1050 TI") Then
                        PubShared.ten50ti += 1
                    ElseIf GraphicsCardName.ToString.ToUpper.Contains("1060") Then
                        PubShared.ten60 += 1
                    ElseIf GraphicsCardName.ToString.ToUpper.Contains("1070 TI") Then
                        PubShared.ten70ti += 1
                    ElseIf GraphicsCardName.ToString.ToUpper.Contains("1070") Then
                        PubShared.ten70 += 1
                    ElseIf GraphicsCardName.ToString.ToUpper.Contains("1080 TI") Then
                        PubShared.ten80ti += 1
                    ElseIf GraphicsCardName.ToString.ToUpper.Contains("1080") Then
                        PubShared.ten80 += 1
                    ElseIf GraphicsCardName.ToString.ToUpper.Contains("2060") Then
                        PubShared.twenty60 += 1
                    ElseIf GraphicsCardName.ToString.ToUpper.Contains("2080 TI") Then
                        PubShared.twenty80 += 1
                    ElseIf GraphicsCardName.ToString.ToUpper.Contains("2080") Then
                        PubShared.twenty80ti += 1
                    ElseIf GraphicsCardName.ToString.ToUpper.Contains("1660 TI") Then
                        PubShared.sixteen60ti += 1
                    ElseIf GraphicsCardName.ToString.ToUpper.Contains("1660") Then
                        PubShared.sixteen60 += 1
                    Else
                        'We have no idea, not current supported by what to mine, but we will assume the worst
                        PubShared.seven50ti += 1
                        LogUpdate("Profitability is a bit off, unknown card. It's ok.  Just Est. Profits will be super high or super low!")
                    End If
                    i = i + 1
                    PubShared.cardcount = PubShared.cardcount + 1
                    PubShared.nvidia = True
                End If



                'Get the list of AMD CARDS
                If GraphicsCardName.ToString.ToUpper.Contains("RADEON") Then
                    LogUpdate("I found an " + GraphicsCardName + "     [" + i.ToString + "]" + "-" + DateAndTime.Now)
                    If GraphicsCardName.ToString.ToUpper.Contains("590") Then
                        PubShared.amdpwr = PubShared.amdpwr + 175
                        PubShared.five80 += 1
                    ElseIf GraphicsCardName.ToString.ToUpper.Contains("580") Then
                        PubShared.amdpwr = PubShared.amdpwr + 160
                        PubShared.five80 += 1
                    ElseIf GraphicsCardName.ToString.ToUpper.Contains("VEGA") Then
                        PubShared.amdpwr = PubShared.amdpwr + 250
                        PubShared.vega += 1
                    ElseIf GraphicsCardName.ToString.ToUpper.Contains("570") Then
                        PubShared.amdpwr = PubShared.amdpwr + 160
                        PubShared.five70 += 1
                    ElseIf GraphicsCardName.ToString.ToUpper.Contains("480") Then
                        PubShared.amdpwr = PubShared.amdpwr + 145
                        PubShared.four80 += 1
                    ElseIf GraphicsCardName.ToString.ToUpper.Contains("470") Then
                        PubShared.amdpwr = PubShared.amdpwr + 120
                        PubShared.four70 += 1
                    ElseIf GraphicsCardName.ToString.ToUpper.Contains("280") Then
                        PubShared.amdpwr = PubShared.amdpwr + 220
                        PubShared.two80 += 1
                    ElseIf GraphicsCardName.ToString.ToUpper.Contains("380") Then
                        PubShared.amdpwr = PubShared.amdpwr + 135
                        PubShared.three80 += 1
                    ElseIf GraphicsCardName.ToString.ToUpper.Contains("FURY") Then
                        PubShared.amdpwr = PubShared.amdpwr + 350
                        PubShared.fury += 1
                    ElseIf GraphicsCardName.ToString.ToUpper.Contains("VII") Then
                        PubShared.amdpwr = PubShared.amdpwr + 230
                        PubShared.vii += 1
                    Else
                        'We have no idea, not current supported by what to mine, but we will assume the worst
                        PubShared.amdpwr = PubShared.amdpwr + 220
                        PubShared.two80 += 1
                        LogUpdate("Profitability is a bit off, unknown card. It's ok.  Just Est. Profits will be super high or super low!")
                    End If
                    i = i + 1
                    PubShared.cardcount = PubShared.cardcount + 1
                    PubShared.amd = True
                End If
            Next

        Catch err As ManagementException
            LogUpdate(err.Message)
            LogUpdate("You might be able To mine, Not 100%, Seems you have older cards...Good Luck Bud!")
        End Try


        ''Get CPU Information
        'Try

        '    Dim MyOBJ As Object
        '    Dim cpu As Object
        '    MyOBJ = GetObject("WinMgmts:").instancesof("Win32_Processor")
        '    For Each cpu In MyOBJ
        '        Return cpu.Name.ToString
        '        Exit For
        '    Next
        'Catch ex As Exception
        '    Return ""


        'End Try


        Try
            CheckWhatToMine()
        Catch ex As Exception
            LogUpdate(ex.Message)
        End Try



        'Set GPU count (6/7/2018)
        GroupBox4.Text = "GPU Stats:" & i.ToString & " Gpu/s Found"

        'Let them know on power draw if amd
        If PubShared.amd = True Then
            LogUpdate("Note, Power for AMD Is just an estimate at this time")
        End If

        'See if they have both AMD and NVIDIA
        If PubShared.amd = True AndAlso PubShared.nvidia = "True" Then
            LogUpdate("TEAM RED/GREEN.  GOOD LUCK!")
        End If

        'Load up coin list



        RefreshCoinlist()

        'Setup whatever was last mined
        If ReturnAIOsetting("lastmined") = "" Then
        Else
            ComboBox1.Text = ReturnAIOsetting("lastmined")
        End If

        'Setup for pollings
        AddHandler My_BgWorker.DoWork, AddressOf My_BgWorker_DoWork

        AddHandler My_BgWorker5.DoWork, AddressOf My_BgWorker5_DoWork
        AddHandler My_BgWorker5.ProgressChanged, AddressOf My_BgWorker5_Progress

        AddHandler My_BgWorker2.DoWork, AddressOf My_BgWorker2_DoWork
        AddHandler My_BgWorker2.ProgressChanged, AddressOf My_BgWorker2_Progress

        AddHandler My_BgWorker3.DoWork, AddressOf My_BgWorker3_DoWork
        AddHandler My_BgWorker3.ProgressChanged, AddressOf My_BgWorker3_Progress

        AddHandler My_BgWorkerGpuStatsApi.DoWork, AddressOf My_BgWorkerGpuStatsApi_DoWork
        AddHandler My_BgWorkerGpuStatsApi.ProgressChanged, AddressOf My_BgWorkerGpuStatsApi_Progess

        AddHandler My_BgWorkerGetMyWorkApi.DoWork, AddressOf My_BgWorkerGetMyWorkApi_DoWork
        AddHandler My_BgWorkerGetMyWorkApi.ProgressChanged, AddressOf My_BgWorkerGetMyWorkApi_Progress

        AddHandler My_BgWorkerMonitor.DoWork, AddressOf My_BgWorkerMonitor_DoWork
        AddHandler My_BgWorkerMonitor.ProgressChanged, AddressOf My_BgWorkerMonitor_Progress

        AddHandler My_BgWorker4.DoWork, AddressOf My_BgWorker4_DoWork
        AddHandler My_BgWorker4.ProgressChanged, AddressOf My_BgWorker4_Progress

        AddHandler My_Bg_DebugMining.DoWork, AddressOf My_Bg_DebugMining_DoWork


        'Future us will clean this
        ''Allow progress for the BgWorker2 (This is used to pull data from GPU's for GPUList)
        My_BgWorker2.WorkerReportsProgress = True
        My_BgWorker3.WorkerReportsProgress = True
        My_BgWorker4.WorkerReportsProgress = True
        My_BgWorker5.WorkerReportsProgress = True
        My_BgWorkerGpuStatsApi.WorkerReportsProgress = True
        My_BgWorkerGetMyWorkApi.WorkerReportsProgress = True

        'My_Bg_DebugMining.WorkerReportsProgress = True




        'Make sure we see atleast one card!
        If PubShared.cardcount >= 1 Then
            My_BgWorker.RunWorkerAsync()
            My_BgWorker2.RunWorkerAsync()
            My_BgWorker3.RunWorkerAsync()
            My_Bg_DebugMining.RunWorkerAsync()

        Else
            'Send an alert
            Dim hostname As String = Environment.MachineName
            hostname = hostname.Replace(" ", "")
            hostname = hostname.Replace(".", "")
            'alert_send(hostname, ReturnAIOsetting("worker"), "   Funny Story, I just started but I can't find any GPU's. Send Help! " & Now.TimeOfDay.ToString)
        End If

        'Check for Updates

        My_BgWorker5.RunWorkerAsync()

        'Don't allow blanks
        If ComboBox1.Text = "" Then
            ComboBox1.Text = "Donate"
        End If

        'Check for topmost
        If ReturnAIOsetting("ontop") = "True" Then
            OnTopChkBx.Checked = True
        Else
            OnTopChkBx.Checked = False
            Me.TopMost = False
        End If

        'If is not blank and autostart is checked, click the button
        If ReturnAIOsetting("automine") = "True" Then
            'Check to see if the schedule is enabled first
            If ReturnAIOsetting("sch") = "False" Then
                ComboBox1.Text = ReturnAIOsetting("lastmined")
            Else
                ComboBox1.Text = WhatToMineToday()
            End If
            'Click on Start
            ' if timed mining does not override
            If TimerSettings.OkToMine(PubShared.TimedMiningSettings) Then
                Button1.PerformClick()
            Else
                LogUpdate("Timed mining is enabled and current hour is not selected so Auto mining will NOT start.")
            End If

            'Enable AutoMine checkmark
            CheckBox1.Checked = True
        End If

        'How long do you wait for Idel time?
        Try


            IdelTIME = ReturnAIOsetting("ideltmr")
        Catch ex As Exception

        End Try

        'Check if Idele mining is checked
        If ReturnAIOsetting("idel") = "True" Then
            'Start the process of checking for IDELE-ISMS
            IdeleCheckBox.Checked = True
            'Disable AutoMine
            'Disable 
        Else
            'Play it cool man
            IdeleCheckBox.Checked = False
        End If

        'Check if SCH was setup
        If ReturnAIOsetting("sch") = "True" Then
            CheckBox3.Checked = True
            'Timer2.Interval = 1000
        End If

        'Get Profitability
        Dim webstring As String = GetWTMString()


        ' Check for whattomine here





        'Check if restart rig is enabled
        If ReturnAIOsetting("restartrig") = "True" Then
            PubShared.AIORestartRig = True
        End If

        'Check if restart mining is enabled
        If ReturnAIOsetting("restartmining") = "True" Then
            PubShared.AIORestartMining = True
        End If

        'DEV TEST
        ' PubShared.cardcount = 4

        inFormLoad = False
        LoadedMain = True


        ''Check if dontaskwebsite = False and APIkey is blank
        'If ReturnAIOsetting("dontaskwebsite") = "False" Then
        '	If ReturnAIOsetting("apikey") = "" Then
        '		Dim l As New NewAPI
        '		l.Show()
        '	End If
        'End If


        Systems_Check()

        'Enable VERSION
        ToolStripStatusLabel1.Visible = True


        'After everything has passed make a backup of the AIOSettings file
        Try
            If System.IO.File.Exists(appPath & "\Settings\Backups\AIOSettings.json") Then
                'Purge old file
                System.IO.File.Delete(appPath & "\Settings\Backups\AIOSettings.json")
                System.IO.File.Copy(appPath & "\Settings\AIOSettings.json", appPath & "\Settings\Backups\AIOSettings.json")
                LogUpdate("Backup of AIOSettings Complete!")
            Else
                System.IO.File.Copy(appPath & "\Settings\AIOSettings.json", appPath & "\Settings\Backups\AIOSettings.json")
                LogUpdate("Backup of AIOSettings Complete!")
            End If
        Catch ex As Exception

        End Try

    End Sub

    Public Function sortlitems(ByVal Items As List(Of ListViewItem)) As List(Of ListViewItem)
        Try


            Dim listzzzz As New List(Of ListViewItem)
            Dim lol As ListViewItem

            Dim a(4) As Integer
            Dim p(4) As Double


            For i As Short = 0 To Items.Count - 1
                a(i) = i
                p(i) = CDbl(Items(i).SubItems(4).Text.Replace("$", "").Trim)
            Next

            Array.Sort(p, a)

            For J As Short = Items.Count - 1 To 0 Step -1

                lol = Items(a(J))

                listzzzz.Add(lol)
            Next

            Return listzzzz
        Catch ex As Exception

        End Try


    End Function

    Public Sub CheckWhatToMine()
        '---------------------WHATTOMINE.COM------------------
        Try


            TodaysDate = Date.Today

            'Dim iitems As List(Of ListViewItem)
            ListView1.Items.Clear()
            ' get the 5 top from whattomine web site and fill in the listview
            Dim lItems As List(Of ListViewItem) = GetCoinAlgoFromWeb(GetWTMString())

            lItems = sortlitems(lItems)

            GroupBox3.Text = "Top 5 Coins to Mine Last Checked:" & Date.Now
            For Each litem As ListViewItem In lItems
                If ListView1.Items.Count >= 5 Then
                Else
                    ListView1.Items.Add(litem)
                End If


            Next
        Catch ex As Exception
            LogUpdate(ex.Message)

        End Try
    End Sub

    Private Sub My_BgWorker5_DoWork(ByVal sender As Object, ByVal e As System.ComponentModel.DoWorkEventArgs)
        Dim worker As BackgroundWorker = CType(sender, BackgroundWorker)

        If worker.CancellationPending Then
            e.Cancel = True
        Else
            'Check for updates every 30 minutes
            If ReturnAIOsetting("firstrun") = "True" Then
            Else


                For i As Integer = 0 To 1 Step 0


                    Try
                        'Slip in online status
                        Try
                            If AIOMinerWebAPI.checkApiOnline() Then

                                Dim request As WebRequest = WebRequest.Create(PubShared.API_LOCATION)
                                request.Method = "GET"
                                Dim response As WebResponse = request.GetResponse()
                                Dim inputstream1 As Stream = response.GetResponseStream()
                                Dim reader As New StreamReader(inputstream1)
                                Dim workspace As String = reader.ReadToEnd
                                inputstream1.Dispose()
                                reader.Close()
                                '
                                If workspace.Contains("OK!") Then

                                    Me.Text = "AIOMiner - API Online"
                                Else
                                    Me.Text = "AIOMiner - API Offline"

                                End If
                            End If



                        Catch ex As Exception

                        End Try


                        If PubShared.ackupdates = False Then

                            'Check if Version is Good
                            Try
                                SetAllowUnsafeHeaderParsing20()
                                Dim VERResults As String
                                Dim address As String = PubShared.WEB_VERSION
                                Dim client As WebClient = New WebClient()
                                Dim reader As StreamReader = New StreamReader(client.OpenRead(address))
                                VERResults = reader.ReadToEnd
                                'MsgBox(VERResults)
                                If VERResults = "" Then
                                    LogUpdate("Unable to check for version updates, check network", eLogLevel.Err)
                                Else
                                    If PubShared.Version = VERResults.Trim Then
                                    Else
                                        worker.ReportProgress(50)
                                    End If
                                End If

                            Catch ex As Exception

                            End Try


                            'Check for a Miners Update
                            Try
                                SetAllowUnsafeHeaderParsing20()
                                Dim VERResults As String
                                Dim address As String = PubShared.WEB_MINER_VERSION
                                Dim client As WebClient = New WebClient()
                                Dim reader As StreamReader = New StreamReader(client.OpenRead(address))
                                VERResults = reader.ReadToEnd
                                'Check to see if VERResults returned anything, if not, might have an issue but don't display the update window
                                If VERResults = "" Then
                                    LogUpdate("Unable to check for miner version updates, check network", eLogLevel.Err)
                                Else
                                    Dim minerver As String = ReturnAIOsetting("minerversion")
                                    If minerver.Trim() = VERResults.Trim() Then
                                    Else
                                        worker.ReportProgress(49)
                                    End If
                                End If
                            Catch ex As Exception

                            End Try

                        End If
                        System.Threading.Thread.Sleep(1800000) '30 Minutes
                    Catch ex As Exception

                    End Try
                Next
            End If
        End If

    End Sub
    Public Sub My_Bg_DebugMining_DoWork(ByVal sender As Object, ByVal e As System.ComponentModel.DoWorkEventArgs)
        Try
            For i As Integer = 0 To 1 Step 0
                If PubShared.DebugMiner = "" Then
                Else
                    LogsListbox.Items.Add(PubShared.DebugMiner)
                End If

                System.Threading.Thread.Sleep(1000)
            Next
        Catch ex As Exception

        End Try
    End Sub
    Private Sub My_BgWorker5_Progress(ByVal sender As Object, ByVal e As ProgressChangedEventArgs)

        Try
            If e.ProgressPercentage = 50 Then  ' Update for AIO
                TextBox1.Visible = True
                TextBox2.Visible = True
                TextBox1.Text = "An update for AIOMiner is available!  Click Here!"

            ElseIf e.ProgressPercentage = 49 Then        ' miner failed so redx it
                TextBox1.Visible = True
                TextBox2.Visible = True
                TextBox1.Text = "A new Miner application is available! Click Here!"

            ElseIf e.ProgressPercentage = 100 Then      ' all done so cleanup

            End If
        Catch ex As Exception
            LogUpdate("Error updating status bar via progress event of monitor worker...." & ex.Message)
        End Try


    End Sub
    Private Sub My_BgWorker_DoWork(ByVal sender As Object, ByVal e As System.ComponentModel.DoWorkEventArgs)
        System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = False


        Dim worker As BackgroundWorker = CType(sender, BackgroundWorker)
        If worker.CancellationPending Then
            e.Cancel = True
        Else
            'Check if you should be ontop


            'Set Maruqee

            'Should of just made this a list vs doing this crap, will clean up later
            Dim coin1 As String
            Dim coin2 As String
            Dim coin3 As String
            Dim coin4 As String
            Dim coin5 As String
            Dim coin6 As String
            Dim coin7 As String
            Try
                For i As Integer = 0 To 1 Step 0
                    If ReturnAIOsetting("marquee") = "True" Then
                        PricesLBL.Font = New Font("Verdana", CInt(ReturnAIOsetting("marqueesize")),
                        FontStyle.Bold)


                        coin1 = ReturnAIOsetting("coinprice1")
                        coin2 = ReturnAIOsetting("coinprice2")
                        coin3 = ReturnAIOsetting("coinprice3")
                        coin4 = ReturnAIOsetting("coinprice4")
                        coin5 = ReturnAIOsetting("coinprice5")
                        coin6 = ReturnAIOsetting("coinprice6")
                        coin7 = ReturnAIOsetting("coinprice7")
                        'PricesLBL.BackColor = Color.Black
                        'PricesLBL.ForeColor = Color.Green

                        PubShared.coinprice = coin1 & ":" & UpdateCoins(coin1) & "     " &
                                  coin2 & ":" & UpdateCoins(coin2) & "     " &
                                  coin3 & ":" & UpdateCoins(coin3) & "     " &
                                  coin4 & ":" & UpdateCoins(coin4) & "     " &
                                  coin5 & ":" & UpdateCoins(coin5) & "     " &
                                  coin6 & ":" & UpdateCoins(coin6) & "     " &
                                  coin7 & ":" & UpdateCoins(coin7)

                        PubShared.coinprice = PubShared.coinprice.Replace(" :", "")

                    Else
                        PubShared.coinprice = ""
                    End If

                    System.Threading.Thread.Sleep(300000)
                Next

                'Label6.Text = "Vertcoin  " + UpdateCoins("VTC")
                '        Label7.Text = "ZCash " + UpdateCoins("ZEC")
                '        Label8.Text = "Komodo " + UpdateCoins("KMD")
                '        Label9.Text = "Mona " + UpdateCoins("MONA")
                '        Label10.Text = "ZenCash " + UpdateCoins("ZEN")
                '        Label11.Text = "BitcoinZ " + UpdateCoins("BTCZ")
                '        Label15.Text = "Bitcoin " + UpdateCoins("BTC")
                '        Label16.Text = "Etherum " + UpdateCoins("ETH")
                '        GroupBox3.Text = "Coin Prices -  Last Updated" & DateTime.Now
                '        System.Threading.Thread.Sleep(300000)
                '    Next
            Catch ex As Exception

            End Try
        End If

    End Sub

    Private Sub My_BgWorkerGpuStatsApi_Progess(ByVal sender As Object, ByVal e As ProgressChangedEventArgs)

    End Sub


    Private Sub My_BgWorker2_Progress(ByVal sender As Object, ByVal e As ProgressChangedEventArgs)
        ' Dim worker As BackgroundWorker = CType(sender, BackgroundWorker)
        Try
            If e.ProgressPercentage = 50 Then           'Update the GPUStatsList
                GpuStatList.Items.Clear()

                If PubShared.GpuStatus = True Then
                    GpuStats.ListView1.Items.Clear()
                End If

                Dim i As Integer = 0
                For Each litem As ListViewItem In rtnlist
                    GpuStatList.Items.Add(litem)
                    If PubShared.GpuStatus = True Then
                        GpuStats.ListView1.Items.Add(GpuStatList.Items(i).Clone)
                    Else

                    End If
                    i = i + 1
                Next

                'Get CPUs also
                'Dim ii As Integer = 0
                'For Each llitem As ListViewItem In rtnlist

                'Next



                PubShared.GpuListView = GpuStatList

            ElseIf e.ProgressPercentage = 1 Then        ' Stop mining for Donations
                Do Until 1 = 2
                    Application.DoEvents()
                    If Button1.Text.ToLower = "stop" Then
                        Button1.PerformClick()
                        Exit Do
                    End If

                    Threading.Thread.Sleep(500)
                Loop



            ElseIf e.ProgressPercentage = 2 Then        ' Start mining for Donations

                Do Until 1 = 2
                    Application.DoEvents()
                    If Button1.Text.ToLower = "start" Then
                        ComboBox1.Text = "Donate"
                        Button1.PerformClick()
                        Exit Do
                    End If
                    Threading.Thread.Sleep(500)
                Loop

            ElseIf e.ProgressPercentage = 3 Then        ' Start mining back to the users account
                KillAllMiningApps()

                Do Until Button1.Text.ToLower = "start"
                    Application.DoEvents()
                    If Button1.Text.ToLower = "stop" Then
                        Button1.PerformClick()
                    End If
                    Threading.Thread.Sleep(500)
                Loop


                Do Until 1 = 2
                    Application.DoEvents()
                    If Button1.Text.ToLower = "start" Then
                        ComboBox1.Text = PubShared.donationPreviousCoin
                        'Change it no matter what
                        Do Until ComboBox1.Text.ToLower <> "donate"
                            Application.DoEvents()
                            If ComboBox1.Text = "Donate" Then
                                ComboBox1.Text = PubShared.donationPreviousCoin
                                Exit Do
                            End If
                            Threading.Thread.Sleep(500)
                        Loop
                        Button1.PerformClick()
                        Exit Do
                    End If
                    Threading.Thread.Sleep(500)
                Loop

                'tiny dancer

            ElseIf e.ProgressPercentage = 4 Then        '  Auto Restart Rig
                'Confirm that it is still enabled 
                If PubShared.AIORestartRig = True Then
                    'PubShared.AIORestartRigtime = ReturnAIOsetting("restartrigtime")
                    Dim HeDidTheMath As String = ReturnAIOsetting("restartrigtime")
                    Try
                        If HeDidTheMath.Contains("12") Then
                            '60 * 12 = 720 * 60 = 43,200 seconds
                            PubShared.AIORestartRigtime = 43200
                        ElseIf HeDidTheMath.Contains("24") Then
                            '60 * 24 = 1440 * 60 = 86,400 seconds
                            PubShared.AIORestartRigtime = 86400
                        ElseIf HeDidTheMath.Contains("3") Then
                            PubShared.AIORestartRigtime = 10800
                        ElseIf HeDidTheMath.Contains("6") Then
                            PubShared.AIORestartRigtime = 21600
                        ElseIf HeDidTheMath.Contains("1") Then
                            PubShared.AIORestartRigtime = 3600
                        End If

                    Catch ex As Exception
                        PubShared.AIORestartRig = False
                        LogUpdate("Unable to process the time for restart rig, contact support", eLogLevel.Err)
                    End Try

                    AutoRestartLBL.Text = "Executing Flying Potato Engine"
                    AutoRestartLBL.Visible = True
                    RestartRigTMR.Interval = 1000
                    RestartRigTMR.Start()
                End If



            ElseIf e.ProgressPercentage = 5 Then        '  Auto Restart Mining
                'Confirm that it is still enabled 
                If PubShared.AIORestartMining = True Then
                    'PubShared.AIORestartRigtime = ReturnAIOsetting("restartrigtime")
                    Dim HeDidTheMath As String = ReturnAIOsetting("restartminingtime")
                    Try
                        If HeDidTheMath.Contains("1 Hour") Then
                            PubShared.AIORestartMiningtime = 3600 '3600
                        ElseIf HeDidTheMath.Contains("3 Hours") Then
                            PubShared.AIORestartMiningtime = 10800
                        ElseIf HeDidTheMath.Contains("6 Hours") Then
                            PubShared.AIORestartMiningtime = 21600
                        ElseIf HeDidTheMath.Contains("12 Hours") Then
                            PubShared.AIORestartMiningtime = 43200
                        ElseIf HeDidTheMath.Contains("24 Hours") Then
                            PubShared.AIORestartMiningtime = 86400
                        End If

                    Catch ex As Exception
                        PubShared.AIORestartMining = False
                        LogUpdate("Unable to process the time for restart mining, contact support", eLogLevel.Err)
                    End Try

                    AutoRestartMiningLBL.Text = "Executing LemonParty Search Unit"
                    AutoRestartMiningLBL.Visible = True
                    RestartMiningTMR.Interval = 1000
                    RestartMiningTMR.Start()
                End If

            ElseIf e.ProgressPercentage = 100 Then      ' all done so cleanup

            End If
        Catch ex As Exception
            LogUpdate("Error updating GpuStatList " & ex.Message)
        End Try
    End Sub
    Private Sub My_BgWorker3_Progress(ByVal sender As Object, ByVal e As ProgressChangedEventArgs)
        ' Dim worker As BackgroundWorker = CType(sender, BackgroundWorker)
        Try
            If e.ProgressPercentage = 50 Then           'Update the GPUStatsList
                GpuStatList.Items.Clear()

                If PubShared.GpuStatus = True Then
                    GpuStats.ListView1.Items.Clear()
                End If

                Dim i As Integer = 0
                For Each litem As ListViewItem In rtnlist
                    GpuStatList.Items.Add(litem)
                    If PubShared.GpuStatus = True Then
                        GpuStats.ListView1.Items.Add(GpuStatList.Items(i).Clone)
                    Else

                    End If
                    i = i + 1
                Next

                PubShared.GpuListView = GpuStatList

            ElseIf e.ProgressPercentage = 1 Then        ' Stop mining for Donations
                Do Until 1 = 2
                    Application.DoEvents()
                    If Button1.Text.ToLower = "stop" Then
                        Button1.PerformClick()
                        Exit Do
                    End If

                    Threading.Thread.Sleep(500)
                Loop



            ElseIf e.ProgressPercentage = 2 Then        ' Start mining for Donations

                Do Until 1 = 2
                    Application.DoEvents()
                    If Button1.Text.ToLower = "start" Then
                        ComboBox1.Text = "Donate"
                        Button1.PerformClick()
                        Exit Do
                    End If
                    Threading.Thread.Sleep(500)
                Loop

            ElseIf e.ProgressPercentage = 3 Then        ' Start mining back to the users account
                KillAllMiningApps()

                Do Until Button1.Text.ToLower = "start"
                    Application.DoEvents()
                    If Button1.Text.ToLower = "stop" Then
                        Button1.PerformClick()
                    End If
                    Threading.Thread.Sleep(500)
                Loop


                Do Until 1 = 2
                    Application.DoEvents()
                    If Button1.Text.ToLower = "start" Then
                        ComboBox1.Text = PubShared.donationPreviousCoin
                        'Change it no matter what
                        Do Until ComboBox1.Text.ToLower <> "donate"
                            Application.DoEvents()
                            If ComboBox1.Text = "Donate" Then
                                ComboBox1.Text = PubShared.donationPreviousCoin
                                Exit Do
                            End If
                            Threading.Thread.Sleep(500)
                        Loop
                        Button1.PerformClick()
                        Exit Do
                    End If
                    Threading.Thread.Sleep(500)
                Loop

                'tiny dancer

            ElseIf e.ProgressPercentage = 4 Then        '  Auto Restart Rig
                'Confirm that it is still enabled 
                If PubShared.AIORestartRig = True Then
                    'PubShared.AIORestartRigtime = ReturnAIOsetting("restartrigtime")
                    Dim HeDidTheMath As String = ReturnAIOsetting("restartrigtime")
                    Try
                        If HeDidTheMath.Contains("12") Then
                            '60 * 12 = 720 * 60 = 43,200 seconds
                            PubShared.AIORestartRigtime = 43200
                        ElseIf HeDidTheMath.Contains("24") Then
                            '60 * 24 = 1440 * 60 = 86,400 seconds
                            PubShared.AIORestartRigtime = 86400
                        ElseIf HeDidTheMath.Contains("1") Then
                            '60 * 24 = 1440 * 60 = 86,400 seconds
                            PubShared.AIORestartRigtime = 3600
                        ElseIf HeDidTheMath.Contains("3") Then
                            '60 * 24 = 1440 * 60 = 86,400 seconds
                            PubShared.AIORestartRigtime = 10800
                        ElseIf HeDidTheMath.Contains("6") Then
                            '60 * 24 = 1440 * 60 = 86,400 seconds
                            PubShared.AIORestartRigtime = 21600
                        End If

                    Catch ex As Exception
                        PubShared.AIORestartRig = False
                        LogUpdate("Unable to process the time for restart rig, contact support", eLogLevel.Err)
                    End Try

                    AutoRestartLBL.Text = "Executing Flying Potato Engine"
                    AutoRestartLBL.Visible = True
                    RestartRigTMR.Interval = 1000
                    RestartRigTMR.Start()
                End If



            ElseIf e.ProgressPercentage = 5 Then        '  Auto Restart Mining
                'Confirm that it is still enabled 
                If PubShared.AIORestartMining = True Then
                    'PubShared.AIORestartRigtime = ReturnAIOsetting("restartrigtime")
                    Dim HeDidTheMath As String = ReturnAIOsetting("restartminingtime")
                    Try
                        If HeDidTheMath.Contains("1 Hour") Then
                            PubShared.AIORestartMiningtime = 3600 '3600
                        ElseIf HeDidTheMath.Contains("3 Hours") Then
                            PubShared.AIORestartMiningtime = 10800
                        ElseIf HeDidTheMath.Contains("6 Hours") Then
                            PubShared.AIORestartMiningtime = 21600
                        ElseIf HeDidTheMath.Contains("12 Hours") Then
                            PubShared.AIORestartMiningtime = 43200
                        ElseIf HeDidTheMath.Contains("24 Hours") Then
                            PubShared.AIORestartMiningtime = 86400
                        End If

                    Catch ex As Exception
                        PubShared.AIORestartMining = False
                        LogUpdate("Unable to process the time for restart mining, contact support", eLogLevel.Err)
                    End Try

                    AutoRestartMiningLBL.Text = "Executing LemonParty Search Unit"
                    AutoRestartMiningLBL.Visible = True
                    RestartMiningTMR.Interval = 1000
                    RestartMiningTMR.Start()
                End If

            ElseIf e.ProgressPercentage = 100 Then      ' all done so cleanup

            End If
        Catch ex As Exception
            LogUpdate("Error updating GpuStatList " & ex.Message)
        End Try
    End Sub

    Public Sub My_BgWorkerMonitor_Progress(ByVal sender As Object, ByVal e As ProgressChangedEventArgs)
        '  Dim worker As BackgroundWorker = CType(sender, BackgroundWorker)

        ' handle updating UI from UI thread to stop issue of updating for bgworker thread directely
        Try
            If e.ProgressPercentage = 50 Then               ' all is ok
                tslblMonitorText.Text = "   Restart Countdown " & rebootcount.ToString()
                tslblMonitorImage.Image = My.Resources.Resources.AIO_GREEN

            ElseIf e.ProgressPercentage = 0 Then        ' miner failed so redx it
                tslblMonitorText.Text = "   Restart Countdown " & rebootcount.ToString()
                tslblMonitorImage.Image = My.Resources.Resources.AIO_RED

            ElseIf e.ProgressPercentage = 100 Then      ' all done so cleanup
                Label8.Text = "0:0:0"
                tslblMonitorImage.Image = Nothing
                tslblMonitorText.Text = ""
            End If
        Catch ex As Exception
            LogUpdate("Error updating status bar via progress event of monitor worker...." & ex.Message)
        End Try


    End Sub
    Public Sub My_BgWorkerMonitor_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs)

        Dim worker As BackgroundWorker = CType(sender, BackgroundWorker)

        If worker.CancellationPending Then
            Label8.Text = "0:0:0"
            e.Cancel = True
        Else
            'Start Timer 
            PubShared.Miner_Started = DateTime.Now

            Dim checkcount As Integer = 0
            ' For i As Integer = 0 To 1 Step 0
            Do While Not worker.CancellationPending

                'Get Mining Uptime
                Try


                    Dim uptm As DateTime = DateTime.Now
                    Dim duratation As TimeSpan = PubShared.Miner_Started - uptm
                    PubShared.miner_uptime = duratation.Days & ":" & duratation.Hours & ":" & duratation.Minutes
                    PubShared.miner_uptime = PubShared.miner_uptime.Replace("-", "")

                    'Update AIOMiner Form
                    Label8.Text = PubShared.miner_uptime


                Catch ex As Exception
                    PubShared.uptime = "-1"
                End Try

                If PubShared.DebugMining = True Then
                    LogUpdate("Monitoring is disabled during debug, please disable Debug mode to continue monitoring")
                Else


                    Try
                        checkcount = checkcount + 1
                        If PubShared.monitoring = False Then
                            rebootcount = ReturnAIOsetting("reboot")
                            Exit Do   '  For

                        End If

                        If stopjob = False Then 'And MonitorMining.ThreadState = ThreadState.Running Then
                            If rebootcount <= 0 Then
                                If ReturnAIOsetting("restart") = "True" Then

                                    'SendEmail("5 Failures occurred, we are rebooting " + System.Net.Dns.GetHostName, ReturnAIOsetting("email"))
                                    Try
                                        'alerts == port in the json (future us fix this)
                                        If ReturnAIOsetting("port") = "True" Then
                                            Dim hostname As String = Environment.MachineName
                                            hostname = hostname.Replace(" ", "")
                                            hostname = hostname.Replace(".", "")
                                            'worker == email address (future us fix this)
                                            'alert_send(hostname, ReturnAIOsetting("worker"), "Error detected on" & hostname & " We are rebooting this machine.  You can disable this reboot in the AIOSettings/Reboot settings.  If you have Auto Enable, Follow Schedule or Timed Mining, you should get an e-mail if we came back up.  If you haven't either our API is down (but how did we just send you this?  but it's possible) or you have a hardware issue!  Good Luck!  ")
                                        End If
                                    Catch ex As Exception

                                    End Try
                                    LogUpdate("X number of Failures, we are going To reboot!")
                                    System.Diagnostics.Process.Start("ShutDown", "/r /t 01")
                                Else
                                    LogUpdate("Restart Is disabled.")
                                    rebootcount = ReturnAIOsetting("reboot")
                                End If
                            End If

                            Monitor_running = False


                        End If

                        If PubShared.process_running = "" Then
                            'No clue, but maybe they fat fingered an manual entry on the json?
                            LogUpdate("We were unable to find the process to start mining, check for an update to your miners or contact AIO!")
                            PubShared.monitoring = False

                            Button1.Text = "Start"
                            Button1.BackgroundImage = My.Resources.Resources.START
                            worker.ReportProgress(100)

                            KillAllMiningApps()
                            Exit Do
                        End If

                        Try
                            For Each p As Process In System.Diagnostics.Process.GetProcesses
                                If p.ProcessName.Equals(PubShared.process_running) Then
                                    Monitor_running = True
                                End If
                            Next
                        Catch ex As Exception

                            'No idea why this would trigger, thus a crazy message as such
                            LogUpdate("Monitoring: Error 17765")
                            Exit Sub
                        End Try

                        'reset checkcount
                        If checkcount >= 31 Then
                            checkcount = 0
                        End If




                        If Monitor_running = True Then
                            rebootcount = ReturnAIOsetting("reboot")
                            worker.ReportProgress(50)
                        Else
                            'Check for network connectivity

                            LogUpdate("An error has been detected, we are restarting your mining!", eLogLevel.Err)
                            Dim Network As Boolean = False
                            Try
                                If ReturnAIOsetting("checkgoogle") = "True" Then
                                    Do Until Network = True
                                        Try
                                            Dim ping As New System.Net.NetworkInformation.Ping
                                            Dim ms = ping.Send("google.com").RoundtripTime()
                                            If CInt(ms) <> 0 Then
                                                Network = True
                                                LogUpdate("We are able to ping google, network looks good.  Restarting mining!", eLogLevel.Err)
                                                Exit Do
                                            Else
                                                LogUpdate("Unable to ping google.com, check network connection! Waiting 30 seconds, trying again", eLogLevel.Err)
                                            End If
                                        Catch ex As Exception
                                            Network = False
                                            LogUpdate("Unable to ping google.com, check network connection! Waiting 10 seconds, trying again", eLogLevel.Err)
                                        End Try
                                        System.Threading.Thread.Sleep(30000)
                                    Loop
                                Else
                                End If

                            Catch ex As Exception

                            End Try


                            rebootcount = rebootcount - 1
                            worker.ReportProgress(0)
                            KillAllMiningApps()
                            'KillAllMiningApps()

                            ''Check Card Count
                            'If GetGpuCount() = PubShared.cardcount Then
                            'Else
                            '    LogUpdate("GPU count has changed since we have started, Please review!", eLogLevel.Info)
                            'End If

                            Try
                                'alerts == port in the json (future us fix this)
                                If ReturnAIOsetting("port") = "True" Then
                                    Dim hostname As String = Environment.MachineName
                                    hostname = hostname.Replace(" ", "")
                                    hostname = hostname.Replace(".", "")
                                    'worker == email address (future us fix this)
                                    'alert_send(hostname, ReturnAIOsetting("worker"), "Minor (as in not urget, but hey it happened) error detected on" & hostname & " We are restarting the mining.    Good Luck!  ")
                                End If
                            Catch ex As Exception

                            End Try

                            CoinToMine(PubShared.algo, PubShared.coin, PubShared.ip, PubShared.port, PubShared.worker, PubShared.password, PubShared.pool)
                        End If

                    Catch ex As Exception
                        LogUpdate(ex.Message, eLogLevel.Err)
                        LogUpdate(ex.StackTrace)
                    End Try

                End If
                System.Threading.Thread.Sleep(5000)        ' 10 second delay   chgd to 5 sec 
                '     Next
            Loop

            worker.ReportProgress(100)
            PubShared.miner_uptime = "0:0:0"
            KillAllMiningApps()

        End If


    End Sub
    Private Sub My_BgWorkerGetMyWorkApi_Progress(ByVal sender As Object, ByVal e As ProgressChangedEventArgs)

        Dim commandText As String = ""

        Try
            Dim workAttempted As Boolean = False

            'List of possible work
            If e.ProgressPercentage = DoWorkForApiRequest.eApiCommands.eUnknown Then
                workAttempted = True
                LogUpdate("Web told me to do something that I don't know about?!?")

            ElseIf e.ProgressPercentage = DoWorkForApiRequest.eApiCommands.eStart Then
                workAttempted = True
                If Me.Button1.Text.ToLower.Equals("start") Then
                    commandText = "Start"
                    LogUpdate("Attempting to start mining via web command ...")
                    Button1.PerformClick()
                End If
            ElseIf e.ProgressPercentage = DoWorkForApiRequest.eApiCommands.eStop Then
                workAttempted = True
                If Me.Button1.Text.ToLower.Equals("stop") Then
                    commandText = "Stop"
                    LogUpdate("Attempting to stop mining via web command ...")
                    Button1.PerformClick()
                End If
            ElseIf e.ProgressPercentage = DoWorkForApiRequest.eApiCommands.eRestartMining Then
                workAttempted = True
                If Me.Button1.Text.ToLower.Equals("stop") Then
                    commandText = "Restart Mining"
                    LogUpdate("Attempting to restart mining via web command ...")

                    Button1.PerformClick()
                    Dim x As Integer = 0

                    Do Until Button1.Text.ToLower = "start"
                        x = x + 1
                        Threading.Thread.Sleep(2000)
                        'Check to make sure shit's not fucked
                        If x > 5 Then
                            LogUpdate("Error on trying to " & commandText.ToLower & " mining from web command.  Error:")
                            Exit Do
                        End If
                    Loop

                    If Not x > 5 Then
                        If Button1.Text.ToLower = "start" Then
                            Button1.PerformClick()
                        End If
                    End If


                End If
            ElseIf e.ProgressPercentage = DoWorkForApiRequest.eApiCommands.eRestartRig Then
                commandText = "Restart Mining Rig"
                workAttempted = True
                LogUpdate("Attempting to reboot machine via web command ...")
                AIOMinerWebAPI.send_rig_jobdone(AIOMinerWebAPI.GetRigname())
                'Stop mining if they are mining
                Dim x As Integer = 0
                If PubShared.monitoring = True Then
                    Button1.PerformClick()
                End If

                LogUpdate("WARNING, RESTARTING RIG IN 5 SECONDS")
                Dim xX As Integer = 0
                Try

                    Do Until xX >= 5
                        Threading.Thread.Sleep(1000)
                        xX = xX + 1
                        If xX >= 5 Then Exit Do
                    Loop
                Catch ex As Exception

                End Try

                System.Diagnostics.Process.Start("ShutDown", "/r /t 01")


            ElseIf e.ProgressPercentage = DoWorkForApiRequest.eApiCommands.eShutdownRig Then
                workAttempted = True
                AIOMinerWebAPI.send_rig_jobdone(AIOMinerWebAPI.GetRigname())
                LogUpdate("WARNING, RIG SHUTDOWN WAS REQUESTED.  RIG WILL BE SHUTDOWN IN 5 SECONDS")
                Try
                    Dim x As Integer = 0
                    Do Until x >= 5
                        Threading.Thread.Sleep(1000)
                        x = x + 1
                        If x >= 5 Then Exit Do
                    Loop
                Catch ex As Exception

                End Try
                Try
                    System.Diagnostics.Process.Start("ShutDown", "-s -t 0")
                Catch ex As Exception

                End Try


            ElseIf e.ProgressPercentage = DoWorkForApiRequest.eApiCommands.eAddNewPool Then
                workAttempted = True

                Dim RigName As String = ""
                Dim PoolPass As String = ""
                Dim PoolWorker As String = ""
                Dim PoolPort As String = ""
                Dim PoolIP As String = ""
                Dim PoolName As String = ""
                Dim CoinName As String = ""
                Dim algo As String = ""

                Dim jsonResult = JsonConvert.DeserializeObject(Of Object)(PubShared.AIOAuxCommand)
                Dim i As Integer = 0
                For Each item In jsonResult
                    Select Case i
                        Case 0
                            RigName = item
                        Case 1
                            If item = "" Then
                                item = "x"
                            End If
                            PoolPass = item
                        Case 2
                            PoolWorker = item
                        Case 3
                            PoolPort = item
                        Case 4
                            PoolIP = item
                        Case 5
                            PoolName = item
                        Case 6
                            CoinName = item
                        Case 7
                            algo = item
                    End Select
                    i = i + 1
                Next

                Dim PoolsConfig As mPools = GetAIOMinerJson()
                Dim TheCoins As List(Of Coin) = PoolsConfig.Pools.Coins.ToList
                Dim PrimaryPool As String = "1"

                'TONYA HARDING THE SHIT OUT OF OLD POOLS
                Dim AllGone As Boolean = False
                Do Until AllGone = True
                    Dim IsPrimaryPool As Coin = TheCoins.Find(Function(x) x.Primary = "1" AndAlso x.Type = CoinName)
                    If IsPrimaryPool IsNot Nothing Then
                        'Find the old pool, remove it's primary flag
                        'Dim SuperOldPrimary As String = "0" 'RIP JACKASS
                        IsPrimaryPool.Primary = "0"
                    Else
                        AllGone = True
                    End If
                Loop
                Try
                    Dim NewCoin As Coin = New Coin With {
                        .Algo = algo,
                        .Type = CoinName,
                        .Ip = PoolIP,
                        .Pool = PoolName,
                        .Port = PoolPort,
                        .altargument = "",
                        .Worker = PoolWorker,
                        .Primary = PrimaryPool,
                        .password = PoolPass
                    }
                    TheCoins.Add(NewCoin)
                Catch ex As Exception
                    LogUpdate("Error Saving your pool!")
                End Try

                PoolsConfig.Pools.Coins = TheCoins.ToArray
                Dim strJson As String = JsonConvert.SerializeObject(PoolsConfig)
                Try
                    Dim appPath As String = Application.StartupPath()
                    System.IO.File.WriteAllText(appPath & "\Settings\AIOMiner.json", strJson)
                    'Update the CCM list
                    RefreshCoinlist()

                Catch ex As Exception
                    LogUpdate(ex.Message, eLogLevel.Err)
                End Try





            ElseIf e.ProgressPercentage = DoWorkForApiRequest.eApiCommands.eChangeCoinMining Then
                workAttempted = True

                'Stop mining if they are mining
                Dim x As Integer = 0
                If PubShared.monitoring = True Then
                    Button1.PerformClick()
                End If

                Do Until Button1.Text.ToLower = "start"
                    x = x + 1
                    Threading.Thread.Sleep(2000)
                    'Check to make sure shit's not fucked
                    If x > 5 Then
                        LogUpdate("Error on trying to " & commandText.ToLower & " mining from web command.  Error:")
                        Exit Do
                    End If
                Loop

                ComboBox1.Text = PubShared.Coin2Change2

                Button1.PerformClick()

            ElseIf e.ProgressPercentage = DoWorkForApiRequest.eApiCommands.eDisable Then
                workAttempted = True
                'Remove the APIKey
                SaveAIOsetting("apienabled", "False")
                PubShared.ApiKey = ""
                PubShared.ShouldBeUpdatingToApi = False
                SaveAIOsetting("apikey", "")
                LogUpdate("Rig was disabled.  Please contact support")
            End If


            If workAttempted Then
                ' Threading.Thread.Sleep(10000)       ' for testing delay
                AIOMinerWebAPI.send_rig_jobdone(AIOMinerWebAPI.GetRigname())
            End If


        Catch ex As Exception
            LogUpdate("Error on trying to " & commandText.ToLower & " mining from web command.  Error:" & ex.Message)
            ' TODO:  reflect back to web?
        End Try

    End Sub
    Private Sub My_BgWorker4_Progress(ByVal sender As Object, ByVal e As ProgressChangedEventArgs)

        If e.ProgressPercentage = 1 Then
            ' ok to mine so if not mining already , then start mining, otherwise continue mining
            If Button1.Text.ToLower.Trim = "stop" Then
                Exit Sub
            Else
                ' if running using a schedule then select based on schedule, otherwise use lastmined value
                If ReturnAIOsetting("sch") = "False" Then
                    ComboBox1.Text = ReturnAIOsetting("lastmined")
                Else
                    ComboBox1.Text = WhatToMineToday()
                End If

                Button1.PerformClick()
            End If
        ElseIf e.ProgressPercentage = 0 Then    ' stop mining as time has changed to non mining hour
            KillAllMiningApps()

            Dim MS As New MinerStopped
            MS.ShowDialog()

            Label12.Visible = False
            LinkLabel1.Visible = False
            PubShared.monitoring = False
            stopjob = True
            ShouldBeMining = ""

            If My_BgWorkerMonitor IsNot Nothing AndAlso My_BgWorkerMonitor.WorkerSupportsCancellation = True Then
                My_BgWorkerMonitor.CancelAsync()
            End If


            Button1.Enabled = True
        End If

    End Sub
    Private Sub My_BgWorker4_DoWork(ByVal sender As Object, ByVal e As System.ComponentModel.DoWorkEventArgs)
        System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = False


        Dim worker As BackgroundWorker = CType(sender, BackgroundWorker)
        If worker.CancellationPending Then
            e.Cancel = True
        Else

            'Check to See if the user has enabled Timed Mining
            If PubShared.TimedMining = True Then
                ' We need to set a global flag so if it's enabled, coinmining will know we exist and should ask us first

            End If


            'If True See if the current hour you should be mining
            '' If you should be, confirm that you are
            '' If you shouldn't be, confirm that you are not
            'Log what you are doing, for each check

            If TimerSettings.OkToMine(PubShared.TimedMiningSettings) Then
                If MinerInstances.RunningMiners.Count = 0 Then
                    worker.ReportProgress(1)        ' we should be mining so signal to ui that we want to kick off the miner
                Else
                    Exit Sub    ' already mining when we should be so nothing to do this time
                End If
            ElseIf PubShared.TimedMining = True AndAlso Not TimerSettings.OkToMine(PubShared.TimedMiningSettings) Then
                ' if timed mining is in effect BUT the current hour should not be mining   stop miner if needed
                If MinerInstances.RunningMiners.Count > 0 Then  ' a miner is running so stop em
                    worker.ReportProgress(0)

                End If
            End If

        End If


    End Sub

    Private Sub My_BgWorkerGpuStatsApi_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs)


        Dim worker As BackgroundWorker = CType(sender, BackgroundWorker)
        If worker.CancellationPending Then
            e.Cancel = True
        Else


            If PubShared.ShouldBeUpdatingToApi = True Then
                Dim hostname As String = ReturnAIOsetting("rigname")
                send_rig_stats(hostname)
                timerGPUStatsApi.Enabled = True
            End If
            Threading.Thread.Sleep(10000)
        End If

    End Sub

    Private Sub My_BgWorkerGetMyWorkApi_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs)
        If PubShared.Subscriber <> False Then
            Dim worker As BackgroundWorker = CType(sender, BackgroundWorker)
            Dim hostname As String = ReturnAIOsetting("rigname")
            If PubShared.Rigname.Length > 0 Then
                hostname = PubShared.Rigname
            Else
                PubShared.Rigname = hostname
            End If

            If worker.CancellationPending Then
                e.Cancel = True
            Else
                Dim myrigjob = get_rig_work(hostname)
                PubShared.myRigJob = myrigjob

                If myrigjob IsNot Nothing Then
                    worker.ReportProgress(myrigjob.decodedrigcommand)
                End If

                timerGetMyWork.Enabled = True
                Threading.Thread.Sleep(15000)
            End If
        End If

    End Sub

    Private Sub My_BgWorker2_DoWork(ByVal sender As Object, ByVal e As System.ComponentModel.DoWorkEventArgs)
        System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = False
        Dim worker As BackgroundWorker = CType(sender, BackgroundWorker)
        If worker.CancellationPending Then
            e.Cancel = True
        Else


            Dim TrueCardCount As Integer
            Dim CardPower As Integer
            Dim TotalPower As Integer
            Dim appPath As String = Application.StartupPath()
            Dim TotalSpeed As String
            Dim TotalSpeed2 As String
            Dim DonationChecks As Integer = -1



            Try
                For z As Integer = 0 To 1 Step 0 'Never end Never





                    'Get System Uptime
                    Try


                        Dim uptm As DateTime = DateTime.Now
                        Dim duratation As TimeSpan = PubShared.DTime_Started - uptm
                        PubShared.uptime = duratation.Days & ":" & duratation.Hours & ":" & duratation.Minutes
                        PubShared.uptime = PubShared.uptime.Replace("-", "")

                        'Update AIOMiner Form
                        Label6.Text = PubShared.uptime


                    Catch ex As Exception
                        PubShared.uptime = "-1"
                    End Try





                    'Check for donations at midnight
                    'Britney Spears


                    'if time.today doesn't == 24 then donation started = True
                    'if it does == 24 set donationstarted to False


                    'If PubShared.aioLoading = False Then
                    '    PubShared.donationTimeLeft = DonationChecks.ToString
                    '    If DonationChecks >= 0 Then
                    '        DonationChecks = DonationChecks - 1
                    '        If DonationChecks <= 0 Then
                    '            'worker.ReportProgress(1)
                    '            DonationChecks = -1
                    '            If PubShared.donationPreviousMining = True Then
                    '                worker.ReportProgress(3)
                    '            Else
                    '                worker.ReportProgress(1)
                    '            End If

                    '            PubShared.donationPreviousMining = False
                    '            PubShared.donationsStarted = False
                    '            PubShared.donationsCompletedToday = True
                    '            'Stop donation mining
                    '        End If
                    '    End If

                    '    'If the hour is not 24, set this trigger

                    '    Dim DonationTime As Integer
                    '    Dim OnlyTime As String = DateTime.Now.ToString("HH")
                    '    If OnlyTime <> "10" Then PubShared.donationsCompletedToday = False


                    '    If OnlyTime = "10" AndAlso PubShared.donationsCompletedToday = False AndAlso PubShared.donationsStarted = False AndAlso ReturnAIOsetting("donation") <> "No Thanks" Then
                    '        'Flip Donation Started Process
                    '        PubShared.donationsStarted = True
                    '        PubShared.donationStartedTime = DateTime.Now
                    '        'Find out how long we should mine


                    '        Select Case ReturnAIOsetting("donation")
                    '            Case "Thank You!"
                    '                DonationTime = 2.5
                    '                '150 Seconds
                    '                DonationChecks = 25
                    '            '50 Checks if time is every 3 seconds

                    '            Case "Whoa, you are amazing!"
                    '                DonationTime = 5.0
                    '                '300 Seconds
                    '                DonationChecks = 50
                    '            '100 Checks if time is every 3 seconds

                    '            Case "Add more features!"
                    '                DonationTime = 7.5
                    '                '450 Seconds
                    '                DonationChecks = 75
                    '            '150 Checks if time is every 3 seconds

                    '            Case "Team Lambo"
                    '                DonationTime = 10.0
                    '                '600 Seconds
                    '                DonationChecks = 100
                    '            '200 Checks if the time is every 3 seconds

                    '            Case "Diamond Platinum Level"
                    '                DonationTime = 15.0
                    '                '900 Seconds
                    '                DonationChecks = 150
                    '                '300 Checks if the time is every 3 seconds

                    '        End Select

                    '        If PubShared.monitoring = True Then
                    '            PubShared.donationPreviousMining = True

                    '            'Get what they were mining and save it
                    '            PubShared.donationPreviousCoin = PubShared.coin

                    '            'Stop Mining
                    '            worker.ReportProgress(1)
                    '            'Start Mining Donation Coin
                    '            worker.ReportProgress(2)
                    '        Else

                    '            'Start Mining Donation Coin
                    '            worker.ReportProgress(2)
                    '        End If

                    '    Else
                    '        'We don't care, yet
                    '    End If
                    'End If


                    'Check if AutoRestart is enabled
                    If PubShared.AIORestartRig = True Then
                        If PubShared.AIORestartRigtime = 999999 Then
                            worker.ReportProgress(4)
                        End If
                    End If

                    'Check if AutoRestart Mining is enabled
                    'Check to see if you are mining or not?
                    If PubShared.monitoring = True Then
                        If PubShared.AIORestartMining = True Then
                            If PubShared.AIORestartMiningtime = 999999 Then
                                worker.ReportProgress(5)
                            End If
                        End If
                    End If

                    TotalSpeed = "0"
                    TotalSpeed2 = "0"
                    TotalPower = 0
                    TrueCardCount = PubShared.cardcount - 1
                    '2 - 1 = 1
                    'if videocard contians 580 power = 120w

                    'Get NVIDIA POWER
                    If PubShared.nvidia = True Then
                        Try
                            For i = 0 To TrueCardCount
                                Try
                                    CardPower = GPU_POWERDRAW(i)

                                Catch ex As Exception
                                    CardPower = "0"
                                End Try

                                TotalPower = TotalPower + CardPower
                            Next

                            Try
                                Label3.Text = "Current GPU Power Draw: " & TotalPower & " Watts"
                            Catch ex As Exception
                                Label3.Text = "Current GPU Power Draw: " & TotalPower.ToString & "Watts"
                                LogUpdate(ex.Message, eLogLevel.Err)

                                LogUpdate(ex.StackTrace)
                            End Try
                        Catch ex As Exception
                            CardPower = GPU_POWERDRAW(0)
                            Label3.Text = "Current GPU Power Draw: " & CardPower.ToString & " Watts"
                            LogUpdate(Label3.Text, eLogLevel.Err)
                            LogUpdate(ex.StackTrace)
                            LogUpdate(ex.Message, eLogLevel.Err)
                        End Try
                    End If

                    'Get power for AMD (kinda)
                    If PubShared.amd = True Then
                        Dim AMDPOWER = 0

                        If Button1.Text = "Start" Then
                            AMDPOWER = PubShared.amdpwr - 85%
                        Else
                            AMDPOWER = PubShared.amdpwr + 15%

                        End If

                        AMDPOWER = TotalPower + AMDPOWER

                        Label3.Text = "Current GPU Power Draw: " & AMDPOWER.ToString & " Watts*"
                    End If

                    '10-29-20  - BR - What the hell, stop checking this every time, even if you are not mining.  
                    Try
                        ' We need to speed up this process, stop checking every time, you already know the answer

                        Select Case PubShared.api.ToLower
                            Case "nbminer"
                                NBminer()
                            Case "miniz"
                                MiniZ()
                            Case "veo"
                                VeoAPI()

                            Case "srbminer"
                                SrbAPI()
                            Case "multi"
                                MultiAPI()
                            Case "bminer"
                                BminerAPI()
                            Case "gminer"
                                GminerAPI()
                            Case "trex"
                                TrexAPI()
                            Case "progpow"
                                ProgPowAPI()
                            Case "lol"
                                LOLMinerAPI()
                            Case "ccminer"
                                CcminerAPI()
                            Case "claymore"
                                claymoreAPIZ()
                            Case "castxmr"
                                CastAPI()
                            Case "dstm"
                                dstmAPI()
                            Case "ewbf"
                                ewbfAPI()
                            Case "phoenix"
                                phoenixAPI()
                            Case "sgminer"
                                sgminerAPI()
                            Case "claymoreold"
                                claymoreAPIold()
                            Case "xmrig"
                                xmrigAPI()
                            Case "na"
                                PubShared.speed = 0
                                PubShared.speedtype = " NO API"

                        End Select
                    Catch ex As Exception
                    End Try



                    If PubShared.Dualspeed = 0 Then
                        Label13.Text = PubShared.speed.ToString & PubShared.speedtype
                    Else
                        Label13.Text = PubShared.speed.ToString & PubShared.speedtype & " " & PubShared.Dualspeed.ToString & PubShared.Dualspeedtype
                    End If



                    '############## IF YOU CHANGE THIS, MAKE SURE YOU UPDATE THE DONATION COUNTER!!! #################
                    System.Threading.Thread.Sleep(3000)

                Next

                'End of the Infinity Loop
            Catch ex As Exception
                LogUpdate("Critical Error, please restart AIOMiner " & ex.Message, eLogLevel.Err)
            End Try

        End If



    End Sub
    Private Sub My_BgWorker3_DoWork(ByVal sender As Object, ByVal e As System.ComponentModel.DoWorkEventArgs)
        Dim worker As BackgroundWorker = CType(sender, BackgroundWorker)
        If worker.CancellationPending Then
            e.Cancel = True
        Else
            Dim cp As New Computer()
            cp.Open()
            cp.FanControllerEnabled = True
            cp.GPUEnabled = True
            For z As Integer = 0 To 1 Step 0 'Never end Never
                Try
                    'Billy Bob
                    rtnlist.Clear()
                    Dim GPUID As Integer = 0

                    For i As Integer = 0 To cp.Hardware.Count() - 1
                        Dim hardware = cp.Hardware(i)
                        hardware.Update()
                        Dim GPUNAME As String = "N/A"
                        Dim GPUFAN As String = "N/A"
                        Dim GPUTEMP As String = "N/A"
                        Dim GPUUTIL As String = "N/A"

                        Select Case hardware.HardwareType

                            Case HardwareType.GpuNvidia


                                GPUNAME = hardware.Name
                                Dim NewNvidia As Boolean = False
                                If GPUNAME.ToLower.Contains("16") Then
                                    'USE NVIDIA FAN
                                    NewNvidia = True
                                    GPUFAN = GPU_FANSPEED(GPUID)
                                    GPUUTIL = GPU_UTILIZATION(GPUID)

                                End If

                                If GPUNAME.ToLower.Contains("20") Then
                                    'USE NVIDIA FAN
                                    NewNvidia = True
                                    GPUFAN = GPU_FANSPEED(GPUID)
                                    GPUUTIL = GPU_UTILIZATION(GPUID)
                                End If

                                For j = 0 To hardware.Sensors.Count - 1
                                    'Temp
                                    Dim sensor = hardware.Sensors(j)

                                    'Patch for 16xx and 20xx video cards
                                    If sensor.SensorType = SensorType.Temperature Then
                                        GPUTEMP = sensor.Value.ToString
                                    End If

                                    'If GPUNAME.ToLower.Contains("1080") Then
                                    '    USE NVIDIA FAN
                                    '    NewNvidia = True
                                    '    GPUFAN = GPU_FANSPEED(GPUID)
                                    'End If
                                    If sensor.SensorType = SensorType.Fan Then
                                        GPUFAN = sensor.Value.ToString

                                    End If
                                    If sensor.Name.ToLower.Contains("fan") Then
                                        If NewNvidia = False Then
                                            GPUFAN = sensor.Value.ToString

                                        End If
                                    End If

                                    'Utilization


                                    If j = 5 Then
                                        If NewNvidia = False Then
                                            GPUUTIL = sensor.Value.ToString
                                        End If

                                    End If
                                    'Let's see if we need to restart mining because your util is low while mining
                                    If PubShared.monitoring = True Then

                                    End If

                                    'NAME | FANSPEED | TEMP


                                Next
                            Case HardwareType.GpuAti


                                GPUNAME = hardware.Name

                                For j = 0 To hardware.Sensors.Count - 1
                                    Dim sensor = hardware.Sensors(j)

                                    If sensor.SensorType = SensorType.Temperature Then
                                        GPUTEMP = sensor.Value.ToString
                                    End If

                                    If sensor.Name.ToLower.Contains("fan") Then
                                        GPUFAN = sensor.Value.ToString
                                    End If

                                    If sensor.SensorType = SensorType.Load Then
                                        GPUUTIL = sensor.Value.ToString
                                    End If

                                    'NAME | FANSPEED | TEMP

                                Next
                        End Select

                        Try

                            rtnlist.Add(New ListViewItem(New String() {GPUNAME, GPUFAN, GPUTEMP, GPUUTIL}))
                            ' LogUpdate("NAME:" & GPUNAME & " FAN: " & GPUFAN & " TEMP: " & GPUTEMP & " UTIL: " & GPUUTIL)
                        Catch ex As Exception

                        End Try
                        GPUID += 1

                    Next

                    'Update the list
                    worker.ReportProgress(50)


                Catch ex As Exception

                End Try
                '############## 
                System.Threading.Thread.Sleep(5000)

            Next

        End If
    End Sub
    Private Sub RefreshCoinlist()
        Dim PoolsConfig As mPools = GetAIOMinerJson()
        Dim TheCoins As List(Of Coin) = PoolsConfig.Pools.Coins.ToList

        'Add coins to the pool that have a primary flag
        Try
            For Each EHash In GetAIOMinerJson().Pools.Coins
                If EHash.Type = "" Then
                Else
                    If EHash.Primary = "1" Then
                        If ComboBox1.Items.Contains(EHash.Type) Then
                        Else
                            ComboBox1.Items.Add(EHash.Type)
                        End If
                    End If
                End If

            Next
        Catch ex As Exception
            LogUpdate(ex.Message, eLogLevel.Err)
        End Try

        'Add Donate to AIO

        If ComboBox1.Items.Contains("Donate") OrElse
                    ComboBox1.Items.Add("Donate") Then
        End If
        'Remove any coins that no longer have the primary flag
        Try
            For Each EHash In GetAIOMinerJson().Pools.Coins
                Dim CoinName As String = EHash.Type
                Dim PrimaryPool As Coin = TheCoins.Find(Function(x) x.Type = CoinName AndAlso x.Primary = "1")
                If PrimaryPool Is Nothing Then
                    ComboBox1.Items.Remove(EHash.Type)
                End If
            Next
            PubShared.ccmlistcombo = ComboBox1
        Catch ex As Exception
            LogUpdate(ex.Message, eLogLevel.Err)
        End Try
    End Sub
    Private Sub MyMiner_Activated(sender As Object, e As EventArgs) Handles Me.Activated

        If PubShared.FirstRun = True Then
            If PubShared.Need2Download = True Then
                PubShared.Need2Download = False
                PubShared.FirstRun = False
                Dim blahX As New Downloader()
                blahX.ShowDialog()
            End If
        End If


        RefreshCoinlist()
        ' Check if debug mining should be enabled from Benchmark MOde




    End Sub

    Private Sub GroupBox3_Enter(sender As Object, e As EventArgs)

    End Sub

    Public Sub Monitor_Start()
        PubShared.monitoring = True

        Label14.Text = "Following Schedule!"
        stopjob = False


        If My_BgWorkerMonitor Is Nothing Then My_BgWorkerMonitor = New BackgroundWorker
        My_BgWorkerMonitor.WorkerSupportsCancellation = True
        My_BgWorkerMonitor.WorkerReportsProgress = True

        If Not My_BgWorkerMonitor.IsBusy Then
            My_BgWorkerMonitor.RunWorkerAsync()
        Else
            My_BgWorkerMonitor.CancelAsync()

        End If



        ' MonitorMining = New Thread(AddressOf MyMonitor)
        'ParentForm.stopjob = False
        ' MonitorMining.Start()
        ' Me.TransparencyKey = Color.LightBlue
        ' Me.BackColor = Color.LightBlue
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click


        If Button1.Text = "Start" AndAlso Not TimerSettings.OkToMine(PubShared.TimedMiningSettings) Then
            KillAllMiningApps()
            LogUpdate("Start pressed but timed mining selected and not a time for mining......")
            Exit Sub
        End If


        Dim PoolsConfig As mPools = GetAIOMinerJson()
        Dim TheCoins As List(Of Coin) = PoolsConfig.Pools.Coins.ToList






        If Button1.Text = "Start" Then

            'Flush the strings, This was made into the json because of 
            'the call back on "last mined", we can create a new value later,
            'but for now just leaving it all the same
            'SaveAIOsetting("ip", "")
            'SaveAIOsetting("port", "")
            'SaveAIOsetting("worker", "")
            'SaveAIOsetting("algo", "")
            'SaveAIOsetting("pool", "")
            'SaveAIOsetting("password", "")

            'Check for Schedule
            If ReturnAIOsetting("sch") = "True" Then
                ComboBox1.Text = WhatToMineToday()
            End If





            'Kill all log files
            FlushLogs()

            'See if the below can be removed with proccess startInfo.EnvironmentVariables
            'TODO TODO TODO TODO TODO
            LogUpdate("setx GPU_FORCE_64BIT_PTR 0")
            Shell("cmd.exe /c setx GPU_FORCE_64BIT_PTR 0", AppWinStyle.Hide)
            LogUpdate("setx GPU_MAX_HEAP_SIZE 100")
            Shell("cmd.exe /c setx GPU_MAX_HEAP_SIZE 100", AppWinStyle.Hide)
            LogUpdate("setx GPU_USE_SYNC_OBJECTS 1")
            Shell("cmd.exe /c setx GPU_USE_SYNC_OBJECTS 1", AppWinStyle.Hide)
            LogUpdate("setx GPU_MAX_ALLOC_PERCENT 100")
            Shell("cmd.exe /c setx GPU_MAX_ALLOC_PERCENT 100", AppWinStyle.Hide)

            'Get the coin name (type) find out the algo
            'Save this for last mined coin
            SaveAIOsetting("lastmined", ComboBox1.Text)

            'Add coins to the pool that have a primary flag

            '##########################################
            ' IF YOU UPDATE BELOW, MAKE SURE YOU UPDATE 
            ' IDLE MINE TIMER1 ALSO
            '##########################################

            Try
                Dim CoinName As String = ComboBox1.Text
                If CoinName = "Donate" Then
                    If PubShared.amd = True Then
                        PubShared.worker = "0x07bC4d2a376b770b69156f6f1616dbc033a94395"
                        PubShared.ip = "us1.ethermine.org"
                        PubShared.algo = "Ethash"
                        PubShared.pool = "Ethash"
                        PubShared.password = ""
                        PubShared.coin = "Donate"
                        PubShared.port = "14444"
                    Else
                        PubShared.ip = "us1.ethermine.org"
                        PubShared.port = "14444"
                        PubShared.worker = "0x07bC4d2a376b770b69156f6f1616dbc033a94395"
                        PubShared.algo = "Ethash"
                        PubShared.pool = "Ethash"
                        PubShared.password = ""
                        PubShared.coin = "Donate"
                    End If

                Else
                    For Each EHash In GetAIOMinerJson().Pools.Coins
                        If EHash.Type = CoinName Then
                            If EHash.Primary = "1" Then
                                PubShared.worker = EHash.Worker
                                PubShared.ip = EHash.Ip
                                PubShared.algo = EHash.Algo
                                PubShared.pool = EHash.Pool
                                PubShared.password = EHash.password
                                PubShared.coin = CoinName
                                PubShared.port = EHash.Port
                                PubShared.altargument = EHash.altargument
                                Exit For
                            End If
                        End If
                    Next
                End If

            Catch ex As Exception
                LogUpdate(ex.Message, eLogLevel.Err)
            End Try

            '##########################################
            ' IF YOU UPDATE ABOVE, MAKE SURE YOU UPDATE 
            ' IDLE MINE (TIMER1) ALSO
            '##########################################



            'Start mining
            Try
                Try
                    'alerts == port in the json (future us fix this)

                    If ReturnAIOsetting("port") = "True" Then
                        Dim hostname As String = Environment.MachineName
                        hostname = hostname.Replace(" ", "")
                        hostname = hostname.Replace(".", "")
                        'worker == email address (future us fix this)
                        'alert_send(hostname, ReturnAIOsetting("worker"), "We have started mining " & ComboBox1.Text & " at " & Now.TimeOfDay.ToString)
                    End If
                Catch ex As Exception

                End Try





                CoinToMine(PubShared.algo, PubShared.coin, PubShared.ip, PubShared.port, PubShared.worker, PubShared.password, PubShared.pool)
                If PubShared.MinerMissing = False Then
                    Button1.Text = "Stop"
                    Button1.BackgroundImage = My.Resources.Resources._STOP

                    'Check for supported pools
                    If SupportedPools() = "" Then
                    Else
                        LinkLabel1.Text = SupportedPools()
                        Label12.Visible = True
                        LinkLabel1.Visible = True
                    End If

                    If PubShared.password = "" Then
                        PubShared.password = "x"
                    End If

                    'Set Monitoring to true
                    'PubShared.monitoring = True

                    Monitor_Start()

                End If

            Catch ex As Exception
                LogUpdate(ex.Message, eLogLevel.Err)
            End Try
        Else


            Dim MS As New MinerStopped
            MS.ShowDialog()

            Label12.Visible = False
            LinkLabel1.Visible = False
            PubShared.monitoring = False
            stopjob = True
            ShouldBeMining = ""

            If My_BgWorkerMonitor IsNot Nothing AndAlso My_BgWorkerMonitor.WorkerSupportsCancellation = True Then
                My_BgWorkerMonitor.CancelAsync()
            End If

            Try
                'alerts == port in the json (future us fix this)
                If ReturnAIOsetting("port") = "True" Then
                    Dim hostname As String = Environment.MachineName
                    hostname = hostname.Replace(" ", "")
                    hostname = hostname.Replace(".", "")
                    'worker == email address (future us fix this)
                    'alert_send(hostname, ReturnAIOsetting("worker"), "We have stopped mining " & ComboBox1.Text & " at " & Now.TimeOfDay.ToString)
                End If
            Catch ex As Exception

            End Try

            If PubShared.DebugMining = True Then
                MsgBox("Reminder you are in debug mode, you will need to close any mining apps that are running manually")
            End If


            Button1.Enabled = True
        End If


    End Sub



    Private Sub ListBox2_SelectedIndexChanged(sender As Object, e As EventArgs)

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs)


    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox1.SelectedIndexChanged
        If ComboBox1.SelectedText = ComboBox1.Text Then
        Else
            LogUpdate(ComboBox1.Text & " selected.")
            PubShared.SelectedCoin = ComboBox1.Text
            CheckBox1.Enabled = True
        End If

    End Sub

    Private Sub OverclockingToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles OverclockingToolStripMenuItem.Click
        Overclocker.ShowDialog()
    End Sub



    Private Sub Button2_Click_1(sender As Object, e As EventArgs)
        'Load Coin Prices

        'If Button2.Text = "Load Prices" Then
        '    AddHandler My_BgWorker.DoWork, AddressOf My_BgWorker_DoWork
        '    If Not My_BgWorker.IsBusy = True Then
        '        My_BgWorker.RunWorkerAsync()
        '        'Button2.Text = "Stop Loading Prices"
        '    End If
        'Else
        '    'My_BgWorker.CancelAsync()
        '    'Button2.Text = "Load Prices"
        'End If


    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs)
        ' Monitor.Close()

        If My_BgWorkerMonitor.IsBusy Then
            If My_BgWorkerMonitor.WorkerSupportsCancellation Then
                My_BgWorkerMonitor.CancelAsync()
            End If
        End If

    End Sub



    Private Sub EMailSettingsToolStripMenuItem_Click(sender As Object, e As EventArgs)
        MyEmail.ShowDialog()
    End Sub


    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged
        If CheckBox1.Checked = True Then
            SaveAIOsetting("automine", "True")
            IdeleCheckBox.Checked = False
            chkTimedMining.Checked = False
        Else
            SaveAIOsetting("automine", "False")
        End If
    End Sub

    Private Sub CheckBox2_CheckedChanged(sender As Object, e As EventArgs) Handles IdeleCheckBox.CheckedChanged

        If IdeleCheckBox.Checked = True Then
            If ComboBox1.Text = "" Then
                LogUpdate("No coin selected! Unable To idle mine!")
                Exit Sub
            End If
        End If

        If Button1.Text = "Stop" Then
            MsgBox("Please Stop mining before you start the idle miner!")
            Exit Sub
        End If

        If chkTimedMining.Checked AndAlso IdeleCheckBox.Checked Then
            LogUpdate("Deselect timed mining prior To attempting idle mining...")
            IdeleCheckBox.Checked = False
            Exit Sub
        End If
        If IdeleCheckBox.Checked = True Then
            LogUpdate("Idle Mine selected!")
            SaveAIOsetting("idel", "True")
            CheckBox1.Checked = False
            CheckBox3.Checked = False
            'Kick off the timers
            Timer1.Enabled = True
            'Added 5/5/2018
            LogUpdate("Starting Timer")
            Button1.Text = "Starting Timer"
            Button1.Enabled = False

        Else
            If Timer1.Enabled Then LogUpdate("Idle Mine disabled!, disabling mining")
            SaveAIOsetting("idel", "False")
            'Kill the Timers
            Timer1.Enabled = False
            Button1.Text = "Start"
            Button1.BackgroundImage = My.Resources.Resources.START

            Button1.Enabled = True

        End If
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Dim IMOVED As Boolean = False
        info.structSize = Len(info)

        'Call the API.
        GetLastInputInfo(info)

        If firstTick = -1337 Then
            firstTick = info.tickCount
            Return
        End If

        If firstTick <> info.tickCount Then
            firstTick = info.tickCount
            IMOVED = True
            'lbltime.Text = "Last Active:  " & Now.ToLongTimeString()
        End If

        If IMOVED = True Then
            If PubShared.monitoring = True Then
                Try
                    'alerts == port in the json (future us fix this)
                    If ReturnAIOsetting("port") = "True" Then
                        Dim hostname As String = Environment.MachineName
                        hostname = hostname.Replace(" ", "")
                        hostname = hostname.Replace(".", "")
                        'worker == email address (future us fix this)
                        'alert_send(hostname, ReturnAIOsetting("worker"), "We have stoppedIdle mining " & ComboBox1.Text & " at " & Now.TimeOfDay.ToString)
                    End If
                Catch ex As Exception

                End Try

                'Exit mining
                MinerStopped.Show()
                'KillAllMiningApps() Removed 5/5/2018
                Button1.Enabled = True
                Button1.Text = "Start"
                Button1.BackgroundImage = My.Resources.Resources.START

                Label12.Visible = False
                LinkLabel1.Visible = False
                PubShared.speed = 0 ' Added 1/3/2018
                PubShared.Dualspeed = 0
            Else
                'Reset IdelTime
                IdelTIME = ReturnAIOsetting("ideltmr")
                Return
            End If
        Else
            If TimerSettings.OkToMine(PubShared.TimedMiningSettings) = False Then
                KillAllMiningApps()
                LogUpdate("Timed Mining override in AIOMiner.   Mining will NOT be started...")
                Return
            End If

            If IdelTIME <= 0 Then
                Try
                    Dim CoinName As String = ComboBox1.Text
                    If CoinName = "Donate" Then
                        If PubShared.amd = True Then
                            PubShared.worker = "0x07bC4d2a376b770b69156f6f1616dbc033a94395"
                            PubShared.ip = "us1.ethermine.org"
                            PubShared.algo = "Ethash"
                            PubShared.pool = "Ethash"
                            PubShared.password = ""
                            PubShared.coin = "Donate"
                            PubShared.port = "14444"
                        Else
                            PubShared.ip = "us1.ethermine.org"
                            PubShared.port = "14444"
                            PubShared.worker = "0x07bC4d2a376b770b69156f6f1616dbc033a94395"
                            PubShared.algo = "Ethash"
                            PubShared.pool = "Ethash"
                            PubShared.password = ""
                            PubShared.coin = "Donate"
                        End If
                    Else
                        For Each iCoin In GetAIOMinerJson().Pools.Coins
                            If iCoin.Type = CoinName Then
                                If iCoin.Primary = "1" Then
                                    PubShared.ip = iCoin.Ip
                                    PubShared.port = iCoin.Port
                                    PubShared.worker = iCoin.Worker
                                    PubShared.algo = iCoin.Algo
                                    PubShared.pool = iCoin.Pool
                                    PubShared.password = iCoin.password
                                    PubShared.coin = iCoin.Type
                                    PubShared.altargument = iCoin.altargument
                                    Exit For
                                End If
                            End If
                        Next
                    End If

                    Try
                        'alerts == port in the json (future us fix this)
                        If ReturnAIOsetting("port") = "True" Then
                            Dim hostname As String = Environment.MachineName
                            hostname = hostname.Replace(" ", "")
                            hostname = hostname.Replace(".", "")
                            'worker == email address (future us fix this)
                            'alert_send(hostname, ReturnAIOsetting("worker"), "We have started Idle mining " & ComboBox1.Text & " at " & Now.TimeOfDay.ToString)
                        End If
                    Catch ex As Exception

                    End Try

                    CoinToMine(PubShared.algo, PubShared.coin, PubShared.ip, PubShared.port, PubShared.worker, PubShared.password, PubShared.pool)
                    'CoinToMine(ReturnAIOsetting("algo"), ReturnAIOsetting("coin"), ReturnAIOsetting("ip"), ReturnAIOsetting("port"), ReturnAIOsetting("worker"), ReturnAIOsetting("password"), ReturnAIOsetting("pool"))
                    Button1.Text = "Stop"
                    Button1.BackgroundImage = My.Resources.Resources._STOP

                    Monitor_Start()

                    'Check for supported pools
                    If SupportedPools() = "" Then
                    Else
                        LinkLabel1.Text = SupportedPools()
                        Label12.Visible = True
                        LinkLabel1.Visible = True
                    End If
                Catch ex As Exception
                    LogUpdate(ex.Message, eLogLevel.Err)
                    Exit Sub
                End Try

                Button1.Text = "Idle Mining!"
                Button1.Enabled = False
                IdelTIME = ReturnAIOsetting("ideltmr")


            Else
                If Button1.Text = "Idle Mining!" Then
                Else

                    IdelTIME = IdelTIME - 1
                    Button1.Text = IdelTIME.ToString + " seconds left to mine"
                    Return
                End If
            End If
        End If










    End Sub

    Private Sub GroupBox1_Enter(sender As Object, e As EventArgs) Handles GroupBox1.Enter

    End Sub

    Private Sub CoinSettingsToolStripMenuItem_Click(sender As Object, e As EventArgs)
        CoinSettings.ShowDialog()
    End Sub

    Private Sub Button3_Click_1(sender As Object, e As EventArgs)
        Dim config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal)
        MessageBox.Show(config.FilePath)
    End Sub

    Private Sub EMailSettingsToolStripMenuItem1_Click(sender As Object, e As EventArgs)
        MyEmail.ShowDialog()
    End Sub

    Private Sub CoinSettingsToolStripMenuItem1_Click(sender As Object, e As EventArgs)
        CoinSettings.ShowDialog()
    End Sub

    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        'Skiptown
        If Button1.Text = "Stop" Then
            Button1.PerformClick()
        End If

        Try

            KillAllMiningApps()

        Catch ex As Exception
        End Try

        Me.Close()
    End Sub

    Private Sub GroupBox2_Enter(sender As Object, e As EventArgs) Handles GroupBox2.Enter

    End Sub

    Private Sub Label13_Click(sender As Object, e As EventArgs) Handles Label13.Click
        MHs.ShowDialog()
    End Sub



    Private Sub ToolStripMenuItem4_Click(sender As Object, e As EventArgs)
        Backup.ShowDialog()
    End Sub

    Private Sub ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem1.Click
        Dim CS As New CoinSettings
        CS.ShowDialog()
    End Sub

    Private Sub ToolStripMenuItem2_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem2.Click
        Dim frm As New MyEmail
        frm.ShowDialog()
    End Sub


    Private Sub Button3_Click_2(sender As Object, e As EventArgs)

    End Sub

    Private Sub ToolStripMenuItem3_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem3.Click
        Dim SCH As New Schedule()
        SCH.ShowDialog()


    End Sub

    Private Sub CheckBox3_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox3.CheckedChanged

        'If CheckBox3.Checked = True Then
        '    If Not TimerSettings.OkToMine(PubShared.TimedMiningSettings) Then
        '        LogUpdate("Timed mining setup does not allow scheduled mining at this time.")
        '        CheckBox3.Checked = False
        '        Label14.Visible = False
        '        Timer2.Stop()
        '        Exit Sub
        '    End If
        'End If




        If CheckBox3.Checked = True Then
            'I'm checked
            If ReturnAIOsetting("monday") = "" Then
                MsgBox("I don't see any schedule setup, please confirm you have one set!")
                CheckBox3.Checked = False
                Exit Sub
            End If

            SaveAIOsetting("sch", "True")
            PubShared.ScheduledMining = True

            Label14.Visible = True
            'Start the timer
            Timer2.Start()
        Else
            SaveAIOsetting("sch", "False")
            PubShared.ScheduledMining = False
            Label14.Visible = False
            'Stop the timer
            Timer2.Stop()
        End If


    End Sub



    Private Sub IdleSettingsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles IdleSettingsToolStripMenuItem.Click
        Idle.ShowDialog()

    End Sub

    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        'If the user is going by WTM (Profit) then use that 
        'else make sure the user is mining what they have for today 




        If Timer2.Interval = 100 Then
            Timer2.Interval = 60000
        End If

        If PubShared.Timer2_Tick_Count >= PubShared.WTM_Check_Tick Then
            Try
                'Reset WTM Check if user was a subscriber 
                If PubShared.Subscriber = False Then
                    'but now is not
                    If PubShared.WTM_Check_Tick <> 1400 Then
                        PubShared.WTM_Check_Tick = 1400
                        SaveAIOsetting("WhatToMineUpdate", "1400")
                    End If
                End If
            Catch ex As Exception

            End Try

            Try
                ListView1.Items.Clear()
                ' get the 5 top from whattomine web site and fill in the listview
                Dim lItems As List(Of ListViewItem) = GetCoinAlgoFromWeb(GetWTMString())
                GroupBox3.Text = "Top 5 Coins to Mine Last Checked:" & Date.Now
                For Each litem As ListViewItem In lItems
                    ListView1.Items.Add(litem)
                Next
            Catch ex As Exception
                LogUpdate("Unable to update WhatToMine!")
            End Try

            TodaysDate = Date.Today
            PubShared.Timer2_Tick_Count = 0
        Else
            PubShared.Timer2_Tick_Count += 1

        End If

        'Check what day it is, and make sure the combobox is selected to that and change if needed.  
        'This is a shit ton of reading, find a way to store it in the memory on load/changes

        If WhatToMineToday() <> "Most Profitable" Then
            'What day is it today, check to see if we are mining what we said we would

        Else
            If PubShared.Timer2_Tick_Count = 1 Then
                If PubShared.ScheduledMining = True Then


                    If PubShared.monitoring = True Then
                        If ComboBox1.Text.ToLower = WhatToMineToday.ToString.ToLower() Then
                        Else
                            Try
                                PubShared.monitoring = False
                                KillAllMiningApps()
                                MinerStopped.Show()
                                LogUpdate("Waiting to change mining....")
                                'Change what you are mining
                                ComboBox1.Text = WhatToMineToday()
                                'Shutdown what you are currently mining 
                            Catch ex As Exception

                            End Try
                        End If
                    Else

                        ComboBox1.Text = WhatToMineToday()
                        'Start Mining if not overridden by timed mining
                        If TimerSettings.OkToMine(PubShared.TimedMiningSettings) Then Button1.PerformClick()
                    End If
                End If
            End If

        End If




    End Sub

    Private Sub LoadOnBootToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LoadOnBootToolStripMenuItem.Click
        StartOnBoot.ShowDialog()


    End Sub

    Private Sub ComboBox1_DrawItem(sender As Object, e As DrawItemEventArgs) Handles ComboBox1.DrawItem
        If e.Index <> -1 Then
            e.Graphics.DrawImage(Me.ImageList1.Images(e.Index) _
              , e.Bounds.Right, e.Bounds.Top)
        End If
    End Sub

    Private Sub ComboBox1_MeasureItem(sender As Object, e As MeasureItemEventArgs) Handles ComboBox1.MeasureItem
        e.ItemHeight = Me.ImageList1.ImageSize.Height
        e.ItemWidth = Me.ImageList1.ImageSize.Width
    End Sub

    Private Sub AutologinToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AutologinToolStripMenuItem.Click
        Autologin.ShowDialog()

    End Sub

    Private Sub Button3_Click_3(sender As Object, e As EventArgs)
        Dim appPath As String = Application.StartupPath()
        Dim PoolsConfig As mPools = GetAIOMinerJson()
        Dim TheCoins As List(Of Coin) = PoolsConfig.Pools.Coins.ToList

        'Add coins to the pool that have a primary flag
        Try
            For Each b3Coin In GetAIOMinerJson().Pools.Coins
                Dim CoinName As String = b3Coin.Type
                Dim PrimaryPool As Coin = TheCoins.Find(Function(x) x.Type = CoinName AndAlso x.Primary = "1")
                If PrimaryPool IsNot Nothing Then
                    If ComboBox1.Items.Contains(b3Coin.Type) Then
                    Else
                        ComboBox1.Items.Add(b3Coin.Type)
                    End If
                End If
            Next
        Catch ex As Exception
            LogUpdate(ex.Message, eLogLevel.Err)
        End Try

        Exit Sub

        'Get a list of all of the coins in the JSON

        Dim EditFlagCoin As Coin = TheCoins.Find(Function(x) x.Primary = "1")
        If EditFlagCoin Is Nothing Then
            MsgBox("Not Found")
        Else
            MsgBox(EditFlagCoin.Type)
        End If

        Exit Sub

        'Confirm the JSON file exists
        Try

            Dim hCoin As String = "HUSH"
            'Dim PrimaryCoin As String
            For Each tCoin In GetAIOMinerJson().Pools.Coins
                If tCoin.Type.Contains(hCoin) Then
                    If tCoin.Primary = "1" Then
                        MsgBox(tCoin.Type)
                    End If
                End If

            Next
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try

    End Sub

    Public Sub CoinsToMineToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CoinsToMineToolStripMenuItem.Click
        Dim PS As New PoolSettings()

        PS.ShowDialog()


    End Sub

    Private Sub SupportToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SupportToolStripMenuItem.Click
        'Open up a webpage showing people how to send an email to be handled as an SMS
        Dim webAddress As String
        webAddress = PubShared.WEB_DISCORD_LINK
        Process.Start(webAddress)
    End Sub

    Private Sub BackupRestoreToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles BackupRestoreToolStripMenuItem.Click
        Backup.ShowDialog()

    End Sub

    Private Sub MyMiner_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing



        'Close GPU Stats
        If PubShared.GpuStatus = True Then
            GpuStats.Close()
        End If

        If Button1.Text = "Stop" Then
            Button1.PerformClick()
        End If

        Try

            KillAllMiningApps()
            'NotifyIcon1.Visible = False
            'NotifyIcon1.Dispose()

            ' kill all bg workers
            My_BgWorker.CancelAsync()
            My_BgWorker2.CancelAsync()
            My_BgWorker3.CancelAsync()
            My_BgWorker4.CancelAsync()
            My_BgWorker5.CancelAsync()
            My_BgWorkerMonitor.CancelAsync()
            My_BgWorkerGpuStatsApi.CancelAsync()
            My_BgWorkerGetMyWorkApi.CancelAsync()
            My_Bg_DebugMining.CancelAsync()

            'Close all Forms
            My.Application.OpenForms.Cast(Of Form)() _
              .Except({Me}) _
              .ToList() _
              .ForEach(Sub(form) form.Close())

        Catch ex As Exception
        End Try

    End Sub

    Private Sub MenuStrip1_ItemClicked(sender As Object, e As ToolStripItemClickedEventArgs) Handles MenuStrip1.ItemClicked

    End Sub

    'Private Sub Button3_Click_4(sender As Object, e As EventArgs)
    '    ListBox1.Items.Clear()

    '    ' TODO:  remove   just for testing
    '    ' HttpPost("https://jsonplaceholder.typicode.com/posts", "title=foo&body=bat&userid=1")
    '    ' HttpPost("https://reqres.in/api/users", "name=morpheus&job=leader")
    '    ' HttpPost("http://''''':5000/api/login", "email='''''&password='''")




    'End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs)


    End Sub

    Private Sub LinkLabel1_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs)
        Process.Start(LinkLabel1.Text)
    End Sub

    Private Sub RestartSettingsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles RestartSettingsToolStripMenuItem.Click
        Dim frm As New Restart

        frm.ShowDialog()


    End Sub

    Private Sub LinkLabel1_Click(sender As Object, e As EventArgs)

    End Sub



    Private Sub MinerSettingsToolStripMenuItem_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub Button4_Click_1(sender As Object, e As EventArgs)
        MsgBox(ReturnAIOsetting("email"))
    End Sub

    Private Sub Button4_Click_2(sender As Object, e As EventArgs)
        'RichTextBox1.Text = ClaymoreAPI()

    End Sub


    Private Sub Marquee_Tick(sender As Object, e As EventArgs) Handles Marquee.Tick


        Marquee.Interval = CInt(ReturnAIOsetting("marqueespeed") & "00")
        PricesLBL.Left = PricesLBL.Left - 10
        PricesLBL.Text = PubShared.coinprice
        If PricesLBL.Left < 0 - PricesLBL.Width Then
            PricesLBL.Left = Me.Width - 10

        End If
    End Sub

    Private Sub BenchmarkMinerSettingsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles BenchmarkMinerSettingsToolStripMenuItem.Click
        Dim DA As New Profitability()

        If PubShared.monitoring = True Then
            LogUpdate("Please stop mining before you load the benchmark!")
        Else
            PubShared.JustCheckingMiners = True
            DA.ShowDialog()
        End If


        'Downloader.Show()



    End Sub

    Private Sub BackgroundWorker3_DoWork(sender As Object, e As DoWorkEventArgs) Handles BackgroundWorker3.DoWork

    End Sub

    Private Sub MinerSettingsToolStripMenuItem_Click_1(sender As Object, e As EventArgs)

    End Sub

    Private Sub OnTopChkBx_CheckedChanged(sender As Object, e As EventArgs) Handles OnTopChkBx.CheckedChanged
        If OnTopChkBx.Checked = True Then
            SaveAIOsetting("ontop", "True")
            Me.TopMost = True
        Else
            SaveAIOsetting("ontop", "False")
            Me.TopMost = False
        End If
    End Sub

    Private Sub ListView1_SelectedIndexChanged(sender As Object, e As EventArgs)

    End Sub

    Private Sub LinkLabel2_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel2.LinkClicked
        Process.Start(GetWTMString())
    End Sub

    Private Sub ListBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles LogsListbox.SelectedIndexChanged

    End Sub

    Private Sub ListView1_SelectedIndexChanged_1(sender As Object, e As EventArgs) Handles ListView1.SelectedIndexChanged

    End Sub

    Private Sub ListView1_DoubleClick(sender As Object, e As EventArgs) Handles ListView1.DoubleClick
        'Dim iitems As List(Of ListViewItem)
        ListView1.Items.Clear()
        ' get the 5 top from whattomine web site and fill in the listview
        Dim lItems As List(Of ListViewItem) = GetCoinAlgoFromWeb(GetWTMString())
        GroupBox3.Text = "Top 5 Coins to Mine Last Checked:" & Date.Now
        For Each litem As ListViewItem In lItems
            ListView1.Items.Add(litem)
        Next
    End Sub



    Private Sub RedditSupportToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles RedditSupportToolStripMenuItem.Click
        'Open up a webpage showing people how to send an email to be handled as an SMS
        Dim webAddress As String
        webAddress = PubShared.WEB_DISCORD_LINK
        Process.Start(webAddress)
    End Sub

    Private Sub ReportAnIssueToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ReportAnIssueToolStripMenuItem.Click
        'Open up a webpage showing people how to send an email to be handled as an SMS
        Dim webAddress As String
        webAddress = PubShared.WEB_GITHUB_LINK
        Process.Start(webAddress)
    End Sub

    Private Sub Button2_Click_2(sender As Object, e As EventArgs)

    End Sub

    Private Sub LinkLabel3_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel3.LinkClicked

        Try
            'Dim items As List(Of ListViewItem)
            ListView1.Items.Clear()
            ' get the 5 top from whattomine web site and fill in the listview
            Dim lItems As List(Of ListViewItem) = GetCoinAlgoFromWeb(GetWTMString())
            GroupBox3.Text = "Top (5) Coins to Mine Last Checked:" & Date.Now
            For Each litem As ListViewItem In lItems
                If ListView1.Items.Count >= 5 Then
                Else
                    ListView1.Items.Add(litem)


                End If
            Next


        Catch ex As Exception

        End Try
    End Sub

    Private Sub UpdateSettingsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles UpdateSettingsToolStripMenuItem.Click
        Dim updatesettings As New UpdateSettings
        'Clear red info as user is going to the right spot
        TextBox1.Visible = False
        TextBox2.Visible = False
        updatesettings.ShowDialog()
    End Sub

    Private Sub AboutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AboutToolStripMenuItem.Click
        About.ShowDialog()

    End Sub

    Private Sub WelcomeScreenAgainPleaseToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles WelcomeScreenAgainPleaseToolStripMenuItem.Click
        Welcome.Show()

    End Sub

    Private Sub RdPartyApplicationsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles RdPartyApplicationsToolStripMenuItem.Click
        ThirdParty.ShowDialog()




    End Sub

    Private Sub chkTimedMining_CheckedChanged(sender As Object, e As EventArgs) Handles chkTimedMining.CheckedChanged
        ' if during form load ignore

        'If inFormLoad Then Exit Sub


        ' if already mining give directions then bail
        'If Button1.Text = "Stop" AndAlso chkTimedMining.Checked Then
        '    LogUpdate("Please stop mining before you start timed mining.")
        '    Exit Sub
        'End If
        If (IdeleCheckBox.Checked) AndAlso chkTimedMining.Checked Then
            LogUpdate("Cannot perform timed and idle mining at the same time")
            chkTimedMining.Checked = False
            Exit Sub
        End If

        Timer4.Enabled = False              ' assume not gonna do timed mining till proven otherwise below

        If chkTimedMining.Checked = True Then
            ' if any settings then if at least one setting is on then handle setting timed mining on or off       otherwise show message and clear timed mining chkbx
            If PubShared.TimedMiningSettings IsNot Nothing AndAlso PubShared.TimedMiningSettings.Settings IsNot Nothing Then
                Dim isOneSelected As Boolean = False
                For i As Short = 0 To PubShared.TimedMiningSettings.Settings.Length - 2
                    If PubShared.TimedMiningSettings.Settings(i).isOn = "True" Then
                        isOneSelected = True
                        Exit For
                    End If
                Next
                If isOneSelected Then
                    PubShared.TimedMiningSettings.Settings(24).hour = "AppTimedMining"
                    If chkTimedMining.Checked Then
                        PubShared.TimedMiningSettings.Settings(24).isOn = "True"
                        ' savetimedmining settings
                        'Set the setting before you save it
                        PubShared.TimedMining = True

                        SaveTimerSettingsJson(PubShared.TimedMiningSettings)

                        'Save timed setting, setting
                        LogUpdate("Timed Mining in effect ...")
                        Timer4.Enabled = True

                    Else
                        PubShared.TimedMiningSettings.Settings(24).isOn = "False"
                        ' save timed mining settings
                        'Set the setting before you save it
                        PubShared.TimedMining = False
                        SaveTimerSettingsJson(PubShared.TimedMiningSettings)

                        LogUpdate("Time Mining no longer in effect ...")

                    End If
                Else
                    MsgBox("I don't see any schedule setup, please confirm you have at least one timed setting set!")
                    chkTimedMining.Checked = False
                End If
            Else
                MsgBox("I don't see any schedule setup, please confirm you have at least one timed setting set!")
                chkTimedMining.Checked = False
            End If
        Else

            If PubShared.TimedMining = True Then LogUpdate("Time Mining no longer in effect ...")
            PubShared.TimedMining = False
            'How in the shit do we tell the file it needs to be off?
            PubShared.TimedMiningSettings.Settings(24).isOn = "False"
            SaveTimerSettingsJson(PubShared.TimedMiningSettings)

        End If


    End Sub

    Private Sub TimedMiningSettingsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles TimedMiningSettingsToolStripMenuItem.Click
        Dim frm As New TimedSettings()

        frm.ShowDialog()

        ' refresh settings locally in obj model
        PubShared.TimedMiningSettings = GetTimerSettingsJson()

    End Sub

    Private Sub Timer4_Tick(sender As Object, e As EventArgs) Handles Timer4.Tick
        My_BgWorker4.RunWorkerAsync()
    End Sub

    Private Sub Button1_TextChanged(sender As Object, e As EventArgs) Handles Button1.TextChanged
        If Button1.Text.ToLower = "start" Then
            Button1.ForeColor = Color.Green
        ElseIf Button1.Text.ToLower = "stop" Then
            Button1.ForeColor = Color.Red
        End If
    End Sub

    Private Sub GpuStatList_SelectedIndexChanged(sender As Object, e As EventArgs) Handles GpuStatList.SelectedIndexChanged

    End Sub

    Private Sub GpuStatList_TextChanged(sender As Object, e As EventArgs) Handles GpuStatList.TextChanged

    End Sub

    Private Sub Button2_Click_3(sender As Object, e As EventArgs) Handles Button2.Click
        If PubShared.GpuStatus = True Then
            LogUpdate("GpuStatus Window is already open?!?")
            Exit Sub
        Else
            PubShared.GpuStatus = True
            GpuStats.Show()

        End If
    End Sub



    Private Sub WindowsUpdatesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles WindowsUpdatesToolStripMenuItem.Click

    End Sub

    Private Sub DonateToAIOMinerToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DonateToAIOMinerToolStripMenuItem.Click
        Process.Start("https://commerce.coinbase.com/checkout/ca002eb4-21ea-496e-aeb9-cc7c29186d7e")
    End Sub

    Private Sub Button3_Click_5(sender As Object, e As EventArgs)
        LogsListbox.Items.Clear()
    End Sub

    Private Sub Button4_Click_3(sender As Object, e As EventArgs)

    End Sub

    Private Sub Statusbar_ItemClicked(sender As Object, e As ToolStripItemClickedEventArgs) Handles Statusbar.ItemClicked

    End Sub

    Private Sub TextBox2_TextChanged(sender As Object, e As EventArgs) Handles TextBox2.TextChanged

    End Sub

    Private Sub TextBox2_Click(sender As Object, e As EventArgs) Handles TextBox2.Click
        TextBox1.Visible = False
        TextBox2.Visible = False
        PubShared.ackupdates = True


    End Sub



    Private Sub TextBox1_Click(sender As Object, e As EventArgs) Handles TextBox1.Click
        If TextBox1.Text = "An update for AIOMiner is available!  Click Here!" Then
            'Process.Start(pubshared.WEB_WIKI_LINK_UPDATES)
            Dim AIOU As New AIOMinerUpdates
            AIOU.ShowDialog()

        ElseIf TextBox1.Text = "A new Miner application is available! Click Here!" Then
            'Process.Start(pubshared.WEB_WIKI_LINK_MINER_UPDATES)
            Dim AIOU As New MinerUpdates
            AIOU.ShowDialog()
        End If

    End Sub

    Private Sub DonateSettingsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DonateSettingsToolStripMenuItem.Click
        Dim frm As New DonateSettings
        frm.ShowDialog()

    End Sub

    Private Sub RestartRigTMR_Tick(sender As Object, e As EventArgs) Handles RestartRigTMR.Tick
        RebootTime = PubShared.AIORestartRigtime
        If RebootTime <= 0 Then
            'RebootTime!
            LogUpdate("A reboot has been schedule, sending out e-mail if enabled")

            Try
                SendEmail(" A reboot has been scheduled for " & My.Computer.Name & ".  Catch you on the flip side!", ReturnAIOsetting("email"))
            Catch ex As Exception
                LogUpdate("Error sending out e-mail before reboot, confirm e-mail", eLogLevel.Err)
            End Try

            MinerStopped.Show()
            Threading.Thread.Sleep(5000)
            'Reboot
            System.Diagnostics.Process.Start("ShutDown", "/r /t 01")
        Else
            PubShared.AIORestartRigtime = PubShared.AIORestartRigtime - 1
            AutoRestartLBL.Text = "Rig Reboot in: " & PubShared.AIORestartRigtime.ToString & " Seconds"
            'Check to see if you are still enabled
            If PubShared.AIORestartRig = False Then
                PubShared.AIORestartRigtime = 999999
                AutoRestartLBL.Visible = False
                RestartRigTMR.Stop()
            End If

        End If
    End Sub

    Private Sub ToolStripStatusLabel3_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub ToolStripStatusLabel3_Click_1(sender As Object, e As EventArgs) Handles Label12.Click

    End Sub

    Private Sub LinkLabel1_Click_1(sender As Object, e As EventArgs) Handles LinkLabel1.Click
        Process.Start(LinkLabel1.Text)
    End Sub

    Private Sub AutoRestartMiningLBL_Click(sender As Object, e As EventArgs) Handles AutoRestartMiningLBL.Click

    End Sub

    Private Sub RestartMiningTMR_Tick(sender As Object, e As EventArgs) Handles RestartMiningTMR.Tick
        RebootTime = PubShared.AIORestartMiningtime
        If RebootTime <= 0 Then
            'RebootTime!
            LogUpdate("A restart of mining has been schedule, bloop de bloops")
            KillAllMiningApps()
            Threading.Thread.Sleep(5000)
            'Reset this back to 999999 so it will kick back on, on the next sweep
            PubShared.AIORestartMiningtime = 999999
            RestartMiningTMR.Stop()
        Else
            PubShared.AIORestartMiningtime = PubShared.AIORestartMiningtime - 1
            AutoRestartMiningLBL.Text = "Mining Restart in: " & PubShared.AIORestartMiningtime.ToString & " Seconds"
            'Check to see if you are still enabled
            'Also, check to see if you are still mining, no need to restart mining if you are not mining
            If PubShared.monitoring = True Then
                If PubShared.AIORestartMining = False Then
                    PubShared.AIORestartMiningtime = 999999
                    AutoRestartMiningLBL.Visible = False
                    RestartMiningTMR.Stop()
                End If
            Else
                PubShared.AIORestartMiningtime = 999999
                AutoRestartMiningLBL.Visible = False
                RestartMiningTMR.Stop()
            End If
        End If

    End Sub

    Private Sub Button4_Click_4(sender As Object, e As EventArgs)
        'MiningFrm.Show()

    End Sub

    Private Sub timerGPUStatsApi_Tick(sender As Object, e As EventArgs) Handles timerGPUStatsApi.Tick
        Try

            If Not PubShared.BusyWithRigCommand Then
                PubShared.BusyWithRigCommand = True
                My_BgWorkerGpuStatsApi.RunWorkerAsync()
                PubShared.BusyWithRigCommand = False
            End If

        Catch ex As Exception
            PubShared.BusyWithRigCommand = False
            'Removed logupdate to clean up the logs
            'LogUpdate("Set BusyWithRigCommand to false in exception handler of send my gpu updates timer tick..."1
        End Try

    End Sub

    Private Sub timerGetMyWork_Tick(sender As Object, e As EventArgs) Handles timerGetMyWork.Tick
        Try
            If Not PubShared.BusyWithRigCommand Then
                PubShared.BusyWithRigCommand = True
                My_BgWorkerGetMyWorkApi.RunWorkerAsync()
                PubShared.BusyWithRigCommand = False
            End If

        Catch ex As Exception
            PubShared.BusyWithRigCommand = False

        End Try

    End Sub

    Private Sub AIOMiner_BackColorChanged(sender As Object, e As EventArgs) Handles Me.BackColorChanged

    End Sub

    Private Sub Button4_Click_5(sender As Object, e As EventArgs)

    End Sub

    Private Sub ComboBox1_TextChanged(sender As Object, e As EventArgs) Handles ComboBox1.TextChanged
        PubShared.SelectedCoin = ComboBox1.Text

    End Sub

    Private Sub Button4_Click_6(sender As Object, e As EventArgs)

    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs)
        MinerUpdates.Show()

    End Sub

    Private Sub Button5_Click_1(sender As Object, e As EventArgs) Handles Button5.Click

        'LogUpdate("TESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTEST")
        If Button5.Text = "<<" Then
            GroupBox4.Visible = True
            GroupBox1.Width = 496
            LogsListbox.Width = 496

            Button5.Text = ">>"
            LogsListbox.Refresh()

        Else
            GroupBox4.Visible = False
            GroupBox1.Width = Me.Width - 15
            LogsListbox.Width = Me.Width - 15
            Button5.Text = "<<"
            LogsListbox.Refresh()
        End If


    End Sub

    Private Sub CheckBox4_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox4.CheckedChanged

        If CheckBox4.Checked = True Then
            If PubShared.monitoring = True Then
                LogUpdate("Please stop mining/testing before you enable/disable debug mode")
                CheckBox4.Checked = False
            Else
                PubShared.DebugMining = True
                LogUpdate("Debug Mode Enabled, Monitoring will not occure, you will need to close the mining window manually")
            End If



        Else


            PubShared.DebugMining = False
            LogUpdate("Debug Mode Disabled")

        End If
    End Sub

    Private Sub BindingSource2_CurrentChanged(sender As Object, e As EventArgs) Handles BindingSource2.CurrentChanged

    End Sub

    Private Sub SystemSettingsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SystemSettingsToolStripMenuItem.Click
        MinerSettings.Show()
    End Sub

    Private Sub Button3_Click_4(sender As Object, e As EventArgs)
        MiniZ()

    End Sub
End Class