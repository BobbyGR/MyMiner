Imports AIOminer.Log
Imports AIOminer.General_Utils
Imports System.Net
Imports System.IO

Public Class MinerStopped

    ' Create a Random object called randomizer 
    ' to generate random numbers.
    Private randomizer As New Random

    ' These integer variables store the numbers 
    ' for the addition problem. 
    Private addend1 As Integer
    Private addend2 As Integer

    ' This integer variable keeps track of the 
    ' remaining time.

    Private timeLeft As Integer
    Private TargetDT As DateTime
    Private CountDownFrom As TimeSpan = TimeSpan.FromMinutes(0.1)

    Private Sub MinerStopped_Load(sender As Object, e As EventArgs) Handles MyBase.Load


        If PubShared.Subscriber = True Then
            LinkLabel1.Visible = False
            ' PictureBox1.Image = My.Resources.Resources.click_to_stop


        Else
            Try
                Dim MyWebClient As New System.Net.WebClient
                Dim ImageInBytes() As Byte = MyWebClient.DownloadData(PubShared.HOSTED_DATA_STORE & "/advertiseaio2.png")
                Dim ImageStream As New IO.MemoryStream(ImageInBytes)
                PictureBox1.Image = New System.Drawing.Bitmap(ImageStream)
            Catch ex As Exception

            End Try

            Try
                Dim MyWebClient As New System.Net.WebClient
                Dim ImageInBytes() As Byte = MyWebClient.DownloadData(pubshared.HOSTED_WEBSITE & "/img/logo/dot.png")
                Dim ImageStream As New IO.MemoryStream(ImageInBytes)
                PictureBox2.Image = New System.Drawing.Bitmap(ImageStream)
            Catch ex As Exception

            End Try
        End If
        'Advertisements



        AIOMiner.Label14.Text = "Changing Coin!"

        'Trigger that you are no longer monitoring
        PubShared.monitoring = False
        PubShared.altargument = ""
        PubShared.api = ""
        PubShared.speed = 0
        PubShared.Dualspeed = 0
        PubShared.speedtype = ""
        PubShared.Dualspeedtype = ""
        PubShared.coin = ""
        PubShared.algo = ""
        PubShared.pool = ""
        PubShared.ip = ""
        PubShared.port = ""






        'If MyMiner.Mining = "EWBF" Then
        '    MyMiner.stopjob = True
        '    'MyMiner.frmZcash.Close()
        'End If

        'If MyMiner.Mining = "Vertcoin" Then
        '    MyMiner.stopjob = True
        'End If

        'Monitor.Close()
        If AIOMiner.IdeleCheckBox.Checked = True Then
            LogUpdate("Idle miner was checked or interupted, closing down any current mining!")
            AIOMiner.Button1.Text = "Start"
            AIOMiner.Button1.BackgroundImage = My.Resources.Resources.START
        Else
            LogUpdate("Stopped Mining, 5 Second Countdown until you can start again")
        End If

        AIOMiner.Button1.Enabled = False
        Timer1.Interval = 500
        TargetDT = DateTime.Now.Add(CountDownFrom)



        'AS U SEE, NO FILE NEEDS TO BE WRITTEN TO THE HARD DRIVE, ITS ALL DONE IN MEMORY



        Try


            '..f'n claymore
            KillAllMiningApps()
        Catch ex As Exception

        End Try

        Try
            '..f'n claymore
            KillAllMiningApps()
        Catch ex As Exception

        End Try

        Timer1.Start()



    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Dim wasrunning As Boolean
        wasrunning = False



        Dim ts As TimeSpan = TargetDT.Subtract(DateTime.Now)
        If ts.TotalMilliseconds > 0 Then
            Label1.Text = "Please hold, ensuring memory was released " & ts.ToString("mm\:ss") & " Left."
        Else
            If AIOMiner.IdeleCheckBox.Checked = True Then
                AIOMiner.Button1.Enabled = False
            Else
                AIOMiner.Button1.Text = "Start"
                AIOMiner.Button1.BackgroundImage = My.Resources.Resources.START
                AIOMiner.Button1.Enabled = True
            End If
            Timer1.Stop()
            PubShared.StoppedMiningIsShowing = False
            Me.Close()

        End If
    End Sub

    Private Sub PictureBox1_Click(sender As Object, e As EventArgs) Handles PictureBox1.Click
        '



        'Disabled 6/3/2018

        If AIOMiner.IdeleCheckBox.Checked = True Then
            AIOMiner.Button1.Enabled = False
        Else
            AIOMiner.Button1.Text = "Start"
            AIOMiner.Button1.BackgroundImage = My.Resources.Resources.START
            AIOMiner.Button1.Enabled = True
        End If
        Timer1.Stop()

        Me.Close()
    End Sub

    Private Sub LinkLabel1_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel1.LinkClicked
        Dim Results As String
        Try
            SetAllowUnsafeHeaderParsing20()
            Dim address As String = pubshared.HOSTED_WEBSITE & "/products/adclick2.html"
            Dim client As WebClient = New WebClient()
            Dim reader As StreamReader = New StreamReader(client.OpenRead(address))
            Results = reader.ReadToEnd
        Catch ex As Exception
            LogUpdate("Advertisement Click failed to find the location!", eLogLevel.Err)
            Results = "err"
        End Try
        'take user to where they should go
        Try
            If Results = "err" Then
            Else
                Process.Start(Results)
            End If

        Catch ex As Exception

        End Try
    End Sub
End Class