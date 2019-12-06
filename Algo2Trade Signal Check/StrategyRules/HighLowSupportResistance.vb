Imports Algo2TradeBLL
Imports System.Threading
Public Class HighLowSupportResistance
    Inherits Rule

    Private ReadOnly _numberOfRecord As Integer = 3

    Public Sub New(ByVal canceller As CancellationTokenSource, ByVal stockCategory As String, ByVal timeFrame As Integer, ByVal useHA As Boolean, ByVal stockName As String, ByVal fileName As String)
        MyBase.New(canceller, stockCategory, timeFrame, useHA, stockName, fileName)
    End Sub
    Public Overrides Async Function RunAsync(startDate As Date, endDate As Date) As Task(Of DataTable)
        Await Task.Delay(0).ConfigureAwait(False)
        Dim ret As New DataTable
        ret.Columns.Add("Date")
        ret.Columns.Add("Instrument")
        ret.Columns.Add("Parameter")
        ret.Columns.Add("Parameter Value")
        ret.Columns.Add("Parameter Range")
        ret.Columns.Add("Match Count")

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
                        Case "Cash"
                            stockPayload = _cmn.GetRawPayload(Common.DataBaseTable.Intraday_Cash, stock, chkDate.AddDays(-7), chkDate)
                        Case "Currency"
                            stockPayload = _cmn.GetRawPayload(Common.DataBaseTable.Intraday_Currency, stock, chkDate.AddDays(-7), chkDate)
                        Case "Commodity"
                            stockPayload = _cmn.GetRawPayload(Common.DataBaseTable.Intraday_Commodity, stock, chkDate.AddDays(-7), chkDate)
                        Case "Future"
                            stockPayload = _cmn.GetRawPayload(Common.DataBaseTable.Intraday_Futures, stock, chkDate.AddDays(-7), chkDate)
                        Case Else
                            Throw New NotImplementedException
                    End Select
                    _canceller.Token.ThrowIfCancellationRequested()
                    If stockPayload IsNot Nothing AndAlso stockPayload.Count > 0 Then
                        OnHeartbeat("Processing Data")
                        Dim XMinutePayload As Dictionary(Of Date, Payload) = Nothing
                        If _timeFrame > 1 Then
                            XMinutePayload = Common.ConvertPayloadsToXMinutes(stockPayload, _timeFrame)
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
                            Dim highMatchingCandles As Dictionary(Of Date, Integer) = Nothing
                            Dim lowMatchingCandles As Dictionary(Of Date, Integer) = Nothing
                            For Each runningPayload In currentDayPayload.Values
                                _canceller.Token.ThrowIfCancellationRequested()
                                Dim highBuffer As Decimal = CalculateBuffer(runningPayload.High, Utilities.Numbers.NumberManipulation.RoundOfType.Floor)
                                Dim lowBuffer As Decimal = CalculateBuffer(runningPayload.Low, Utilities.Numbers.NumberManipulation.RoundOfType.Floor)
                                Dim highCounter As Integer = 0
                                Dim lowCounter As Integer = 0
                                For Each subPayload In currentDayPayload.Values
                                    _canceller.Token.ThrowIfCancellationRequested()
                                    If (subPayload.High >= runningPayload.High - highBuffer AndAlso subPayload.High <= runningPayload.High + highBuffer) OrElse
                                        (subPayload.Low >= runningPayload.High - highBuffer AndAlso subPayload.Low <= runningPayload.High + highBuffer) Then
                                        highCounter += 1
                                    End If
                                    If (subPayload.Low >= runningPayload.Low - lowBuffer AndAlso subPayload.Low <= runningPayload.Low + lowBuffer) OrElse
                                        (subPayload.High >= runningPayload.Low - lowBuffer AndAlso subPayload.High <= runningPayload.Low + lowBuffer) Then
                                        lowCounter += 1
                                    End If
                                Next
                                If highMatchingCandles Is Nothing Then highMatchingCandles = New Dictionary(Of Date, Integer)
                                highMatchingCandles.Add(runningPayload.PayloadDate, highCounter)
                                If lowMatchingCandles Is Nothing Then lowMatchingCandles = New Dictionary(Of Date, Integer)
                                lowMatchingCandles.Add(runningPayload.PayloadDate, lowCounter)
                            Next

                            If highMatchingCandles IsNot Nothing AndAlso highMatchingCandles.Count > 0 Then
                                Dim counter As Integer = 0
                                Dim lastHigh As Decimal = Decimal.MinValue
                                For Each runningCandle In highMatchingCandles.OrderByDescending(Function(x)
                                                                                                    Return x.Value
                                                                                                End Function)
                                    Dim high As Decimal = currentDayPayload(runningCandle.Key).High
                                    If high <> lastHigh Then
                                        lastHigh = high
                                        Dim buffer As Decimal = CalculateBuffer(high, Utilities.Numbers.NumberManipulation.RoundOfType.Floor)
                                        Dim row As DataRow = ret.NewRow
                                        row("Date") = runningCandle.Key
                                        row("Instrument") = currentDayPayload(runningCandle.Key).TradingSymbol
                                        row("Parameter") = "High"
                                        row("Parameter Value") = high
                                        row("Parameter Range") = String.Format("{0} - {1}", high + buffer, high - buffer)
                                        row("Match Count") = runningCandle.Value
                                        ret.Rows.Add(row)
                                        counter += 1
                                        If counter = _numberOfRecord Then Exit For
                                    End If
                                Next
                            End If
                            If lowMatchingCandles IsNot Nothing AndAlso lowMatchingCandles.Count > 0 Then
                                Dim counter As Integer = 0
                                Dim lastLow As Decimal = Decimal.MinValue
                                For Each runningCandle In lowMatchingCandles.OrderByDescending(Function(x)
                                                                                                   Return x.Value
                                                                                               End Function)
                                    Dim low As Decimal = currentDayPayload(runningCandle.Key).Low
                                    If lastLow <> low Then
                                        lastLow = low
                                        Dim buffer As Decimal = CalculateBuffer(low, Utilities.Numbers.NumberManipulation.RoundOfType.Floor)
                                        Dim row As DataRow = ret.NewRow
                                        row("Date") = runningCandle.Key
                                        row("Instrument") = currentDayPayload(runningCandle.Key).TradingSymbol
                                        row("Parameter") = "Low"
                                        row("Parameter Value") = low
                                        row("Parameter Range") = String.Format("{0} - {1}", low + buffer, low - buffer)
                                        row("Match Count") = runningCandle.Value
                                        ret.Rows.Add(row)
                                        counter += 1
                                        If counter = _numberOfRecord Then Exit For
                                    End If
                                Next
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