Imports System.Reflection
Imports System.Text.RegularExpressions
Imports System.Net
Imports System.IO
Imports System.Management



Imports AIOminer.Log
Imports AIOminer.JSON_Utils
Imports Newtonsoft.Json

Public Class General_Utils

    Public Shared Function Plexiglass(dialog As Form) As DialogResult
        Using plexi = New Form()
            plexi.FormBorderStyle = FormBorderStyle.None
            plexi.Bounds = Screen.FromPoint(dialog.Location).Bounds
            plexi.StartPosition = FormStartPosition.Manual
            plexi.AutoScaleMode = AutoScaleMode.None
            plexi.ShowInTaskbar = False
            plexi.BackColor = Color.Black
            plexi.Opacity = 0.45
            plexi.Show()
            dialog.StartPosition = FormStartPosition.CenterParent
            Return dialog.ShowDialog(plexi)
        End Using
    End Function


    Public Shared Function CheckOpenPort(ip As String, port As Integer) As Boolean

        Dim clientSocket As New Net.Sockets.TcpClient()
        Dim stopWatch As New Stopwatch()
        Dim StopWatchTimeMs As Int32
        Dim I As Int16
        I = 0
        Dim PingTotals As Integer
        PingTotals = 0
        Dim PingTotalZ As Integer
        PingTotalZ = 0

        Try
            stopWatch.Start()
            clientSocket.Connect(ip, port)
            clientSocket.Close()
            stopWatch.Stop()
            StopWatchTimeMs = stopWatch.ElapsedMilliseconds
            PingTotals = PingTotals + StopWatchTimeMs
        Catch ex As Exception
            Return False
        End Try

        If PingTotals <= "100000" Then
            Return True
        Else
            Return False
        End If

    End Function
    Public Shared Function ReturnAvaliableDiskSpace() As String
        Try

            Dim appPath As String = Application.StartupPath()
            Dim FreeSpace As String = ""
            Dim drives As DriveInfo() = DriveInfo.GetDrives()
            For Each drive In drives
                If appPath.Contains(drive.ToString) Then
                    Dim drive_to_use As New DriveInfo(drive.ToString)
                    Dim good_stuff As Integer = drive_to_use.TotalFreeSpace / 1024 / 1024 / 1024
                    Return Convert.ToString(good_stuff)
                    'not needed but you never know
                    Exit For
                End If
            Next

            'You shouldn't get here
            Return "ERR"
        Catch ex As Exception
            Return "ERR"
            LogUpdate(ex.Message, eLogLevel.Err)
        End Try
    End Function

    Public Shared Function ReturnVirtualMemory() As String
        'Dim VM_SIZE As String = ""
        'Try
        '    Dim WmiSelect As New ObjectQuery("Select * FROM Win32_OperatingSystem ")
        '    Dim searcher As New ManagementObjectSearcher(WmiSelect)

        '    For Each item In searcher.Get()
        '        MsgBox(item("TotalVirtualMemorySize") * 1KB) * 0.10)

        '        'VM_SIZE = WmiResults.GetPropertyValue("TotalVirtualMemory").ToString

        '        '  Exit For


        '    Next
        '    Return VM_SIZE

        'Catch ex As Exception
        '    Return "ERR"
        '    LogUpdate(ex.Message, eLogLevel.Err)
        'End Try

        Try
            'Get-WmiObject Win32_PageFileusage | Select-Object AllocatedBaseSize
            Dim args As String
            Dim p As Process = New Process
            Dim output As String
            args = "Get-WmiObject Win32_PageFileusage | Select-Object AllocatedBaseSize | foreach { $_.AllocatedBaseSize }"
            With p
                .StartInfo.CreateNoWindow = True
                .StartInfo.UseShellExecute = False
                .StartInfo.RedirectStandardOutput = True
                .StartInfo.FileName = "powershell.exe"
                .StartInfo.Arguments = args
                .Start()
                output = .StandardOutput.ReadToEnd.Trim
            End With
            Return output
        Catch ex As Exception
            Return "0"
        End Try

    End Function

    Public Shared Function ReturnAIOsetting(ASettings As String) As String
        Try

            Dim ThingToReturn As String
            ThingToReturn = ""
            Try
                Dim SettingsConfig As mAIOS = GetAIOSettings()
                Dim TheConfig As List(Of MAINAPP) = SettingsConfig.AIOSs.MAINAPPs.ToList
                Try


                    ThingToReturn = TheConfig.Find(Function(x) x.type = ASettings).value
                Catch ex As Exception

                End Try

                Return ThingToReturn
            Catch ex As Exception
                LogUpdate(ex.Message, eLogLevel.Err)
            End Try
            Return ThingToReturn


        Catch ex As Exception
            'Error Reading the file, look at the backup

        End Try

    End Function

    Public Shared Function GetWTMString()
        Dim Beginning As String
        Beginning = "https://whattomine.com/coins?utf8=%E2%9C%93"

        Dim Middle As String = ""

        Dim Ending As String
        '&l2z=true&eth=true&grof=true&x11gf=true&cn=true&eq=true&lre=true&ns=true&lbry=true&bk2bf=true&bk14=true&pas=true&skh=true&n5=true&factor%5Bcost%5D=0.1

        Ending = "&factor%5Bcost%5D=" + PubShared.powercosts + "&sort=Profitability24&volume=0&revenue=24h&factor%5Bexchanges%5D%5B%5D=&factor%5Bexchanges%5D%5B%5D=abucoins&factor%5Bexchanges%5D%5B%5D=bitfinex&factor%5Bexchanges%5D%5B%5D=bittrex&factor%5Bexchanges%5D%5B%5D=cryptopia&factor%5Bexchanges%5D%5B%5D=hitbtc&factor%5Bexchanges%5D%5B%5D=poloniex&factor%5Bexchanges%5D%5B%5D=yobit&dataset=Main&commit=Calculate"
        '&xn=true&factor%5Bxn_hr%5D=4.8&

        'This fucking shit right here....whoa
        Dim eth As Integer = 0
        Dim ethash_pwr As Integer = 0
        Dim zhash As Integer = 0
        Dim zhash_pwr As Integer = 0
        Dim cngpu_pwr As Integer = 0
        Dim cngpu As Integer = 0
        Dim CNHeavy As Integer = 0
        Dim CNHeavy_pwr As Integer = 0
        Dim CNFast As Integer = 0
        Dim CNFast_pwr As Integer = 0
        Dim Aion As Integer = 0
        Dim Aion_pwr As Integer = 0
        Dim cuckaroo29 As Integer = 0
        Dim cuckaroo29_pwr As Integer = 0
        Dim bcd As Integer = 0
        Dim bcd_pwr As Integer = 0
        Dim x25x As Integer = 0
        Dim x25x_pwr As Integer = 0
        Dim mtp As Integer = 0
        Dim mtp_pwr As Integer = 0
        Dim PHI1612 As Integer = 0
        Dim PHI1612_pwr As Integer = 0
        Dim CryptoNightR As Integer = 0
        Dim CryptoNightR_pwr As Integer = 0
        'Dim CryptoNightV7 As Integer = 0
        'Dim Lyra2REv2 As Integer = 0
        Dim ProgPow As Integer = 0
        Dim ProgPow_pwr As Integer = 0
        Dim NeoScrypt As Integer = 0
        Dim NeoScrypt_pwr As Integer = 0
        Dim TimeTravel10 As Integer = 0
        Dim TimeTravel10_pwr As Integer = 0
        Dim X16R As Integer = 0
        Dim X16R_pwr As Integer = 0
        Dim Xevan As Integer = 0
        Dim Xevan_pwr As Integer = 0
        Dim phi2 As Integer = 0
        Dim phi2_pwr As Integer = 0
        Dim Lyrarev3 As Integer = 0
        Dim Lyrarev3_pwr As Integer = 0
        Dim CuckooCycle As Integer = 0
        Dim CuckooCycle_pwr As Integer = 0
        Dim cuckatoo31 As Integer = 0
        Dim cuckatoo31_pwr As Integer = 0
        Dim hex As Integer = 0
        Dim hex_pwr As Integer = 0
        Dim devicename As Integer
        Dim cnr_pwr As Integer = 0
        Dim beam_pwr As Integer = 0
        Dim timetravel_pwr As Integer = 0
        Dim lyra2rev3_pwr As Integer = 0





        If PubShared.three80 > 0 Then
            devicename = PubShared.three80
            Lyrarev3 = Lyrarev3 + (0 * devicename)
            CuckooCycle = CuckooCycle + (0 * devicename)
            eth = eth + (20 * devicename)
            TimeTravel10 = TimeTravel10 + (4.5 * devicename)
            PHI1612 = PHI1612 + (0.0 * devicename)
            X16R = X16R + (0.0 * devicename)
            CryptoNightR = CryptoNightR + (530 * devicename)
            ProgPow = ProgPow + (4.8 * devicename)
            NeoScrypt = NeoScrypt + (350 * devicename)
            phi2 = phi2 + (0.0 * devicename)
            zhash = zhash + (0.0 * devicename)
            hex = hex + (0.0 * devicename)
            CNHeavy = CNHeavy + (500 * devicename)
            Xevan = Xevan + (0.0 * devicename)
            CNFast = CNFast + (930.0 * devicename)
            Aion = Aion + (75.0 * devicename)
            cuckaroo29 = cuckaroo29 + (0.00 * devicename)
            bcd = bcd + (4.9 * devicename)
            x25x = x25x + (0.45 * devicename)
            mtp = mtp + (0.00 * devicename)
            cuckatoo31 = cuckatoo31 + (0.0 * devicename)
            'Power
            'Lyrarev3_pwr = Lyrarev3_pwr + ( * devicename)
            'CuckooCycle_pwr = CuckooCycle_pwr + ( * devicename)
            'eth_pwr = eth_pwr + ( * devicename)
            'TimeTravel10_pwr = TimeTravel10_pwr + ( * devicename)
            'PHI1612_pwr = PHI1612_pwr + ( * devicename)
            'X16R_pwr = X16R_pwr + ( * devicename)
            'CryptoNightR_pwr = CryptoNightR_pwr + (* devicename)
            'ProgPow_pwr = ProgPow_pwr + ( * devicename)
            'NeoScrypt_pwr = NeoScrypt_pwr + ( * devicename)
            'phi2_pwr = phi2_pwr + ( * devicename)
            'zhash_pwr = zhash_pwr + (* devicename)
            'hex_pwr = hex_pwr + ( * devicename)
            'CNHeavy_pwr = CNHeavy_pwr + (* devicename)
            'Xevan_pwr = Xevan_pwr + ( * devicename)
            'CNFast_pwr = CNFast_pwr + ( * devicename)
            'Aion_pwr = Aion_pwr + ( * devicename)
            'cuckaroo29_pwr = cuckaroo29_pwr + ( * devicename)
            'bcd_pwr = bcd_pwr + (* devicename)
            'x25x_pwr = x25x_pwr + ( * devicename)
            'mtp_pwr = mtp_pwr + ( * devicename)
            'cuckatoo31_pwr = cuckatoo31_pwr + ( * devicename)
            'PubShared
            Middle = Middle & "&adapt_380=true&adapt_q_=380=" & PubShared.three80.ToString
        End If

        If PubShared.fury > 0 Then
            devicename = PubShared.fury
            eth = eth + (16.5 * devicename)
            TimeTravel10 = TimeTravel10 + (0 * devicename)
            PHI1612 = PHI1612 + (0.0 * devicename)
            X16R = X16R + (0.0 * devicename)

            CryptoNightR = CryptoNightR + (0 * devicename)
            ProgPow = ProgPow + (6.6 * devicename)

            NeoScrypt = NeoScrypt + (1250 * devicename)

            phi2 = phi2 + (0.0 * devicename)
            zhash = zhash + (0.0 * devicename)
            hex = hex + (0.0 * devicename)
            CNHeavy = CNHeavy + (400 * devicename)

            Xevan = Xevan + (0.0 * devicename)

            CNFast = CNFast + (900.0 * devicename)
            Aion = Aion + (140.0 * devicename)
            cuckaroo29 = cuckaroo29 + (0.00 * devicename)
            bcd = bcd + (8.2 * devicename)
            x25x = x25x + (1.1 * devicename)
            mtp = mtp + (0.00 * devicename)
            cuckatoo31 = cuckatoo31 + (0.0 * devicename)
            Middle = Middle & "&adapt_fury=true&adapt_q_fury=" & PubShared.fury.ToString
        End If

        If PubShared.four70 > 0 Then
            devicename = PubShared.four70
            Lyrarev3 = Lyrarev3 + (32 * devicename)
            CuckooCycle = CuckooCycle + (0 * devicename)
            eth = eth + (26 * devicename)
            TimeTravel10 = TimeTravel10 + (7.5 * devicename)
            PHI1612 = PHI1612 + (10.0 * devicename)
            X16R = X16R + (4.5 * devicename)

            CryptoNightR = CryptoNightR + (660.0 * devicename)
            ProgPow = ProgPow + (6.4 * devicename)

            NeoScrypt = NeoScrypt + (680 * devicename)

            phi2 = phi2 + (0 * devicename)
            zhash = zhash + (0.0 * devicename)
            hex = hex + (0.0 * devicename)
            CNHeavy = CNHeavy + (590 * devicename)

            Xevan = Xevan + (0.0 * devicename)

            CNFast = CNFast + (1150.0 * devicename)
            Aion = Aion + (80.0 * devicename)
            cuckaroo29 = cuckaroo29 + (0.00 * devicename)
            bcd = bcd + (8.2 * devicename)
            x25x = x25x + (0.65 * devicename)
            mtp = mtp + (0.00 * devicename)
            cuckatoo31 = cuckatoo31 + (0.2 * devicename)
            'POwer
            ethash_pwr = ethash_pwr + (120 * devicename)
            zhash_pwr = zhash_pwr + (110 * devicename)
            CNHeavy_pwr = CNHeavy_pwr + (100 * devicename)
            cngpu_pwr = cngpu_pwr + (110 * devicename)
            cnr_pwr = cnr_pwr + (120 * devicename)
            CNFast_pwr = CNFast_pwr + (100 * devicename)
            Aion_pwr = Aion_pwr + (110 * devicename)
            CuckooCycle_pwr = CuckooCycle_pwr + (0 * devicename)
            cuckaroo29_pwr = cuckaroo29_pwr + (0 * devicename)
            cuckatoo31_pwr = cuckatoo31_pwr + (110 * devicename)
            beam_pwr = beam_pwr + (105 * devicename)
            NeoScrypt_pwr = NeoScrypt_pwr + (140 * devicename)
            bcd_pwr = bcd_pwr + (120 * devicename)
            timetravel_pwr = timetravel_pwr + (120 * devicename)
            X16R_pwr = X16R_pwr + (120 * devicename)
            phi2_pwr = phi2_pwr + (0 * devicename)
            Xevan_pwr = Xevan_pwr + (0 * devicename)
            hex_pwr = hex_pwr + (120 * devicename)
            PHI1612_pwr = PHI1612_pwr + (120 * devicename)
            ProgPow_pwr = ProgPow_pwr + (130 * devicename)
            x25x_pwr = x25x_pwr + (75 * devicename)
            mtp_pwr = mtp_pwr + (0 * devicename)
            lyra2rev3_pwr = lyra2rev3_pwr + (130 * devicename)

            Middle = Middle & "&adapt_470=true&adapt_q_470=" & PubShared.four70.ToString
        End If

        If PubShared.four80 > 0 Then
            devicename = PubShared.four80
            Lyrarev3 = Lyrarev3 + (39 * devicename)
            CuckooCycle = CuckooCycle + (0 * devicename)
            eth = eth + (29.5 * devicename)
            PHI1612 = PHI1612 + (15.0 * devicename)

            TimeTravel10 = TimeTravel10 + (9.0 * devicename)
            X16R = X16R + (7.0 * devicename)
            CryptoNightR = CryptoNightR + (830 * devicename)
            ProgPow = ProgPow + (7.9 * devicename)

            NeoScrypt = NeoScrypt + (820 * devicename)

            phi2 = phi2 + (0 * devicename)
            zhash = zhash + (0.0 * devicename)
            hex = hex + (2.7 * devicename)
            CNHeavy = CNHeavy + (900 * devicename)

            Xevan = Xevan + (1.6 * devicename)

            CNFast = CNFast + (1250.0 * devicename)
            Aion = Aion + (85.0 * devicename)
            cuckaroo29 = cuckaroo29 + (0.0 * devicename)
            bcd = bcd + (8.6 * devicename)
            x25x = x25x + (4.3 * devicename)
            mtp = mtp + (0.0 * devicename)
            CNFast = CNFast + (1650.0 * devicename)
            Aion = Aion + (95.0 * devicename)
            cuckaroo29 = cuckaroo29 + (1.45 * devicename)
            bcd = bcd + (10.1 * devicename)
            x25x = x25x + (0.83 * devicename)
            mtp = mtp + (0.6 * devicename)
            cuckatoo31 = cuckatoo31 + (0.38 * devicename)
            'Power
            ethash_pwr = ethash_pwr + (135 * devicename)
            zhash_pwr = zhash_pwr + (120 * devicename)
            CNHeavy_pwr = CNHeavy_pwr + (110 * devicename)
            cngpu_pwr = cngpu_pwr + (120 * devicename)
            cnr_pwr = cnr_pwr + (130 * devicename)
            CNFast_pwr = CNFast_pwr + (110 * devicename)
            Aion_pwr = Aion_pwr + (120 * devicename)
            CuckooCycle_pwr = CuckooCycle_pwr + (0 * devicename)
            cuckaroo29_pwr = cuckaroo29_pwr + (130 * devicename)
            cuckatoo31_pwr = cuckatoo31_pwr + (120 * devicename)
            beam_pwr = beam_pwr + (120 * devicename)
            NeoScrypt_pwr = NeoScrypt_pwr + (150 * devicename)
            bcd_pwr = bcd_pwr + (130 * devicename)
            timetravel_pwr = timetravel_pwr + (130 * devicename)
            X16R_pwr = X16R_pwr + (130 * devicename)
            phi2_pwr = phi2_pwr + (0 * devicename)
            Xevan_pwr = Xevan_pwr + (120 * devicename)
            hex_pwr = hex_pwr + (130 * devicename)
            PHI1612_pwr = PHI1612_pwr + (130 * devicename)
            ProgPow_pwr = ProgPow_pwr + (140 * devicename)
            x25x_pwr = x25x_pwr + (80 * devicename)
            mtp_pwr = mtp_pwr + (130 * devicename)
            lyra2rev3_pwr = lyra2rev3_pwr + (140 * devicename)
            Middle = Middle & "&adapt_480=true&adapt_q_480=" & PubShared.four80.ToString

        End If

        If PubShared.five70 > 0 Then
            devicename = PubShared.five70
            Lyrarev3 = Lyrarev3 + (35 * devicename)
            CuckooCycle = CuckooCycle + (0 * devicename)
            eth = eth + (27.9 * devicename)
            PHI1612 = PHI1612 + (13.0 * devicename)

            TimeTravel10 = TimeTravel10 + (8.0 * devicename)
            X16R = X16R + (5.0 * devicename)
            CryptoNightR = CryptoNightR + (730 * devicename)
            ProgPow = ProgPow + (6.7 * devicename)

            NeoScrypt = NeoScrypt + (700 * devicename)

            phi2 = phi2 + (13 * devicename)
            zhash = zhash + (0.0 * devicename)
            hex = hex + (0.0 * devicename)
            CNHeavy = CNHeavy + (610 * devicename)

            Xevan = Xevan + (0.0 * devicename)


            CNFast = CNFast + (1650.0 * devicename)
            Aion = Aion + (95.0 * devicename)
            cuckaroo29 = cuckaroo29 + (1.45 * devicename)
            bcd = bcd + (10.1 * devicename)
            x25x = x25x + (0.7 * devicename)
            mtp = mtp + (0.6 * devicename)
            cuckatoo31 = cuckatoo31 + (0.2 * devicename)
            'power
            ethash_pwr = ethash_pwr + (120 * devicename)
            zhash_pwr = zhash_pwr + (100 * devicename)
            CNHeavy_pwr = CNHeavy_pwr + (110 * devicename)
            cngpu_pwr = cngpu_pwr + (110 * devicename)
            cnr_pwr = cnr_pwr + (120 * devicename)
            CNFast_pwr = CNFast_pwr + (110 * devicename)
            Aion_pwr = Aion_pwr + (100 * devicename)
            CuckooCycle_pwr = CuckooCycle_pwr + (0 * devicename)
            cuckaroo29_pwr = cuckaroo29_pwr + (0 * devicename)
            cuckatoo31_pwr = cuckatoo31_pwr + (100 * devicename)
            beam_pwr = beam_pwr + (110 * devicename)
            NeoScrypt_pwr = NeoScrypt_pwr + (140 * devicename)
            bcd_pwr = bcd_pwr + (110 * devicename)
            timetravel_pwr = timetravel_pwr + (110 * devicename)
            X16R_pwr = X16R_pwr + (110 * devicename)
            phi2_pwr = phi2_pwr + (0 * devicename)
            Xevan_pwr = Xevan_pwr + (0 * devicename)
            hex_pwr = hex_pwr + (110 * devicename)
            PHI1612_pwr = PHI1612_pwr + (120 * devicename)
            ProgPow_pwr = ProgPow_pwr + (130 * devicename)
            x25x_pwr = x25x_pwr + (75 * devicename)
            mtp_pwr = mtp_pwr + (0 * devicename)
            lyra2rev3_pwr = lyra2rev3_pwr + (120 * devicename)

            Middle = Middle & "&adapt_570=true&adapt_q_570=" & PubShared.five70.ToString

        End If

        If PubShared.five80 > 0 Then
            devicename = PubShared.five80
            Lyrarev3 = Lyrarev3 + (39 * devicename)
            CuckooCycle = CuckooCycle + (0 * devicename)
            eth = eth + (30.2 * devicename)
            PHI1612 = PHI1612 + (15.0 * devicename)

            TimeTravel10 = TimeTravel10 + (8.2 * devicename)
            X16R = X16R + (7.0 * devicename)
            CryptoNightR = CryptoNightR + (830 * devicename)
            ProgPow = ProgPow + (7.9 * devicename)

            NeoScrypt = NeoScrypt + (820 * devicename)

            phi2 = phi2 + (0 * devicename)
            zhash = zhash + (0.0 * devicename)
            hex = hex + (0.0 * devicename)
            CNHeavy = CNHeavy + (900 * devicename)

            Xevan = Xevan + (1.6 * devicename)


            CNFast = CNFast + (1650.0 * devicename)
            Aion = Aion + (95.0 * devicename)
            cuckaroo29 = cuckaroo29 + (1.45 * devicename)
            bcd = bcd + (10.1 * devicename)
            x25x = x25x + (0.8 * devicename)
            mtp = mtp + (0.6 * devicename)
            cuckatoo31 = cuckatoo31 + (0.35 * devicename)
            'power
            ethash_pwr = ethash_pwr + (135 * devicename)
            zhash_pwr = zhash_pwr + (110 * devicename)
            CNHeavy_pwr = CNHeavy_pwr + (115 * devicename)
            cngpu_pwr = cngpu_pwr + (120 * devicename)
            cnr_pwr = cnr_pwr + (130 * devicename)
            CNFast_pwr = CNFast_pwr + (115 * devicename)
            Aion_pwr = Aion_pwr + (110 * devicename)
            CuckooCycle_pwr = CuckooCycle_pwr + (0 * devicename)
            cuckaroo29_pwr = cuckaroo29_pwr + (120 * devicename)
            cuckatoo31_pwr = cuckatoo31_pwr + (110 * devicename)
            beam_pwr = beam_pwr + (120 * devicename)
            NeoScrypt_pwr = NeoScrypt_pwr + (150 * devicename)
            bcd_pwr = bcd_pwr + (120 * devicename)
            timetravel_pwr = timetravel_pwr + (120 * devicename)
            X16R_pwr = X16R_pwr + (120 * devicename)
            phi2_pwr = phi2_pwr + (0 * devicename)
            Xevan_pwr = Xevan_pwr + (120 * devicename)
            hex_pwr = hex_pwr + (120 * devicename)
            PHI1612_pwr = PHI1612_pwr + (130 * devicename)
            ProgPow_pwr = ProgPow_pwr + (140 * devicename)
            x25x_pwr = x25x_pwr + (80 * devicename)
            mtp_pwr = mtp_pwr + (120 * devicename)
            lyra2rev3_pwr = lyra2rev3_pwr + (130 * devicename)

            Middle = Middle & "&adapt_580=true&adapt_q_580=" & PubShared.five80.ToString
        End If

        If PubShared.vega > 0 Then
            devicename = PubShared.vega
            Lyrarev3 = Lyrarev3 + (59 * devicename)
            CuckooCycle = CuckooCycle + (0 * devicename)
            eth = eth + (40 * devicename)
            PHI1612 = PHI1612 + (0.0 * devicename)
            TimeTravel10 = TimeTravel10 + (16.5 * devicename)
            X16R = X16R + (13 * devicename)
            CryptoNightR = CryptoNightR + (1800 * devicename)
            ProgPow = ProgPow + (17 * devicename)
            NeoScrypt = NeoScrypt + (2000 * devicename)
            phi2 = phi2 + (0.0 * devicename)
            zhash = zhash + (0.0 * devicename)
            hex = hex + (0.0 * devicename)
            CNHeavy = CNHeavy + (1400 * devicename)
            Xevan = Xevan + (0 * devicename)
            CNFast = CNFast + (3700.0 * devicename)
            Aion = Aion + (145.0 * devicename)
            cuckaroo29 = cuckaroo29 + (1.9 * devicename)
            bcd = bcd + (14.6 * devicename)
            x25x = x25x + (2.1 * devicename)
            mtp = mtp + (0.00 * devicename)
            cuckatoo31 = cuckatoo31 + (0.8 * devicename)
            'power
            ethash_pwr = ethash_pwr + (230 * devicename)
            zhash_pwr = zhash_pwr + (250 * devicename)
            CNHeavy_pwr = CNHeavy_pwr + (220 * devicename)
            cngpu_pwr = cngpu_pwr + (270 * devicename)
            cnr_pwr = cnr_pwr + (270 * devicename)
            CNFast_pwr = CNFast_pwr + (220 * devicename)
            Aion_pwr = Aion_pwr + (210 * devicename)
            CuckooCycle_pwr = CuckooCycle_pwr + (0 * devicename)
            cuckaroo29_pwr = cuckaroo29_pwr + (250 * devicename)
            cuckatoo31_pwr = cuckatoo31_pwr + (250 * devicename)
            beam_pwr = beam_pwr + (180 * devicename)
            NeoScrypt_pwr = NeoScrypt_pwr + (250 * devicename)
            bcd_pwr = bcd_pwr + (250 * devicename)
            timetravel_pwr = timetravel_pwr + (250 * devicename)
            X16R_pwr = X16R_pwr + (250 * devicename)
            phi2_pwr = phi2_pwr + (0 * devicename)
            Xevan_pwr = Xevan_pwr + (0 * devicename)
            hex_pwr = hex_pwr + (250 * devicename)
            PHI1612_pwr = PHI1612_pwr + (0 * devicename)
            ProgPow_pwr = ProgPow_pwr + (270 * devicename)
            x25x_pwr = x25x_pwr + (160 * devicename)
            mtp_pwr = mtp_pwr + (0 * devicename)
            lyra2rev3_pwr = lyra2rev3_pwr + (240 * devicename)

            Middle = Middle & "&adapt_vega64=true&adapt_q_vega64=" & PubShared.vega.ToString
        End If

        If PubShared.vii > 0 Then
            devicename = PubShared.vii
            Lyrarev3 = Lyrarev3 + (90 * devicename)
            CuckooCycle = CuckooCycle + (5 * devicename)
            eth = eth + (78 * devicename)
            PHI1612 = PHI1612 + (0.0 * devicename)
            TimeTravel10 = TimeTravel10 + (30 * devicename)
            X16R = X16R + (20 * devicename)
            CryptoNightR = CryptoNightR + (2800 * devicename)
            ProgPow = ProgPow + (25 * devicename)
            NeoScrypt = NeoScrypt + (2150 * devicename)
            phi2 = phi2 + (0.0 * devicename)
            zhash = zhash + (49 * devicename)
            hex = hex + (19 * devicename)
            CNHeavy = CNHeavy + (2200 * devicename)
            Xevan = Xevan + (0 * devicename)
            CNFast = CNFast + (5200 * devicename)
            Aion = Aion + (160.0 * devicename)
            cuckaroo29 = cuckaroo29 + (5 * devicename)
            bcd = bcd + (23 * devicename)
            x25x = x25x + (2.45 * devicename)
            mtp = mtp + (0.00 * devicename)

            cuckatoo31 = cuckatoo31 + (0.95 * devicename)
            'Power
            ethash_pwr = ethash_pwr + (230 * devicename)
            zhash_pwr = zhash_pwr + (180 * devicename)
            CNHeavy_pwr = CNHeavy_pwr + (170 * devicename)
            cngpu_pwr = cngpu_pwr + (240 * devicename)
            cnr_pwr = cnr_pwr + (240 * devicename)
            CNFast_pwr = CNFast_pwr + (230 * devicename)
            Aion_pwr = Aion_pwr + (160 * devicename)
            CuckooCycle_pwr = CuckooCycle_pwr + (190 * devicename)
            cuckaroo29_pwr = cuckaroo29_pwr + (170 * devicename)
            cuckatoo31_pwr = cuckatoo31_pwr + (200 * devicename)
            beam_pwr = beam_pwr + (190 * devicename)
            NeoScrypt_pwr = NeoScrypt_pwr + (250 * devicename)
            bcd_pwr = bcd_pwr + (240 * devicename)
            timetravel_pwr = timetravel_pwr + (240 * devicename)
            X16R_pwr = X16R_pwr + (230 * devicename)
            phi2_pwr = phi2_pwr + (0 * devicename)
            Xevan_pwr = Xevan_pwr + (0 * devicename)
            hex_pwr = hex_pwr + (240 * devicename)
            PHI1612_pwr = PHI1612_pwr + (0 * devicename)
            ProgPow_pwr = ProgPow_pwr + (270 * devicename)
            x25x_pwr = x25x_pwr + (140 * devicename)
            mtp_pwr = mtp_pwr + (0 * devicename)
            lyra2rev3_pwr = lyra2rev3_pwr + (240 * devicename)
            Middle = Middle & "&adapt_vii=true&adapt_q_vii=" & PubShared.vii.ToString
        End If

        'Nvidia
        If PubShared.ten50ti > 0 Then
            devicename = PubShared.ten50ti
            Lyrarev3 = Lyrarev3 + (18.8 * devicename)
            CuckooCycle = CuckooCycle + (1.8 * devicename)
            eth = eth + (13.9 * devicename)
            phi2 = phi2 + (2.4 * devicename)
            zhash = zhash + (17 * devicename)
            hex = hex + (5 * devicename)
            CNHeavy = CNHeavy + (390 * devicename)
            PHI1612 = PHI1612 + (9.5 * devicename)
            TimeTravel10 = TimeTravel10 + (11 * devicename)
            X16R = X16R + (6 * devicename)
            CryptoNightR = CryptoNightR + (250 * devicename)
            ProgPow = ProgPow + (5.8 * devicename)
            NeoScrypt = NeoScrypt + (420 * devicename)
            Xevan = Xevan + (1.7 * devicename)
            CNFast = CNFast + (640.0 * devicename)
            Aion = Aion + (80.0 * devicename)
            cuckaroo29 = cuckaroo29 + (0.00 * devicename)
            bcd = bcd + (8.5 * devicename)
            x25x = x25x + (0.55 * devicename)
            mtp = mtp + (0.00 * devicename)
            cuckatoo31 = cuckatoo31 + (0.0 * devicename)
            'Power
            ethash_pwr = ethash_pwr + (70 * devicename)
            zhash_pwr = zhash_pwr + (75 * devicename)
            CNHeavy_pwr = CNHeavy_pwr + (50 * devicename)
            cngpu_pwr = cngpu_pwr + (70 * devicename)
            cnr_pwr = cnr_pwr + (50 * devicename)
            CNFast_pwr = CNFast_pwr + (50 * devicename)
            Aion_pwr = Aion_pwr + (75 * devicename)
            CuckooCycle_pwr = CuckooCycle_pwr + (75 * devicename)
            cuckaroo29_pwr = cuckaroo29_pwr + (0 * devicename)
            cuckatoo31_pwr = cuckatoo31_pwr + (0 * devicename)
            beam_pwr = beam_pwr + (75 * devicename)
            NeoScrypt_pwr = NeoScrypt_pwr + (75 * devicename)
            bcd_pwr = bcd_pwr + (75 * devicename)
            timetravel_pwr = timetravel_pwr + (75 * devicename)
            X16R_pwr = X16R_pwr + (75 * devicename)
            phi2_pwr = phi2_pwr + (75 * devicename)
            Xevan_pwr = Xevan_pwr + (75 * devicename)
            hex_pwr = hex_pwr + (75 * devicename)
            PHI1612_pwr = PHI1612_pwr + (75 * devicename)
            ProgPow_pwr = ProgPow_pwr + (75 * devicename)
            x25x_pwr = x25x_pwr + (75 * devicename)
            mtp_pwr = mtp_pwr + (0 * devicename)
            lyra2rev3_pwr = lyra2rev3_pwr + (75 * devicename)

            Middle = Middle & "&adapt_1050Ti=true&adapt_q_1050Ti=" & PubShared.ten50ti.ToString

        End If
        If PubShared.ten60 > 0 Then
            devicename = PubShared.ten60
            Lyrarev3 = Lyrarev3 + (26.5 * devicename)
            CuckooCycle = CuckooCycle + (3.3 * devicename)
            eth = eth + (22.5 * devicename)
            PHI1612 = PHI1612 + (13.5 * devicename)

            TimeTravel10 = TimeTravel10 + (11 * devicename)
            X16R = X16R + (5 * devicename)
            CryptoNightR = CryptoNightR + (390 * devicename)
            ProgPow = ProgPow + (7.8 * devicename)

            NeoScrypt = NeoScrypt + (680 * devicename)

            phi2 = phi2 + (3.9 * devicename)
            zhash = zhash + (27 * devicename)
            hex = hex + (7.5 * devicename)
            CNHeavy = CNHeavy + (590 * devicename)
            Xevan = Xevan + (2.3 * devicename)

            CNFast = CNFast + (1000.0 * devicename)
            Aion = Aion + (120.0 * devicename)
            cuckaroo29 = cuckaroo29 + (2.4 * devicename)
            bcd = bcd + (11.8 * devicename)
            x25x = x25x + (0.8 * devicename)
            mtp = mtp + (1.1 * devicename)
            cuckatoo31 = cuckatoo31 + (0.0 * devicename)
            Middle = Middle & "&adapt_10606=true&adapt_q_10606=" & PubShared.ten60.ToString

        End If


        If PubShared.ten70 > 0 Then
            devicename = PubShared.ten70
            Lyrarev3 = Lyrarev3 + (44.6 * devicename)
            CuckooCycle = CuckooCycle + (4.7 * devicename)
            eth = eth + (30 * devicename)
            PHI1612 = PHI1612 + (23 * devicename)
            TimeTravel10 = TimeTravel10 + (25.5 * devicename)
            X16R = X16R + (14 * devicename)
            CryptoNightR = CryptoNightR + (600 * devicename)
            ProgPow = ProgPow + (13 * devicename)
            NeoScrypt = NeoScrypt + (1150 * devicename)
            Xevan = Xevan + (3.9 * devicename)
            phi2 = phi2 + (5.6 * devicename)
            zhash = zhash + (43 * devicename)
            hex = hex + (11 * devicename)
            CNHeavy = CNHeavy + (800 * devicename)
            CNFast = CNFast + (1400.0 * devicename)
            Aion = Aion + (200.0 * devicename)
            cuckaroo29 = cuckaroo29 + (4.0 * devicename)
            bcd = bcd + (19.9 * devicename)
            x25x = x25x + (1.3 * devicename)
            mtp = mtp + (1.8 * devicename)
            'Power
            ethash_pwr = ethash_pwr + (120 * devicename)
            zhash_pwr = zhash_pwr + (130 * devicename)
            CNHeavy_pwr = CNHeavy_pwr + (110 * devicename)
            cngpu_pwr = cngpu_pwr + (130 * devicename)
            cnr_pwr = cnr_pwr + (110 * devicename)
            CNFast_pwr = CNFast_pwr + (110 * devicename)
            Aion_pwr = Aion_pwr + (130 * devicename)
            CuckooCycle_pwr = CuckooCycle_pwr + (130 * devicename)
            cuckaroo29_pwr = cuckaroo29_pwr + (130 * devicename)
            cuckatoo31_pwr = cuckatoo31_pwr + (0 * devicename)
            beam_pwr = beam_pwr + (130 * devicename)
            NeoScrypt_pwr = NeoScrypt_pwr + (130 * devicename)
            bcd_pwr = bcd_pwr + (130 * devicename)
            timetravel_pwr = timetravel_pwr + (130 * devicename)
            X16R_pwr = X16R_pwr + (130 * devicename)
            phi2_pwr = phi2_pwr + (130 * devicename)
            Xevan_pwr = Xevan_pwr + (130 * devicename)
            hex_pwr = hex_pwr + (130 * devicename)
            PHI1612_pwr = PHI1612_pwr + (130 * devicename)
            ProgPow_pwr = ProgPow_pwr + (130 * devicename)
            x25x_pwr = x25x_pwr + (130 * devicename)
            mtp_pwr = mtp_pwr + (130 * devicename)
            lyra2rev3_pwr = lyra2rev3_pwr + (130 * devicename)

            'Middle = Middle & "&eth=true&factor%5Beth_hr%5D=29"
            Middle = Middle & "&adapt_1070=true&adapt_q_1070=" & PubShared.ten70.ToString
        End If


        If PubShared.ten70ti > 0 Then
            devicename = PubShared.ten70ti
            Lyrarev3 = Lyrarev3 + (51.5 * devicename)
            CuckooCycle = CuckooCycle + (4.9 * devicename)
            eth = eth + (30.5 * devicename)
            PHI1612 = PHI1612 + (26.5 * devicename)

            TimeTravel10 = TimeTravel10 + (26 * devicename)
            X16R = X16R + (16 * devicename)
            CryptoNightR = CryptoNightR + (500 * devicename)
            ProgPow = ProgPow + (14 * devicename)

            NeoScrypt = NeoScrypt + (1200 * devicename)
            phi2 = phi2 + (6.5 * devicename)
            zhash = zhash + (44 * devicename)
            hex = hex + (11.2 * devicename)
            CNHeavy = CNHeavy + (830 * devicename)
            Xevan = Xevan + (4.6 * devicename)

            CNFast = CNFast + (1420.0 * devicename)
            Aion = Aion + (205.0 * devicename)
            cuckaroo29 = cuckaroo29 + (4.5 * devicename)
            bcd = bcd + (22.5 * devicename)
            x25x = x25x + (1.4 * devicename)
            mtp = mtp + (2.1 * devicename)
            cuckatoo31 = cuckatoo31 + (0.0 * devicename)
            'Power
            ethash_pwr = ethash_pwr + (130 * devicename)
            zhash_pwr = zhash_pwr + (130 * devicename)
            CNHeavy_pwr = CNHeavy_pwr + (110 * devicename)
            cngpu_pwr = cngpu_pwr + (130 * devicename)
            cnr_pwr = cnr_pwr + (110 * devicename)
            CNFast_pwr = CNFast_pwr + (110 * devicename)
            Aion_pwr = Aion_pwr + (130 * devicename)
            CuckooCycle_pwr = CuckooCycle_pwr + (130 * devicename)
            cuckaroo29_pwr = cuckaroo29_pwr + (130 * devicename)
            cuckatoo31_pwr = cuckatoo31_pwr + (0 * devicename)
            beam_pwr = beam_pwr + (130 * devicename)
            NeoScrypt_pwr = NeoScrypt_pwr + (130 * devicename)
            bcd_pwr = bcd_pwr + (130 * devicename)
            timetravel_pwr = timetravel_pwr + (130 * devicename)
            X16R_pwr = X16R_pwr + (130 * devicename)
            phi2_pwr = phi2_pwr + (130 * devicename)
            Xevan_pwr = Xevan_pwr + (130 * devicename)
            hex_pwr = hex_pwr + (130 * devicename)
            PHI1612_pwr = PHI1612_pwr + (130 * devicename)
            ProgPow_pwr = ProgPow_pwr + (130 * devicename)
            x25x_pwr = x25x_pwr + (130 * devicename)
            mtp_pwr = mtp_pwr + (130 * devicename)
            lyra2rev3_pwr = lyra2rev3_pwr + (130 * devicename)

            Middle = Middle & "&adapt_1070Ti=true&adapt_q_1070Ti=" & PubShared.ten70ti.ToString

        End If

        If PubShared.sixteen60 > 0 Then
            devicename = PubShared.sixteen60
            Lyrarev3 = Lyrarev3 + (42 * devicename)
            CuckooCycle = CuckooCycle + (3.7 * devicename)
            eth = eth + (20.5 * devicename)
            PHI1612 = PHI1612 + (25.8 * devicename)
            TimeTravel10 = TimeTravel10 + (26.0 * devicename)
            X16R = X16R + (15.7 * devicename)
            CryptoNightR = CryptoNightR + (520.0 * devicename)
            ProgPow = ProgPow + (9.8 * devicename)
            NeoScrypt = NeoScrypt + (550.0 * devicename)
            phi2 = phi2 + (5.6 * devicename)
            zhash = zhash + (34.8 * devicename)
            hex = hex + (13.0 * devicename)
            CNHeavy = CNHeavy + (700.0 * devicename)
            Xevan = Xevan + (3.8 * devicename)
            CNFast = CNFast + (1100.0 * devicename)
            Aion = Aion + (150.0 * devicename)
            cuckaroo29 = cuckaroo29 + (3.7 * devicename)
            bcd = bcd + (18.5 * devicename)
            x25x = x25x + (1.0 * devicename)
            mtp = mtp + (1.95 * devicename)
            cuckatoo31 = cuckatoo31 + (0.0 * devicename)
            'Power
            ethash_pwr = ethash_pwr + (90 * devicename)
            zhash_pwr = zhash_pwr + (90 * devicename)
            CNHeavy_pwr = CNHeavy_pwr + (70 * devicename)
            cngpu_pwr = cngpu_pwr + (90 * devicename)
            cnr_pwr = cnr_pwr + (70 * devicename)
            CNFast_pwr = CNFast_pwr + (70 * devicename)
            Aion_pwr = Aion_pwr + (90 * devicename)
            CuckooCycle_pwr = CuckooCycle_pwr + (90 * devicename)
            cuckaroo29_pwr = cuckaroo29_pwr + (90 * devicename)
            cuckatoo31_pwr = cuckatoo31_pwr + (0 * devicename)
            beam_pwr = beam_pwr + (90 * devicename)
            NeoScrypt_pwr = NeoScrypt_pwr + (90 * devicename)
            bcd_pwr = bcd_pwr + (90 * devicename)
            timetravel_pwr = timetravel_pwr + (90 * devicename)
            X16R_pwr = X16R_pwr + (90 * devicename)
            phi2_pwr = phi2_pwr + (90 * devicename)
            Xevan_pwr = Xevan_pwr + (90 * devicename)
            hex_pwr = hex_pwr + (90 * devicename)
            PHI1612_pwr = PHI1612_pwr + (90 * devicename)
            ProgPow_pwr = ProgPow_pwr + (90 * devicename)
            x25x_pwr = x25x_pwr + (90 * devicename)
            mtp_pwr = mtp_pwr + (90 * devicename)
            lyra2rev3_pwr = lyra2rev3_pwr + (90 * devicename)

            Middle = Middle & "&adapt_1660=true&adapt_q_1660=" & PubShared.sixteen60.ToString

        End If

        If PubShared.sixteen60ti > 0 Then
            devicename = PubShared.sixteen60ti
            Lyrarev3 = Lyrarev3 + (43 * devicename)
            CuckooCycle = CuckooCycle + (4.3 * devicename)
            eth = eth + (25.7 * devicename)
            PHI1612 = PHI1612 + (26.8 * devicename)
            TimeTravel10 = TimeTravel10 + (26.5 * devicename)
            X16R = X16R + (15.9 * devicename)
            CryptoNightR = CryptoNightR + (500.0 * devicename)
            ProgPow = ProgPow + (9.8 * devicename)
            NeoScrypt = NeoScrypt + (1050.0 * devicename)
            phi2 = phi2 + (5.6 * devicename)
            zhash = zhash + (34.8 * devicename)
            hex = hex + (13.0 * devicename)
            CNHeavy = CNHeavy + (650.0 * devicename)
            Xevan = Xevan + (4.0 * devicename)
            CNFast = CNFast + (1100.0 * devicename)
            Aion = Aion + (160.0 * devicename)
            cuckaroo29 = cuckaroo29 + (4.3 * devicename)
            bcd = bcd + (18.9 * devicename)
            x25x = x25x + (1.1 * devicename)
            mtp = mtp + (1.85 * devicename)
            cuckatoo31 = cuckatoo31 + (0.0 * devicename)
            Middle = Middle & "&adapt_1660ti=true&adapt_q_1660ti=" & PubShared.sixteen60ti.ToString

        End If
        If PubShared.ten80 > 0 Then
            devicename = PubShared.ten80
            Lyrarev3 = Lyrarev3 + (58.5 * devicename)
            CuckooCycle = CuckooCycle + (5.4 * devicename)
            eth = eth + (40 * devicename)
            PHI1612 = PHI1612 + (30.2 * devicename)

            TimeTravel10 = TimeTravel10 + (30.5 * devicename)
            X16R = X16R + (19 * devicename)
            CryptoNightR = CryptoNightR + (520 * devicename)
            ProgPow = ProgPow + (15.7 * devicename)

            NeoScrypt = NeoScrypt + (1500 * devicename)
            phi2 = phi2 + (6.7 * devicename)
            zhash = zhash + (47 * devicename)
            hex = hex + (13.5 * devicename)
            CNHeavy = CNHeavy + (700 * devicename)
            Xevan = Xevan + (5.1 * devicename)

            CNFast = CNFast + (1180.0 * devicename)
            Aion = Aion + (230.0 * devicename)
            cuckaroo29 = cuckaroo29 + (4.8 * devicename)
            bcd = bcd + (26.0 * devicename)
            x25x = x25x + (1.7 * devicename)
            mtp = mtp + (2.1 * devicename)
            cuckatoo31 = cuckatoo31 + (0.0 * devicename)
            'Power
            ethash_pwr = ethash_pwr + (150 * devicename)
            zhash_pwr = zhash_pwr + (150 * devicename)
            CNHeavy_pwr = CNHeavy_pwr + (110 * devicename)
            cngpu_pwr = cngpu_pwr + (150 * devicename)
            cnr_pwr = cnr_pwr + (110 * devicename)
            CNFast_pwr = CNFast_pwr + (110 * devicename)
            Aion_pwr = Aion_pwr + (150 * devicename)
            CuckooCycle_pwr = CuckooCycle_pwr + (150 * devicename)
            cuckaroo29_pwr = cuckaroo29_pwr + (150 * devicename)
            cuckatoo31_pwr = cuckatoo31_pwr + (0 * devicename)
            beam_pwr = beam_pwr + (150 * devicename)
            NeoScrypt_pwr = NeoScrypt_pwr + (150 * devicename)
            bcd_pwr = bcd_pwr + (150 * devicename)
            timetravel_pwr = timetravel_pwr + (150 * devicename)
            X16R_pwr = X16R_pwr + (150 * devicename)
            phi2_pwr = phi2_pwr + (160 * devicename)
            Xevan_pwr = Xevan_pwr + (150 * devicename)
            hex_pwr = hex_pwr + (160 * devicename)
            PHI1612_pwr = PHI1612_pwr + (150 * devicename)
            ProgPow_pwr = ProgPow_pwr + (160 * devicename)
            x25x_pwr = x25x_pwr + (150 * devicename)
            mtp_pwr = mtp_pwr + (150 * devicename)
            lyra2rev3_pwr = lyra2rev3_pwr + (150 * devicename)

            Middle = Middle & "&adapt_1080=true&adapt_q_1080=" & PubShared.ten80.ToString
        End If

        If PubShared.ten80ti > 0 Then
            devicename = PubShared.ten80ti
            Lyrarev3 = Lyrarev3 + (77 * devicename)
            CuckooCycle = CuckooCycle + (7.4 * devicename)
            eth = eth + (50 * devicename)
            PHI1612 = PHI1612 + (40.5 * devicename)
            TimeTravel10 = TimeTravel10 + (49.5 * devicename)
            X16R = X16R + (29.0 * devicename)
            CryptoNightR = CryptoNightR + (750 * devicename)
            ProgPow = ProgPow + (18.2 * devicename)
            NeoScrypt = NeoScrypt + (1900 * devicename)
            Xevan = Xevan + (6.9 * devicename)
            phi2 = phi2 + (8.9 * devicename)
            zhash = zhash + (63 * devicename)
            hex = hex + (17 * devicename)
            CNHeavy = CNHeavy + (1000 * devicename)

            CNFast = CNFast + (1730.0 * devicename)
            Aion = Aion + (300.0 * devicename)
            cuckaroo29 = cuckaroo29 + (6.3 * devicename)
            bcd = bcd + (35.0 * devicename)
            x25x = x25x + (2.3 * devicename)
            mtp = mtp + (1.9 * devicename)
            cuckatoo31 = cuckatoo31 + (1.45 * devicename)
            'Power
            ethash_pwr = ethash_pwr + (190 * devicename)
            zhash_pwr = zhash_pwr + (200 * devicename)
            CNHeavy_pwr = CNHeavy_pwr + (150 * devicename)
            cngpu_pwr = cngpu_pwr + (190 * devicename)
            cnr_pwr = cnr_pwr + (150 * devicename)
            CNFast_pwr = CNFast_pwr + (150 * devicename)
            Aion_pwr = Aion_pwr + (200 * devicename)
            CuckooCycle_pwr = CuckooCycle_pwr + (190 * devicename)
            cuckaroo29_pwr = cuckaroo29_pwr + (190 * devicename)
            cuckatoo31_pwr = cuckatoo31_pwr + (190 * devicename)
            beam_pwr = beam_pwr + (190 * devicename)
            NeoScrypt_pwr = NeoScrypt_pwr + (190 * devicename)
            bcd_pwr = bcd_pwr + (190 * devicename)
            timetravel_pwr = timetravel_pwr + (200 * devicename)
            X16R_pwr = X16R_pwr + (190 * devicename)
            phi2_pwr = phi2_pwr + (200 * devicename)
            Xevan_pwr = Xevan_pwr + (190 * devicename)
            hex_pwr = hex_pwr + (200 * devicename)
            PHI1612_pwr = PHI1612_pwr + (200 * devicename)
            ProgPow_pwr = ProgPow_pwr + (200 * devicename)
            x25x_pwr = x25x_pwr + (190 * devicename)
            mtp_pwr = mtp_pwr + (190 * devicename)
            lyra2rev3_pwr = lyra2rev3_pwr + (190 * devicename)

            Middle = Middle & "&adapt_1080Ti=true&adapt_q_1080Ti=" & PubShared.ten80ti.ToString
        End If
        If PubShared.twenty60 > 0 Then
            devicename = PubShared.twenty60
            Lyrarev3 = Lyrarev3 + (54 * devicename)
            CuckooCycle = CuckooCycle + (5.2 * devicename)
            eth = eth + (27.6 * devicename)
            TimeTravel10 = TimeTravel10 + (34 * devicename)
            PHI1612 = PHI1612 + (32.8 * devicename)
            X16R = X16R + (19.9 * devicename)
            CryptoNightR = CryptoNightR + (530 * devicename)
            ProgPow = ProgPow + (15.7 * devicename)
            NeoScrypt = NeoScrypt + (1300 * devicename)
            phi2 = phi2 + (7.5 * devicename)
            zhash = zhash + (48 * devicename)
            hex = hex + (17.1 * devicename)
            CNHeavy = CNHeavy + (730 * devicename)
            Xevan = Xevan + (5.0 * devicename)
            CNFast = CNFast + (1220 * devicename)
            Aion = Aion + (220 * devicename)
            cuckaroo29 = cuckaroo29 + (4.5 * devicename)
            bcd = bcd + (24.2 * devicename)
            x25x = x25x + (1.4 * devicename)
            mtp = mtp + (2.1 * devicename)
            cuckatoo31 = cuckatoo31 + (0.0 * devicename)
            'Power
            ethash_pwr = ethash_pwr + (130 * devicename)
            zhash_pwr = zhash_pwr + (130 * devicename)
            CNHeavy_pwr = CNHeavy_pwr + (100 * devicename)
            cngpu_pwr = cngpu_pwr + (130 * devicename)
            cnr_pwr = cnr_pwr + (110 * devicename)
            CNFast_pwr = CNFast_pwr + (100 * devicename)
            Aion_pwr = Aion_pwr + (130 * devicename)
            CuckooCycle_pwr = CuckooCycle_pwr + (130 * devicename)
            cuckaroo29_pwr = cuckaroo29_pwr + (130 * devicename)
            cuckatoo31_pwr = cuckatoo31_pwr + (0 * devicename)
            beam_pwr = beam_pwr + (130 * devicename)
            NeoScrypt_pwr = NeoScrypt_pwr + (130 * devicename)
            bcd_pwr = bcd_pwr + (130 * devicename)
            timetravel_pwr = timetravel_pwr + (130 * devicename)
            X16R_pwr = X16R_pwr + (130 * devicename)
            phi2_pwr = phi2_pwr + (130 * devicename)
            Xevan_pwr = Xevan_pwr + (130 * devicename)
            hex_pwr = hex_pwr + (130 * devicename)
            PHI1612_pwr = PHI1612_pwr + (130 * devicename)
            ProgPow_pwr = ProgPow_pwr + (130 * devicename)
            x25x_pwr = x25x_pwr + (130 * devicename)
            mtp_pwr = mtp_pwr + (130 * devicename)
            lyra2rev3_pwr = lyra2rev3_pwr + (130 * devicename)

            Middle = Middle & "&adapt_2060=true&adapt_q_2060=" & devicename.ToString
        End If



        If PubShared.twenty70 > 0 Then
            devicename = PubShared.twenty70
            Lyrarev3 = Lyrarev3 + (60.5 * devicename)
            CuckooCycle = CuckooCycle + (6.7 * devicename)
            eth = eth + (38 * devicename)
            TimeTravel10 = TimeTravel10 + (38.5 * devicename)
            PHI1612 = PHI1612 + (37 * devicename)
            X16R = X16R + (26 * devicename)
            CryptoNightR = CryptoNightR + (700 * devicename)
            ProgPow = ProgPow + (19.1 * devicename)
            NeoScrypt = NeoScrypt + (1700 * devicename)
            phi2 = phi2 + (8.6 * devicename)
            zhash = zhash + (55 * devicename)
            hex = hex + (19.5 * devicename)
            CNHeavy = CNHeavy + (1020 * devicename)

            Xevan = Xevan + (5.5 * devicename)

            CNFast = CNFast + (1650 * devicename)
            Aion = Aion + (250 * devicename)
            cuckaroo29 = cuckaroo29 + (6 * devicename)
            bcd = bcd + (27.5 * devicename)
            x25x = x25x + (1.6 * devicename)
            mtp = mtp + (2.6 * devicename)
            cuckatoo31 = cuckatoo31 + (0.0 * devicename)
            'power
            ethash_pwr = ethash_pwr + (150 * devicename)
            zhash_pwr = zhash_pwr + (150 * devicename)
            CNHeavy_pwr = CNHeavy_pwr + (120 * devicename)
            cngpu_pwr = cngpu_pwr + (150 * devicename)
            cnr_pwr = cnr_pwr + (140 * devicename)
            CNFast_pwr = CNFast_pwr + (120 * devicename)
            Aion_pwr = Aion_pwr + (150 * devicename)
            CuckooCycle_pwr = CuckooCycle_pwr + (150 * devicename)
            cuckaroo29_pwr = cuckaroo29_pwr + (150 * devicename)
            cuckatoo31_pwr = cuckatoo31_pwr + (0 * devicename)
            beam_pwr = beam_pwr + (150 * devicename)
            NeoScrypt_pwr = NeoScrypt_pwr + (150 * devicename)
            bcd_pwr = bcd_pwr + (150 * devicename)
            timetravel_pwr = timetravel_pwr + (150 * devicename)
            X16R_pwr = X16R_pwr + (150 * devicename)
            phi2_pwr = phi2_pwr + (150 * devicename)
            Xevan_pwr = Xevan_pwr + (150 * devicename)
            hex_pwr = hex_pwr + (150 * devicename)
            PHI1612_pwr = PHI1612_pwr + (150 * devicename)
            ProgPow_pwr = ProgPow_pwr + (150 * devicename)
            x25x_pwr = x25x_pwr + (150 * devicename)
            mtp_pwr = mtp_pwr + (150 * devicename)
            lyra2rev3_pwr = lyra2rev3_pwr + (150 * devicename)

            Middle = Middle & "&adapt_2070=true&adapt_q_2070=" & devicename.ToString
        End If

        If PubShared.twenty80 > 0 Then
            devicename = PubShared.twenty80
            eth = eth + (48 * devicename)
            PHI1612 = PHI1612 + (25 * devicename)
            Lyrarev3 = Lyrarev3 + (81 * devicename)
            CuckooCycle = CuckooCycle + (8.4 * devicename)
            TimeTravel10 = TimeTravel10 + (46.5 * devicename)
            X16R = X16R + (24.5 * devicename)
            CryptoNightR = CryptoNightR + (720 * devicename)
            ProgPow = ProgPow + (24.4 * devicename)
            NeoScrypt = NeoScrypt + (2000 * devicename)
            Xevan = Xevan + (7.4 * devicename)
            phi2 = phi2 + (10 * devicename)
            zhash = zhash + (74 * devicename)
            hex = hex + (20 * devicename)
            CNHeavy = CNHeavy + (1000 * devicename)
            CNFast = CNFast + (1730.0 * devicename)
            Aion = Aion + (335.0 * devicename)
            cuckaroo29 = cuckaroo29 + (7.5 * devicename)
            bcd = bcd + (37.0 * devicename)
            x25x = x25x + (2.1 * devicename)
            mtp = mtp + (3.5 * devicename)
            cuckatoo31 = cuckatoo31 + (0.0 * devicename)
            'Power
            ethash_pwr = ethash_pwr + (190 * devicename)
            zhash_pwr = zhash_pwr + (190 * devicename)
            CNHeavy_pwr = CNHeavy_pwr + (130 * devicename)
            cngpu_pwr = cngpu_pwr + (190 * devicename)
            cnr_pwr = cnr_pwr + (150 * devicename)
            CNFast_pwr = CNFast_pwr + (130 * devicename)
            Aion_pwr = Aion_pwr + (190 * devicename)
            CuckooCycle_pwr = CuckooCycle_pwr + (190 * devicename)
            cuckaroo29_pwr = cuckaroo29_pwr + (190 * devicename)
            cuckatoo31_pwr = cuckatoo31_pwr + (0 * devicename)
            beam_pwr = beam_pwr + (190 * devicename)
            NeoScrypt_pwr = NeoScrypt_pwr + (190 * devicename)
            bcd_pwr = bcd_pwr + (190 * devicename)
            timetravel_pwr = timetravel_pwr + (190 * devicename)
            X16R_pwr = X16R_pwr + (190 * devicename)
            phi2_pwr = phi2_pwr + (190 * devicename)
            Xevan_pwr = Xevan_pwr + (190 * devicename)
            hex_pwr = hex_pwr + (190 * devicename)
            PHI1612_pwr = PHI1612_pwr + (190 * devicename)
            ProgPow_pwr = ProgPow_pwr + (190 * devicename)
            x25x_pwr = x25x_pwr + (190 * devicename)
            mtp_pwr = mtp_pwr + (190 * devicename)
            lyra2rev3_pwr = lyra2rev3_pwr + (190 * devicename)


            Middle = Middle & "&adapt_2080=true&adapt_q_2080=" & devicename.ToString
        End If

        If PubShared.twenty80ti > 0 Then
            devicename = PubShared.twenty80ti
            Lyrarev3 = Lyrarev3 + (98 * devicename)
            CuckooCycle = CuckooCycle + (10.1 * devicename)
            eth = eth + (56.5 * devicename)
            PHI1612 = PHI1612 + (33 * devicename)

            TimeTravel10 = TimeTravel10 + (50 * devicename)
            X16R = X16R + (30 * devicename)
            CryptoNightR = CryptoNightR + (1000 * devicename)
            ProgPow = ProgPow + (26.5 * devicename)

            NeoScrypt = NeoScrypt + (2300 * devicename)
            Xevan = Xevan + (9.8 * devicename)
            phi2 = phi2 + (12.8 * devicename)
            zhash = zhash + (80 * devicename)
            hex = hex + (21.5 * devicename)
            CNHeavy = CNHeavy + (1200 * devicename)

            CNFast = CNFast + (2300.0 * devicename)
            Aion = Aion + (375.0 * devicename)
            cuckaroo29 = cuckaroo29 + (9.5 * devicename)
            bcd = bcd + (45.5 * devicename)
            x25x = x25x + (2.6 * devicename)
            mtp = mtp + (4.3 * devicename)
            cuckatoo31 = cuckatoo31 + (2.0 * devicename)
            'Power
            ethash_pwr = ethash_pwr + (220 * devicename)
            zhash_pwr = zhash_pwr + (220 * devicename)
            CNHeavy_pwr = CNHeavy_pwr + (160 * devicename)
            cngpu_pwr = cngpu_pwr + (220 * devicename)
            cnr_pwr = cnr_pwr + (190 * devicename)
            CNFast_pwr = CNFast_pwr + (160 * devicename)
            Aion_pwr = Aion_pwr + (220 * devicename)
            CuckooCycle_pwr = CuckooCycle_pwr + (220 * devicename)
            cuckaroo29_pwr = cuckaroo29_pwr + (220 * devicename)
            cuckatoo31_pwr = cuckatoo31_pwr + (220 * devicename)
            beam_pwr = beam_pwr + (220 * devicename)
            NeoScrypt_pwr = NeoScrypt_pwr + (220 * devicename)
            bcd_pwr = bcd_pwr + (220 * devicename)
            timetravel_pwr = timetravel_pwr + (220 * devicename)
            X16R_pwr = X16R_pwr + (220 * devicename)
            phi2_pwr = phi2_pwr + (220 * devicename)
            Xevan_pwr = Xevan_pwr + (220 * devicename)
            hex_pwr = hex_pwr + (220 * devicename)
            PHI1612_pwr = PHI1612_pwr + (220 * devicename)
            ProgPow_pwr = ProgPow_pwr + (220 * devicename)
            x25x_pwr = x25x_pwr + (220 * devicename)
            mtp_pwr = mtp_pwr + (220 * devicename)
            lyra2rev3_pwr = lyra2rev3_pwr + (220 * devicename)

            Middle = Middle & "&adapt_2080ti=true&adapt_q_2080ti=" & PubShared.ten80ti.ToString
        End If

        'Add the Math in here
        Middle = Middle & "&eth=true&factor%5Beth_hr%5D=" & eth.ToString & "&factor%5Beth_p%5D=" & ethash_pwr.ToString
        Middle = Middle & "&ns=true&factor%5Bns_hr%5D=" & NeoScrypt.ToString & "&factor%5Bns_p%5D=" & NeoScrypt_pwr.ToString
        Middle = Middle & "&phi=true&factor%5Bphi_hr%5D=" & PHI1612.ToString & "&factor%5Bphi_p%5D=" & phi2_pwr.ToString
        Middle = Middle & "&tt10=true&factor%5Btt10_hr%5D=" & TimeTravel10.ToString & "&factor%5Btt10_p%5D=" & timetravel_pwr.ToString
        Middle = Middle & "&x16r=true&factor%5Bx16r_hr%5D=" & X16R.ToString & "&factor%5Bx16r_p%5D=" & X16R_pwr.ToString
        ' DUPE Middle = Middle & "&zh=true&factor%5Bzh_hr%5D=" & zhash.ToString & "%5Beth_p%5D=" & zhash_pwr.ToString
        Middle = Middle & "&xn=true&factor%5Bxn_hr%5D=" & Xevan.ToString & "&factor%5Bxn_p%5D=" & Xevan_pwr.ToString
        Middle = Middle & "&phi2=true&factor%5Bphi2_hr%5D=" & phi2.ToString & "&factor%5Bphi2_p%5D=" & phi2_pwr.ToString
        Middle = Middle & "&cnh=true&factor%5Bcnh_hr%5D=" & CNHeavy.ToString & "&factor%5Bcnh_p%5D=" & CNHeavy_pwr.ToString
        Middle = Middle & "&zh=true&factor%5Bzh_hr%5D=" & zhash.ToString & "&factor%5Bzh_p%5D=" & zhash_pwr.ToString
        Middle = Middle & "&hx=true&factor%5Bhx_hr%5D=" & hex.ToString & "&factor%5Bhx_p%5D=" & hex_pwr.ToString
        Middle = Middle & "&ppw=true&factor%5Bppw_hr%5D=" & ProgPow.ToString & "&factor%5Bppw_p%5D=" & ProgPow_pwr.ToString
        Middle = Middle & "&cnr=true&factor%5Bcnr_hr%5D=" & CryptoNightR.ToString & "&factor%5Bcnr_p%5D=" & cnr_pwr.ToString
        Middle = Middle & "&lrev3=true&factor%5Blrev3_hr%5D=" & Lyrarev3.ToString & "&factor%5Blrev3_p%5D=" & lyra2rev3_pwr.ToString
        Middle = Middle & "&cnf=true&factor%5Bcnf_hr%5D=" & CNFast.ToString & "&factor%5Bcnf_p%5D=" & CNFast_pwr.ToString
        ' Equihash still fucking here? Middle = Middle & "&eqa=true&factor%5Beqa_hr%5D=" & Aion.ToString & "%5Beth_p%5D=" & eqa_pwr.ToString
        Middle = Middle & "&cr29=true&factor%5Bcr29_hr%5D=" & cuckaroo29.ToString & "&factor%5Bcr29_p%5D=" & cuckaroo29_pwr.ToString
        Middle = Middle & "&bcd=true&factor%5Bbcd_hr%5D=" & bcd.ToString & "&factor%5Bbcd_p%5D=" & bcd_pwr.ToString
        Middle = Middle & "&x25x=true&factor%5Bx25x_hr%5D=" & x25x.ToString & "&factor%5Bx25x_p%5D=" & x25x_pwr.ToString
        Middle = Middle & "&mtp=true&factor%5Bmtp_hr%5D=" & mtp.ToString & "&factor%5Bmtp_p%5D=" & mtp_pwr.ToString
        Middle = Middle & "&cc=true&factor%5Bcc_hr%5D=" & CuckooCycle.ToString & "&factor%5Bcc_p%5D=" & CuckooCycle_pwr.ToString
        Middle = Middle & "&ct31=true&factor%5Bct31_hr%5D=" & cuckatoo31.ToString & "&factor%5Bct31_p%5D=" & cuckatoo31.ToString
        Return Beginning & Middle & Ending



    End Function
    Public Shared Function IsCurrentTimeBetween(ByVal dteStartTime As DateTime, ByVal dteEndTime As DateTime) As Boolean

        Dim startTime, endTime, currentTime As TimeSpan

        startTime = dteStartTime.TimeOfDay
        endTime = dteEndTime.TimeOfDay
        currentTime = Now.TimeOfDay

        If endTime < startTime Then
            'the times span midnight     
            Return (currentTime <= endTime)
        Else
            'the times do not span midnight
            Return (startTime <= currentTime AndAlso currentTime <= endTime)
        End If

    End Function
    Public Shared Sub SaveAIOsetting(ASettings As String, SettingValue As String)
        Dim thingtochange As MAINAPP
        Dim appPath As String = Application.StartupPath()
        'Find value
        Try
            Dim SettingsConfig As mAIOS = GetAIOSettings()
            Dim TheConfig As List(Of MAINAPP) = SettingsConfig.AIOSs.MAINAPPs.ToList
            thingtochange = TheConfig.Find(Function(x) x.type = ASettings)
            thingtochange.value = SettingValue
            'passback json
            SettingsConfig.AIOSs.MAINAPPs = TheConfig.ToArray
            Dim strJson As String = JsonConvert.SerializeObject(SettingsConfig)
            updateSettingsCache("aiosettings", strJson)
            Dim i As Int16 = 0
            'Try it 3 times :|
            'For realz!!!1!!!
            Try
                Do While i <= 3
                    Try
                        System.IO.File.WriteAllText(appPath & "\Settings\AIOSettings.json", strJson)
                        i = 4
                    Catch ex As Exception
                        i = i + 1
                    End Try
                Loop

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

            Catch ex As Exception
                LogUpdate(ex.Message, eLogLevel.Err)
                Throw
            End Try
        Catch ex As Exception
            LogUpdate(ex.Message, eLogLevel.Err)
            'Throw
        End Try



        'Change value
        'Save value

    End Sub

    Public Shared Function ValidateEmail(ByVal strCheck As String) As Boolean
        Try
            Dim vEmailAddress As New System.Net.Mail.MailAddress(strCheck)
        Catch ex As Exception
            Return False
        End Try
        Return True
    End Function

    Public Shared Sub SetAllowUnsafeHeaderParsing20()
        Dim a As New System.Net.Configuration.SettingsSection
        Dim aNetAssembly As System.Reflection.Assembly = Assembly.GetAssembly(a.GetType)
        Dim aSettingsType As Type = aNetAssembly.GetType("System.Net.Configuration.SettingsSectionInternal")
        Dim args1 As Object()
