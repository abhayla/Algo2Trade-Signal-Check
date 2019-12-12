Imports Algo2TradeBLL
Imports System.Threading
Public Class BollingerWithATRBands
    Inherits Rule
    Public Sub New(ByVal canceller As CancellationTokenSource, ByVal stockCategory As String, ByVal timeFrame As Integer, ByVal useHA As Boolean, ByVal stockName As String, ByVal fileName As String)
        MyBase.New(canceller, stockCategory, timeFrame, useHA, stockName, fileName)
    End Sub
    Public Overrides Async Function RunAsync(ByVal startDate As Date, ByVal endDate As Date) As Task(Of DataTable)
        Await Task.Delay(0).ConfigureAwait(False)
        Dim ret As New DataTable
        ret.Columns.Add("Date")
        ret.Columns.Add("Instrument")
        ret.Columns.Add("Signal")
        ret.Columns.Add("Price")
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
                        Dim ATRHighPayload As Dictionary(Of Date, Decimal) = Nothing
                        Dim ATRLowPayload As Dictionary(Of Date, Decimal) = Nothing
                        Indicator.ATRBands.CalculateATRBands(2, 5, Payload.PayloadFields.Close, inputPayload, ATRHighPayload, ATRLowPayload)
                        Dim bollingerHighPayload As Dictionary(Of Date, Decimal) = Nothing
                        Dim bollingerLowPayload As Dictionary(Of Date, Decimal) = Nothing
                        Dim SMAPayload As Dictionary(Of Date, Decimal) = Nothing
                        Indicator.BollingerBands.CalculateBollingerBands(20, Payload.PayloadFields.Close, 2, inputPayload, bollingerHighPayload, bollingerLowPayload, SMAPayload)
                        If currentDayPayload IsNot Nothing AndAlso currentDayPayload.Count > 0 Then
                            Dim buyPrice As Decimal = 0
                            Dim sellPrice As Decimal = 0
                            For Each runningPayload In currentDayPayload.Keys
                                _canceller.Token.ThrowIfCancellationRequested()
                                If buyPrice <> 0 AndAlso currentDayPayload(runningPayload).High > buyPrice Then
                                    Dim row As DataRow = ret.NewRow
                                    row("Date") = currentDayPayload(runningPayload).PayloadDate
                                    row("Instrument") = currentDayPayload(runningPayload).TradingSymbol
                                    row("Signal") = 1
                                    row("Price") = Math.Round(buyPrice, 2)
                                    ret.Rows.Add(row)
                                    buyPrice = 0
                                    sellPrice = 0
                                ElseIf sellPrice <> 0 AndAlso currentDayPayload(runningPayload).Low < sellPrice Then
                                    Dim row As DataRow = ret.NewRow
                                    row("Date") = currentDayPayload(runningPayload).PayloadDate
                                    row("Instrument") = currentDayPayload(runningPayload).TradingSymbol
                                    row("Signal") = -1
                                    row("Price") = Math.Round(sellPrice, 2)
                                    ret.Rows.Add(row)
                                    buyPrice = 0
                                    sellPrice = 0
                                End If
                                If ATRLowPayload(runningPayload) >= bollingerLowPayload(runningPayload) AndAlso
                                    ATRLowPayload(currentDayPayload(runningPayload).PreviousCandlePayload.PayloadDate) < bollingerLowPayload(currentDayPayload(runningPayload).PreviousCandlePayload.PayloadDate) Then
                                    If buyPrice = 0 Then
                                        buyPrice = ATRHighPayload(runningPayload)
                                    Else
                                        buyPrice = If(ATRHighPayload(runningPayload) < buyPrice, ATRHighPayload(runningPayload), buyPrice)
                                    End If
                                End If
                                If ATRHighPayload(runningPayload) <= bollingerHighPayload(runningPayload) AndAlso
                                    ATRHighPayload(currentDayPayload(runningPayload).PreviousCandlePayload.PayloadDate) > bollingerHighPayload(currentDayPayload(runningPayload).PreviousCandlePayload.PayloadDate) Then
                                    If sellPrice = 0 Then
                                        sellPrice = ATRLowPayload(runningPayload)
                                    Else
                                        sellPrice = If(ATRLowPayload(runningPayload) > sellPrice, ATRLowPayload(runningPayload), sellPrice)
                                    End If
                                End If
                                If buyPrice <> 0 AndAlso ATRHighPayload(runningPayload) >= bollingerHighPayload(runningPayload) AndAlso
                                    ATRHighPayload(currentDayPayload(runningPayload).PreviousCandlePayload.PayloadDate) < bollingerHighPayload(currentDayPayload(runningPayload).PreviousCandlePayload.PayloadDate) Then
                                    buyPrice = If(ATRHighPayload(runningPayload) < buyPrice, ATRHighPayload(runningPayload), buyPrice)
                                End If
                                If sellPrice <> 0 AndAlso ATRLowPayload(runningPayload) <= bollingerLowPayload(runningPayload) AndAlso
                                    ATRLowPayload(currentDayPayload(runningPayload).PreviousCandlePayload.PayloadDate) > bollingerLowPayload(currentDayPayload(runningPayload).PreviousCandlePayload.PayloadDate) Then
                                    sellPrice = If(ATRLowPayload(runningPayload) > sellPrice, ATRLowPayload(runningPayload), sellPrice)
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
