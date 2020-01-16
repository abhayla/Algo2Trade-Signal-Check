Imports Algo2TradeBLL
Imports System.Threading
Public Class SupertrendSMAOpenHighLow
    Inherits Rule
    Public Sub New(ByVal canceller As CancellationTokenSource, ByVal stockCategory As Integer, ByVal timeFrame As Integer, ByVal useHA As Boolean, ByVal stockName As String, ByVal fileName As String)
        MyBase.New(canceller, stockCategory, timeFrame, useHA, stockName, fileName)
    End Sub
    Public Overrides Async Function RunAsync(ByVal startDate As Date, ByVal endDate As Date) As Task(Of DataTable)
        Await Task.Delay(0).ConfigureAwait(False)
        Dim ret As New DataTable
        ret.Columns.Add("Date")
        ret.Columns.Add("Trading Symbol")
        ret.Columns.Add("Time")
        ret.Columns.Add("Direction")

        Dim stockData As StockSelection = New StockSelection(_canceller, _category, _cmn, _fileName)
        AddHandler stockData.Heartbeat, AddressOf OnHeartbeat
        AddHandler stockData.WaitingFor, AddressOf OnWaitingFor
        AddHandler stockData.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
        AddHandler stockData.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
        Dim chkDate As Date = startDate
        While chkDate <= endDate
            _canceller.Token.ThrowIfCancellationRequested()
            Dim stockList As List(Of String) = Nothing
            If _instrumentName Is Nothing OrElse _instrumentName = "" Then
                stockList = Await stockData.GetStockList(chkDate).ConfigureAwait(False)
            Else
                stockList = New List(Of String)
                stockList.Add(_instrumentName)
            End If
            _canceller.Token.ThrowIfCancellationRequested()
            If stockList IsNot Nothing AndAlso stockList.Count > 0 Then
                For Each stock In stockList
                    _canceller.Token.ThrowIfCancellationRequested()
                    Dim stockPayload As Dictionary(Of Date, Payload) = Nothing
                    Select Case _category
                        Case Common.DataBaseTable.Intraday_Cash, Common.DataBaseTable.Intraday_Commodity, Common.DataBaseTable.Intraday_Currency, Common.DataBaseTable.Intraday_Futures
                            stockPayload = _cmn.GetRawPayload(_category, stock, chkDate.AddDays(-8), chkDate)
                        Case Common.DataBaseTable.EOD_Cash, Common.DataBaseTable.EOD_Commodity, Common.DataBaseTable.EOD_Currency, Common.DataBaseTable.EOD_Futures, Common.DataBaseTable.EOD_POSITIONAL
                            stockPayload = _cmn.GetRawPayload(_category, stock, chkDate.AddDays(-200), chkDate)
                    End Select
                    _canceller.Token.ThrowIfCancellationRequested()
                    If stockPayload IsNot Nothing AndAlso stockPayload.Count > 0 Then
                        Dim XMinutePayload As Dictionary(Of Date, Payload) = Nothing
                        If _timeFrame > 1 Then
                            Dim exchangeStartTime As Date = New Date(chkDate.Year, chkDate.Month, chkDate.Day, 9, 15, 0)
                            XMinutePayload = Common.ConvertPayloadsToXMinutes(stockPayload, _timeFrame, exchangeStartTime)
                        Else
                            XMinutePayload = stockPayload
                        End If
                        _canceller.Token.ThrowIfCancellationRequested()
                        Dim inputPayload As Dictionary(Of Date, Payload) = Nothing
                        If _useHA Then
                            Indicator.HeikenAshi.ConvertToHeikenAshi(XMinutePayload, inputPayload)
                        Else
                            inputPayload = XMinutePayload
                        End If
                        _canceller.Token.ThrowIfCancellationRequested()
                        Dim currentDayPayload As Dictionary(Of Date, Payload) = Nothing
                        For Each runningPayload In inputPayload.Keys
                            _canceller.Token.ThrowIfCancellationRequested()
                            If runningPayload.Date = chkDate.Date Then
                                If currentDayPayload Is Nothing Then currentDayPayload = New Dictionary(Of Date, Payload)
                                currentDayPayload.Add(runningPayload, inputPayload(runningPayload))
                            End If
                        Next

                        'Main Logic
                        If currentDayPayload IsNot Nothing AndAlso currentDayPayload.Count > 0 Then
                            _canceller.Token.ThrowIfCancellationRequested()
                            Dim eodPayload As Dictionary(Of Date, Payload) = Nothing
                            Select Case _category
                                Case Common.DataBaseTable.Intraday_Cash
                                    eodPayload = _cmn.GetRawPayload(Common.DataBaseTable.EOD_Cash, stock, chkDate.AddDays(-200), chkDate)
                                Case Common.DataBaseTable.Intraday_Currency
                                    eodPayload = _cmn.GetRawPayload(Common.DataBaseTable.EOD_Currency, stock, chkDate.AddDays(-200), chkDate)
                                Case Common.DataBaseTable.Intraday_Commodity
                                    eodPayload = _cmn.GetRawPayload(Common.DataBaseTable.EOD_Commodity, stock, chkDate.AddDays(-200), chkDate)
                                Case Common.DataBaseTable.Intraday_Futures
                                    eodPayload = _cmn.GetRawPayload(Common.DataBaseTable.EOD_Futures, stock, chkDate.AddDays(-200), chkDate)
                            End Select
                            _canceller.Token.ThrowIfCancellationRequested()
                            If eodPayload IsNot Nothing AndAlso eodPayload.Count > 0 Then
                                Dim supertrendPayload As Dictionary(Of Date, Decimal) = Nothing
                                Dim supertrendColorPayload As Dictionary(Of Date, Color) = Nothing
                                Indicator.Supertrend.CalculateSupertrend(7, 3, eodPayload, supertrendPayload, supertrendColorPayload)
                                Dim smaPayload As Dictionary(Of Date, Decimal) = Nothing
                                Indicator.SMA.CalculateSMA(200, Payload.PayloadFields.Close, eodPayload, smaPayload)

                                If eodPayload.LastOrDefault.Value.PreviousCandlePayload IsNot Nothing AndAlso
                                    eodPayload.LastOrDefault.Value.PreviousCandlePayload.Close > smaPayload(eodPayload.LastOrDefault.Value.PreviousCandlePayload.PayloadDate) AndAlso
                                    supertrendColorPayload(eodPayload.LastOrDefault.Value.PreviousCandlePayload.PayloadDate) = Color.Green Then
                                    Dim open As Decimal = eodPayload.LastOrDefault.Value.Open
                                    If open = currentDayPayload.FirstOrDefault.Value.High Then
                                        For Each runningPayload In currentDayPayload
                                            _canceller.Token.ThrowIfCancellationRequested()
                                            Dim highBreaked As Boolean = False
                                            If runningPayload.Value.Ticks IsNot Nothing AndAlso runningPayload.Value.Ticks.Count > 0 Then
                                                For Each runningTick In runningPayload.Value.Ticks
                                                    If runningTick.Open > open Then
                                                        Dim row As DataRow = ret.NewRow
                                                        row("Date") = runningTick.PayloadDate.ToString("dd-MM-yyyy")
                                                        row("Trading Symbol") = runningTick.TradingSymbol
                                                        row("Time") = runningTick.PayloadDate.ToString("HH:mm:ss")
                                                        row("Direction") = "Buy"
                                                        ret.Rows.Add(row)
                                                        highBreaked = True
                                                        Exit For
                                                    End If
                                                Next
                                            End If
                                            If highBreaked Then Exit For
                                        Next
                                    End If
                                ElseIf eodPayload.LastOrDefault.Value.PreviousCandlePayload IsNot Nothing AndAlso
                                    eodPayload.LastOrDefault.Value.PreviousCandlePayload.Close < smaPayload(eodPayload.LastOrDefault.Value.PreviousCandlePayload.PayloadDate) AndAlso
                                    supertrendColorPayload(eodPayload.LastOrDefault.Value.PreviousCandlePayload.PayloadDate) = Color.Red Then
                                    Dim open As Decimal = eodPayload.LastOrDefault.Value.Open
                                    If open = currentDayPayload.FirstOrDefault.Value.Low Then
                                        For Each runningPayload In currentDayPayload
                                            _canceller.Token.ThrowIfCancellationRequested()
                                            Dim lowBreaked As Boolean = False
                                            If runningPayload.Value.Ticks IsNot Nothing AndAlso runningPayload.Value.Ticks.Count > 0 Then
                                                For Each runningTick In runningPayload.Value.Ticks
                                                    If runningTick.Open < open Then
                                                        Dim row As DataRow = ret.NewRow
                                                        row("Date") = runningTick.PayloadDate.ToString("dd-MM-yyyy")
                                                        row("Trading Symbol") = runningTick.TradingSymbol
                                                        row("Time") = runningTick.PayloadDate.ToString("HH:mm:ss")
                                                        row("Direction") = "Sell"
                                                        ret.Rows.Add(row)
                                                        lowBreaked = True
                                                        Exit For
                                                    End If
                                                Next
                                            End If
                                            If lowBreaked Then Exit For
                                        Next
                                    End If
                                End If
                            End If
                        End If
                    End If
                Next
            End If
            chkDate = chkDate.AddDays(1)
        End While
        Return ret
    End Function
End Class