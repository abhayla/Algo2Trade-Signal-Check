Imports Algo2TradeBLL
Imports System.Threading
Public Class DonchianBreakoutShortTrend
    Inherits Rule
    Public Sub New(ByVal canceller As CancellationTokenSource, ByVal stockCategory As Integer, ByVal timeFrame As Integer, ByVal useHA As Boolean, ByVal stockName As String, ByVal fileName As String)
        MyBase.New(canceller, stockCategory, timeFrame, useHA, stockName, fileName)
    End Sub
    Public Overrides Async Function RunAsync(startDate As Date, endDate As Date) As Task(Of DataTable)
        Await Task.Delay(0).ConfigureAwait(False)
        Dim ret As New DataTable
        ret.Columns.Add("Date")
        ret.Columns.Add("Instrument")
        ret.Columns.Add("Signal")
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
                        Dim highBollingerPayload As Dictionary(Of Date, Decimal) = Nothing
                        Dim lowBollingerPayload As Dictionary(Of Date, Decimal) = Nothing
                        Dim SMAPayload As Dictionary(Of Date, Decimal) = Nothing
                        Indicator.BollingerBands.CalculateBollingerBands(20, Payload.PayloadFields.Close, 1, inputPayload, highBollingerPayload, lowBollingerPayload, SMAPayload)
                        Dim highDonchianPayload As Dictionary(Of Date, Decimal) = Nothing
                        Dim lowDonchianPayload As Dictionary(Of Date, Decimal) = Nothing
                        Dim middleDonchianPayload As Dictionary(Of Date, Decimal) = Nothing
                        Indicator.DonchianChannel.CalculateDonchianChannel(10, 10, inputPayload, highDonchianPayload, lowDonchianPayload, middleDonchianPayload)
                        If currentDayPayload IsNot Nothing AndAlso currentDayPayload.Count > 0 Then
                            Dim outsideBollingerSignal As Boolean = False
                            Dim returnInsideBollinger As Boolean = False
                            For Each runningPayload In currentDayPayload.Keys
                                _canceller.Token.ThrowIfCancellationRequested()
                                If currentDayPayload(runningPayload).Low > highBollingerPayload(runningPayload) Then
                                    outsideBollingerSignal = True
                                ElseIf currentDayPayload(runningPayload).High < lowBollingerPayload(runningPayload) Then
                                    outsideBollingerSignal = False
                                    returnInsideBollinger = False
                                End If
                                If outsideBollingerSignal AndAlso currentDayPayload(runningPayload).Low < highBollingerPayload(runningPayload) Then
                                    returnInsideBollinger = True
                                End If
                                If outsideBollingerSignal AndAlso returnInsideBollinger AndAlso
                                    currentDayPayload(runningPayload).Low < lowDonchianPayload(runningPayload) Then
                                    Dim row As DataRow = ret.NewRow
                                    row("Date") = currentDayPayload(runningPayload).PayloadDate
                                    row("Instrument") = currentDayPayload(runningPayload).TradingSymbol
                                    row("Signal") = -1
                                    ret.Rows.Add(row)
                                    outsideBollingerSignal = False
                                    returnInsideBollinger = False
                                End If
                            Next
                        End If
                    End If
                Next
            End If
            chkDate = chkDate.AddDays(1)
        End While
        Return ret
    End Function
End Class