#Disable Warning BC42104 ' Variable is used before it has been assigned a value
        Dim anInstance As Object = aSettingsType.InvokeMember("Section", BindingFlags.Static Or BindingFlags.GetProperty Or BindingFlags.NonPublic, Nothing, Nothing, args1)
#Enable Warning BC42104 ' Variable is used before it has been assigned a value
        Dim aUseUnsafeHeaderParsing As FieldInfo = aSettingsType.GetField("useUnsafeHeaderParsing", BindingFlags.NonPublic Or BindingFlags.Instance)
        aUseUnsafeHeaderParsing.SetValue(anInstance, True)
    End Sub
    Public Shared Sub KillAllMiningApps()

        ' make sure they all get killed ok 
        'Do Until MinerInstances.RunningMiners.Count = 0
        'Removed : Message	"Object reference not set to an instance of an object."	String

        'Do it twice
        Try
            If MinerInstances.RunningMiners IsNot Nothing Then
                For Each p As Process In MinerInstances.RunningMiners
                    If Not p.HasExited Then
                        'p.Kill()
                        Dim aProcess As System.Diagnostics.Process
                        aProcess = System.Diagnostics.Process.GetProcessById(p.Id)
                        aProcess.Kill()
                    End If
                Next
            End If
            MinerInstances.RunningMiners = New List(Of Process)()
        Catch ex As Exception
            If ex.Message.ToUpper.Contains("ACCESS IS DENIED") Then
            Else
                LogUpdate(ex.Message, eLogLevel.Err)
            End If
        End Try


        'Loop

        'The Claymore Clause
        Try
            Dim CM As Boolean = False
            Dim p() As Process

            Do Until CM = True
                p = Process.GetProcessesByName("EthDcrMiner64")
                If p.Count > 0 Then
                    For Each Z As Process In System.Diagnostics.Process.GetProcessesByName("EthDcrMiner64")
                        Z.Kill()
                    Next
                Else
                    CM = True
                    Exit Do
                End If
                System.Threading.Thread.Sleep(2000)
            Loop
        Catch ex As Exception

        End Try


        'The Phoenix Clause
        Try
            Dim CM As Boolean = False
            Dim p() As Process

            Do Until CM = True
                p = Process.GetProcessesByName("PhoenixMiner")
                If p.Count > 0 Then
                    For Each Z As Process In System.Diagnostics.Process.GetProcessesByName("Phoenix")
                        Z.Kill()
                    Next
                Else
                    CM = True
                    Exit Do
                End If
                System.Threading.Thread.Sleep(2000)
            Loop
        Catch ex As Exception

        End Try


        'The GMiner Fucker
        Try
            Dim CM As Boolean = False
            Dim p() As Process

            Do Until CM = True
                p = Process.GetProcessesByName("miner")
                If p.Count > 0 Then
                    For Each Z As Process In System.Diagnostics.Process.GetProcessesByName("miner")
                        Z.Kill()
                    Next
                Else
                    CM = True
                    Exit Do
                End If
                System.Threading.Thread.Sleep(2000)
            Loop
        Catch ex As Exception

        End Try

        'The bminer Fucker
        Try
            Dim CM As Boolean = False
            Dim p() As Process

            Do Until CM = True
                p = Process.GetProcessesByName("bminer")
                If p.Count > 0 Then
                    For Each Z As Process In System.Diagnostics.Process.GetProcessesByName("bminer")
                        Z.Kill()
                    Next
                Else
                    CM = True
                    Exit Do
                End If
                System.Threading.Thread.Sleep(2000)
            Loop
        Catch ex As Exception

        End Try

        'The nbminer Fucker
        Try
            Dim CM As Boolean = False
            Dim p() As Process

            Do Until CM = True
                p = Process.GetProcessesByName("nbminer")
                If p.Count > 0 Then
                    For Each Z As Process In System.Diagnostics.Process.GetProcessesByName("nbminer")
                        Z.Kill()
                    Next
                Else
                    CM = True
                    Exit Do
                End If
                System.Threading.Thread.Sleep(2000)
            Loop
        Catch ex As Exception

        End Try


        'Kill ETHPill if it's running
        If ReturnAIOsetting("nvidia") = "True" Then
            Dim EP As Boolean = False
            Dim q() As Process

            Do Until EP = True
                q = Process.GetProcessesByName("OhGodAnETHlargementPill-r2")
                If q.Count > 0 Then
                    For Each Z As Process In System.Diagnostics.Process.GetProcessesByName("OhGodAnETHlargementPill-r2")
                        Z.Kill()
                    Next
                Else
                    EP = True
                    Exit Do
                End If
                System.Threading.Thread.Sleep(2000)
            Loop
        End If
    End Sub
    Public Shared Function SupportedPools() As String
        'Support for Ethermine
        Dim supported As String = ""

        If PubShared.donationsStarted = False Then


            If PubShared.ip.ToUpper.Contains("BSOD") Then
                supported = "https://bsod.pw/?address=" & PubShared.worker
            End If

            If PubShared.ip.ToUpper.Contains("2MINERS.COM") Then
                supported = "https://2miners.com/en/account/" & PubShared.worker
            End If

            If PubShared.ip.ToUpper.Contains("COINBLOCKERS.COM") Then
                supported = "https://coinblockers.com/workers/" & PubShared.worker
            End If


            If PubShared.ip.ToLower.Contains("btgpool.pro") Then
                supported = "http://btgpool.pro/workers/" & PubShared.worker
            End If



            If PubShared.ip.ToUpper.Contains("ETHERMINE") Then
                Dim LL As String
                If PubShared.ip.ToUpper.Contains("ETC") Then
                    LL = "https://etc.ethermine.org/miners/" & PubShared.worker
                Else
                    LL = "https://ethermine.org/miners/" & PubShared.worker
                End If


                Dim re1 As String = "((?:[a-z][a-z]+))" 'Word 1
                Dim re2 As String = "(.)"   'Any Single Character 1
                Dim re3 As String = "(.)"   'Any Single Character 2
                Dim re4 As String = "(.)"   'Any Single Character 3
                Dim re5 As String = "((?:[a-z][a-z\.\d\-]+)\.(?:[a-z][a-z\-]+))(?![\w\.])"  'Fully Qualified Domain Name 1
                Dim re6 As String = "(.)"   'Any Single Character 4
                Dim re7 As String = "((?:[a-z][a-z]+))" 'Word 2
                Dim re8 As String = "(.)"   'Any Single Character 5
                Dim re9 As String = "((?:[a-z][a-z]*[0-9]+[a-z0-9]*))"  'Alphanum 1

                Dim r As Regex = New Regex(re1 + re2 + re3 + re4 + re5 + re6 + re7 + re8 + re9, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                Dim m As Match = r.Match(LL)
                If (m.Success) Then
                    Dim alphanum1 = m.Groups(9)
                    If PubShared.ip.ToUpper.Contains("ETC") Then
                        supported = "https://etc.ethermine.org/miners/" & alphanum1.ToString
                    Else
                        supported = "https://ethermine.org/miners/" & alphanum1.ToString
                    End If

                Else
                    supported = LL
                End If

            End If



            'Support for FlyPool
            If PubShared.ip.ToUpper.Contains("FLYPOOL") Then
                Dim LL As String
                LL = "https://zcash.flypool.org/miners/" & PubShared.worker

                Dim re1 As String = "((?:[a-z][a-z]+))" 'Word 1
                Dim re2 As String = "(.)"   'Any Single Character 1
                Dim re3 As String = "(.)"   'Any Single Character 2
                Dim re4 As String = "(.)"   'Any Single Character 3
                Dim re5 As String = "((?:[a-z][a-z\.\d\-]+)\.(?:[a-z][a-z\-]+))(?![\w\.])"  'Fully Qualified Domain Name 1
                Dim re6 As String = "(.)"   'Any Single Character 4
                Dim re7 As String = "((?:[a-z][a-z]+))" 'Word 2
                Dim re8 As String = "(.)"   'Any Single Character 5
                Dim re9 As String = "((?:[a-z][a-z]*[0-9]+[a-z0-9]*))"  'Alphanum 1

                Dim r As Regex = New Regex(re1 + re2 + re3 + re4 + re5 + re6 + re7 + re8 + re9, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                Dim m As Match = r.Match(LL)
                If (m.Success) Then
                    Dim alphanum1 = m.Groups(9)
                    supported = "https://zcash.flypool.org/miners/" & alphanum1.ToString
                Else

                    supported = LL
                End If
            End If
            Return supported
        Else

        End If

        Return ""

    End Function


    Public Shared Function TimeNow() As String
        Dim HMS As String
        HMS = TimeOfDay.Hour.ToString + ":" + TimeOfDay.Minute.ToString + ":" + TimeOfDay.Second.ToString
        Return HMS

    End Function

    ' http stuff


    Public Shared Function HttpPost(url As String, data As String) As String
        Dim req As WebRequest = WebRequest.Create(url)
        req.ContentType = "application/x-www-form-urlencoded"
        req.Method = "POST"

        Dim bytes() As Byte = System.Text.Encoding.ASCII.GetBytes(data)
        req.ContentLength = bytes.Length
        Dim os As System.IO.Stream = req.GetRequestStream()
        os.Write(bytes, 0, bytes.Length)
        os.Close()
        Dim resp As WebResponse = req.GetResponse()
        If resp Is Nothing Then Return Nothing
        Dim sr As System.IO.StreamReader = New System.IO.StreamReader(resp.GetResponseStream())
        Dim rtnval As String = sr.ReadToEnd().Trim()

        Return rtnval


    End Function





#Region "Unused?"

    'Public Shared Function RemoveCharacter(ByVal stringToCleanUp As String, ByVal characterToRemove As String) As String
    '    ' replace the target with nothing
    '    ' Replace() returns a new String and does not modify the current one
    '    Return stringToCleanUp.Replace(characterToRemove, "")
    'End Function



    ' Public LastSysTime As DateTime

    'Public Function logo(what As String) As String
    '    Dim appPath As String = Application.StartupPath()
    '    If what = "ZCash" Then
    '        Return appPath & "/images/zcash-logo-1.png"
    '    End If
    '    Return what
    'End Function





    'Public Function GetNISTTime(ByVal host As String) As DateTime

    '    Dim timeStr As String

    '    timeStr = ""


    '    Try
    '        Dim reader As New StreamReader(New TcpClient(host, 13).GetStream)
    '        'LastSysTime = DateTime.UtcNow()
    '        timeStr = reader.ReadToEnd()
    '        reader.Close()
    '    Catch ex As SocketException

    '    Catch ex As Exception

    '    End Try

    '    Dim jd As Integer = Integer.Parse(timeStr.Substring(1, 5))
    '    Dim yr As Integer = Integer.Parse(timeStr.Substring(7, 2))
    '    Dim mo As Integer = Integer.Parse(timeStr.Substring(10, 2))
    '    Dim dy As Integer = Integer.Parse(timeStr.Substring(13, 2))
    '    Dim hr As Integer = Integer.Parse(timeStr.Substring(16, 2))
    '    Dim mm As Integer = Integer.Parse(timeStr.Substring(19, 2))
    '    Dim sc As Integer = Integer.Parse(timeStr.Substring(22, 2))
    '    Dim Temp As Integer = CInt(AscW(timeStr(7)))

    '    Return New DateTime(yr + 2000, mo, dy, hr, mm, sc)

    'End Function

#End Region

End Class
